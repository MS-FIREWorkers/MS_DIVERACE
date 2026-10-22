using UnityEngine;
using GamepadCore.Unity;

public class DualSensePcmChannelTest : MonoBehaviour
{
    private const int SampleRate = 48000;

    [Header("テスト設定")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float amplitude = 0.5f;

    [Header("左右の周波数")]
    [SerializeField] private int leftFrequency = 80;
    [SerializeField] private int rightFrequency = 200;

    // private bool hasTested;

    private void Start()
    {
        // GamepadCoreのデバイス検出を待つ
        Invoke(nameof(StartTest), 2.0f);
    }

    private void StartTest()
    {
        if (!GamepadCoreApi.IsInitialized)
        {
            Debug.LogError("GamepadCoreが初期化されていません。");
            return;
        }

        int connectedCount = GamepadCoreApi.ConnectedCount;

        Debug.Log($"GamepadCore ConnectedCount: {connectedCount}");

        if (connectedCount <= 0)
        {
            Debug.LogWarning("DualSenseがまだ検出されていません。");
            return;
        }

        int index = 0;

        if (GamepadCoreApi.GetDeviceType(index) != GamepadDeviceType.DualSense)
        {
            Debug.LogWarning("Index 0 がDualSenseではありません。");
            return;
        }

        Debug.Log($"左右周波数テスト開始: " + $"Left={leftFrequency}Hz / Right={rightFrequency}Hz");

        PlayStereoPcm(index);

        // hasTested = true;
    }

    private void PlayStereoPcm(int index)
    {
        int frameCount = Mathf.RoundToInt(SampleRate * duration);

        // Stereo:
        // [0] = Left
        // [1] = Right
        // [2] = Left
        // [3] = Right
        // ...
        short[] samples = new short[frameCount * 2];

        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)SampleRate;

            float leftValue = Mathf.Sin(2f * Mathf.PI * leftFrequency * time) * amplitude;

            float rightValue = Mathf.Sin(2f * Mathf.PI * rightFrequency * time) * amplitude;

            short leftSample = (short)(leftValue * short.MaxValue);

            short rightSample = (short)(rightValue * short.MaxValue);

            samples[i * 2] = leftSample;
            samples[i * 2 + 1] = rightSample;
        }

        int result = GamepadCoreApi.PlayPcm16(index, samples);

        Debug.Log($"PCM送信完了: " + $"{frameCount} frames / " + $"{duration:F2}s / " +
            $"Left={leftFrequency}Hz / " + $"Right={rightFrequency}Hz / " +
            $"amplitude={amplitude:F2} / " + $"result={result}");
    }

    private void OnDisable()
    {
        if (GamepadCoreApi.IsInitialized && GamepadCoreApi.ConnectedCount > 0)
        {
            GamepadCoreApi.StopAudioHaptics(0);
        }
    }
}