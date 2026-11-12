// ==================================================
// File: CustomPostProcessExclusionMaskPass.cs
// Created: 2026/10/29
// Author:
// Summary: Render Graphでポストプロセス除外マスクを生成
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// ==================================================
// CustomPostProcessExclusionMaskPass
// ==================================================

public static class CustomPostProcessExclusionMaskPass
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Shader Tags -----

    private static readonly ShaderTagId[] ShaderTags =
    {
        new ShaderTagId("UniversalForward"),
        new ShaderTagId("UniversalForwardOnly"),
        new ShaderTagId("SRPDefaultUnlit")
    };

    // ----- Pass Data -----

    private class PassData
    {
        public RendererListHandle rendererList;
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Mask Generation -----

    public static TextureHandle CreateMask(
        RenderGraph renderGraph,
        ContextContainer frameData,
        RenderTextureDescriptor descriptor,
        TextureHandle depth,
        LayerMask excludedLayers,
        Material maskMaterial)
    {
        if (maskMaterial == null || excludedLayers.value == 0 || !depth.IsValid())
        {
            return TextureHandle.nullHandle;
        }

        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

        // ----- Create Mask Texture -----

        RenderTextureDescriptor maskDescriptor = descriptor;

        maskDescriptor.colorFormat = RenderTextureFormat.ARGB32;
        maskDescriptor.depthBufferBits = 0;
        maskDescriptor.msaaSamples = 1;

        TextureHandle mask =
            UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                maskDescriptor,
                "MS41_ExclusionMask",
                true
            );

        // ----- Drawing Settings -----

        SortingSettings sortingSettings = new SortingSettings(cameraData.camera) { criteria = SortingCriteria.CommonOpaque };
        DrawingSettings drawingSettings = new DrawingSettings(ShaderTags[0], sortingSettings);

        for (int i = 1; i < ShaderTags.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, ShaderTags[i]);
        }

        drawingSettings.overrideMaterial = maskMaterial;
        drawingSettings.overrideMaterialPassIndex = 0;

        // ----- Filtering Settings -----

        FilteringSettings filteringSettings =
            new FilteringSettings(
                RenderQueueRange.opaque,
                excludedLayers
            );

        // ----- Renderer List -----

        RendererListParams rendererListParams =
            new RendererListParams(
                renderingData.cullResults,
                drawingSettings,
                filteringSettings
            );

        RendererListHandle rendererList =
            renderGraph.CreateRendererList(
                rendererListParams
            );

        // ----- Raster Pass -----

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("MS41 Exclusion Mask", out PassData passData))
        {
            passData.rendererList = rendererList;
            builder.UseRendererList(rendererList);

            builder.SetRenderAttachment(
                mask,
                0,
                AccessFlags.Write
            );

            builder.SetRenderAttachmentDepth(
                depth,
                AccessFlags.Read
            );

            builder.SetRenderFunc(
                (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(
                        data.rendererList
                    );
                }
            );
        }

        return mask;
    }
}