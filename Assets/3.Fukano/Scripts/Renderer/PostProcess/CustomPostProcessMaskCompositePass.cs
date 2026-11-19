// ==================================================
// File: CustomPostProcessMaskCompositePass.cs
// Created: 2026/10/29
// Author:
// Summary: Render Graphで除外マスクを使用して画像を合成
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ==================================================
// CustomPostProcessMaskCompositePass
// ==================================================

public static class CustomPostProcessMaskCompositePass
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Shader Properties -----

    private static readonly int OriginalTextureId = Shader.PropertyToID("_OriginalTexture");
    private static readonly int ExclusionMaskId = Shader.PropertyToID("_ExclusionMask");

    // ----- Pass Data -----

    private class PassData
    {
        public TextureHandle effectResult;
        public TextureHandle original;
        public TextureHandle mask;

        public Material material;
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Composite -----

    public static TextureHandle AddCompositePass(
        RenderGraph renderGraph,
        TextureHandle original,
        TextureHandle effectResult,
        TextureHandle mask,
        RenderTextureDescriptor descriptor,
        Material material,
        string passName)
    {
        if (!original.IsValid() || !effectResult.IsValid() || !mask.IsValid() || material == null)
        {
            return effectResult;
        }

        if (material.passCount < 1)
            return effectResult;

        TextureHandle destination =
            UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                descriptor,
                passName + "_Texture",
                false
            );

        using (var builder =
            renderGraph.AddRasterRenderPass<PassData>(
                passName,
                out PassData passData))
        {
            passData.original = original;
            passData.effectResult = effectResult;
            passData.mask = mask;
            passData.material = material;

            // グローバルテクスチャの設定を許可
            builder.AllowGlobalStateModification(true);

            builder.UseTexture(original, AccessFlags.Read);
            builder.UseTexture(effectResult, AccessFlags.Read);
            builder.UseTexture(mask, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

            builder.SetRenderFunc(
                (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(
                        OriginalTextureId,
                        data.original
                    );

                    context.cmd.SetGlobalTexture(
                        ExclusionMaskId,
                        data.mask
                    );

                    Blitter.BlitTexture(
                        context.cmd,
                        data.effectResult,
                        new Vector4(1, 1, 0, 0),
                        data.material,
                        0
                    );
                }
            );
        }
        return destination;
    }
}