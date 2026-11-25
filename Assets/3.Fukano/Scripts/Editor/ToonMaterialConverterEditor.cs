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
    private const string PlayerToonShaderName = "Custom/PlayerCharacterToon";
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineModeId = Shader.PropertyToID("_OutlineMode");
    private static readonly int OutlineOnlyId = Shader.PropertyToID("_OutlineOnly");
    private static readonly int SilhouetteWidthPixelsId = Shader.PropertyToID("_SilhouetteWidthPixels");
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int ShadowThresholdId = Shader.PropertyToID("_ShadowThreshold");
    private static readonly int LightThresholdId = Shader.PropertyToID("_LightThreshold");
    private static readonly int SpecularStrengthId = Shader.PropertyToID("_SpecularStrength");
    private static readonly int SpecularSmoothnessId = Shader.PropertyToID("_SpecularSmoothness");

    // ==================================================
    // ----- Unity Editor Events -----
    // ==================================================

    public override void OnInspectorGUI()
    {
        DrawConverterSettings();
        ToonMaterialConverter converter = (ToonMaterialConverter)target;
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Convert To Toon"))
            ConvertToToon(converter);

        if (GUILayout.Button("Update Outline Settings"))
            UpdateMaterials(converter, "Update Toon Outline", ApplyOutline);

        using (new EditorGUI.DisabledScope(!converter.OverrideShadowSettings || converter.Records.Count == 0))
        {
            if (GUILayout.Button("Update Shadow Settings"))
                UpdateMaterials(converter, "Update Toon Shadow", ApplyShadow);
        }

        using (new EditorGUI.DisabledScope(!converter.OverrideSpecularSettings || converter.Records.Count == 0))
        {
            if (GUILayout.Button("Update Specular Settings"))
                UpdateMaterials(converter, "Update Toon Specular", ApplySpecular);
        }

        if (GUILayout.Button("Restore Original Materials"))
            RestoreOriginals(converter);
    }

    private void DrawConverterSettings()
    {
        serializedObject.Update();
        SerializedProperty outlineEnabled = serializedObject.FindProperty("_enableOutline");
        SerializedProperty outlineOnly = serializedObject.FindProperty("_outlineOnly");
        SerializedProperty outlineMode = serializedObject.FindProperty("_outlineMode");
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            bool useSilhouette = outlineOnly.boolValue ||
                outlineMode.intValue == (int)ToonMaterialConverter.OutlineMode.ScreenSpaceSilhouette;
            bool show = property.name switch
            {
                "_outlineOnly" => outlineEnabled.boolValue,
                "_outlineMode" => outlineEnabled.boolValue && !outlineOnly.boolValue,
                "_silhouetteWidthPixels" => outlineEnabled.boolValue && useSilhouette,
                "_outlineColor" or "_outlineWidth" => outlineEnabled.boolValue && !useSilhouette,
                _ => true
            };
            if (!show)
                continue;

            using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                EditorGUILayout.PropertyField(property, includeChildren: true);
        }

        serializedObject.ApplyModifiedProperties();
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

        Transform targetRoot = converter.TargetRoot;
        Renderer[] renderers = System.Array.FindAll(
            targetRoot.GetComponentsInChildren<Renderer>(true),
            renderer =>
            {
                ToonMaterialConverter owner = renderer.GetComponentInParent<ToonMaterialConverter>(true);
                return owner == null || owner == converter;
            });
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[Warning] 変換対象のRendererが見つかりません。", converter);
            return;
        }

        // AssetDatabaseを通してフォルダを作成する。
        EnsureAssetFolder(OutputRoot);
        string folderName = targetRoot.name + "_Toon";
        string folderPath = OutputRoot + "/" + folderName;
        EnsureAssetFolder(folderPath);

        Dictionary<Material, Material> cache = new();
        Undo.RecordObject(converter, "Convert To Toon");

        foreach (Renderer renderer in renderers)
        {
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
                        ApplyShadow(converter, toon);
                        ApplySpecular(converter, toon);
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
                        ApplyShadow(converter, toon);
                        ApplySpecular(converter, toon);
                        EditorUtility.SetDirty(toon);
                    }

                    cache.Add(original, toon);
                }

                converted[i] = toon;
            }

            Undo.RecordObject(renderer, "Convert To Toon");
            converter.AddRecord(renderer, originals, converted);
            renderer.sharedMaterials = converted;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(converter);
        EditorUtility.SetDirty(converter);
        AssetDatabase.SaveAssets();
        Debug.Log("Toon Material変換完了", converter);
    }

    // ==================================================
    // ----- Custom Methods: Toon Settings -----
    // ==================================================

    private void UpdateMaterials(ToonMaterialConverter converter, string undoLabel,
        System.Action<ToonMaterialConverter, Material> applySettings)
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

                Undo.RecordObject(material, undoLabel);
                applySettings(converter, material);
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
        bool outlineOnly = converter.OutlineOnly && material.HasProperty(OutlineOnlyId);
        if (material.HasProperty(OutlineOnlyId))
            material.SetFloat(OutlineOnlyId, outlineOnly ? 1.0f : 0.0f);
        if (material.HasProperty(OutlineModeId))
        {
            ToonMaterialConverter.OutlineMode mode = outlineOnly
                ? ToonMaterialConverter.OutlineMode.ScreenSpaceSilhouette
                : converter.SelectedOutlineMode;
            material.SetFloat(OutlineModeId, converter.EnableOutline ? (float)mode : -1.0f);
        }
        if (material.HasProperty(SilhouetteWidthPixelsId))
            material.SetFloat(SilhouetteWidthPixelsId, converter.SilhouetteWidthPixels);
    }

    private void ApplySpecular(ToonMaterialConverter converter, Material material)
    {
        if (!converter.OverrideSpecularSettings)
            return;

        if (material.HasProperty(SpecularStrengthId))
            material.SetFloat(SpecularStrengthId, converter.SpecularStrength);
        if (material.HasProperty(SpecularSmoothnessId))
            material.SetFloat(SpecularSmoothnessId, converter.SpecularSmoothness);
    }

    private void ApplyShadow(ToonMaterialConverter converter, Material material)
    {
        if (!converter.OverrideShadowSettings)
            return;

        if (material.HasProperty(ShadowColorId))
            material.SetColor(ShadowColorId, converter.ShadowColor);
        if (material.HasProperty(ShadowThresholdId))
            material.SetFloat(ShadowThresholdId, converter.ShadowThreshold);
        if (material.HasProperty(LightThresholdId))
            material.SetFloat(LightThresholdId, converter.LightThreshold);
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
            PrefabUtility.RecordPrefabInstancePropertyModifications(record.renderer);
            EditorUtility.SetDirty(record.renderer);
        }

        converter.ClearRecords();
        PrefabUtility.RecordPrefabInstancePropertyModifications(converter);
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

        if (destination.shader.name != PlayerToonShaderName || !source.HasProperty("_BASE_COLOR_MAP"))
            return;

        CopyTexture(source, "_BASE_COLOR_MAP", destination, "_BaseMap");
        CopyTexture(source, "_NORMAL_MAP", destination, "_BumpMap");
        CopyTexture(source, "_EMISSION_COLOR_MAP", destination, "_EmissionMap");
        CopyTexture(source, "_OPACITY_MAP", destination, "_OpacityMap");

        if (source.HasProperty("_BASE_COLOR"))
            destination.SetColor("_BaseColor", source.GetColor("_BASE_COLOR"));
        if (source.HasProperty("_EMISSION_COLOR") && destination.GetTexture("_EmissionMap") != null)
            destination.SetColor("_EmissionColor", source.GetColor("_EMISSION_COLOR"));

        if (destination.GetTexture("_OpacityMap") != null)
        {
            destination.SetFloat("_AlphaClip", 1.0f);
            destination.EnableKeyword("_ALPHATEST_ON");
        }
    }

    private static void CopyTexture(Material source, string sourceProperty, Material destination, string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty))
            return;

        destination.SetTexture(destinationProperty, source.GetTexture(sourceProperty));
        if (destinationProperty == "_BaseMap")
        {
            destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
            destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
        }
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
