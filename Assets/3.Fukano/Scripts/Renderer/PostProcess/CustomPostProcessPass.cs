// ==================================================
// File: CustomPostProcessPass.cs
// Created: 2026/10/22
// Author: Fukano
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

    private CustomPostProcessSettings _settings;

    // ==================================================
    // Unity Functions
    // ==================================================

    public override void RecordRenderGraph(
        RenderGraph renderGraph,
        ContextContainer frameData)
    {
        if (_settings == null || !_settings.HasActiveEffects())
            return;

        UniversalResourceData resourceData =
            frameData.Get<UniversalResourceData>();

        UniversalCameraData cameraData =
            frameData.Get<UniversalCameraData>();

        if (resourceData.isActiveTargetBackBuffer)
            return;

        TextureHandle source =
            resourceData.activeColorTexture;

        TextureHandle depth =
            resourceData.cameraDepthTexture;

        if (!source.IsValid())
            return;

        RenderTextureDescriptor descriptor =
            cameraData.cameraTargetDescriptor;

        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;

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
                Debug.LogWarning(
                    $"[Warning] {effect.name}: " +
                    "Camera Depth Texture is invalid.");
                continue;
            }

            source = effect.RecordEffect(
                renderGraph,
                source,
                depth,
                descriptor,
                effectIndex
            );

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

    public void Setup(CustomPostProcessSettings settings)
    {
        _settings = settings;
    }
}
