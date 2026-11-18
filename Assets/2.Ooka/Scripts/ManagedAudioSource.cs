
using System.Collections.Generic;
using UnityEngine;

// ==================================================
// ----- Managed Audio Source -----
// ==================================================

// シーン上に配置したAudioSource（電車・風・環境音など）をAudioDataで管理するスクリプト
//
// ・起動時にAudioDataの設定をAudioSourceへ適用し、AudioManagerのMixerグループへ出力する
// ・音量は「AudioDataの基準音量 × 各スクリプトの補正値」で決定する
// ・Occlusion / Relative / PlayerFallなどがAudioSource.volumeを直接変更しないように、
//   補正値はSetVolumeMultiplierで登録する
//
// 補正値の合計が1を超えても、最終的なAudioSource.volumeは1が上限になる。
// 接近ブーストなどで音量を上げたい音は、AudioDataのVolumeを1より小さくして余裕を持たせること

[DefaultExecutionOrder(-50)] // 他のスクリプトのAwake / Startより先に初期化する
[RequireComponent(typeof(AudioSource))]
public class ManagedAudioSource : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("設定")]
    [Tooltip("このAudioSourceに適用するAudioData")]
    [SerializeField] private AudioData _audioData;

    [Tooltip("Start時に自動的に再生する")]
    [SerializeField] private bool _playOnStart = true;


    // ==================================================
    // ----- Private Variables -----
    // ==================================================

    private AudioSource _audioSource;

    // 音量補正値の登録元（スクリプト）ごとの値
    private readonly Dictionary<Object, float> _multipliers = new Dictionary<Object, float>();


    // ==================================================
    // ----- Properties -----
    // ==================================================

    public AudioSource Source => _audioSource;
    public AudioData Data => _audioData;


    // ==================================================
    // ----- Unity Standard Methods -----
    // ==================================================

    // コンポーネント追加時にAudioSourceのPlay On Awakeをオフにする
    // clipが空の状態でPlay On Awakeが実行されると、AudioLowPassFilterなどが付いている場合に
    // "Only custom filters can be played" の警告が発生するため
    private void Reset()
    {
        AudioSource source = GetComponent<AudioSource>();

        if (source != null)
        {
            source.playOnAwake = false;
        }
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        if (_audioData == null)
        {
            Debug.LogError($"{name} のManagedAudioSourceにAudioDataが設定されていません。", this);
            return;
        }

        _audioData.ApplyTo(_audioSource);

        if (AudioManager.Instance != null)
        {
            _audioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(_audioData.Channel);
        }
        else
        {
            Debug.LogWarning("AudioManagerが見つかりません。Mixerグループは設定されません。", this);
        }

        ApplyVolume();
    }

    private void Start()
    {
        if (_playOnStart)
        {
            Play();
        }
    }


    // ==================================================
    // ----- Custom Methods -----
    // ==================================================

    // AudioSourceの再生を開始する
    public void Play()
    {
        if (_audioSource != null && _audioSource.clip != null)
        {
            _audioSource.Play();
        }
    }

    // AudioSourceの再生を停止する
    public void Stop()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
    }

    // 音量の補正値を登録する（0以上）
    // 同じownerで呼び出した場合は、登録済みの値を更新する
    // 例：managedAudioSource.SetVolumeMultiplier(this, 0.35f);
    public void SetVolumeMultiplier(Object owner, float multiplier)
    {
        _multipliers[owner] = Mathf.Max(0f, multiplier);
        ApplyVolume();
    }

    // 登録した音量補正値を取り消す
    public void RemoveVolumeMultiplier(Object owner)
    {
        if (_multipliers.Remove(owner))
        {
            ApplyVolume();
        }
    }

    // AudioDataの基準音量 × すべての補正値をAudioSourceへ反映する
    private void ApplyVolume()
    {
        if (_audioSource == null || _audioData == null)
        {
            return;
        }

        float volume = _audioData.Volume;

        foreach (KeyValuePair<Object, float> pair in _multipliers)
        {
            volume *= pair.Value;
        }

        _audioSource.volume = Mathf.Clamp01(volume);
    }
}
