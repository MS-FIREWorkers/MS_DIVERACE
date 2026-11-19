// ==================================================
// File: OutlineRendererFeature.cs
// Created: 2026/10/14
// Author: 深野大輝
// Summary: URP RenderGraphでToonOutlineパスを描画する。
// ==================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    // ==================================================
    // ----- Variables -----
    // ==================================================

    [Header("Outline Settings")]
    [Tooltip("アウトラインを描画するレイヤー")]
    [SerializeField, FormerlySerializedAs("outlineLayer")]
    private LayerMask _outlineLayer = ~0;

    private OutlineRenderPass _outlinePass;

    // ==================================================
    // ----- Unity / URP Events -----
    // ==================================================

    public override void Create()
    {
        _outlinePass = new OutlineRenderPass
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_outlinePass == null)
        {
            Debug.LogError("[Error] OutlineRenderPassが初期化されていません。");
            return;
        }

        _outlinePass.Setup(_outlineLayer);
        renderer.EnqueuePass(_outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        _outlinePass = null;
    }

    // ==================================================
    // ----- Render Pass -----
    // ==================================================

    private class OutlineRenderPass : ScriptableRenderPass
    {
        // ==================================================
        // ----- Variables -----
        // ==================================================

        private LayerMask _layerMask;
        private readonly ShaderTagId _outlineShaderTag = new("ToonOutline");

        private class PassData
        {
            public RendererListHandle rendererList;
        }

        // ==================================================
        // ----- Unity / URP Events -----
        // ==================================================

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            FilteringSettings filteringSettings = new(RenderQueueRange.opaque, _layerMask);
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                _outlineShaderTag,
                renderingData,
                cameraData,
                lightData,
                cameraData.defaultOpaqueSortFlags
            );

            RendererListParams rendererListParams = new(
                renderingData.cullResults,
                drawingSettings,
                filteringSettings
            );

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Toon Outline Pass", out var passData))
            {
                passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }

        // ==================================================
        // ----- Custom Methods -----
        // ==================================================

        public void Setup(LayerMask layer)
        {
            _layerMask = layer;
        }
    }
}
