using GamepadCore.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

public class DualSenseWaveformTest : MonoBehaviour
{
    private const int DeviceIndex = 0;
    private const int SampleRate = 48000;
    private const int PreviewSampleCount = 960;

    [Header("波形設定")]
    [Range(50f, 100f)]
    [SerializeField] private float _frequency = 50.0f;

    [Range(0.01f, 1.0f)]
    [SerializeField] private float _amplitude = 0.2f;

    [Range(0.1f, 1f)]
    [SerializeField] private float _duration = 0.5f;

    [Header("波形ごとの強さ補正")]
    [Range(1f, 3f)]
    [SerializeField] private float _noiseGain = 2.0f;

    [Header("波形表示")]
    [SerializeField] private bool _showWaveform = true;

    private float[] _previewSamples = new float[PreviewSampleCount];
    private string _currentWaveformName = "未再生";
    private Texture2D _lineTexture;

    private void Awake()
    {
        _lineTexture = Texture2D.whiteTexture;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            StopHaptics();
            return;
        }

        if (!GamepadCoreApi.IsInitialized || GamepadCoreApi.ConnectedCount <= DeviceIndex)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
            PlayWaveform(0);
        else if (keyboard.digit2Key.wasPressedThisFrame)
            PlayWaveform(1);
        else if (keyboard.digit3Key.wasPressedThisFrame)
            PlayWaveform(2);
        else if (keyboard.digit4Key.wasPressedThisFrame)
            PlayWaveform(3);
    }

    private void PlayWaveform(int waveformType)
    {
        int frameCount = Mathf.RoundToInt(SampleRate * _duration);
        short[] samples = new short[frameCount * 2];

        string[] names = { "正弦波", "三角波", "矩形波", "ノイズ波" };
        _currentWaveformName = names[waveformType];

        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)SampleRate;
            float phase = (time * _frequency) % 1f;

            float value = waveformType switch
            {
                0 => Mathf.Sin(2f * Mathf.PI * phase),
                1 => 1f - 4f * Mathf.Abs(phase - 0.5f),
                2 => phase < 0.5f ? 1f : -1f,
                3 => Random.Range(-1f, 1f),
                _ => 0f
            };

            float waveformGain = waveformType == 3 ? _noiseGain : 1f;
            float output = value * _amplitude * waveformGain;
            output = Mathf.Clamp(output, -1f, 1f);

            short sample = (short)(output * short.MaxValue);

            // PCMは左右チャンネルに同じサンプルを設定する。
            samples[i * 2] = sample;
            samples[i * 2 + 1] = sample;

            // グラフには、実際に送信する直前の値を保存する。
            if (i < PreviewSampleCount)
                _previewSamples[i] = sample / (float)short.MaxValue;
        }

        GamepadCoreApi.ConfigureDualSense(DeviceIndex, microphone: false, headset: false, speaker: false,
            microphoneVolume: 0, audioVolume: 0, rumbleMode: 0xFC, rumbleReduction: 0, triggerReduction: 0);

        int result = GamepadCoreApi.PlayPcm16(DeviceIndex, samples);

        Debug.Log($"PCM送信: {_currentWaveformName} / " +
            $"{_frequency:F0}Hz / 振幅設定={_amplitude:F2} / " + $"{_duration:F2}秒 / result={result}");
    }

    private void StopHaptics()
    {
        if (GamepadCoreApi.IsInitialized && GamepadCoreApi.ConnectedCount > DeviceIndex)
        {
            GamepadCoreApi.StopAudioHaptics(DeviceIndex);
            Debug.Log("PCMハプティクス停止を要求しました");
        }
    }

    private void OnGUI()
    {
        if (!_showWaveform)
            return;

        const float graphWidth = 600f;
        const float graphHeight = 180f;
        const float left = 20f;
        const float top = 20f;

        GUI.Box(new Rect(left, top, graphWidth + 20f, graphHeight + 70f), $"波形確認：{_currentWaveformName}");

        Rect graphRect = new Rect(left + 10f, top + 35f, graphWidth, graphHeight);

        GUI.Box(graphRect, "");

        // 中央のゼロライン
        DrawLine(new Vector2(graphRect.x, graphRect.center.y), new Vector2(graphRect.xMax, graphRect.center.y), 1f);

        // 最初の960サンプル（0.02秒）を描画する。
        // 50Hzなら約1周期分を確認できる。
        for (int i = 1; i < PreviewSampleCount; i++)
        {
            float previousX = graphRect.x + (i - 1) / (float)(PreviewSampleCount - 1) * graphRect.width;

            float currentX = graphRect.x + i / (float)(PreviewSampleCount - 1) * graphRect.width;

            float previousY = graphRect.center.y - _previewSamples[i - 1] * graphRect.height * 0.45f;

            float currentY = graphRect.center.y - _previewSamples[i] * graphRect.height * 0.45f;

            DrawLine(new Vector2(previousX, previousY), new Vector2(currentX, currentY), 2f);
        }

        GUI.Label(new Rect(left + 10f, top + graphHeight + 45f, graphWidth, 25f),
            "横軸：時間（最初の0.02秒）　縦軸：正規化したPCM振幅");
    }

    private void DrawLine(Vector2 start, Vector2 end, float thickness)
    {
        Vector2 difference = end - start;
        float angle = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;

        GUIUtility.RotateAroundPivot(angle, start);

        GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, difference.magnitude, thickness), _lineTexture);

        GUIUtility.RotateAroundPivot(-angle, start);
    }

    private void OnDisable()
    {
        StopHaptics();
    }
}
