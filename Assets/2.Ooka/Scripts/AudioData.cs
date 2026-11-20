using UnityEngine;

// ==================================================
// ----- Audio Channel -----
// ==================================================

// 出力先のMixerグループの種類
public enum AudioChannel
{
    BGM,
    SE,
}

// ==================================================
// ----- Audio Data -----
// ==================================================

// 1つの音の再生設定をまとめたScriptableObject（1音につき1アセット）
// AudioManagerからの再生と、シーン上に配置したAudioSourceへの設定適用の両方で使用する
//
// 作成方法：Projectウィンドウで右クリック → Create → Audio → AudioData

[CreateAssetMenu(fileName = "AudioData_New", menuName = "Audio/AudioData")]
public class AudioData : ScriptableObject
{
    // ==================================================
    // ----- Basic Settings -----
    // ==================================================

    [Header("基本設定")]
    [Tooltip("再生するAudioClip")]
    [SerializeField] private AudioClip _clip;

    [Tooltip("出力先のMixerグループ（実際のグループはAudioManagerが割り当てる）")]
    [SerializeField] private AudioChannel _channel = AudioChannel.SE;

    [Tooltip("音量")]
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;

    [Tooltip("再生のたびに、この範囲（最小値・最大値）からランダムにピッチを選択する。固定する場合は同じ値にする")]
    [SerializeField] private Vector2 _pitchRange = Vector2.one;

    [Tooltip("ループ再生するかどうか")]
    [SerializeField] private bool _loop;


    // ==================================================
    // ----- 3D Settings -----
    // ==================================================

    [Header("3D設定")]
    [Tooltip("0 = 2D、1 = 3D。AudioManager.PlaySEは2D、PlaySEAtは3Dで再生するため、この値は主にシーン上のAudioSourceに使用する")]
    [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0f;

    [Tooltip("3Dサウンドの最小距離")]
    [SerializeField, Min(0f)] private float _minDistance = 1f;

    [Tooltip("3Dサウンドの最大距離")]
    [SerializeField, Min(0.01f)] private float _maxDistance = 50f;

    [Tooltip("距離による音量減衰の方式")]
    [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;

    [Tooltip("ドップラー効果の強さ")]
    [SerializeField, Range(0f, 5f)] private float _dopplerLevel = 1f;


    // ==================================================
    // ----- Properties -----
    // ==================================================

    public AudioClip Clip => _clip;
    public AudioChannel Channel => _channel;
    public float Volume => _volume;
    public bool Loop => _loop;


    // ==================================================
    // ----- Custom Methods -----
    // ==================================================

    // pitchRangeの範囲からランダムにピッチを選択する
    public float GetRandomPitch()
    {
        return Random.Range(_pitchRange.x, _pitchRange.y);
    }

    // AudioSourceにこのデータの設定を適用する（再生は行わない）
    // 出力先のMixerグループはAudioManager.Instance.GetMixerGroup(Channel)で取得して設定する
    public void ApplyTo(AudioSource source)
    {
        source.clip = _clip;
        source.volume = _volume;
        source.pitch = GetRandomPitch();
        source.loop = _loop;
        source.spatialBlend = _spatialBlend;
        source.minDistance = _minDistance;
        source.maxDistance = _maxDistance;
        source.rolloffMode = _rolloffMode;
        source.dopplerLevel = _dopplerLevel;
    }


    // ==================================================
    // ----- Unity Standard Methods -----
    // ==================================================

    // Inspectorで値を変更したときに、不正な値を補正する
    private void OnValidate()
    {
        if (_pitchRange.x > _pitchRange.y)
        {
            _pitchRange = new Vector2(_pitchRange.y, _pitchRange.x);
        }

        if (_maxDistance < _minDistance)
        {
            _maxDistance = _minDistance;
        }
    }
}
