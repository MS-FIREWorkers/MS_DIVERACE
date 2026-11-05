
/*
    PlayerMaskRendererFeature.cs
    20261015  作成者：要記入
    プレイヤーの輪郭マスクを生成し、
    輪郭周辺の背景にのみ速度ブラーを適用する。
*/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class PlayerMaskRendererFeature : ScriptableRendererFeature
{
    // ==================================================
    // ----- Private Fields -----
    // ==================================================

    [Header("プレイヤーマスク")]
    [Tooltip("輪郭マスクを生成する対象のレイヤー。")]
    [SerializeField] private LayerMask _playerLayer;

    [Tooltip("プレイヤーを白色で描画するマスク専用マテリアル。")]
    [SerializeField] private Material _maskMaterial;

    [Header("速度ブラー")]
    [Tooltip("マスクを参照して背景をぼかすマテリアル。")]
    [SerializeField] private Material _blurMaterial;

    private PlayerMaskPass _maskPass;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    public override void Create()
    {
        _maskPass = new PlayerMaskPass();

        // 不透明オブジェクトの描画後、透明パーティクルの描画前に歪みを適用する。
        _maskPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_maskPass == null || _maskMaterial == null || _blurMaterial == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        _maskPass.Setup(_playerLayer, _maskMaterial, _blurMaterial);
        renderer.EnqueuePass(_maskPass);
    }

    // ==================================================
    // ----- Player Mask Pass -----
    // ==================================================

    private sealed class PlayerMaskPass : ScriptableRenderPass
    {
        private static readonly int MaskTextureId = Shader.PropertyToID("_PlayerScreenMask");

        private LayerMask _playerLayer;
        private Material _maskMaterial;
        private Material _blurMaterial;

        private class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        private class BlurPassData
        {
            public TextureHandle source;
            public Material material;
        }

        public PlayerMaskPass()
        {
            // カメラのカラーと深度情報を要求する。
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public void Setup(LayerMask playerLayer, Material maskMaterial, Material blurMaterial)
        {
            _playerLayer = playerLayer;
            _maskMaterial = maskMaterial;
            _blurMaterial = blurMaterial;
        }

        // ==================================================
        // ----- Render Graph -----
        // ==================================================

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;

            // プレイヤーのマスクを保持する一時テクスチャを生成する。
            var maskDesc = renderGraph.GetTextureDesc(source);
            maskDesc.name = "PlayerScreenMask";
            maskDesc.depthBufferBits = 0;
            maskDesc.clearBuffer = true;
            maskDesc.clearColor = Color.black;

            TextureHandle maskTexture = renderGraph.CreateTexture(maskDesc);

            // プレイヤーの描画対象だけを抽出する。
            UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalLightData lightData = frameContext.Get<UniversalLightData>();

            FilteringSettings filterSettings = new FilteringSettings(
                RenderQueueRange.opaque, _playerLayer.value
            );

            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                new ShaderTagId("UniversalForward"),
                renderingData, cameraData, lightData,
                cameraData.defaultOpaqueSortFlags
            );

            drawSettings.SetShaderPassName(1, new ShaderTagId("UniversalForwardOnly"));
            drawSettings.SetShaderPassName(2, new ShaderTagId("SRPDefaultUnlit"));
            drawSettings.SetShaderPassName(3, new ShaderTagId("UniversalGBuffer"));

            drawSettings.overrideMaterial = _maskMaterial;
            drawSettings.overrideMaterialPassIndex = 0;

            RendererListParams rendererListParams = new RendererListParams(
                renderingData.cullResults, drawSettings, filterSettings
            );

            RendererListHandle rendererList = renderGraph.CreateRendererList(rendererListParams);

            // ==================================================
            // ----- Mask Generation -----
            // ==================================================

            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                "Generate Player Screen Mask", out var passData))
            {
                passData.rendererList = rendererList;

                builder.UseRendererList(rendererList);
                builder.SetRenderAttachment(maskTexture, 0, AccessFlags.Write);

                // 元の深度バッファで遮蔽を判定し、深度値は変更しない。
                builder.SetRenderAttachmentDepth(
                    resourceData.activeDepthTexture, AccessFlags.Read
                );

                // 次のブラー処理で使うテクスチャとして公開する。
                builder.SetGlobalTextureAfterPass(maskTexture, MaskTextureId);

                builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            // ==================================================
            // ----- Masked Blur -----
            // ==================================================

            var outputDesc = renderGraph.GetTextureDesc(source);
            outputDesc.name = "PlayerMaskedSpeedBlur";
            outputDesc.depthBufferBits = 0;
            outputDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(outputDesc);

            using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(
                "Player Masked Speed Blur", out var passData))
            {
                passData.source = source;
                passData.material = _blurMaterial;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseGlobalTexture(MaskTextureId, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (BlurPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(
                        context.cmd,
                        data.source,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0
                    );
                });
            }

            // ブラー処理後の画像をカメラ出力に設定する。
            resourceData.cameraColor = destination;
        }
    }
}
