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

    [Header("Outline Settings")]
    [Tooltip("アウトライン描画の有効・無効")]
    [SerializeField, FormerlySerializedAs("enableOutline")]
    private bool _enableOutline = true;

    [Tooltip("アウトラインの色")]
    [SerializeField, FormerlySerializedAs("outlineColor")]
    private Color _outlineColor = Color.black;

    [Tooltip("アウトラインの太さ")]
    [SerializeField, Range(0.0f, 0.2f), FormerlySerializedAs("outlineWidth")]
    private float _outlineWidth = 0.05f;

    [SerializeField, HideInInspector, FormerlySerializedAs("records")]
    private List<RendererMaterialRecord> _records = new();

    [SerializeField, HideInInspector, FormerlySerializedAs("materialEntries")]
    private List<ToonMaterialEntry> _materialEntries = new();

    public Shader ToonShader => _toonShader;
    public IReadOnlyList<RendererMaterialRecord> Records => _records;
    public IReadOnlyList<ToonMaterialEntry> MaterialEntries => _materialEntries;
    public bool EnableOutline => _enableOutline;
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
