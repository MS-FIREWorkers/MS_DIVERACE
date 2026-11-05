// ==================================================
// File: ToonMaterialConverterEditor.cs
// Created: 2026/10/14
// Author: 深野大輝
// Summary: Toonマテリアルの生成、設定更新、復元をInspectorから行う。
// Place: Assets/Editor/ToonMaterialConverterEditor.cs
// ==================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ToonMaterialConverter))]
public class ToonMaterialConverterEditor : Editor
{
    // ==================================================
    // ----- Variables -----
    // ==================================================

    private const string OutputRoot = "Assets/3.Fukano/GeneratedToonMaterials";
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    // ==================================================
    // ----- Unity Editor Events -----
    // ==================================================

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ToonMaterialConverter converter = (ToonMaterialConverter)target;
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Convert To Toon"))
            ConvertToToon(converter);

        if (GUILayout.Button("Update Outline Settings"))
            UpdateOutline(converter);

        if (GUILayout.Button("Restore Original Materials"))
            RestoreOriginals(converter);
    }

    // ==================================================
    // ----- Custom Methods: Conversion -----
    // ==================================================

    private void ConvertToToon(ToonMaterialConverter converter)
    {
        if (converter.ToonShader == null)
        {
            Debug.LogError("[Error] Toon Shaderが未設定です。", converter);
            return;
        }

        if (converter.Records.Count > 0)
        {
            Debug.LogWarning("[Warning] 変換済みです。先にRestoreしてください。", converter);
            return;
        }

        // AssetDatabaseを通してフォルダを作成する。
        EnsureAssetFolder(OutputRoot);
        string folderName = converter.gameObject.name + "_Toon";
        string folderPath = OutputRoot + "/" + folderName;
        EnsureAssetFolder(folderPath);

        Dictionary<Material, Material> cache = new();
        Renderer[] renderers = converter.GetComponentsInChildren<Renderer>(true);
        Undo.RecordObject(converter, "Convert To Toon");

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            ToonMaterialConverter owner = renderer.GetComponentInParent<ToonMaterialConverter>();
            if (owner != converter)
                continue;

            Material[] originals = renderer.sharedMaterials;
            Material[] converted = new Material[originals.Length];

            for (int i = 0; i < originals.Length; i++)
            {
                Material original = originals[i];
                if (original == null)
                    continue;

                if (!cache.TryGetValue(original, out Material toon))
                {
                    toon = converter.FindToonMaterial(original);
                    if (toon == null)
                    {
                        toon = new Material(converter.ToonShader)
                        {
                            name = original.name + "_Toon"
                        };

                        CopyProperties(original, toon);
                        ApplyOutline(converter, toon);
                        string path = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + toon.name + ".mat");
                        AssetDatabase.CreateAsset(toon, path);
                        converter.RegisterToonMaterial(original, toon);
                    }
                    else
                    {
                        Undo.RecordObject(toon, "Update Toon Material");
                        if (toon.shader != converter.ToonShader)
                        {
                            toon.shader = converter.ToonShader;
                            CopyProperties(original, toon);
                        }

                        ApplyOutline(converter, toon);
                        EditorUtility.SetDirty(toon);
                    }

                    cache.Add(original, toon);
                }

                converted[i] = toon;
            }

            Undo.RecordObject(renderer, "Convert To Toon");
            converter.AddRecord(renderer, originals, converted);
            renderer.sharedMaterials = converted;
            EditorUtility.SetDirty(renderer);
        }

        EditorUtility.SetDirty(converter);
        AssetDatabase.SaveAssets();
        Debug.Log("Toon Material変換完了", converter);
    }

    // ==================================================
    // ----- Custom Methods: Outline -----
    // ==================================================

    private void UpdateOutline(ToonMaterialConverter converter)
    {
        HashSet<Material> updated = new();
        foreach (ToonMaterialConverter.RendererMaterialRecord record in converter.Records)
        {
            if (record == null || record.toonMaterials == null)
                continue;

            foreach (Material material in record.toonMaterials)
            {
                if (material == null || !updated.Add(material))
                    continue;

                Undo.RecordObject(material, "Update Toon Outline");
                ApplyOutline(converter, material);
                EditorUtility.SetDirty(material);
            }
        }

        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
    }

    private void ApplyOutline(ToonMaterialConverter converter, Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty(OutlineColorId))
            material.SetColor(OutlineColorId, converter.OutlineColor);
        if (material.HasProperty(OutlineWidthId))
            material.SetFloat(OutlineWidthId, converter.EnableOutline ? converter.OutlineWidth : 0.0f);
    }

    // ==================================================
    // ----- Custom Methods: Restore -----
    // ==================================================

    private void RestoreOriginals(ToonMaterialConverter converter)
    {
        Undo.RecordObject(converter, "Restore Original Materials");
        foreach (ToonMaterialConverter.RendererMaterialRecord record in converter.Records)
        {
            if (record == null || record.renderer == null || record.originalMaterials == null)
                continue;

            Undo.RecordObject(record.renderer, "Restore Original Materials");
            record.renderer.sharedMaterials = record.originalMaterials;
            EditorUtility.SetDirty(record.renderer);
        }

        converter.ClearRecords();
        EditorUtility.SetDirty(converter);
        Debug.Log("元Materialへ復元しました。", converter);
    }

    // ==================================================
    // ----- Custom Methods: Material Properties -----
    // ==================================================

    private void CopyProperties(Material source, Material destination)
    {
        string baseMapProperty = null;
        if (source.HasProperty("_BaseMap"))
            baseMapProperty = "_BaseMap";
        else if (source.HasProperty("_MainTex"))
            baseMapProperty = "_MainTex";

        if (baseMapProperty != null)
        {
            destination.SetTexture("_BaseMap", source.GetTexture(baseMapProperty));
            destination.SetTextureScale("_BaseMap", source.GetTextureScale(baseMapProperty));
            destination.SetTextureOffset("_BaseMap", source.GetTextureOffset(baseMapProperty));
        }

        if (source.HasProperty("_BaseColor"))
            destination.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        else if (source.HasProperty("_Color"))
            destination.SetColor("_BaseColor", source.GetColor("_Color"));

        if (source.HasProperty("_BumpMap"))
            destination.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        if (source.HasProperty("_BumpScale"))
            destination.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));

        bool isAlphaClip = source.HasProperty("_AlphaClip")
            ? source.GetFloat("_AlphaClip") > 0.5f
            : source.IsKeywordEnabled("_ALPHATEST_ON");

        destination.SetFloat("_AlphaClip", isAlphaClip ? 1.0f : 0.0f);
        if (isAlphaClip)
            destination.EnableKeyword("_ALPHATEST_ON");
        else
            destination.DisableKeyword("_ALPHATEST_ON");

        if (source.HasProperty("_Cutoff"))
            destination.SetFloat("_Cutoff", source.GetFloat("_Cutoff"));
        if (source.HasProperty("_EmissionMap"))
            destination.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
        if (source.HasProperty("_EmissionColor"))
            destination.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
    }

    // ==================================================
    // ----- Custom Methods: Asset Folders -----
    // ==================================================

    private void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parentPath = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName))
            return;

        EnsureAssetFolder(parentPath);
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
#endif
