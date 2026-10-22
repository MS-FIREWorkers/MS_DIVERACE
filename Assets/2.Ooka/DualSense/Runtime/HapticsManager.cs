/*
    HapticsManager.cs
    DIVE RACE のハプティクスを管理するクラス。
    落下・横移動・障害物接近・衝突・通過の振動を生成し、DualSenseへ送信する。
*/

using UnityEngine;
using UnityEngine.Serialization;
using GamepadCore.Unity;

/// <summary>
/// DIVE RACE のハプティクスを管理するクラス。
///
/// 落下・横移動・障害物接近の振動をPCMデータとして生成し、
/// DualSenseへ送信する。
///
/// ゲーム本体からは、このクラスの公開メソッドを呼び出して
/// 振動の開始・更新・停止を指示する。
/// </summary>
public class HapticsManager : MonoBehaviour
{
    // ==================================================
    // ----- Fields -----
    // ==================================================
    // GamepadCoreが認識したコントローラーの番号。
    // 現在は、最初に認識されたコントローラーを使用する。
    private const int _deviceIndex = 0;

    // PCMデータのサンプリング周波数。
    // 48000Hzは1秒間に48000個のサンプルを生成する設定。
    private const int _sampleRate = 48000;

    // ==================================================
    // ----- Falling Haptics Settings -----
    // ==================================================
    [Header("落下ハプティクス")]

    [Tooltip("落下速度が最低のときの振幅。0～1の範囲で指定する")]
    [FormerlySerializedAs("minAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _minAmplitude = 0.05f;

    [Tooltip("落下速度が最大のときの振幅。0～1の範囲で指定する")]
    [FormerlySerializedAs("maxAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _maxAmplitude = 0.7f;

    [Tooltip("ノイズ波形の強さを調整する倍率。大きいほど強くなる")]
    [FormerlySerializedAs("noiseGain")]
    [SerializeField, Min(0f)]
    private float _noiseGain = 2.5f;

    [Tooltip("1回に生成・送信するPCMデータの長さ（秒）。短くすると更新頻度が上がる")]
    [FormerlySerializedAs("clipDuration")]
    [SerializeField, Min(0.05f)]
    private float _clipDuration = 0.5f;


    // ==================================================
    // ----- Horizontal Movement Settings -----
    // ==================================================
    [Header("横移動ハプティクス")]

    [Tooltip("横移動の強さが最低のときの振幅")]
    [FormerlySerializedAs("minHorizontalAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _minHorizontalAmplitude = 0.15f;

    [Tooltip("横移動の強さが最大のときの振幅")]
    [FormerlySerializedAs("maxHorizontalAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _maxHorizontalAmplitude = 0.6f;

    [Tooltip("横移動の三角波の周波数（Hz）。値が大きいほど振動が細かくなる")]
    [FormerlySerializedAs("horizontalFrequencyHz")]
    [SerializeField, Min(1f)]
    private float _horizontalFrequencyHz = 50f;

    [Tooltip("横移動時に、方向と反対側へ残す振動の割合。0.35なら35%")]
    [FormerlySerializedAs("horizontalSecondaryWeight")]
    [SerializeField, Range(0f, 1f)]
    private float _horizontalSecondaryWeight = 0.35f;


    // ==================================================
    // ----- Obstacle Approach Settings -----
    // ==================================================
    [Header("障害物接近ハプティクス")]

    [Tooltip("接近振動の振幅。仕様上の目安は0.1～0.2")]
    [FormerlySerializedAs("approachAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _approachAmplitude = 0.15f;

    [Tooltip("接近振動の三角波の周波数（Hz）")]
    [FormerlySerializedAs("approachFrequencyHz")]
    [SerializeField, Min(1f)]
    private float _approachFrequencyHz = 50f;

    [Tooltip("1回の短い接近振動の長さ（秒）")]
    [FormerlySerializedAs("approachPulseDuration")]
    [SerializeField, Min(0.01f)]
    private float _approachPulseDuration = 0.08f;

    [Tooltip("接近振動の開始から次の振動開始までの間隔（秒）")]
    [FormerlySerializedAs("approachInterval")]
    [SerializeField, Min(0.01f)]
    private float _approachInterval = 0.35f;


    // ==================================================
    // ----- Obstacle Collision Settings -----
    // ==================================================
    [Header("障害物衝突ハプティクス")]

    [Tooltip("衝突時の振幅。仕様上の目安は0.2～0.3")]
    [FormerlySerializedAs("collisionAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _collisionAmplitude = 0.25f;

    [Tooltip("衝突振動の周波数（Hz）")]
    [FormerlySerializedAs("collisionFrequencyHz")]
    [SerializeField, Min(1f)]
    private float _collisionFrequencyHz = 50f;

    [Tooltip("衝突時の強い振動を再生する時間（秒）")]
    [FormerlySerializedAs("collisionPulseDuration")]
    [SerializeField, Min(0.01f)]
    private float _collisionPulseDuration = 0.12f;


    // ==================================================
    // ----- Obstacle Passing Settings -----
    // ==================================================
    [Header("障害物通過ハプティクス")]

    [Tooltip("通過振動の振幅。仕様上の目安は0.2～0.3")]
    [FormerlySerializedAs("passingAmplitude")]
    [SerializeField, Range(0f, 1f)]
    private float _passingAmplitude = 0.25f;

    [Tooltip("通過振動の周波数（Hz）")]
    [FormerlySerializedAs("passingFrequencyHz")]
    [SerializeField, Min(1f)]
    private float _passingFrequencyHz = 50f;

    [Tooltip("障害物が近くを通過するときに振動を続ける時間（秒）")]
    [FormerlySerializedAs("passingDuration")]
    [SerializeField, Min(0.01f)]
    private float _passingDuration = 0.6f;

    [Tooltip("進行方向と反対側に残す振動の割合。0.35なら35%")]
    [FormerlySerializedAs("passingSecondaryWeight")]
    [SerializeField, Range(0f, 1f)]
    private float _passingSecondaryWeight = 0.35f;


    // ==================================================
    // ----- Runtime State -----
    // ==================================================
    // 横移動の状態
    private bool _isHorizontalActive;
    private float _horizontalIntensity;
    // -1：左、0：方向指定なし、+1：右
    private float _horizontalDirection;

    // 障害物接近の状態
    private bool _isObstacleApproachActive;

    // 障害物通過振動の状態
    private bool _isObstaclePassingActive;

    // 通過振動の残り時間
    private float _passingRemainingTime;

    // 障害物の移動方向。-1：左、+1：右
    private float _passingDirection;

    // 接近振動の累積時間。
    // PCMデータの生成ごとにリセットせず、振動パターンの継続に使う。
    private double _approachElapsedTime;

    // 衝突振動を再生する必要があるかどうか
    // 衝突イベントが発生したときに trueにし、再生後に falseへ戻す
    private bool _isObstacleCollisionPending;

    // ゲームの一時停止と落下の状態
    private bool _isPaused;
    private bool _isFallingActive;

    // 落下速度を0～1に正規化した値
    private float _fallSpeedRatio;

    // 落下振動の周波数と波形
    private float _currentFrequencyHz = 50f;
    private HapticWaveform _currentWaveform = HapticWaveform.Noise;

    // 次のPCMデータを生成するまでの残り時間
    private float _playbackTimer;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    private void Update()
    {
        // 一時停止中は新しいPCMデータを生成しない。
        if (_isPaused)
        {
            return;
        }

        // 通過振動の残り時間を減らす。
        // 一時停止中は、この処理より前でreturnするため時間は減らない。
        if (_isObstaclePassingActive)
        {
            _passingRemainingTime -= Time.unscaledDeltaTime;

            if (_passingRemainingTime <= 0f)
            {
                _isObstaclePassingActive = false;
                _passingRemainingTime = 0f;
            }
        }

        // いずれの振動も要求されていない場合は処理しない。
        // 接近振動だけが有効な場合も、ここを通過できる。
        if (!_isFallingActive && !_isHorizontalActive && !_isObstacleApproachActive &&
            !_isObstacleCollisionPending && !_isObstaclePassingActive)
        {
            return;
        }

        // PCMデータの生成間隔を管理する。
        _playbackTimer -= Time.unscaledDeltaTime;

        if (_playbackTimer <= 0f)
        {
            PlayCombinedHaptics();
            _playbackTimer = _clipDuration;
        }
    }

    // ==================================================
    // ----- Public Methods -----
    // ==================================================

    public void RequestHaptics(HapticType type, float intensity, float frequencyHz = 50f,
        HapticWaveform waveform = HapticWaveform.Noise, float direction = 0f)
    {
        intensity = Mathf.Clamp01(intensity);
        frequencyHz = Mathf.Clamp(frequencyHz, 1f, 1000f);

        switch (type)
        {
            case HapticType.Falling:
                // 落下速度に応じて振動の強さを更新する。
                _fallSpeedRatio = intensity;
                _currentFrequencyHz = frequencyHz;
                _currentWaveform = waveform;
                _isFallingActive = intensity > 0f;
                break;

            case HapticType.HorizontalMovement:
                // 横移動の強さと方向を更新する。
                _horizontalIntensity = intensity;
                _horizontalDirection = Mathf.Clamp(direction, -1f, 1f);
                _isHorizontalActive = intensity > 0f;
                break;

            default:
                // 接近振動などは専用メソッドから操作する。
                Debug.LogWarning($"未対応の振動リクエスト: {type}", this);
                break;
        }
    }

    /// <summary>
    /// ゲームの一時停止状態を設定する。
    /// trueで新しい振動データの生成を停止し、falseで再開する。
    /// </summary>
    public void SetPaused(bool paused)
    {
        _isPaused = paused;

        // 再開時に次の振動生成を待たされないようにする。
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 落下振動を停止する。
    /// 横移動や接近振動の状態には影響しない。
    /// </summary>
    public void StopFallingHaptics()
    {
        _isFallingActive = false;
        _fallSpeedRatio = 0f;
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 横移動振動を停止する。
    /// 落下や接近振動の状態には影響しない。
    /// </summary>
    public void StopHorizontalHaptics()
    {
        _isHorizontalActive = false;
        _horizontalIntensity = 0f;
        _horizontalDirection = 0f;
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物接近振動を開始する。
    /// 開始直後のPCMデータから振動パターンを生成する。
    /// </summary>
    public void StartObstacleApproachHaptics()
    {
        _isObstacleApproachActive = true;
        _approachElapsedTime = 0.0;
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物接近振動を停止する。
    /// 落下や横移動の振動は停止しない。
    /// </summary>
    public void StopObstacleApproachHaptics()
    {
        _isObstacleApproachActive = false;
        _approachElapsedTime = 0.0;
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物通過振動を開始する。directionは-1（左方向）または+1（右方向）。
    /// </summary>
    public void StartObstaclePassingHaptics(float direction)
    {
        // 方向を左または右に限定する。
        _passingDirection = direction < 0f ? -1f : 1f;

        // 指定した時間だけ通過振動を有効にする。
        _passingRemainingTime = _passingDuration;
        _isObstaclePassingActive = true;

        // 次のPCMデータをすぐに生成する。
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物通過振動を停止する。
    /// </summary>
    public void StopObstaclePassingHaptics()
    {
        _isObstaclePassingActive = false;
        _passingRemainingTime = 0f;
        _passingDirection = 0f;
        _playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物との衝突時に、1回分の強い振動を要求する。
    /// 実際の振動は次のPCM生成時に再生する。
    /// </summary>
    public void TriggerObstacleCollisionHaptics()
    {
        _isObstacleCollisionPending = true;
        _playbackTimer = 0f;
    }

    // ==================================================
    // ----- Haptics Processing -----
    // ==================================================

    /// <summary>
    /// 有効な振動を合成し、左右のPCMデータとしてDualSenseへ送信する。
    /// 各振動のサンプル値を加算してから、PCMの範囲に収める。
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

        if (!GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= _deviceIndex)
        {
            return;
        }

        int frameCount = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * _clipDuration));

        // ステレオPCMは、左・右の順にサンプルを格納する。
        short[] samples = new short[frameCount * 2];

        // 落下速度と横移動の強さを振幅へ変換する。
        float fallAmplitude = Mathf.Lerp(_minAmplitude, _maxAmplitude, _fallSpeedRatio);

        float horizontalAmplitude = Mathf.Lerp(_minHorizontalAmplitude,
            _maxHorizontalAmplitude, _horizontalIntensity);

        // このPCMデータの開始時点での接近振動の経過時間。
        double approachStartTime = _approachElapsedTime;

        for (int i = 0; i < frameCount; i++)
        {
            float time = (float)i / _sampleRate;

            float left = 0f;
            float right = 0f;

            // 落下振動：左右に同じ波形を加算する。
            if (_isFallingActive)
            {
                float phase = 2f * Mathf.PI * _currentFrequencyHz * time;

                float wave = GetWave(phase, _currentWaveform);

                // ノイズ波形のみ、実機テストに基づく倍率を適用する。
                float gain = _currentWaveform == HapticWaveform.Noise ? _noiseGain : 1f;

                float fall = wave * fallAmplitude * gain;

                left += fall;
                right += fall;
            }

            // 横移動振動：移動方向の側を強く、反対側を弱くする。
            if (_isHorizontalActive)
            {
                float phase = 2f * Mathf.PI * _horizontalFrequencyHz * time;

                float wave = GetWave(phase, HapticWaveform.Triangle);
                float horizontal = wave * horizontalAmplitude;

                float leftWeight = _horizontalDirection < 0f ? 1f : _horizontalSecondaryWeight;

                float rightWeight = _horizontalDirection > 0f ? 1f : _horizontalSecondaryWeight;

                left += horizontal * leftWeight;
                right += horizontal * rightWeight;
            }

            // 障害物接近振動：一定間隔で短い三角波を発生させる。
            // PCM内の各サンプルの時刻から振動中かどうかを判定する。
            if (_isObstacleApproachActive)
            {
                double sampleTime = approachStartTime + time;

                double pulsePosition = sampleTime % _approachInterval;

                if (pulsePosition < _approachPulseDuration)
                {
                    float phase = 2f * Mathf.PI * _approachFrequencyHz * time;

                    float wave = GetWave(phase, HapticWaveform.Triangle);

                    left += wave * _approachAmplitude;
                    right += wave * _approachAmplitude;
                }
            }

            // 衝突振動：短く強い三角波を左右に加算する。
            if (_isObstacleCollisionPending && time < _collisionPulseDuration)
            {
                float phase = 2f * Mathf.PI * _collisionFrequencyHz * time;

                float wave = GetWave(phase, HapticWaveform.Square);
                float collision = wave * _collisionAmplitude;

                left += collision;
                right += collision;
            }

            // 障害物通過振動：移動方向の側を強く振動させる。
            if (_isObstaclePassingActive)
            {
                float phase = 2f * Mathf.PI * _passingFrequencyHz * time;

                // 仕様に合わせて矩形波を使用する。
                float wave = GetWave(phase, HapticWaveform.Square);
                float passing = wave * _passingAmplitude;

                float leftWeight = _passingDirection < 0f ? 1f : _passingSecondaryWeight;

                float rightWeight = _passingDirection > 0f ? 1f : _passingSecondaryWeight;

                left += passing * leftWeight;
                right += passing * rightWeight;
            }

            // 複数の振動を合成した後、PCMの許容範囲に変換する。
            samples[i * 2] = ToPcmSample(left);
            samples[i * 2 + 1] = ToPcmSample(right);
        }

        int result = GamepadCoreApi.PlayPcm16(_deviceIndex, samples);

        if (result != 1)
        {
            Debug.LogWarning($"PCM振動の再生要求に失敗: {result}", this);
        }

        // 次のPCMデータでも接近振動の周期を継続させる。
        if (_isObstacleApproachActive)
        {
            _approachElapsedTime += _clipDuration;
        }

        // 衝突振動を今回生成したPCMデータに含めたため、再生要求を解除する。
        // ※PCMデータの生成・送信を要求した段階であり、実機での再生完了を確認したわけではない。
        _isObstacleCollisionPending = false;
    }

    /// <summary>
    /// 指定された波形の値を-1～1の範囲で生成する。
    /// phaseはラジアン単位の位相。
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
                // ランダムな値を使ってノイズ波形を生成する。
                return Random.Range(-1f, 1f);
        }
    }

    /// <summary>
    /// -1～1の浮動小数点値を、符号付き16bit PCMサンプルへ変換する。
    /// </summary>
    private short ToPcmSample(float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);
        return (short)(value * short.MaxValue);
    }
}
