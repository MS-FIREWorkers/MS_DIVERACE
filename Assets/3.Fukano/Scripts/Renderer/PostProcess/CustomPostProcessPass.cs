// ==================================================
// File: CustomPostProcessPass.cs
// Created: 2026/10/22
// Author:
// Summary: Render Graph対応ポストプロセス実行Pass
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ==================================================
// CustomPostProcessPass
// ==================================================

public class CustomPostProcessPass : ScriptableRenderPass
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Post Process Settings -----

    private CustomPostProcessSettings _settings;

    // ----- Exclusion Composite Settings -----

    private Material _exclusionCompositeMaterial;

    // ----- Exclusion Settings -----

    private bool _isExclusionEnabled;
    private LayerMask _excludedLayers;
    private Material _exclusionMaskMaterial;

    // ----- Exclusion Debug Settings -----

    private bool _isExclusionDebugEnabled;
    private Material _exclusionDebugMaterial;

    // ==================================================
    // Unity Functions
    // ==================================================

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_settings == null || !_settings.HasActiveEffects())
            return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (resourceData.isActiveTargetBackBuffer)
            return;

        TextureHandle source = resourceData.activeColorTexture;
        TextureHandle depth = resourceData.cameraDepthTexture;

        if (!source.IsValid())
            return;

        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;

        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;

        // ==================================================
        // Exclusion Mask
        // STEP 2で除外マスク生成処理を追加する
        // ==================================================

        TextureHandle exclusionMask = TextureHandle.nullHandle;
        bool hasExclusion = _isExclusionEnabled && _excludedLayers.value != 0 && _exclusionMaskMaterial != null;

        if (hasExclusion)
        {
            TextureHandle depthAttachment = resourceData.activeDepthTexture;

            if (depthAttachment.IsValid())
            {
                exclusionMask =
                    CustomPostProcessExclusionMaskPass.CreateMask(
                        renderGraph,
                        frameData,
                        descriptor,
                        depthAttachment,
                        _excludedLayers,
                        _exclusionMaskMaterial
                    );
            }
            else
            {
                Debug.LogWarning(
                    "[Warning] CustomPostProcessPass: " +
                    "Depth attachment is invalid.");
            }
        }

        // ==================================================
        // Exclusion Mask Debug
        // ==================================================

        if (_isExclusionDebugEnabled)
        {
            if (exclusionMask.IsValid() &&
                _exclusionDebugMaterial != null)
            {
                TextureHandle debugOutput =
                    CustomPostProcessBlitter.AddBlitPass(
                        renderGraph,
                        exclusionMask,
                        descriptor,
                        _exclusionDebugMaterial,
                        0,
                        "MS41 Exclusion Mask Debug"
                    );

                if (debugOutput.IsValid())
                {
                    resourceData.cameraColor = debugOutput;

                    // デバッグ中は通常のエフェクト処理を停止
                    return;
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Warning] CustomPostProcessPass: " +
                    "Exclusion debug is enabled, " +
                    "but mask or debug material is invalid.");
            }
        }


        int effectIndex = 0;

        foreach (var entry in _settings.Effects)
        {
            if (entry == null || !entry.isEnabled)
                continue;

            CustomPostEffect effect = entry.effect;

            if (effect == null || !effect.IsActive())
                continue;

            if (effect.RequiresDepth && !depth.IsValid())
            {
                Debug.LogWarning($"[Warning] {effect.name}: " + "Camera Depth Texture is invalid.");
                continue;
            }

            // ----- Execute Effect -----

            TextureHandle original = source;

            TextureHandle effectResult = effect.RecordEffect(
                renderGraph,
                source,
                depth,
                descriptor,
                effectIndex
            );

            // ----- Apply Exclusion Mask -----

            bool shouldApplyExclusion =
                exclusionMask.IsValid() &&
                _exclusionCompositeMaterial != null;

            if (shouldApplyExclusion &&
                effectResult.IsValid() &&
                !effectResult.Equals(original))
            {
                source =
                    CustomPostProcessMaskCompositePass.AddCompositePass(
                        renderGraph,
                        original,
                        effectResult,
                        exclusionMask,
                        descriptor,
                        _exclusionCompositeMaterial,
                        $"MS41 Mask Composite {effectIndex}"
                    );
            }
            else
            {
                source = effectResult;
            }

            effectIndex++;
        }

        if (effectIndex > 0)
        {
            resourceData.cameraColor = source;
        }
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Setup -----

    public void Setup(
        CustomPostProcessSettings settings,
        bool isExclusionEnabled,
        LayerMask excludedLayers,
        Material exclusionMaskMaterial,
        bool isExclusionDebugEnabled,
        Material exclusionDebugMaterial,
        Material exclusionCompositeMaterial)
    {
        _settings = settings;

        _isExclusionEnabled = isExclusionEnabled;
        _excludedLayers = excludedLayers;
        _exclusionMaskMaterial = exclusionMaskMaterial;

        _isExclusionDebugEnabled = isExclusionDebugEnabled;
        _exclusionDebugMaterial = exclusionDebugMaterial;

        _exclusionCompositeMaterial = exclusionCompositeMaterial;
    }
}