// ==================================================
// File: CustomPostProcessBlitter.cs
// Created: 2026/10/22
// Author: Fukano
// Summary: Render Graph用フルスクリーン描画ユーティリティ
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ==================================================
// CustomPostProcessBlitter
// ==================================================

public static class CustomPostProcessBlitter
{
    // ==================================================
    // Variables
    // ==================================================

    private class PassData
    {
        public TextureHandle source;
        public Material material;
        public int materialPass;
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // 通常のColor専用Blit
    public static TextureHandle AddBlitPass(
        RenderGraph renderGraph,
        TextureHandle source,
        RenderTextureDescriptor descriptor,
        Material material,
        int materialPass,
        string passName)
    {
        return AddBlitPassInternal(
            renderGraph,
            source,
            default,
            false,
            descriptor,
            material,
            materialPass,
            passName
        );
    }

    // Depth対応Blit
    public static TextureHandle AddBlitPass(
        RenderGraph renderGraph,
        TextureHandle source,
        TextureHandle depth,
        RenderTextureDescriptor descriptor,
        Material material,
        int materialPass,
        string passName)
    {
        return AddBlitPassInternal(
            renderGraph,
            source,
            depth,
            true,
            descriptor,
            material,
            materialPass,
            passName
        );
    }

    // ==================================================
    // Internal Blit
    // ==================================================

    private static TextureHandle AddBlitPassInternal(
        RenderGraph renderGraph,
        TextureHandle source,
        TextureHandle depth,
        bool requiresDepth,
        RenderTextureDescriptor descriptor,
        Material material,
        int materialPass,
        string passName)
    {
        if (!source.IsValid() || material == null)
            return source;

        if (materialPass < 0 ||
            materialPass >= material.passCount)
            return source;

        if (requiresDepth && !depth.IsValid())
            return source;

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
                out var passData))
        {
            passData.source = source;
            passData.material = material;
            passData.materialPass = materialPass;

            builder.UseTexture(
                source,
                AccessFlags.Read
            );

            if (requiresDepth)
            {
                builder.UseTexture(
                    depth,
                    AccessFlags.Read
                );
            }

            builder.SetRenderAttachment(
                destination,
                0,
                AccessFlags.Write
            );

            builder.SetRenderFunc(
                (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(
                        context.cmd,
                        data.source,
                        new Vector4(1, 1, 0, 0),
                        data.material,
                        data.materialPass
                    );
                }
            );
        }

        return destination;
    }
}
