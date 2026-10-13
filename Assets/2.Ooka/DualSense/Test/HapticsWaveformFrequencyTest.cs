using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsWaveformFrequencyTest : MonoBehaviour
{
    [SerializeField] private HapticsManager hapticsManager;

    [SerializeField, Range(0.01f, 1f)]
    private float intensity = 0.5f;

    private float frequencyHz = 50f;
    private HapticWaveform waveform = HapticWaveform.Sine;
    private bool isActive;
    private bool isPaused;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || hapticsManager == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            SetPattern(50f, HapticWaveform.Sine);

        if (keyboard.digit2Key.wasPressedThisFrame)
            SetPattern(100f, HapticWaveform.Sine);

        if (keyboard.digit3Key.wasPressedThisFrame)
            SetPattern(80f, HapticWaveform.Triangle);

        if (keyboard.digit4Key.wasPressedThisFrame)
            SetPattern(80f, HapticWaveform.Square);

        if (keyboard.digit5Key.wasPressedThisFrame)
            SetPattern(80f, HapticWaveform.Noise);

        if (keyboard.jKey.wasPressedThisFrame)
        {
            isActive = true;
            Debug.Log("振動テスト開始");
        }

        if (keyboard.lKey.wasPressedThisFrame)
        {
            isActive = false;
            hapticsManager.StopFallingHaptics();
            Debug.Log("振動テスト停止");
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
            hapticsManager.SetPaused(isPaused);
            Debug.Log(isPaused ? "一時停止" : "再開");
        }

        if (isActive && !isPaused)
        {
            hapticsManager.RequestHaptics(HapticType.Falling,
                intensity, frequencyHz, waveform);
        }
    }

    private void SetPattern(float frequency, HapticWaveform newWaveform)
    {
        frequencyHz = frequency;
        waveform = newWaveform;

        Debug.Log($"設定変更：{waveform}, {frequencyHz}Hz");
    }
}
