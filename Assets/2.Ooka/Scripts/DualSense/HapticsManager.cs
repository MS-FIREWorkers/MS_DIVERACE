using UnityEngine;
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
    // GamepadCoreが認識したコントローラーの番号。
    // 現在は、最初に認識されたコントローラーを使用する。
    private const int DeviceIndex = 0;

    // PCMデータのサンプリング周波数。
    // 48000Hzは1秒間に48000個のサンプルを生成する設定。
    private const int SampleRate = 48000;

    [Header("落下ハプティクス")]

    [Tooltip("落下速度が最低のときの振幅。0～1の範囲で指定する")]
    [SerializeField, Range(0f, 1f)]
    private float minAmplitude = 0.05f;

    [Tooltip("落下速度が最大のときの振幅。0～1の範囲で指定する")]
    [SerializeField, Range(0f, 1f)]
    private float maxAmplitude = 0.7f;

    [Tooltip("ノイズ波形の強さを調整する倍率。大きいほど強くなる")]
    [SerializeField, Min(0f)]
    private float noiseGain = 2.5f;

    [Tooltip("1回に生成・送信するPCMデータの長さ（秒）。短くすると更新頻度が上がる")]
    [SerializeField, Min(0.05f)]
    private float clipDuration = 0.5f;


    [Header("横移動ハプティクス")]

    [Tooltip("横移動の強さが最低のときの振幅")]
    [SerializeField, Range(0f, 1f)]
    private float minHorizontalAmplitude = 0.15f;

    [Tooltip("横移動の強さが最大のときの振幅")]
    [SerializeField, Range(0f, 1f)]
    private float maxHorizontalAmplitude = 0.6f;

    [Tooltip("横移動の三角波の周波数（Hz）。値が大きいほど振動が細かくなる")]
    [SerializeField, Min(1f)]
    private float horizontalFrequencyHz = 50f;

    [Tooltip("横移動時に、方向と反対側へ残す振動の割合。0.35なら35%")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalSecondaryWeight = 0.35f;


    [Header("障害物接近ハプティクス")]

    [Tooltip("接近振動の振幅。仕様上の目安は0.1～0.2")]
    [SerializeField, Range(0f, 1f)]
    private float approachAmplitude = 0.15f;

    [Tooltip("接近振動の三角波の周波数（Hz）")]
    [SerializeField, Min(1f)]
    private float approachFrequencyHz = 50f;

    [Tooltip("1回の短い接近振動の長さ（秒）")]
    [SerializeField, Min(0.01f)]
    private float approachPulseDuration = 0.08f;

    [Tooltip("接近振動の開始から次の振動開始までの間隔（秒）")]
    [SerializeField, Min(0.01f)]
    private float approachInterval = 0.35f;


    [Header("障害物衝突ハプティクス")]

    [Tooltip("衝突時の振幅。仕様上の目安は0.2～0.3")]
    [SerializeField, Range(0f, 1f)]
    private float collisionAmplitude = 0.25f;

    [Tooltip("衝突振動の周波数（Hz）")]
    [SerializeField, Min(1f)]
    private float collisionFrequencyHz = 50f;

    [Tooltip("衝突時の強い振動を再生する時間（秒）")]
    [SerializeField, Min(0.01f)]
    private float collisionPulseDuration = 0.12f;


    [Header("障害物通過ハプティクス")]

    [Tooltip("通過振動の振幅。仕様上の目安は0.2～0.3")]
    [SerializeField, Range(0f, 1f)]
    private float passingAmplitude = 0.25f;

    [Tooltip("通過振動の周波数（Hz）")]
    [SerializeField, Min(1f)]
    private float passingFrequencyHz = 50f;

    [Tooltip("障害物が近くを通過するときに振動を続ける時間（秒）")]
    [SerializeField, Min(0.01f)]
    private float passingDuration = 0.6f;

    [Tooltip("進行方向と反対側に残す振動の割合。0.35なら35%")]
    [SerializeField, Range(0f, 1f)]
    private float passingSecondaryWeight = 0.35f;


    // 横移動の状態
    private bool isHorizontalActive;
    private float horizontalIntensity;
    // -1：左、0：方向指定なし、+1：右
    private float horizontalDirection;

    // 障害物接近の状態
    private bool isObstacleApproachActive;

    // 障害物通過振動の状態
    private bool isObstaclePassingActive;

    // 通過振動の残り時間
    private float passingRemainingTime;

    // 障害物の移動方向。-1：左、+1：右
    private float passingDirection;

    // 接近振動の累積時間。
    // PCMデータの生成ごとにリセットせず、振動パターンの継続に使う。
    private double approachElapsedTime;

    // 衝突振動を再生する必要があるかどうか
    // 衝突イベントが発生したときに trueにし、再生後に falseへ戻す
    private bool isObstacleCollisionPending;

    // ゲームの一時停止と落下の状態
    private bool isPaused;
    private bool isFallingActive;

    // 落下速度を0～1に正規化した値
    private float fallSpeedRatio;

    // 落下振動の周波数と波形
    private float currentFrequencyHz = 50f;
    private HapticWaveform currentWaveform = HapticWaveform.Noise;

    // 次のPCMデータを生成するまでの残り時間
    private float playbackTimer;

    /// <summary>
    /// 指定した種類の振動を要求する。
    ///
    /// intensity：振動の強さ。0～1。
    /// frequencyHz：振動の周波数（Hz）。
    /// waveform：落下振動に使用する波形。
    /// direction：横移動の方向。-1が左、+1が右。
    ///
    /// Fallingは、毎フレーム呼び出してintensityを更新できる。
    /// HorizontalMovementも、方向や強さの更新に使用できる。
    /// </summary>
    public void RequestHaptics(HapticType type, float intensity, float frequencyHz = 50f,
        HapticWaveform waveform = HapticWaveform.Noise, float direction = 0f)
    {
        intensity = Mathf.Clamp01(intensity);
        frequencyHz = Mathf.Clamp(frequencyHz, 1f, 1000f);

        switch (type)
        {
            case HapticType.Falling:
                // 落下速度に応じて振動の強さを更新する。
                fallSpeedRatio = intensity;
                currentFrequencyHz = frequencyHz;
                currentWaveform = waveform;
                isFallingActive = intensity > 0f;
                break;

            case HapticType.HorizontalMovement:
                // 横移動の強さと方向を更新する。
                horizontalIntensity = intensity;
                horizontalDirection = Mathf.Clamp(direction, -1f, 1f);
                isHorizontalActive = intensity > 0f;
                break;

            default:
                // 接近振動などは専用メソッドから操作する。
                Debug.LogWarning($"未対応の振動リクエスト: {type}", this);
                break;
        }
    }

    private void Update()
    {
        // 一時停止中は新しいPCMデータを生成しない。
        if (isPaused)
        {
            return;
        }

        // 通過振動の残り時間を減らす。
        // 一時停止中は、この処理より前でreturnするため時間は減らない。
        if (isObstaclePassingActive)
        {
            passingRemainingTime -= Time.unscaledDeltaTime;

            if (passingRemainingTime <= 0f)
            {
                isObstaclePassingActive = false;
                passingRemainingTime = 0f;
            }
        }

        // いずれの振動も要求されていない場合は処理しない。
        // 接近振動だけが有効な場合も、ここを通過できる。
        if (!isFallingActive && !isHorizontalActive && !isObstacleApproachActive &&
            !isObstacleCollisionPending && !isObstaclePassingActive)
        {
            return;
        }

        // PCMデータの生成間隔を管理する。
        playbackTimer -= Time.unscaledDeltaTime;

        if (playbackTimer <= 0f)
        {
            PlayCombinedHaptics();
            playbackTimer = clipDuration;
        }
    }

    /// <summary>
    /// ゲームの一時停止状態を設定する。
    /// trueで新しい振動データの生成を停止し、falseで再開する。
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;

        // 再開時に次の振動生成を待たされないようにする。
        playbackTimer = 0f;
    }

    /// <summary>
    /// 落下振動を停止する。
    /// 横移動や接近振動の状態には影響しない。
    /// </summary>
    public void StopFallingHaptics()
    {
        isFallingActive = false;
        fallSpeedRatio = 0f;
        playbackTimer = 0f;
    }

    /// <summary>
    /// 横移動振動を停止する。
    /// 落下や接近振動の状態には影響しない。
    /// </summary>
    public void StopHorizontalHaptics()
    {
        isHorizontalActive = false;
        horizontalIntensity = 0f;
        horizontalDirection = 0f;
        playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物接近振動を開始する。
    /// 開始直後のPCMデータから振動パターンを生成する。
    /// </summary>
    public void StartObstacleApproachHaptics()
    {
        isObstacleApproachActive = true;
        approachElapsedTime = 0.0;
        playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物接近振動を停止する。
    /// 落下や横移動の振動は停止しない。
    /// </summary>
    public void StopObstacleApproachHaptics()
    {
        isObstacleApproachActive = false;
        approachElapsedTime = 0.0;
        playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物通過振動を開始する。directionは-1（左方向）または+1（右方向）。
    /// </summary>
    public void StartObstaclePassingHaptics(float direction)
    {
        // 方向を左または右に限定する。
        passingDirection = direction < 0f ? -1f : 1f;

        // 指定した時間だけ通過振動を有効にする。
        passingRemainingTime = passingDuration;
        isObstaclePassingActive = true;

        // 次のPCMデータをすぐに生成する。
        playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物通過振動を停止する。
    /// </summary>
    public void StopObstaclePassingHaptics()
    {
        isObstaclePassingActive = false;
        passingRemainingTime = 0f;
        passingDirection = 0f;
        playbackTimer = 0f;
    }

    /// <summary>
    /// 障害物との衝突時に、1回分の強い振動を要求する。
    /// 実際の振動は次のPCM生成時に再生する。
    /// </summary>
    public void TriggerObstacleCollisionHaptics()
    {
        isObstacleCollisionPending = true;
        playbackTimer = 0f;
    }

    /// <summary>
    /// 有効な振動を合成し、左右のPCMデータとしてDualSenseへ送信する。
    /// 各振動のサンプル値を加算してから、PCMの範囲に収める。
    /// </summary>
    private void PlayCombinedHaptics()
    {
        if (isPaused)
        {
            return;
        }

        if (!isFallingActive && !isHorizontalActive && !isObstacleApproachActive &&
            !isObstacleCollisionPending && !isObstaclePassingActive)
        {
            return;
        }

        if (!GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= DeviceIndex)
        {
            return;
        }

        int frameCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * clipDuration));

        // ステレオPCMは、左・右の順にサンプルを格納する。
        short[] samples = new short[frameCount * 2];

        // 落下速度と横移動の強さを振幅へ変換する。
        float fallAmplitude = Mathf.Lerp(minAmplitude, maxAmplitude, fallSpeedRatio);

        float horizontalAmplitude = Mathf.Lerp(minHorizontalAmplitude, maxHorizontalAmplitude, horizontalIntensity);

        // このPCMデータの開始時点での接近振動の経過時間。
        double approachStartTime = approachElapsedTime;

        for (int i = 0; i < frameCount; i++)
        {
            float time = (float)i / SampleRate;

            float left = 0f;
            float right = 0f;

            // 落下振動：左右に同じ波形を加算する。
            if (isFallingActive)
            {
                float phase = 2f * Mathf.PI * currentFrequencyHz * time;

                float wave = GetWave(phase, currentWaveform);

                // ノイズ波形のみ、実機テストに基づく倍率を適用する。
                float gain = currentWaveform == HapticWaveform.Noise ? noiseGain : 1f;

                float fall = wave * fallAmplitude * gain;

                left += fall;
                right += fall;
            }

            // 横移動振動：移動方向の側を強く、反対側を弱くする。
            if (isHorizontalActive)
            {
                float phase = 2f * Mathf.PI * horizontalFrequencyHz * time;

                float wave = GetWave(phase, HapticWaveform.Triangle);
                float horizontal = wave * horizontalAmplitude;

                float leftWeight = horizontalDirection < 0f ? 1f : horizontalSecondaryWeight;

                float rightWeight = horizontalDirection > 0f ? 1f : horizontalSecondaryWeight;

                left += horizontal * leftWeight;
                right += horizontal * rightWeight;
            }

            // 障害物接近振動：一定間隔で短い三角波を発生させる。
            // PCM内の各サンプルの時刻から振動中かどうかを判定する。
            if (isObstacleApproachActive)
            {
                double sampleTime = approachStartTime + time;

                double pulsePosition = sampleTime % approachInterval;

                if (pulsePosition < approachPulseDuration)
                {
                    float phase = 2f * Mathf.PI * approachFrequencyHz * time;

                    float wave = GetWave(phase, HapticWaveform.Triangle);

                    left += wave * approachAmplitude;
                    right += wave * approachAmplitude;
                }
            }

            // 衝突振動：短く強い三角波を左右に加算する。
            if (isObstacleCollisionPending && time < collisionPulseDuration)
            {
                float phase = 2f * Mathf.PI * collisionFrequencyHz * time;

                float wave = GetWave(phase, HapticWaveform.Square);
                float collision = wave * collisionAmplitude;

                left += collision;
                right += collision;
            }

            // 障害物通過振動：移動方向の側を強く振動させる。
            if (isObstaclePassingActive)
            {
                float phase = 2f * Mathf.PI * passingFrequencyHz * time;

                // 仕様に合わせて矩形波を使用する。
                float wave = GetWave(phase, HapticWaveform.Square);
                float passing = wave * passingAmplitude;

                float leftWeight = passingDirection < 0f ? 1f : passingSecondaryWeight;

                float rightWeight = passingDirection > 0f ? 1f : passingSecondaryWeight;

                left += passing * leftWeight;
                right += passing * rightWeight;
            }

            // 複数の振動を合成した後、PCMの許容範囲に変換する。
            samples[i * 2] = ToPcmSample(left);
            samples[i * 2 + 1] = ToPcmSample(right);
        }

        int result = GamepadCoreApi.PlayPcm16(DeviceIndex, samples);

        if (result != 1)
        {
            Debug.LogWarning($"PCM振動の再生要求に失敗: {result}", this);
        }

        // 次のPCMデータでも接近振動の周期を継続させる。
        if (isObstacleApproachActive)
        {
            approachElapsedTime += clipDuration;
        }

        // 衝突振動を今回生成したPCMデータに含めたため、再生要求を解除する。
        // ※PCMデータの生成・送信を要求した段階であり、実機での再生完了を確認したわけではない。
        isObstacleCollisionPending = false;
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
