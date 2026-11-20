using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// ==================================================
// ----- Audio Manager -----
// ==================================================

// 音の再生（BGM・SE）とAudioMixerの音量管理を担当するスクリプト（シングルトン）
//
// ・音の設定はAudioData（ScriptableObject）で管理する
// ・AudioIdとAudioDataの対応はAudioLibrary（ScriptableObject）で一元管理する
// ・BGMはAudioSourceを2つ使い、フェードアウト・フェードインで切り替える
// ・SEはAudioSourceのプールから再生する
// ・シーンをまたいで常駐する（DontDestroyOnLoad）。シーンには1つだけ配置すること
//
// 使い方：
//   AudioManager.Instance.PlayBGM(bgmData, fadeOutTime: 1f, fadeInTime: 1f);
//   AudioManager.Instance.StopBGM(fadeOutTime: 1f);
//   AudioManager.Instance.PlaySE(seData);
//   AudioManager.Instance.PlaySEAt(seData, position);
//
// AudioIdで指定する場合：
//   AudioManager.Instance.PlayBGM(AudioId.BgmTest, fadeOutTime: 1f, fadeInTime: 1f);
//   AudioManager.Instance.PlaySE(AudioId.PlayerJump);
//   AudioManager.Instance.PlaySEAt(AudioId.PlayerJump, position);

[DefaultExecutionOrder(-100)] // ManagedAudioSourceなど、他のスクリプトのAwakeより先に初期化する
public class AudioManager : MonoBehaviour
{
    // ==================================================
    // ----- Singleton -----
    // ==================================================

    public static AudioManager Instance { get; private set; }


    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("AudioMixer")]
    [Tooltip("音量管理に使用するAudioMixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("公開パラメータ名")]
    [Tooltip("Master音量の公開パラメータ名")]
    [SerializeField] private string _masterVolumeParameter = "MasterVolume";

    [Tooltip("BGM音量の公開パラメータ名")]
    [SerializeField] private string _bgmVolumeParameter = "BGMVolume";

    [Tooltip("SE音量の公開パラメータ名")]
    [SerializeField] private string _seVolumeParameter = "SEVolume";

    [Header("出力先Mixerグループ")]
    [Tooltip("未設定の場合は、AudioMixerのMaster/BGMを探して使用する")]
    [SerializeField] private AudioMixerGroup _bgmGroup;

    [Tooltip("未設定の場合は、AudioMixerのMaster/SEを探して使用する")]
    [SerializeField] private AudioMixerGroup _seGroup;

    [Header("SEプール")]
    [Tooltip("起動時に用意するAudioSourceの数")]
    [SerializeField, Min(1)] private int _initialSePoolSize = 8;

    [Tooltip("同時に再生できるSEの上限。超えた場合は古いSEから上書きする")]
    [SerializeField, Min(1)] private int _maxSePoolSize = 24;

    [Header("AudioLibrary")]
    [Tooltip("AudioIdとAudioDataの対応表。通常は1つ。カテゴリごとに分ける場合は複数登録する")]
    [SerializeField] private AudioLibrary[] _libraries;


    // ==================================================
    // ----- Private Variables -----
    // ==================================================

    // BGM 1本分の再生状態（フェード切り替え用に2つ使用する）
    private class BgmChannel
    {
        public AudioSource _source;
        public AudioData _data;
        public float _level;       // フェード係数（0～1）
        public float _targetLevel; // フェードの目標値
        public Coroutine _fade;
    }

    private readonly BgmChannel[] _bgmChannels = new BgmChannel[2];
    private int _activeBgmIndex;

    private readonly List<AudioSource> _sePool = new List<AudioSource>();
    private int _seOverwriteIndex;

    // AudioIdとAudioDataの対応表（起動時にAudioLibraryから作成する）
    private readonly Dictionary<AudioId, AudioData> _audioTable = new Dictionary<AudioId, AudioData>();


    // ==================================================
    // ----- Properties -----
    // ==================================================

    // 現在再生中（フェードイン中を含む）のBGM。再生していなければnull
    public AudioData CurrentBGM
    {
        get
        {
            BgmChannel active = _bgmChannels[_activeBgmIndex];
            return (active != null && active._targetLevel > 0f) ? active._data : null;
        }
    }


    // ==================================================
    // ----- Unity Standard Methods -----
    // ==================================================

    // Domain Reloadを無効にした場合でも、前回のInstanceが残らないようにする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        Instance = null;
    }

    private void Awake()
    {
        // すでに別のAudioManagerが存在する場合は、このGameObjectを破棄する
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoadはルートのGameObjectにしか適用されない
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        ResolveMixerGroups();
        CreateBgmChannels();
        CreateSePool();
        BuildAudioTable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // ==================================================
    // ----- Initialization -----
    // ==================================================

    // Inspectorで未設定の場合、AudioMixerからグループを取得する
    private void ResolveMixerGroups()
    {
        if (_audioMixer == null)
        {
            Debug.LogError("AudioMixerが設定されていません。");
            return;
        }

        if (_bgmGroup == null)
        {
            _bgmGroup = FindGroup("BGM");
        }

        if (_seGroup == null)
        {
            _seGroup = FindGroup("SE");
        }
    }

    // AudioMixerから指定した名前のグループを取得する
    private AudioMixerGroup FindGroup(string groupName)
    {
        AudioMixerGroup[] groups = _audioMixer.FindMatchingGroups("Master/" + groupName);

        foreach (AudioMixerGroup group in groups)
        {
            if (group.name == groupName)
            {
                return group;
            }
        }

        Debug.LogWarning($"AudioMixerにグループが見つかりません: Master/{groupName}");
        return null;
    }

    // BGM用のAudioSourceを2つ生成する
    private void CreateBgmChannels()
    {
        for (int i = 0; i < _bgmChannels.Length; i++)
        {
            GameObject child = new GameObject($"BGM_Source_{i}");
            child.transform.SetParent(transform);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // BGMは2D固定
            source.outputAudioMixerGroup = _bgmGroup;

            _bgmChannels[i] = new BgmChannel { _source = source };
        }
    }

    // SE用のAudioSourceプールを初期生成する
    private void CreateSePool()
    {
        for (int i = 0; i < _initialSePoolSize; i++)
        {
            CreateSeSource();
        }
    }

    // AudioLibraryからAudioIdとAudioDataの対応表を作成する
    // AudioLibraryにはID名（文字列）が保存されているため、名前からAudioIdを取得する
    // 同じAudioIdが複数登録されている場合は、先に登録されたものを使用する
    private void BuildAudioTable()
    {
        _audioTable.Clear();

        if (_libraries == null)
        {
            return;
        }

        foreach (AudioLibrary library in _libraries)
        {
            if (library == null)
            {
                continue;
            }

            foreach (AudioEntry entry in library.Entries)
            {
                // 未設定の行は登録しない（警告はAudioLibraryのInspector上で表示される）
                if (string.IsNullOrEmpty(entry.Name) || entry.Data == null)
                {
                    continue;
                }

                // 名前からAudioIdを取得する
                // AudioIdは、AudioLibraryに入力した名前をもとに自動生成されるため、
                // 更新前の名前はここで見つからない
                if (!System.Enum.TryParse(entry.Name, out AudioId id) || id == AudioId.None)
                {
                    Debug.LogWarning(
                        $"AudioId に {entry.Name} が存在しません。メニューの Tools → Audio → AudioIdを更新 を実行してください。（{library.name}）",
                        library);
                    continue;
                }

                if (_audioTable.ContainsKey(id))
                {
                    Debug.LogWarning(
                        $"AudioId {id} が重複して登録されています。先に登録されたものを使用します。（{library.name}）",
                        library);
                    continue;
                }

                _audioTable.Add(id, entry.Data);
            }
        }
    }


    // ==================================================
    // ----- BGM -----
    // ==================================================

    // BGMを再生する。再生中のBGMがある場合は、フェードアウトさせて切り替える
    //
    // fadeOutTime：再生中のBGMのフェードアウト時間（秒）
    // fadeInTime ：新しいBGMのフェードイン時間（秒）
    // waitFadeOut：trueなら、フェードアウト完了後に新しいBGMを再生する
    //              falseなら、フェードアウトとフェードインを同時に行う（クロスフェード）
    public void PlayBGM(AudioData data, float fadeOutTime = 1f, float fadeInTime = 1f, bool waitFadeOut = false)
    {
        if (!IsPlayable(data))
        {
            return;
        }

        if (data.Channel != AudioChannel.BGM)
        {
            Debug.LogWarning($"{data.name} のChannelがBGMではありません。BGMグループで再生します。", data);
        }

        BgmChannel current = _bgmChannels[_activeBgmIndex];

        // すでに同じBGMが再生中（フェードイン中を含む）なら何もしない
        if (current._data == data && current._targetLevel > 0f)
        {
            return;
        }

        // 新しいBGMは、空いている方のAudioSourceで再生する
        // 前のフェードアウトが途中だった場合は、その音をここで停止する
        int nextIndex = 1 - _activeBgmIndex;
        BgmChannel next = _bgmChannels[nextIndex];

        StopChannelImmediately(next);

        next._data = data;
        data.ApplyTo(next._source);
        next._source.outputAudioMixerGroup = _bgmGroup;
        next._source.spatialBlend = 0f;
        next._source.volume = 0f;
        next._level = 0f;

        bool hasCurrent = current._data != null;
        float fadeInDelay = (waitFadeOut && hasCurrent) ? fadeOutTime : 0f;

        StartFade(next, 1f, fadeInTime, fadeInDelay, false, true);

        if (hasCurrent)
        {
            StartFade(current, 0f, fadeOutTime, 0f, true, false);
        }

        _activeBgmIndex = nextIndex;
    }

    // AudioIdで指定してBGMを再生する（引数の意味はPlayBGM(AudioData)と同じ）
    public void PlayBGM(AudioId id, float fadeOutTime = 1f, float fadeInTime = 1f, bool waitFadeOut = false)
    {
        if (TryGetAudioData(id, out AudioData data))
        {
            PlayBGM(data, fadeOutTime, fadeInTime, waitFadeOut);
        }
    }

    // 再生中のBGMをフェードアウトして停止する
    public void StopBGM(float fadeOutTime = 1f)
    {
        BgmChannel current = _bgmChannels[_activeBgmIndex];

        if (current._data == null)
        {
            return;
        }

        StartFade(current, 0f, fadeOutTime, 0f, true, false);
    }

    // BGMのフェード処理を開始する
    private void StartFade(BgmChannel channel, float target, float duration,
        float delay, bool stopWhenSilent, bool startPlayback)
    {
        if (channel._fade != null)
        {
            StopCoroutine(channel._fade);
        }

        channel._targetLevel = target;
        channel._fade = StartCoroutine(
            FadeRoutine(channel, target, duration, delay, stopWhenSilent, startPlayback));
    }

    // BGMのフェード処理を実行する
    private IEnumerator FadeRoutine(BgmChannel channel, float target, float duration,
        float delay, bool stopWhenSilent, bool startPlayback)
    {
        // フェード開始まで待機する（Time.timeScaleの影響を受けないようにunscaledを使用）
        float waited = 0f;

        while (waited < delay)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (startPlayback)
        {
            channel._source.Play();
        }

        float startLevel = channel._level;

        if (duration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                channel._level = Mathf.Lerp(startLevel, target, elapsed / duration);

                ApplyBgmVolume(channel);
                yield return null;
            }
        }

        channel._level = target;
        ApplyBgmVolume(channel);
        channel._fade = null;

        // 無音になったら再生を停止する
        if (stopWhenSilent && target <= 0f)
        {
            channel._source.Stop();
            channel._data = null;
        }
    }

    // AudioDataの基準音量にフェード係数を掛けて反映する
    private void ApplyBgmVolume(BgmChannel channel)
    {
        if (channel._data != null)
        {
            channel._source.volume = channel._data.Volume * channel._level;
        }
    }

    // BGMチャンネルを即座に停止して初期状態に戻す
    private void StopChannelImmediately(BgmChannel channel)
    {
        if (channel._fade != null)
        {
            StopCoroutine(channel._fade);
            channel._fade = null;
        }

        channel._source.Stop();
        channel._data = null;
        channel._level = 0f;
        channel._targetLevel = 0f;
    }


    // ==================================================
    // ----- SE -----
    // ==================================================

    // SEを2Dで再生する（UIの音など、位置に関係ない音）
    public void PlaySE(AudioData data)
    {
        PlaySEInternal(data, false, Vector3.zero);
    }

    // SEを指定した位置から3Dで再生する（ワンショット用。ループするSEは対象外）
    public void PlaySEAt(AudioData data, Vector3 position)
    {
        PlaySEInternal(data, true, position);
    }

    // AudioIdで指定してSEを2Dで再生する
    public void PlaySE(AudioId id)
    {
        if (TryGetAudioData(id, out AudioData data))
        {
            PlaySEInternal(data, false, Vector3.zero);
        }
    }

    // AudioIdで指定してSEを指定した位置から3Dで再生する
    public void PlaySEAt(AudioId id, Vector3 position)
    {
        if (TryGetAudioData(id, out AudioData data))
        {
            PlaySEInternal(data, true, position);
        }
    }

    // SEを再生する共通処理
    private void PlaySEInternal(AudioData data, bool is3D, Vector3 position)
    {
        if (!IsPlayable(data))
        {
            return;
        }

        if (data.Channel != AudioChannel.SE)
        {
            Debug.LogWarning($"{data.name} のChannelがSEではありません。SEグループで再生します。", data);
        }

        if (data.Loop)
        {
            Debug.LogWarning(
                $"{data.name} はループ設定ですが、プール再生のSEはループしません。" +
                "ループする音は、シーン上のAudioSourceにApplyToを使ってください。",
                data);
        }

        AudioSource source = GetSeSource();

        data.ApplyTo(source);
        source.outputAudioMixerGroup = _seGroup;
        source.loop = false;

        if (is3D)
        {
            source.transform.position = position;
            source.spatialBlend = 1f;
        }
        else
        {
            source.transform.localPosition = Vector3.zero;
            source.spatialBlend = 0f;
        }

        source.Play();
    }

    // SE用のAudioSourceを生成してプールに追加する
    private AudioSource CreateSeSource()
    {
        GameObject child = new GameObject($"SE_Source_{_sePool.Count}");
        child.transform.SetParent(transform);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = _seGroup;

        _sePool.Add(source);
        return source;
    }

    // 再生していないAudioSourceを返す
    // 空きがなければプールを増やし、上限に達していれば古いSEを上書きする
    private AudioSource GetSeSource()
    {
        foreach (AudioSource source in _sePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        if (_sePool.Count < _maxSePoolSize)
        {
            return CreateSeSource();
        }

        AudioSource overwritten = _sePool[_seOverwriteIndex];
        _seOverwriteIndex = (_seOverwriteIndex + 1) % _sePool.Count;

        overwritten.Stop();
        return overwritten;
    }


    // ==================================================
    // ----- Common -----
    // ==================================================

    // シーン上に配置したAudioSource（3Dの電車の音など）に設定するMixerグループを取得する
    // 例：source.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(data.Channel);
    public AudioMixerGroup GetMixerGroup(AudioChannel channel)
    {
        return channel == AudioChannel.BGM ? _bgmGroup : _seGroup;
    }

    // AudioIdに対応するAudioDataを取得する
    // 登録されていない場合は警告を表示してfalseを返す
    private bool TryGetAudioData(AudioId id, out AudioData data)
    {
        if (_audioTable.TryGetValue(id, out data))
        {
            return true;
        }

        Debug.LogWarning($"AudioId {id} がAudioLibraryに登録されていません。");
        return false;
    }

    // AudioDataが再生可能か確認する
    private bool IsPlayable(AudioData data)
    {
        if (data == null)
        {
            Debug.LogWarning("AudioDataがnullです。");
            return false;
        }

        if (data.Clip == null)
        {
            Debug.LogWarning($"{data.name} にAudioClipが設定されていません。", data);
            return false;
        }

        return true;
    }


    // ==================================================
    // ----- Mixer Volume -----
    // ==================================================

    // Master音量を設定する
    public void SetMasterVolume(float volume)
    {
        SetVolume(_masterVolumeParameter, volume);
    }

    // BGM音量を設定する
    public void SetBGMVolume(float volume)
    {
        SetVolume(_bgmVolumeParameter, volume);
    }

    // SE音量を設定する
    public void SetSEVolume(float volume)
    {
        SetVolume(_seVolumeParameter, volume);
    }

    // 音量を0～1からデシベルに変換してAudioMixerへ設定する
    private void SetVolume(string parameterName, float volume)
    {
        if (_audioMixer == null)
        {
            Debug.LogError("AudioMixerが設定されていません。");
            return;
        }

        volume = Mathf.Clamp01(volume);

        float decibel = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;

        bool success = _audioMixer.SetFloat(parameterName, decibel);

        if (!success)
        {
            Debug.LogWarning($"AudioMixerのパラメータが見つかりません: {parameterName}");
        }
    }


    // ==================================================
    // ----- Inspector Test Methods -----
    // ==================================================

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
