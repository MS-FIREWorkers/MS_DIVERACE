using UnityEngine;
using UnityEngine.InputSystem;
using GamepadCore.Unity;

public class DualSenseWaveformTest : MonoBehaviour
{
    private const int DeviceIndex = 0;
    private const int SampleRate = 48000;

    [Header("îgå`ê›íË")]
    [Range(50f, 100f)]
    [SerializeField] private float frequency = 50.0f;

    [Range(0.01f, 1.0f)]
    [SerializeField] private float amplitude = 0.2f;

    [Range(0.1f, 1f)]
    [SerializeField] private float duration = 0.5f;

    [Header("îgå`Ç≤Ç∆ÇÃã≠Ç≥ï‚ê≥")]
    [Range(1f, 3f)]
    [SerializeField] private float noiseGain = 2.0f;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= DeviceIndex)
            return;

        // 1ÅFê≥å∑îgÅiî‰ärópÅj
        if (keyboard.digit1Key.wasPressedThisFrame) PlayWaveform(0);

        // 2ÅFéOäpîg
        if (keyboard.digit2Key.wasPressedThisFrame) PlayWaveform(1);

        // 3ÅFãÈå`îg
        if (keyboard.digit3Key.wasPressedThisFrame) PlayWaveform(2);

        // 3ÅFÉmÉCÉYîg
        if (keyboard.digit4Key.wasPressedThisFrame) PlayWaveform(3);

        // 0ÅFPCMÉnÉvÉeÉBÉNÉXí‚é~
        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            // GamepadCoreApi.StopAudioHaptics(DeviceIndex);
            Debug.Log("PCMÉnÉvÉeÉBÉNÉXí‚é~ÇóvãÅÇµÇ‹ÇµÇΩ");
        }
    }

    private void PlayWaveform(int waveformType)
    {
        int frameCount = Mathf.RoundToInt(SampleRate * duration);

        short[] samples = new short[frameCount * 2];

        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)SampleRate;
            float phase = (time * frequency) % 1f;

            //float value = waveformType switch
            //{
            //    0 => Mathf.Sin(2f * Mathf.PI * phase),
            //    1 => 1f - 4f * Mathf.Abs(phase - 0.5f),
            //    2 => phase < 0.5f ? 1f : -1f,
            //    _ => 0f
            //};

            float value = waveformType switch
            {
                0 => Mathf.Sin(2f * Mathf.PI * phase),
                1 => 1f - 4f * Mathf.Abs(phase - 0.5f),
                2 => phase < 0.5f ? 1f : -1f,
                3 => Random.Range(-1f, 1f),
                _ => 0f
            };

            // short sample = (short)(Mathf.Clamp(value * amplitude, -1f, 1f) * short.MaxValue);
            float waveformGain = waveformType == 3 ? noiseGain : 1f;
            float output = value * amplitude * waveformGain;

            short sample = (short)(Mathf.Clamp(output, -1f, 1f) * short.MaxValue);

            // ç∂âEÉ`ÉÉÉìÉlÉãÇ…ìØÇ∂îgå`ÇëóÇÈ
            samples[i * 2] = sample;
            samples[i * 2 + 1] = sample;
        }

        GamepadCoreApi.ConfigureDualSense(DeviceIndex, microphone: false, headset: false,
            speaker: false, microphoneVolume: 0, audioVolume: 0, rumbleMode: 0xFC,
            rumbleReduction: 0, triggerReduction: 0);

        int result = GamepadCoreApi.PlayPcm16(DeviceIndex, samples);

        string[] names = { "ê≥å∑îg", "éOäpîg", "ãÈå`îg", "ÉmÉCÉYîg" };

        Debug.Log($"PCMëóêM: {names[waveformType]} / " + $"{frequency:F0}Hz / " +
            $"êUïùê›íË={amplitude:F2} / " + $"{duration:F2}ïb / result={result}");
    }

    private void OnDisable()
    {
        if (GamepadCoreApi.IsInitialized && GamepadCoreApi.ConnectedCount > DeviceIndex)
        {
            // GamepadCoreApi.StopAudioHaptics(DeviceIndex);
        }
    }
}
