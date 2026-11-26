/*
    HapticsManager.cs
    DIVE RACE のハプティクス制御を管理するクラスです。
    落下・水平移動・障害物への接近・衝突・通過に応じた振動を生成し、DualSenseへ送信します。

    【ストリーミング方式】
    GamepadCoreのPlayPcm16は「上書き再生」ではなく、1秒分のリングバッファへの「追記」です。
    長いPCMをまとめて送ると、後から要求した振動(衝突など)は先に送った分の再生が終わるまで
    再生されず、画面とズレて感じられます。
    そのため本クラスは、短いPCM(_chunkDuration)を、再生が途切れない最小限の量(_leadTime)だけ
    先読みして送り続けます。振動の状態は「PCMを作る瞬間」の値が使われるので、
    リングに触れた・衝突したなどの変化は、最大でも _leadTime + _chunkDuration 程度で反映されます。
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
    private const int _sampleRate = 48000;

    private const double _twoPi = 2.0 * System.Math.PI;

    // 1フレームで送信するチャンク数の上限です(フレーム落ち後の追いつき用)。
    private const int _maxChunksPerFrame = 8;

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

    [Tooltip("衝突パルス中に、落下振動へ掛ける倍率(0～1)。小さいほど衝突が際立つ")]
    [SerializeField, Range(0f, 1f)] private float _fallDuckOnCollision = 0.3f;

    [Header("障害物通過振動設定")]
    [Tooltip("障害物通過時に使用する振動の設定アセット")]
    [SerializeField] private PassingHapticSettings _passingSettings;

    [Header("PCM設定")]
    [Tooltip("1回に生成して送信するPCMデータの長さ（秒）。小さいほど細かく送る")]
    [SerializeField, Min(0.005f)] private float _chunkDuration = 0.02f;

    [Tooltip("再生に先行して送信しておく長さ（秒）。小さいほど遅延が減るが、フレームが乱れると途切れやすい")]
    [SerializeField, Min(0.01f)] private float _leadTime = 0.05f;


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
    // 接近振動を開始したPCMフレーム位置です。
    private long _approachStartFrame;

    // 障害物通過時の振動設定です。
    private bool _isObstaclePassingActive;
    private float _passingRemainingTime;
    // 障害物の移動方向です。-1: 左、+1: 右
    private float _passingDirection;

    // 衝突パルスの状態です。開始したPCMフレーム位置から、設定された長さだけ再生します。
    private bool _isCollisionActive;
    private long _collisionStartFrame;

    // ゲームの一時停止と再開に関する設定です。
    private bool _isPaused;
    private bool _isFallingActive;

    // 正規化された強度の値（0～1）です。
    private float _fallSpeedRatio;

    // 波形・周波数の診断テスト用に保持しています。通常のゲーム処理では意味ベースのAPIを使用します。
    private bool _hasFallingOverride;
    private float _fallingFrequencyOverride;
    private HapticWaveform _fallingWaveformOverride;

    // ストリーミング用の状態です。
    private short[] _chunkSamples;      // 送信用バッファ(使い回します)
    private int _chunkFrames;           // 1チャンクのフレーム数
    private long _sampleCursor;         // 生成済みのフレーム総数(位相を連続させるために使用)
    private double _bufferedUntil;      // 送信済みPCMの再生が終わる見込み時刻(秒)

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

        _chunkFrames = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * _chunkDuration));
        _chunkSamples = new short[_chunkFrames * 2];
    }

    private void OnDisable()
    {
        StopAudioHaptics();
        _bufferedUntil = 0.0;
    }

    private void Update()
    {
        // 一時停止中は新しいPCMデータを生成しません。
        if (_isPaused)
        {
            return;
        }

        // 通過振動の残り時間を減らします。
        if (_isObstaclePassingActive)
        {
            _passingRemainingTime -= Time.unscaledDeltaTime;

            if (_passingRemainingTime <= 0f)
            {
                _isObstaclePassingActive = false;
                _passingRemainingTime = 0f;
            }
        }

        // 再生すべき振動がなければ何も送りません(送信済みの分は自然に再生し終わります)。
        if (!_isFallingActive && !_isHorizontalActive && !_isObstacleApproachActive &&
            !_isCollisionActive && !_isObstaclePassingActive)
        {
            return;
        }

        PumpHaptics();
    }

    // ==================================================
    // ----- 公開メソッド -----
    // ==================================================

    /// <summary>
    /// 落下振動の強さ(0～1)を設定します。毎フレーム呼んで構いません。
    /// </summary>
    public void SetFalling(float fallSpeedRatio)
    {
        _fallSpeedRatio = Mathf.Clamp01(fallSpeedRatio);
        _isFallingActive = _fallSpeedRatio > 0f;
        _hasFallingOverride = false;
    }

    public void SetHorizontalMovement(float intensity, float direction)
    {
        _horizontalIntensity = Mathf.Clamp01(intensity);
        _horizontalDirection = Mathf.Clamp(direction, -1f, 1f);
        _isHorizontalActive = _horizontalIntensity > 0f;
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
    }

    public void StopHorizontalMovement()
    {
        StopHorizontalHaptics();
    }

    /// <summary>
    /// 障害物接近振動を開始します。
    /// </summary>
    public void StartObstacleApproachHaptics()
    {
        _isObstacleApproachActive = true;
        _approachStartFrame = _sampleCursor;
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
        _passingDirection = direction < 0f ? -1f : 1f;
        _passingRemainingTime = _passingSettings != null ? _passingSettings._duration : 0f;
        _isObstaclePassingActive = true;
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
    }

    /// <summary>
    /// 障害物との衝突時に、一度だけ短い振動を要求します。
    /// 次に生成するPCMチャンクから、衝突パルスが始まります。
    /// </summary>
    public void TriggerObstacleCollisionHaptics()
    {
        _isCollisionActive = true;
        _collisionStartFrame = _sampleCursor;
    }

    public void PlayObstacleCollision()
    {
        TriggerObstacleCollisionHaptics();
    }

    // ==================================================
    // ----- ハプティクス処理 -----
    // ==================================================

    /// <summary>
    /// 再生が途切れない範囲(_leadTime)まで、短いPCMチャンクを生成して送信します。
    /// 毎フレーム呼ばれ、先読み量が足りない分だけ送ります。
    /// </summary>
    private void PumpHaptics()
    {
        if (_fallingSettings == null || _horizontalSettings == null ||
            _approachSettings == null || _collisionSettings == null ||
            _passingSettings == null ||
            !GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= _deviceIndex)
        {
            return;
        }

        double now = Time.realtimeSinceStartupAsDouble;

        // 送信済み分を再生し終わっている(途切れた)場合は、今を起点にします。
        if (_bufferedUntil < now)
        {
            _bufferedUntil = now;
        }

        double chunkSeconds = (double)_chunkFrames / _sampleRate;

        for (int n = 0; n < _maxChunksPerFrame && _bufferedUntil - now < _leadTime; n++)
        {
            FillChunk();

            int result = GamepadCoreApi.PlayPcm16(_deviceIndex, _chunkSamples);

            if (result != 1)
            {
                Debug.LogWarning($"PCM振動の再生に失敗しました: {result}", this);
                return;
            }

            _bufferedUntil += chunkSeconds;
            _sampleCursor += _chunkFrames;

            // 衝突パルスを最後まで生成したら終了します。
            if (_isCollisionActive &&
                (double)(_sampleCursor - _collisionStartFrame) / _sampleRate >= _collisionSettings._duration)
            {
                _isCollisionActive = false;
            }
        }
    }

    /// <summary>
    /// 現在の振動状態から、1チャンク分のステレオPCMを生成して _chunkSamples に書き込みます。
    /// 時刻は「再生開始からの通算フレーム」を使うため、チャンクの継ぎ目でも波形が連続します。
    /// </summary>
    private void FillChunk()
    {
        // 落下速度と水平移動の強さを振動へ変換します。
        float fallAmplitude = Mathf.Lerp(_fallingSettings._minAmplitude,
            _fallingSettings._maxAmplitude, _fallSpeedRatio);

        float horizontalAmplitude = Mathf.Lerp(_horizontalSettings._minAmplitude,
            _horizontalSettings._maxAmplitude, _horizontalIntensity);

        for (int i = 0; i < _chunkFrames; i++)
        {
            long frame = _sampleCursor + i;
            double time = (double)frame / _sampleRate;

            float left = 0f;
            float right = 0f;

            // 衝突パルス：開始位置からの経過時間で、矩形波を生成します。
            bool inCollision = false;
            float collision = 0f;

            if (_isCollisionActive)
            {
                double collisionTime = (double)(frame - _collisionStartFrame) / _sampleRate;

                if (collisionTime >= 0.0 && collisionTime < _collisionSettings._duration)
                {
                    inCollision = true;
                    float phase = Phase(_collisionSettings._frequencyHz, collisionTime);
                    collision = GetWave(phase, HapticWaveform.Square) * _collisionSettings._amplitude;
                }
            }

            // 落下振動：左右に同じ波形を合成します。
            if (_isFallingActive)
            {
                float frequency = _hasFallingOverride
                    ? _fallingFrequencyOverride : _fallingSettings._frequencyHz;
                HapticWaveform waveform = _hasFallingOverride
                    ? _fallingWaveformOverride : _fallingSettings._waveform;

                float wave = GetWave(Phase(frequency, time), waveform);

                // ノイズ波形のみ、設定されたゲインを適用します。
                float gain = waveform == HapticWaveform.Noise ? _fallingSettings._noiseGain : 1f;

                float fall = wave * fallAmplitude * gain;

                // 衝突パルス中は落下振動を弱めて、衝突を際立たせます。
                if (inCollision)
                {
                    fall *= _fallDuckOnCollision;
                }

                left += fall;
                right += fall;
            }

            // 水平移動振動：移動方向に応じて左右の強度を変えます。
            if (_isHorizontalActive)
            {
                float wave = GetWave(Phase(_horizontalSettings._frequencyHz, time), HapticWaveform.Triangle);
                float horizontal = wave * horizontalAmplitude;

                float leftWeight = _horizontalDirection < 0f ? 1f : _horizontalSettings._secondaryWeight;
                float rightWeight = _horizontalDirection > 0f ? 1f : _horizontalSettings._secondaryWeight;

                left += horizontal * leftWeight;
                right += horizontal * rightWeight;
            }

            // 接近振動：短いパルスを一定間隔で発生させます。
            if (_isObstacleApproachActive)
            {
                double sampleTime = (double)(frame - _approachStartFrame) / _sampleRate;
                double pulsePosition = sampleTime % _approachSettings._interval;

                if (pulsePosition < _approachSettings._pulseDuration)
                {
                    // パルスの先頭が必ず位相0から始まるようにします。
                    float wave = GetWave(Phase(_approachSettings._frequencyHz, pulsePosition),
                        HapticWaveform.Triangle);

                    left += wave * _approachSettings._amplitude;
                    right += wave * _approachSettings._amplitude;
                }
            }

            // 衝突振動を加算します。
            left += collision;
            right += collision;

            // 通過振動：移動方向に応じて左右の強度を変えます。
            if (_isObstaclePassingActive)
            {
                float wave = GetWave(Phase(_passingSettings._frequencyHz, time), HapticWaveform.Square);
                float passing = wave * _passingSettings._amplitude;

                float leftWeight = _passingDirection < 0f ? 1f : _passingSettings._secondaryWeight;
                float rightWeight = _passingDirection > 0f ? 1f : _passingSettings._secondaryWeight;

                left += passing * leftWeight;
                right += passing * rightWeight;
            }

            // 合成した振動をPCMの許容範囲に収めます。
            _chunkSamples[i * 2] = ToPcmSample(left);
            _chunkSamples[i * 2 + 1] = ToPcmSample(right);
        }
    }

    private void StopAudioHaptics()
    {
        if (GamepadCoreApi.IsInitialized && GamepadCoreApi.ConnectedCount > _deviceIndex)
        {
            GamepadCoreApi.StopAudioHaptics(_deviceIndex);
        }
    }

    /// <summary>
    /// 周波数と時刻から、0～2πの位相を求めます。
    /// 長時間プレイしてもfloatの精度が落ちないよう、doubleで計算してから丸めます。
    /// </summary>
    private static float Phase(double frequencyHz, double timeSeconds)
    {
        return (float)((_twoPi * frequencyHz * timeSeconds) % _twoPi);
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
