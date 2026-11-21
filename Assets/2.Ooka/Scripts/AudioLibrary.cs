using System.Collections.Generic;
using System.Text;
using UnityEngine;

// ==================================================
// ----- Audio Library -----
// ==================================================

// ID名とAudioDataの対応表をまとめたScriptableObject
// 音の登録・差し替えは、このアセットで一元管理する
//
// 作成方法：Projectウィンドウで右クリック → Create → Audio → AudioLibrary
// 作成後、AudioManagerの「Libraries」に登録して使用する
//
// 新しい音の追加方法：
//   ① 「登録リスト」に行を追加し、Nameに名前（例：PlayerJump）を入力して、AudioDataを割り当てる
//   ② このアセットを保存する（Ctrl+S、またはFile → Save Project）と、AudioIdが自動で更新される
//      手動で更新する場合は、メニューの Tools → Audio → AudioIdを更新 を実行する
//      （このアセットのInspector右上の「︙」メニューからも実行できる）
//   ③ 再コンパイルが完了すると、AudioId.PlayerJump のように、コードで使用できる

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio/AudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    // ==================================================
    // ----- Inspector Settings -----
    // ==================================================

    [Header("登録リスト")]
    [Tooltip("ID名とAudioDataの対応表。同じ名前は1つだけ登録する")]
    [SerializeField] private List<AudioEntry> _entries = new List<AudioEntry>();


    // ==================================================
    // ----- Properties -----
    // ==================================================

    public IReadOnlyList<AudioEntry> Entries => _entries;


    // ==================================================
    // ----- Custom Methods -----
    // ==================================================

    // ID名を自動補正する
    // ・先頭の英字を大文字にする
    // ・空白とアンダースコアを1つのアンダースコアに統一する
    // ・先頭と末尾のアンダースコアを削除する
    public static string NormalizeName(string idName)
    {
        if (string.IsNullOrEmpty(idName))
        {
            return idName;
        }

        StringBuilder builder = new StringBuilder();
        bool previousWasSeparator = false;

        foreach (char character in idName.Trim())
        {
            if (char.IsWhiteSpace(character) || character == '_')
            {
                if (builder.Length > 0 && !previousWasSeparator)
                {
                    builder.Append('_');
                    previousWasSeparator = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasSeparator = false;
        }

        // 末尾のアンダースコアを削除する
        if (builder.Length > 0 && builder[builder.Length - 1] == '_')
        {
            builder.Length--;
        }

        // 先頭の英字を大文字にする
        if (builder.Length > 0)
        {
            builder[0] = char.ToUpperInvariant(builder[0]);
        }

        return builder.ToString();
    }


    // ID名として使用できるか確認する
    // ・先頭は大文字の英字
    // ・英数字とアンダースコアのみ使用可能
    // ・アンダースコアは名前の途中にのみ使用可能
    // 「None」は未設定を表すために予約されているため、使用できない
    public static bool IsValidName(string idName)
    {
        if (string.IsNullOrEmpty(idName) || idName == "None")
        {
            return false;
        }

        if (idName[0] < 'A' || idName[0] > 'Z')
        {
            return false;
        }

        bool previousWasSeparator = false;

        for (int i = 0; i < idName.Length; i++)
        {
            char character = idName[i];

            bool isLetter =
                (character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z');

            bool isDigit = character >= '0' && character <= '9';

            if (character == '_')
            {
                // 先頭・末尾・連続したアンダースコアは不可
                if (i == 0 || i == idName.Length - 1 || previousWasSeparator)
                {
                    return false;
                }

                previousWasSeparator = true;
                continue;
            }

            if (!isLetter && !isDigit)
            {
                return false;
            }

            previousWasSeparator = false;
        }

        return true;
    }


    // ==================================================
    // ----- Unity Standard Methods -----
    // ==================================================

    // Inspectorで値を変更したときに、登録内容を自動補正・確認する
    // （追加したばかりの空の行は、警告の対象にしない）
    private void OnValidate()
    {
        HashSet<string> registeredNames = new HashSet<string>();

        for (int i = 0; i < _entries.Count; i++)
        {
            AudioEntry entry = _entries[i];

            // ID名を自動補正する
            string normalizedName = NormalizeName(entry.Name);

            if (normalizedName != entry.Name)
            {
                entry.SetName(normalizedName);
                _entries[i] = entry;
            }

            bool hasName = !string.IsNullOrEmpty(entry.Name);
            bool hasData = entry.Data != null;

            if (!hasName && !hasData)
            {
                continue;
            }

            if (!hasName)
            {
                Debug.LogWarning($"{name}：Element {i} のNameが未設定です。", this);
                continue;
            }

            if (!IsValidName(entry.Name))
            {
                Debug.LogWarning(
                    $"{name}：Element {i}（{entry.Name}）のNameは使用できません。" +
                    "先頭は大文字の英字、英数字とアンダースコアのみ使用できます。" +
                    "アンダースコアは名前の途中にのみ使用でき、「None」も使用できません。",
                    this);
                continue;
            }

            // AudioDataの有無にかかわらず、重複したID名を検出する
            if (!registeredNames.Add(entry.Name))
            {
                Debug.LogWarning($"{name}：Element {i}（{entry.Name}）のNameが重複しています。", this);
                continue;
            }

            if (!hasData)
            {
                Debug.LogWarning($"{name}：Element {i}（{entry.Name}）のAudioDataが未設定です。", this);
            }
        }
    }
}
