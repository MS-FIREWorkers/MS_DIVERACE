// ==================================================
// File: CustomPostProcessFeature.cs
// Created: 2026/10/21
// Author: Fukano
// Summary: ポストプロセス用Renderer Feature
//          実行時はPostEffectManagerの設定を使用する
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ==================================================
// CustomPostProcessFeature
// ==================================================

public class CustomPostProcessFeature : ScriptableRendererFeature
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Post Process Settings -----

    [Header("Post Process Settings")]
    [Tooltip("実行するポストエフェクトの設定アセット")]
    [SerializeField]
    private CustomPostProcessSettings _settings;

    // ----- Rendering Settings -----

    [Header("Rendering Settings")]
    [Tooltip("ポストプロセスの実行タイミング")]
    [SerializeField]
    private RenderPassEvent _renderPassEvent =
        RenderPassEvent.BeforeRenderingPostProcessing;

    private CustomPostProcessPass _postProcessPass;

    // ----- Exclusion Settings -----

    [Header("Exclusion Settings")]
    [Tooltip("Layer除外機能を有効にするか")]
    [SerializeField]
    private bool _isExclusionEnabled = false;

    [Tooltip("ポストエフェクトから除外するLayer")]
    [SerializeField]
    private LayerMask _excludedLayers = 0;

    [Tooltip("除外マスク描画用Material")]
    [SerializeField]
    private Material _exclusionMaskMaterial;

    // ----- Exclusion Composite Settings -----

    [Header("Exclusion Composite Settings")]
    [Tooltip("除外マスクによる合成に使用するMaterial")]
    [SerializeField]
    private Material _exclusionCompositeMaterial;

    // ----- Exclusion Debug Settings -----

    [Header("Exclusion Debug Settings")]
    [Tooltip("除外マスクを白黒で画面に表示する")]
    [SerializeField]
    private bool _isExclusionDebugEnabled = false;

    [Tooltip("除外マスクのデバッグ表示用Material")]
    [SerializeField]
    private Material _exclusionDebugMaterial;

    // ----- Public Properties -----

    public bool IsExclusionEnabled => _isExclusionEnabled;

    public LayerMask ExcludedLayers => _excludedLayers;

    // ==================================================
    // Unity Functions
    // ==================================================

    public override void Create()
    {
        _postProcessPass = new CustomPostProcessPass();

        _postProcessPass.renderPassEvent =
            _renderPassEvent;
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        // ----- Validation -----

        if (_postProcessPass == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // ----- Get Active Settings -----

        CustomPostProcessSettings activeSettings =
            GetActiveSettings();

        if (activeSettings == null)
            return;

        if (!activeSettings.HasActiveEffects())
            return;

        // ----- Setup Render Pass -----

        _postProcessPass.renderPassEvent =
            _renderPassEvent;

        _postProcessPass.Setup(
            activeSettings,
            _isExclusionEnabled,
            _excludedLayers,
            _exclusionMaskMaterial,
            _isExclusionDebugEnabled,
            _exclusionDebugMaterial,
            _exclusionCompositeMaterial
        );

        // ----- Depth Requirement -----

        bool requiresDepth = false;

        foreach (var entry in activeSettings.Effects)
        {
            if (entry == null || !entry.isEnabled)
                continue;

            CustomPostEffect effect = entry.effect;

            if (effect == null || !effect.IsActive())
                continue;

            if (effect.RequiresDepth)
            {
                requiresDepth = true;
                break;
            }
        }

        bool requiresExclusionDepth =
            _isExclusionEnabled &&
            _excludedLayers.value != 0 &&
            _exclusionMaskMaterial != null;

        _postProcessPass.ConfigureInput(
            requiresDepth || requiresExclusionDepth
                ? ScriptableRenderPassInput.Depth
                : ScriptableRenderPassInput.None
        );

        renderer.EnqueuePass(_postProcessPass);
    }

    protected override void Dispose(bool disposing)
    {
        _postProcessPass = null;
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Settings -----

    private CustomPostProcessSettings GetActiveSettings()
    {
        if (Application.isPlaying)
        {
            PostEffectManager manager =
                PostEffectManager.Instance;

            if (manager != null && manager.IsInitialized)
            {
                return manager.RuntimeSettings;
            }
        }

        return _settings;
    }

    // ----- Exclusion Settings -----

    public bool TryGetExclusionSettings(
        out LayerMask excludedLayers)
    {
        excludedLayers = _excludedLayers;

        return _isExclusionEnabled &&
               _excludedLayers.value != 0;
    }
}