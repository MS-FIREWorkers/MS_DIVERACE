using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerVolumeController : MonoBehaviour
{
    [Header("AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("音量パラメータ名")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string seVolumeParameter = "SEVolume";

    // Master音量を変更
    public void SetMasterVolume(float volume)
    {
        SetVolume(masterVolumeParameter, volume);
    }

    // BGM音量を変更
    public void SetBGMVolume(float volume)
    {
        SetVolume(bgmVolumeParameter, volume);
    }

    // SE音量を変更
    public void SetSEVolume(float volume)
    {
        SetVolume(seVolumeParameter, volume);
    }

    // 音量を0～1の値からデシベルに変換して設定
    private void SetVolume(string parameterName, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixerが設定されていません。");
            return;
        }

        volume = Mathf.Clamp01(volume);

        // 0は無音として扱う
        float decibel = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;

        audioMixer.SetFloat(parameterName, decibel);
    }


    [ContextMenu("Test/Master 50%")]
    private void TestMasterVolume()
    {
        SetMasterVolume(0.5f);
    }

    [ContextMenu("Test/BGM 50%")]
    private void TestBGMVolume()
    {
        SetBGMVolume(0.5f);
    }

    [ContextMenu("Test/SE 50%")]
    private void TestSEVolume()
    {
        SetSEVolume(0.5f);
    }

    [ContextMenu("Test/Reset Volume")]
    private void ResetVolume()
    {
        SetMasterVolume(1f);
        SetBGMVolume(1f);
        SetSEVolume(1f);
    }
}
