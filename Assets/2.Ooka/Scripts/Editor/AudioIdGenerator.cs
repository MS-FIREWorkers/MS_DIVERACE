using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==================================================
// ----- Audio Id Generator -----
// ==================================================

// AudioLibraryに入力したID名から、AudioId（enum）のスクリプトを自動生成するエディタ拡張
// このスクリプトは、Editorフォルダ（Assets/Scripts/Editor）に配置すること
//
// 使い方：
//   ① AudioLibraryのInspectorで、行を追加してID名を入力し、AudioDataを割り当てる
//   ② AudioLibraryを保存する（Ctrl+S、またはFile → Save Project）と、AudioIdが自動で更新される
//      手動で更新する場合は、メニューの Tools → Audio → AudioIdを更新 を実行する
//      （AudioLibraryのInspector右上の「︙」メニューからも実行できる）
//   ③ Unityの再コンパイルが完了すると、AudioId.ID名 がコードで使用できる
//
// ・プロジェクト内のすべてのAudioLibraryを対象にする
// ・ゲーム実行中は更新できない（実行中に保存した場合は、停止後にメニューから更新する）
// ・ID名に問題がある場合は、エラーを表示して更新を中止する（AudioIdは変更されない）
// ・内容が変わらない場合は書き込まないため、再コンパイルは発生しない

public static class AudioIdGenerator
{
    // ==================================================
    // ----- Constants -----
    // ==================================================

    // AudioId.csが見つからない場合に作成する場所
    private const string _defaultOutputPath = "Assets/Scripts/AudioId.cs";

    private const string _menuPath = "Tools/Audio/AudioIdを更新";


    // ==================================================
    // ----- Menu Items -----
    // ==================================================

    [MenuItem(_menuPath)]
    private static void GenerateFromMenu()
    {
        Generate();
    }

    // AudioLibraryのInspectorの「︙」メニューから実行する
    [MenuItem("CONTEXT/AudioLibrary/AudioIdを更新")]
    private static void GenerateFromContext(MenuCommand command)
    {
        Generate();
    }


    // ==================================================
    // ----- Generate -----
    // ==================================================

    // AudioId.csを更新する
    // isAutomaticがtrueの場合（保存時の自動更新）は、更新の必要がないときにログを表示しない
    public static void Generate(bool isAutomatic = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "ゲーム実行中はAudioIdを更新できません。実行を停止してから、" +
                "メニューの Tools → Audio → AudioIdを更新 を実行してください。");
            return;
        }

        // すべてのAudioLibraryからID名を集める
        if (!TryCollectNames(out List<string> names))
        {
            Debug.LogError("AudioIdの更新を中止しました。AudioLibraryのID名を修正してから、もう一度保存（またはメニューから実行）してください。");
            return;
        }

        // 名前から決めた番号が重複していないか確認する
        if (HasDuplicateValue(names))
        {
            Debug.LogError("AudioIdの更新を中止しました。番号が重複しているID名があります。どちらかの名前を変更してください。");
            return;
        }

        string source = BuildSource(names);
        string outputPath = FindOutputPath();

        // 内容が変わらない場合は書き込まない（不要な再コンパイルを避ける）
        if (File.Exists(outputPath))
        {
            string currentSource = File.ReadAllText(outputPath).Replace("\r\n", "\n");

            if (currentSource == source)
            {
                if (!isAutomatic)
                {
                    Debug.Log($"AudioIdは最新の状態です。（{names.Count}件）");
                }

                return;
            }
        }

        File.WriteAllText(outputPath, source, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(outputPath);

        Debug.Log($"AudioIdを更新しました。（{names.Count}件）再コンパイルが完了すると使用できます。\n{outputPath}");
    }

    // すべてのAudioLibraryから、ID名を重複なく集めて名前順に並べる
    // 使用できない名前があった場合はエラーを表示し、falseを返す
    private static bool TryCollectNames(out List<string> names)
    {
        HashSet<string> nameSet = new HashSet<string>();
        bool hasError = false;

        foreach (string guid in AssetDatabase.FindAssets("t:AudioLibrary"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(path);

            if (library == null)
            {
                continue;
            }

            foreach (AudioEntry entry in library.Entries)
            {
                // 空の行は無視する
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                if (!AudioLibrary.IsValidName(entry.Name))
                {
                    Debug.LogError(
                        $"{path}：ID名「{entry.Name}」は使用できません。" +
                        "先頭は大文字の英字、英数字とアンダースコアのみ使用できます。" +
                        "アンダースコアは名前の途中にのみ使用でき、「None」も使用できません。",
                        library);
                    hasError = true;
                    continue;
                }

                nameSet.Add(entry.Name);
            }
        }

        names = new List<string>(nameSet);
        names.Sort(System.StringComparer.Ordinal);

        return !hasError;
    }

    // 異なる名前から、同じ番号が作られていないか確認する
    private static bool HasDuplicateValue(List<string> names)
    {
        Dictionary<int, string> usedValues = new Dictionary<int, string>();
        bool hasDuplicate = false;

        foreach (string name in names)
        {
            int value = GetValue(name);

            if (usedValues.TryGetValue(value, out string otherName))
            {
                Debug.LogError($"ID名「{name}」と「{otherName}」の番号が重複しています。");
                hasDuplicate = true;
                continue;
            }

            usedValues.Add(value, name);
        }

        return hasDuplicate;
    }

    // 名前から番号を作成する（FNV-1aというハッシュ計算を使用）
    // 同じ名前なら常に同じ番号になるため、他のIDを追加・削除しても既存の番号は変わらない
    private static int GetValue(string name)
    {
        uint hash = 2166136261;

        foreach (byte byteValue in Encoding.UTF8.GetBytes(name))
        {
            hash ^= byteValue;
            hash = unchecked(hash * 16777619);
        }

        int value = (int)(hash & 0x7FFFFFFF);

        // 0はNoneで使用するため、割り当てない
        return value == 0 ? 1 : value;
    }

    // AudioId.csの内容を作成する
    private static string BuildSource(List<string> names)
    {
        StringBuilder builder = new StringBuilder();

        builder.Append("// ==================================================\n");
        builder.Append("// ----- Audio Id（自動生成） -----\n");
        builder.Append("// ==================================================\n");
        builder.Append("\n");
        builder.Append("// このファイルはAudioIdGeneratorによって自動生成される。手動で編集しないこと\n");
        builder.Append("//\n");
        builder.Append("// 更新方法：\n");
        builder.Append("//   AudioLibraryのInspectorでID名を入力して保存すると、自動で更新される\n");
        builder.Append("//   （手動で更新する場合は、メニューの Tools → Audio → AudioIdを更新 を実行する）\n");
        builder.Append("//\n");
        builder.Append("// ・名前は、AudioLibraryに入力したID名と同じになる\n");
        builder.Append("// ・番号は名前から自動で決まる（他のIDを追加・削除しても、既存の番号は変わらない）\n");
        builder.Append("\n");
        builder.Append("public enum AudioId\n");
        builder.Append("{\n");
        builder.Append("    None = 0, // 未設定\n");

        if (names.Count > 0)
        {
            builder.Append("\n");
        }

        foreach (string name in names)
        {
            builder.Append($"    {name} = {GetValue(name)},\n");
        }

        builder.Append("}\n");

        return builder.ToString();
    }

    // 既存のAudioId.csの場所を探す（移動されていても対応できるようにする）
    // 見つからない場合は、既定の場所に作成する
    private static string FindOutputPath()
    {
        foreach (string guid in AssetDatabase.FindAssets("AudioId t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetFileNameWithoutExtension(path) == "AudioId")
            {
                return path;
            }
        }

        return _defaultOutputPath;
    }
}


// ==================================================
// ----- Auto Update -----
// ==================================================

// AudioLibraryが保存されたときに、AudioIdを自動で更新するクラス
// （Ctrl+S、またはFile → Save Project で、変更のあるAudioLibraryが保存されたとき）
public class AudioLibrarySaveProcessor : AssetModificationProcessor
{
    // ==================================================
    // ----- Private Variables -----
    // ==================================================

    // 更新の実行が予約済みかどうか（複数のAudioLibraryを同時に保存しても、更新は1回だけにする）
    private static bool _isScheduled;


    // ==================================================
    // ----- Unity Standard Methods -----
    // ==================================================

    // アセットが保存される直前にUnityから呼び出される
    private static string[] OnWillSaveAssets(string[] paths)
    {
        foreach (string path in paths)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AudioLibrary))
            {
                ScheduleGenerate();
                break;
            }
        }

        return paths;
    }


    // ==================================================
    // ----- Custom Methods -----
    // ==================================================

    // 保存の処理が終わった後に、AudioIdの更新を1回だけ実行する
    private static void ScheduleGenerate()
    {
        if (_isScheduled)
        {
            return;
        }

        _isScheduled = true;

        EditorApplication.delayCall += () =>
        {
            _isScheduled = false;
            AudioIdGenerator.Generate(true);
        };
    }
}
