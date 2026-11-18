using UnityEditor;
using UnityEngine;

// 仮組のInspectorに、演出の再生とリセット用ボタンを追加する。
[CustomEditor(typeof(SkyshaftPrototype))]
public sealed class SkyshaftPrototypeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // publicフィールドを通常どおり表示し、その下に仮組の操作ボタンを追加する。
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "cameraTag");

        var cameraTag = serializedObject.FindProperty("cameraTag");
        cameraTag.stringValue = EditorGUILayout.TagField(new GUIContent("Camera Tag", "取得するカメラ自身のGameObjectに付けるタグ。"), cameraTag.stringValue);

        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("Player Tag・Camera Tagでの取得を優先します。取得できない場合、Character・View Cameraの手動指定を使います。入口の発動判定はPlayer Tagで行います。", MessageType.Info);
 
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            // 時間経過による演出の再生は、Playモード中だけ受け付ける。
            if (GUILayout.Button("Play Whiteout")) 
                ((SkyshaftPrototype)target).PlayWhiteout();
        }

        if (GUILayout.Button("Reset Preview"))
        {
            // 編集中のリセットはUndoで戻せるようにし、保存対象の変更として記録する。
            Undo.RecordObject(target, "Reset Skyshaft Preview");
            ((SkyshaftPrototype)target).ResetPreview();
            EditorUtility.SetDirty(target);
        }

        if (GUI.changed)
        {
            // 編集中も設定変更が見えるよう、更新とSceneビューの再描画を要求する。
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}

// タグを文字入力せず、登録済みタグから選択できるようにする。
[CustomEditor(typeof(SkyshaftEntranceTrigger))]
public sealed class SkyshaftEntranceTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "playerTag");

        var tag = serializedObject.FindProperty("playerTag");
        tag.stringValue = EditorGUILayout.TagField(new GUIContent("Player Tag", "Playerの親に付けるタグ。子Colliderからも判別します。"), tag.stringValue);

        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("Playerの直接指定は不要です。指定タグを持つ本人または子の3D Colliderが範囲に入ると発動します。BoxColliderのCenter・Size・Edit Colliderで範囲を調整してください。", MessageType.Info);
    }
}
