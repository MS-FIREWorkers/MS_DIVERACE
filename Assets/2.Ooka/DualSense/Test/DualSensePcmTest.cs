using System.Collections;
using UnityEngine;
using GamepadCore.Unity;

public class DualSensePcmTest : MonoBehaviour
{
    private const int SampleRate = 48000;

    [Header("PCM設定")]
    [Tooltip("振動の周波数です。")]
    [SerializeField] private int frequency = 120;

    [Tooltip("振動時間（秒）です。")]
    [SerializeField] private float duration = 1.0f;

    [Tooltip("振動の強さです。0～1です。")]
    [SerializeField] private float amplitude = 0.5f;

    private IEnumerator Start()
    {
        Debug.Log("===== PCM 左チャンネル検証 =====");

        // 接続検出を待つ
        yield return new WaitForSeconds(2f);

        if (!GamepadCoreApi.IsInitialized)
        {
            Debug.LogError("GamepadCoreが初期化されていません。");
            yield break;
        }

        if (GamepadCoreApi.ConnectedCount <= 0)
        {
            Debug.LogWarning("DualSenseが検出されていません。");
            yield break;
        }

        int frameCount =
            Mathf.RoundToInt(SampleRate * duration);

        short[] samples =
            new short[frameCount * 2];

        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)SampleRate;

            float value =
                Mathf.Sin(2f * Mathf.PI * frequency * time)
                * amplitude;

            short sample =
                (short)(value * short.MaxValue);

            // 左だけ
            samples[i * 2] = sample;

            // 右は無音
            samples[i * 2 + 1] = 0;
        }

        int result =
            GamepadCoreApi.PlayPcm16(0, samples);

        Debug.Log(
            $"左PCM送信: " +
            $"{duration:F2}s / " +
            $"{frequency}Hz / " +
            $"amplitude={amplitude:F2} / " +
            $"result={result}"
        );
    }
}