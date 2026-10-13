using UnityEngine;
using UnityEngine.InputSystem;
using GamepadCore.Unity;

public class DualSenseFallHapticsTest : MonoBehaviour
{
    private const int DeviceIndex = 0;
    private const int SampleRate = 48000;

    [Header("落下速度の模擬設定")]
    [SerializeField] private float maxFallSpeed = 50f;
    [SerializeField] private float simulatedFallSpeed = 0f;

    [Header("ハプティクス設定")]
    // [Range(50f, 100f)]
    // [SerializeField] private float frequency = 100f;

    [Range(0.01f, 0.3f)]
    [SerializeField] private float minAmplitude = 0.01f;

    [Range(0.01f, 0.3f)]
    [SerializeField] private float maxAmplitude = 0.3f;

    [Range(1f, 3f)]
    [SerializeField] private float noiseGain = 2.5f;

    [SerializeField] private float clipDuration = 0.5f;

    private bool isPaused;
    private float playbackTimer;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SetFallSpeed(10f);

        if (keyboard.digit2Key.wasPressedThisFrame)
            SetFallSpeed(25f);

        if (keyboard.digit3Key.wasPressedThisFrame)
            SetFallSpeed(50f);

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
            playbackTimer = 0f;

            // GamepadCoreApi.StopAudioHaptics(DeviceIndex);

            Debug.Log(isPaused ? "落下ハプティクス停止（再生中のクリップ終了を待機）" : "落下ハプティクス再開");
        }

        if (isPaused || simulatedFallSpeed <= 0f)
            return;

        playbackTimer -= Time.unscaledDeltaTime;

        if (playbackTimer <= 0f)
        {
            PlayFallNoise();
            playbackTimer = clipDuration;
        }
    }

    private void SetFallSpeed(float speed)
    {
        simulatedFallSpeed = speed;
        Debug.Log($"模擬落下速度: {simulatedFallSpeed:F1}");
    }

    private void PlayFallNoise()
    {
        if (!GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= DeviceIndex)
            return;

        float speedRatio = Mathf.Clamp01(simulatedFallSpeed / maxFallSpeed);

        float amplitude = Mathf.Lerp(minAmplitude, maxAmplitude, speedRatio);

        int frameCount = Mathf.RoundToInt(SampleRate * clipDuration);

        short[] samples = new short[frameCount * 2];

        for (int i = 0; i < frameCount; i++)
        {
            float noise = Random.Range(-1f, 1f);
            float output = noise * amplitude * noiseGain;

            short sample = (short)(Mathf.Clamp(output, -1f, 1f) * short.MaxValue);

            // 左右に同じPCMを送り、左右差をなくす
            samples[i * 2] = sample;
            samples[i * 2 + 1] = sample;
        }

        int result = GamepadCoreApi.PlayPcm16(DeviceIndex, samples);

        Debug.Log($"落下速度={simulatedFallSpeed:F1}, " +
            $"振幅={amplitude:F3}, result={result}");
    }

    private void OnDisable()
    {
        // StopAudioHapticsは今回の検証では呼び出さない
        // 再生中のPCMクリップは自然終了させる
    }
}
