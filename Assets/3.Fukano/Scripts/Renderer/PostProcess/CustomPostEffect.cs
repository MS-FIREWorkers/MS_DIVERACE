// ==================================================
// File: CustomPostEffect.cs
// Created: 2026/10/21
// Author: Fukano
// Summary: ポストエフェクト共通基底クラス
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

// ==================================================
// CustomPostEffect
// ==================================================

public abstract class CustomPostEffect : ScriptableObject
{
    // ==================================================
    // Variables
    // ==================================================

    [Header("Effect Settings")]
    [Tooltip("エフェクトを有効にするか")]
    [SerializeField]
    private bool _isEnabled = true;

    [Tooltip("エフェクトの元となるMaterial")]
    [SerializeField]
    private Material _material;

    [System.NonSerialized]
    private Material _runtimeMaterial;

    [System.NonSerialized]
    private Material _cachedSourceMaterial;

    public bool IsEnabled => _isEnabled;

    public Material EffectMaterial => GetRuntimeMaterial();

    // 深度テクスチャが必要か
    public virtual bool RequiresDepth => false;

    // ==================================================
    // Unity Functions
    // ==================================================

    protected virtual void OnDisable()
    {
        ReleaseMaterial();
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    public virtual bool IsActive()
    {
        return _isEnabled && _material != null;
    }

    public Material GetRuntimeMaterial()
    {
        if (_material == null)
        {
            ReleaseMaterial();
            return null;
        }

        if (_runtimeMaterial != null &&
            _cachedSourceMaterial != _material)
        {
            ReleaseMaterial();
        }

        if (_runtimeMaterial == null)
        {
            _runtimeMaterial = new Material(_material)
            {
                name = $"{name}_RuntimeMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };

            _cachedSourceMaterial = _material;
        }

        return _runtimeMaterial;
    }

    public virtual void SetupMaterial()
    {
    }

    public virtual void SetupMaterial(Material material)
    {
    }

    public virtual int GetMaterialPass()
    {
        return 0;
    }

    private void ReleaseMaterial()
    {
        if (_runtimeMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_runtimeMaterial);
        else
            DestroyImmediate(_runtimeMaterial);

        _runtimeMaterial = null;
        _cachedSourceMaterial = null;
    }

    // ==================================================
    // Render Graph Functions
    // ==================================================

    // 通常のColor専用Effect
    public virtual TextureHandle RecordEffect(
        RenderGraph renderGraph,
        TextureHandle source,
        RenderTextureDescriptor descriptor,
        int effectIndex)
    {
        Material material = EffectMaterial;

        if (material == null)
            return source;

        int materialPass = GetMaterialPass();

        if (materialPass < 0 ||
            materialPass >= material.passCount)
            return source;

        SetupMaterial();
        //SetupMaterial(material);

        return CustomPostProcessBlitter.AddBlitPass(
            renderGraph,
            source,
            descriptor,
            material,
            materialPass,
            $"CustomPostEffect_{effectIndex}_{name}"
        );
    }

    // Depth対応Effect用
    public virtual TextureHandle RecordEffect(
        RenderGraph renderGraph,
        TextureHandle source,
        TextureHandle depth,
        RenderTextureDescriptor descriptor,
        int effectIndex)
    {
        // デフォルトは従来の処理へ転送
        return RecordEffect(
            renderGraph,
            source,
            descriptor,
            effectIndex
        );
    }

    // ----- Runtime Control -----

    public virtual void SetEnabled(bool isEnabled)
    {
        _isEnabled = isEnabled;
    }
}
