// ==================================================
// File: ToonMaterialConverter.cs
// Created: 2026/10/14
// Author: 深野大輝
// Summary: Toonマテリアル変換設定と元マテリアルの復元情報を保持する。
// ==================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ToonMaterialConverter : MonoBehaviour
{
    public enum OutlineMode
    {
        MeshExtrusion,
        ScreenSpaceSilhouette
    }

    // ==================================================
    // ----- Serializable Data -----
    // ==================================================

    [Serializable]
    public class RendererMaterialRecord
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] toonMaterials;
    }

    [Serializable]
    public class ToonMaterialEntry
    {
        public Material original;
        public Material toon;
    }

    // ==================================================
    // ----- Variables -----
    // ==================================================

    [Header("Toon Settings")]
    [Tooltip("変換先のトゥーンシェーダー")]
    [SerializeField, FormerlySerializedAs("toonShader")]
    private Shader _toonShader;

    [Tooltip("変換対象。未指定の場合はこのコンポーネントのGameObjectを使用します。")]
    [SerializeField] private Transform _targetRoot;


    [Header("Outline Settings")]
    [Tooltip("アウトライン描画の有効・無効")]
    [SerializeField, FormerlySerializedAs("enableOutline")]
    private bool _enableOutline = true;

    [Tooltip("本体と影を隠し、画面シルエットの輪郭線だけを表示します。")]
    [SerializeField] private bool _outlineOnly;

    [Tooltip("画面シルエット方式の色はRenderer Featureの共有設定を使用します。")]
    [SerializeField] private OutlineMode _outlineMode;

    [Tooltip("画面シルエット方式の線幅。単位は描画解像度のピクセルです。")]
    [SerializeField, Range(1, 4)] private int _silhouetteWidthPixels = 2;

    [Tooltip("アウトラインの色")]
    [SerializeField, FormerlySerializedAs("outlineColor")]
    private Color _outlineColor = Color.black;

    [Tooltip("アウトラインの太さ")]
    [SerializeField, Range(0.0f, 0.2f), FormerlySerializedAs("outlineWidth")]
    private float _outlineWidth = 0.05f;


    [Header("Shadow Settings")]
    [Tooltip("変換先マテリアルの影設定を、このコンバーターの設定で上書きします。")]
    [SerializeField] private bool _overrideShadowSettings;
    [SerializeField] private Color _shadowColor = new(0.3f, 0.3f, 0.4f, 1.0f);

    [Tooltip("影から中間色へ切り替わる明るさ")]
    [SerializeField, Range(0.0f, 1.0f)] private float _shadowThreshold = 0.3f;

    [Tooltip("中間色から明部へ切り替わる明るさ。適用時は影のしきい値以上になります。")]
    [SerializeField, Range(0.0f, 1.0f)] private float _lightThreshold = 0.6f;



    [Header("Specular Highlight")]
    [Tooltip("対応するToonマテリアルの反射ハイライト設定を上書きします。")]
    [SerializeField] private bool _overrideSpecularSettings;

    [Tooltip("反射ハイライトの強さ。0で無効になります。")]
    [SerializeField, Range(0.0f, 2.0f)] private float _specularStrength = 0.25f;

    [Tooltip("値が高いほどハイライトが小さく鋭くなります。")]
    [SerializeField, Range(0.0f, 1.0f)] private float _specularSmoothness = 0.6f;


    [SerializeField, HideInInspector, FormerlySerializedAs("records")]
    private List<RendererMaterialRecord> _records = new();

    [SerializeField, HideInInspector, FormerlySerializedAs("materialEntries")]
    private List<ToonMaterialEntry> _materialEntries = new();

    public Shader ToonShader => _toonShader;
    public Transform TargetRoot => _targetRoot != null ? _targetRoot : transform;
    public IReadOnlyList<RendererMaterialRecord> Records => _records;
    public IReadOnlyList<ToonMaterialEntry> MaterialEntries => _materialEntries;
    public bool OverrideShadowSettings => _overrideShadowSettings;
    public Color ShadowColor => _shadowColor;
    public float ShadowThreshold => _shadowThreshold;
    public float LightThreshold => Mathf.Max(_shadowThreshold, _lightThreshold);
    public bool OverrideSpecularSettings => _overrideSpecularSettings;
    public float SpecularStrength => _specularStrength;
    public float SpecularSmoothness => _specularSmoothness;
    public bool EnableOutline => _enableOutline;
    public bool OutlineOnly => _enableOutline && _outlineOnly;
    public OutlineMode SelectedOutlineMode => _outlineMode;
    public int SilhouetteWidthPixels => _silhouetteWidthPixels;
    public Color OutlineColor => _outlineColor;
    public float OutlineWidth => _outlineWidth;

    // ==================================================
    // ----- Custom Methods -----
    // ==================================================

    public void AddRecord(Renderer renderer, Material[] originals, Material[] converted)
    {
        if (renderer == null || originals == null || converted == null)
        {
            Debug.LogError("[Error] RendererまたはMaterial配列が無効です。", this);
            return;
        }

        _records.Add(new RendererMaterialRecord
        {
            renderer = renderer,
            originalMaterials = (Material[])originals.Clone(),
            toonMaterials = (Material[])converted.Clone()
        });
    }

    public void ClearRecords()
    {
        _records.Clear();
    }

    public Material FindToonMaterial(Material original)
    {
        if (original == null)
            return null;

        foreach (ToonMaterialEntry entry in _materialEntries)
        {
            if (entry != null && entry.original == original)
                return entry.toon;
        }

        return null;
    }

    public void RegisterToonMaterial(Material original, Material toon)
    {
        if (original == null || toon == null)
        {
            Debug.LogError("[Error] Materialの登録に失敗しました。", this);
            return;
        }

        foreach (ToonMaterialEntry entry in _materialEntries)
        {
            if (entry != null && entry.original == original)
            {
                entry.toon = toon;
                return;
            }
        }

        _materialEntries.Add(new ToonMaterialEntry
        {
            original = original,
            toon = toon
        });
    }
}
