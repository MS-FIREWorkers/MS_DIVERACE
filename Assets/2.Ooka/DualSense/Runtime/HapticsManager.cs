/*
    HapticsManager.cs
    DIVE RACE のハプティクス制御を管理するクラスです。
    落下・水平移動・障害物への接近・衝突・通過に応じた振動を生成し、DualSenseへ送信します。
*/

using UnityEngine;
using GamepadCore.Unity;

/// <summary>
/// DIVE RACE のハプティクス制御を管理するクラスです。
///
/// 落下・水平移動・障害物に関する振動をPCMデータとして生成し、
/// DualSenseへ送信します。
///
/// ゲーム本体からは、このクラスの公開メソッドを呼び出して
/// 振動の開始・更新・停止を指示します。
/// </summary>
public class HapticsManager : MonoBehaviour
{
    // ==================================================
    // ----- フィールド -----
    // ==================================================
    // GamepadCoreで認識されたコントローラーの番号です。
    // 現在は、最初に認識されたコントローラーを使用します。
    private const int _deviceIndex = 0;

    // PCMデータのサンプリングレートです。
    // 48,000Hzの場合、1秒あたり48,000サンプルを生成します。
    private const int _sampleRate = 48000;

    [Header("落下振動設定")]
    [Tooltip("落下振動の振幅・波形設定アセット")]
    [SerializeField] private ContinuousHapticSettings _fallingSettings;

    [Header("水平移動振動設定")]
    [Tooltip("水平移動振動の振幅・方向設定アセット")]
    [SerializeField] private DirectionalHapticSettings _horizontalSettings;

    [Header("障害物接近振動設定")]
    [Tooltip("障害物接近時に使用する繰り返しパルスの設定アセット")]
    [SerializeField] private RepeatingPulseHapticSettings _approachSettings;

    [Header("障害物衝突振動設定")]
    [Tooltip("障害物衝突時に使用する単発パルスの設定アセット")]
    [SerializeField] private PulseHapticSettings _collisionSettings;

    [Header("障害物通過振動設定")]
    [Tooltip("障害物通過時に使用する振動の設定アセット")]
    [SerializeField] private PassingHapticSettings _passingSettings;

    [Header("PCM設定")]
    [Tooltip("1回に生成して送信するPCMデータの長さ（秒）")]
    [SerializeField, Min(0.05f)] private float _clipDuration = 0.5f;


    // ==================================================
    // ----- 実行時状態 -----
    // ==================================================
    // 水平移動の方向です。
    private bool _isHorizontalActive;
    private float _horizontalIntensity;
    // -1: 左、0: 方向指定なし、+1: 右
    private float _horizontalDirection;

    // 障害物接近時の設定です。
    private bool _isObstacleApproachActive;

    // 障害物通過時の振動設定です。
    private bool _isObstaclePassingActive;

    // 通過振動の残り時間です。
    private float _passingRemainingTime;

    // 障害物の移動方向です。-1: 左、+1: 右
    private float _passingDirection;

    // 接近振動の継続時間です。
    // PCMデータの生成開始時にリセットし、振動パターンの継続に使用します。
    private double _approachElapsedTime;

    // 衝突振動を再生する必要があるかを示します。
    // 衝突イベントを受け取ったときにtrueにし、再生後はfalseに戻します。
    private bool _isObstacleCollisionPending;

    // ゲームの一時停止と再開に関する設定です。
    private bool _isPaused;
    private bool _isFallingActive;

    // 正規化された強度の値（0～1）です。
    private float _fallSpeedRatio;

    // 波形・周波数の診断テスト用に保持しています。通常のゲーム処理では意味ベースのAPIを使用します。
    private bool _hasFallingOverride;
    private float _fallingFrequencyOverride;
    private HapticWaveform _fallingWaveformOverride;

    // 現在のPCMデータを生成するまでの残り時間です。
    private float _playbackTimer;

    // ==================================================
    // ----- Unityイベント -----
    // ==================================================
    private void Awake()
    {
        if (_fallingSettings == null || _horizontalSettings == null ||
            _approachSettings == null || _collisionSettings == null ||
            _passingSettings == null)
        {
            Debug.LogError("HapticsManager に必要なハプティクス設定アセットがすべて割り当てられていません。", this);
        }
    }

    private void OnDisable()
    {
        StopAudioHaptics();
    }

    private void OnApplicationQuit()
    {
        // StopAudioHaptics();
    }

    private void Update()
    {
        // 一時停止中は新しいPCMデータを生成しません。
        if (_isPaused)
        {
            return;
        }

        // 通過振動の残り時間を減らします。
        // 一時停止中はこの処理より前にreturnするため、時間は進みません。
        if (_isObstaclePassingActive)
        {
            _passingRemainingTime -= Time.unscaledDeltaTime;

            if (_passingRemainingTime <= 0f)
            {
                _isObstaclePassingActive = false;
                _passingRemainingTime = 0f;
            }
        }

        // 水平移動の振動が要求されていない場合は処理しません。
        // 接近振動が有効な場合は、水平移動と通過振動を合成できます。
        if (!_isFallingActive && !_isHorizontalActive && !_isObstacleApproachActive &&
            !_isObstacleCollisionPending && !_isObstaclePassingActive)
        {
            return;
        }

        // PCMデータの生成間隔を管理します。
        _playbackTimer -= Time.unscaledDeltaTime;

        if (_playbackTimer <= 0f)
        {
            PlayCombinedHaptics();
            _playbackTimer = _clipDuration;
        }
    }

    // ==================================================
    // ----- 公開メソッド -----
    // ==================================================

    public void SetFalling(float fallSpeedRatio)
    {
        _fallSpeedRatio = Mathf.Clamp01(fallSpeedRatio);
        _isFallingActive = _fallSpeedRatio > 0f;
        _hasFallingOverride = false;
        _playbackTimer = 0f;
    }

    public void SetHorizontalMovement(float intensity, float direction)
    {
        _horizontalIntensity = Mathf.Clamp01(intensity);
        _horizontalDirection = Mathf.Clamp(direction, -1f, 1f);
        _isHorizontalActive = _horizontalIntensity > 0f;
        _playbackTimer = 0f;
    }

    public void RequestHaptics(HapticType type, float intensity, float frequencyHz = 50f,
        HapticWaveform waveform = HapticWaveform.Noise, float direction = 0f)
    {
        switch (type)
        {
            case HapticType.Falling:
                _fallSpeedRatio = Mathf.Clamp01(intensity);
                _fallingFrequencyOverride = Mathf.Clamp(frequencyHz, 1f, 1000f);
                _fallingWaveformOverride = waveform;
                _hasFallingOverride = true;
                _isFallingActive = _fallSpeedRatio > 0f;
                _playbackTimer = 0f;
                break;
            case HapticType.HorizontalMovement:
                SetHorizontalMovement(intensity, direction);
                break;
            case HapticType.ObstacleCollision:
                PlayObstacleCollision();
                break;
            default:
                Debug.LogWarning($"未対応のハプティクス要求です: {type}", this);
                break;
        }
    }

    /// <summary>
    /// ゲームの一時停止状態を設定します。
    /// trueで新しい振動データの生成を停止し、falseで再開します。
    /// </summary>
    public void SetPaused(bool paused)
    {
        _isPaused = paused;

        // 再開時に古い振動データを待たないようにします。
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 落下振動を停止します。
    /// 水平移動や接近振動の状態には影響しません。
    /// </summary>
    public void StopFallingHaptics()
    {
        _isFallingActive = false;
        _fallSpeedRatio = 0f;
        _hasFallingOverride = false;
        _playbackTimer = 0f;
    }

    public void StopFalling()
    {
        StopFallingHaptics();
    }

    /// <summary>
    /// 水平移動振動を停止します。
    /// 接近振動や通過振動の状態には影響しません。
    /// </summary>
    public void StopHorizontalHaptics()
    {
        _isHorizontalActive = false;
        _horizontalIntensity = 0f;
        _horizontalDirection = 0f;
        _playbackTimer = 0f;
    }

    public void StopHorizontalMovement()
    {
        StopHorizontalHaptics();
    }

    /// <summary>
    /// 障害物接近振動を開始します。
    /// 開始時にPCMデータから振動パターンを生成します。
    /// </summary>
    public void StartObstacleApproachHaptics()
    {
        _isObstacleApproachActive = true;
        _approachElapsedTime = 0.0;
        _playbackTimer = 0f;
    }

    public void StartObstacleApproach()
    {
        StartObstacleApproachHaptics();
    }

    /// <summary>
    /// 障害物接近振動を停止します。
    /// 水平移動の振動は停止しません。
    /// </summary>
    public void StopObstacleApproachHaptics()
    {
        _isObstacleApproachActive = false;
        _approachElapsedTime = 0.0;
        _playbackTimer = 0f;
    }

    public void StopObstacleApproach()
    {
        StopObstacleApproachHaptics();
    }

    /// <summary>
    /// 障害物通過振動を開始します。directionは-1（左方向）または+1（右方向）です。
    /// </summary>
    public void StartObstaclePassingHaptics(float direction)
    {
        // 指定された方向に応じて左右の強度を決定します。
        _passingDirection = direction < 0f ? -1f : 1f;

        // 指定時間だけ通過振動を有効にします。
        _passingRemainingTime = _passingSettings != null ? _passingSettings._duration : 0f;
        _isObstaclePassingActive = true;

        // PCMデータを必要に応じて生成します。
        _playbackTimer = 0f;
    }

    public void PlayObstaclePassing(float direction)
    {
        StartObstaclePassingHaptics(direction);
    }

    /// <summary>
    /// 障害物通過振動を停止します。
    /// </summary>
    public void StopObstaclePassingHaptics()
    {
        _isObstaclePassingActive = false;
        _passingRemainingTime = 0f;
        _passingDirection = 0f;
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物との衝突時に、一度だけ短い振動を要求します。
    /// 実際の振動は次のPCM生成時に再生されます。
    /// </summary>
    public void TriggerObstacleCollisionHaptics()
    {
        _isObstacleCollisionPending = true;
        _playbackTimer = 0f;
    }

    public void PlayObstacleCollision()
    {
        TriggerObstacleCollisionHaptics();
    }

    // ==================================================
    // ----- ハプティクス処理 -----
    // ==================================================

    /// <summary>
    /// 有効な振動要求を合成し、左右のPCMデータとしてDualSenseへ送信します。
    /// 各振動のサンプル値を合成し、PCMの範囲内に収めます。
    /// </summary>
    private void PlayCombinedHaptics()
    {
        if (_isPaused)
        {
            return;
        }

        if (!_isFallingActive && !_isHorizontalActive && !_isObstacleApproachActive &&
            !_isObstacleCollisionPending && !_isObstaclePassingActive)
        {
            return;
        }

        if (_fallingSettings == null || _horizontalSettings == null ||
            _approachSettings == null || _collisionSettings == null ||
            _passingSettings == null ||
            !GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= _deviceIndex)
        {
            return;
        }

        int frameCount = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * _clipDuration));

        // ステレオPCMは、左・右の順にサンプルを格納します。
        short[] samples = new short[frameCount * 2];

        // 落下速度と水平移動の強さを振動へ変換します。
        float fallAmplitude = Mathf.Lerp(_fallingSettings._minAmplitude,
            _fallingSettings._maxAmplitude, _fallSpeedRatio);

        float horizontalAmplitude = Mathf.Lerp(_horizontalSettings._minAmplitude,
            _horizontalSettings._maxAmplitude, _horizontalIntensity);

        // PCMデータの開始時点から接近振動が経過した時間です。
        double approachStartTime = _approachElapsedTime;

        for (int i = 0; i < frameCount; i++)
        {
            float time = (float)i / _sampleRate;

            float left = 0f;
            float right = 0f;

            // 落下振動：左右に同じ波形を合成します。
            if (_isFallingActive)
            {
                float frequency = _hasFallingOverride
                    ? _fallingFrequencyOverride : _fallingSettings._frequencyHz;
                HapticWaveform waveform = _hasFallingOverride
                    ? _fallingWaveformOverride : _fallingSettings._waveform;
                float phase = 2f * Mathf.PI * frequency * time;

                float wave = GetWave(phase, waveform);

                // ノイズ波形のみ、設定されたゲインを適用します。
                float gain = waveform == HapticWaveform.Noise ? _fallingSettings._noiseGain : 1f;

                float fall = wave * fallAmplitude * gain;

                left += fall;
                right += fall;
            }

            // 水平移動振動：移動方向に応じて左右の強度を変えます。
            if (_isHorizontalActive)
            {
                float phase = 2f * Mathf.PI * _horizontalSettings._frequencyHz * time;

                float wave = GetWave(phase, HapticWaveform.Triangle);
                float horizontal = wave * horizontalAmplitude;

                float leftWeight = _horizontalDirection < 0f ? 1f : _horizontalSettings._secondaryWeight;

                float rightWeight = _horizontalDirection > 0f ? 1f : _horizontalSettings._secondaryWeight;

                left += horizontal * leftWeight;
                right += horizontal * rightWeight;
            }

            // 接近振動：短いパルスを一定間隔で発生させます。
            // PCM内の各サンプルの時刻から、振動を出すか判定します。
            if (_isObstacleApproachActive)
            {
                double sampleTime = approachStartTime + time;

                double pulsePosition = sampleTime % _approachSettings._interval;

                if (pulsePosition < _approachSettings._pulseDuration)
                {
                    float phase = 2f * Mathf.PI * _approachSettings._frequencyHz * time;

                    float wave = GetWave(phase, HapticWaveform.Triangle);

                    left += wave * _approachSettings._amplitude;
                    right += wave * _approachSettings._amplitude;
                }
            }

            // 衝突振動：短いパルスを左右に加算します。
            if (_isObstacleCollisionPending && time < _collisionSettings._duration)
            {
                float phase = 2f * Mathf.PI * _collisionSettings._frequencyHz * time;

                float wave = GetWave(phase, HapticWaveform.Square);
                float collision = wave * _collisionSettings._amplitude;

                left += collision;
                right += collision;
            }

            // 通過振動：移動方向に応じて左右の強度を変えます。
            if (_isObstaclePassingActive)
            {
                float phase = 2f * Mathf.PI * _passingSettings._frequencyHz * time;

                // 設定に合わせて矩形波を使用します。
                float wave = GetWave(phase, HapticWaveform.Square);
                float passing = wave * _passingSettings._amplitude;

                float leftWeight = _passingDirection < 0f ? 1f : _passingSettings._secondaryWeight;

                float rightWeight = _passingDirection > 0f ? 1f : _passingSettings._secondaryWeight;

                left += passing * leftWeight;
                right += passing * rightWeight;
            }

            // 合成した振動をPCMの許容範囲に収めます。
            samples[i * 2] = ToPcmSample(left);
            samples[i * 2 + 1] = ToPcmSample(right);
        }

        int result = GamepadCoreApi.PlayPcm16(_deviceIndex, samples);

        if (result != 1)
        {
            Debug.LogWarning($"PCM振動の再生に失敗しました: {result}", this);
        }

        // 生成したPCMデータで接近振動の時間を更新します。
        if (_isObstacleApproachActive)
        {
            _approachElapsedTime += _clipDuration;
        }

        // 衝突振動は一度だけ生成するPCMデータに含めるため、再生要求を解除します。
        // PCMデータの生成と送信が成功しても、実機での振動再生を保証するものではありません。
        _isObstacleCollisionPending = false;
    }

    private void StopAudioHaptics()
    {
        if (GamepadCoreApi.IsInitialized && GamepadCoreApi.ConnectedCount > _deviceIndex)
        {
            GamepadCoreApi.StopAudioHaptics(_deviceIndex);
        }
    }

    /// <summary>
    /// 指定された波形の値を-1～1の範囲で生成します。
    /// phaseはラジアン単位の位相です。
    /// </summary>
    private float GetWave(float phase, HapticWaveform waveform)
    {
        switch (waveform)
        {
            case HapticWaveform.Sine:
                return Mathf.Sin(phase);

            case HapticWaveform.Triangle:
                return 2f / Mathf.PI * Mathf.Asin(Mathf.Sin(phase));

            case HapticWaveform.Square:
                return Mathf.Sin(phase) >= 0f ? 1f : -1f;

            case HapticWaveform.Noise:
            default:
                // ランダムな値を使ってノイズ波形を生成します。
                return Random.Range(-1f, 1f);
        }
    }

    /// <summary>
    /// -1～1の浮動小数点値を、符号付き16bit PCMサンプルへ変換します。
    /// </summary>
    private short ToPcmSample(float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);
        return (short)(value * short.MaxValue);
    }
}
