using UnityEngine;

// ==================================================
// ----- Audio Entry -----
// ==================================================

// ID名とAudioDataを1組にした構造体
// AudioLibraryのリストの1行分として使用する

[System.Serializable]
public struct AudioEntry
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Tooltip("音を識別するID名。先頭は大文字の英字、英数字とアンダースコアを使用できます（例：PlayerJump、Se_Attack）。AudioLibraryを保存すると、この名前でAudioIdを使用できる")]
    [Delayed][SerializeField] private string _name;

    [Tooltip("このIDで再生するAudioData")]
    [SerializeField] private AudioData _audioData;


    // ==================================================
    // ----- Properties -----
    // ==================================================

    public string Name => _name;
    public AudioData Data => _audioData;


    // ==================================================
    // ----- Custom Methods -----
    // ==================================================

    // ID名を設定する
    internal void SetName(string name)
    {
        _name = name;
    }
}
