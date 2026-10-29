using UnityEngine;
using UnityEngine.Audio;

// 全体の音量とAudioMixerの管理を担当するScript

public class AudioManager : MonoBehaviour
{
    [Header("AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("公開パラメータ名")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string seVolumeParameter = "SEVolume";

    // Master音量
    public void SetMasterVolume(float volume)
    {
        SetVolume(masterVolumeParameter, volume);
    }

    // BGM音量
    public void SetBGMVolume(float volume)
    {
        SetVolume(bgmVolumeParameter, volume);
    }

    // SE音量
    public void SetSEVolume(float volume)
    {
        SetVolume(seVolumeParameter, volume);
    }

    // 音量を0～1からデシベルに変換
    private void SetVolume(string parameterName, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixerが設定されていません。");
            return;
        }

        volume = Mathf.Clamp01(volume);

        float decibel = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;

        bool success = audioMixer.SetFloat(parameterName, decibel);

        if (!success)
        {
            Debug.LogWarning($"AudioMixerのパラメータが見つかりません: {parameterName}");
        }
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
