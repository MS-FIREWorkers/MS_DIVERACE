
// ==================================================
// File: CustomPostProcessPass.cs
// Created: 2026/10/21
// Author: MS41
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

    // Render Graphに渡す描画情報
    private class PassData
    {
        public TextureHandle source;
        public Material material;
        public int materialPass;
    }

    // ==================================================
    // Unity Functions
    // ==================================================

    // Render Graphへ描画処理を登録
    public override void RecordRenderGraph(
        RenderGraph renderGraph,
        ContextContainer frameData)
    {
        if (_settings == null)
            return;

        if (!_settings.HasActiveEffects())
            return;

        UniversalResourceData resourceData =
            frameData.Get<UniversalResourceData>();

        UniversalCameraData cameraData =
            frameData.Get<UniversalCameraData>();

        // バックバッファを直接読み取らない
        if (resourceData.isActiveTargetBackBuffer)
            return;

        TextureHandle source = resourceData.activeColorTexture;

        if (!source.IsValid())
            return;

        // 中間テクスチャの基本設定
        RenderTextureDescriptor descriptor =
            cameraData.cameraTargetDescriptor;

        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;

        int effectIndex = 0;

        // 設定リストの順序で実行
        foreach (var entry in _settings.Effects)
        {
            if (entry == null || !entry.isEnabled)
                continue;

            CustomPostEffect effect = entry.effect;

            if (effect == null || !effect.IsActive())
                continue;

            Material material = effect.EffectMaterial;

            if (material == null)
                continue;

            int materialPass = effect.GetMaterialPass();

            if (materialPass < 0 ||
                materialPass >= material.passCount)
                continue;

            // Shaderパラメータ更新
            effect.SetupMaterial();

            // 描画先テクスチャを生成
            TextureHandle destination =
                UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    descriptor,
                    $"CustomPostEffect_{effectIndex}",
                    false
                );

            // Raster Passを登録
            AddEffectPass(
                renderGraph,
                source,
                destination,
                material,
                materialPass,
                effect.name
            );

            // 次のエフェクトの入力にする
            source = destination;

            effectIndex++;
        }

        // 処理結果を後続のURP Passへ渡す
        if (effectIndex > 0)
        {
            resourceData.cameraColor = source;
        }
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // 使用する設定を登録
    public void Setup(CustomPostProcessSettings settings)
    {
        _settings = settings;
    }

    // エフェクト描画Passを登録
    private void AddEffectPass(
        RenderGraph renderGraph,
        TextureHandle source,
        TextureHandle destination,
        Material material,
        int materialPass,
        string effectName)
    {
        using (var builder =
            renderGraph.AddRasterRenderPass<PassData>(
                $"Custom Post Effect {effectName}",
                out var passData))
        {
            // 描画情報を登録
            passData.source = source;
            passData.material = material;
            passData.materialPass = materialPass;

            // 入力テクスチャ
            builder.UseTexture(
                source,
                AccessFlags.Read
            );

            // 出力テクスチャ
            builder.SetRenderAttachment(
                destination,
                0,
                AccessFlags.Write
            );

            // 実際の描画処理
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
    }
}
