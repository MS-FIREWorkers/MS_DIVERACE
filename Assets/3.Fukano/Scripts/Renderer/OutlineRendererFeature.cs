// ==================================================
// File: OutlineRendererFeature.cs
// Created: 2026/10/14
// Author: 深野大輝
// Summary: URP RenderGraphでToonOutlineパスを描画する。
// ==================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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

    [SerializeField] private Shader _silhouetteCompositeShader;
    [SerializeField] private Shader _outlineTargetMaskShader;
    [SerializeField] private Color _silhouetteColor = Color.black;

    private Material _silhouetteComposite;
    private readonly Material[] _outlineTargetMasks = new Material[4];
    private OutlineRenderPass _outlinePass;

    // ==================================================
    // ----- Unity / URP Events -----
    // ==================================================

    public override void Create()
    {
        CoreUtils.Destroy(_silhouetteComposite);
        _silhouetteComposite = _silhouetteCompositeShader != null
            ? CoreUtils.CreateEngineMaterial(_silhouetteCompositeShader)
            : null;
        for (int i = 0; i < _outlineTargetMasks.Length; i++)
        {
            CoreUtils.Destroy(_outlineTargetMasks[i]);
            _outlineTargetMasks[i] = null;
            if (_outlineTargetMaskShader == null)
                continue;

            Vector4 coverage = Vector4.zero;
            coverage[i] = 1.0f;
            Material mask = CoreUtils.CreateEngineMaterial(_outlineTargetMaskShader);
            mask.SetVector("_WidthCoverage", coverage);
            _outlineTargetMasks[i] = mask;
        }
        _outlinePass = new OutlineRenderPass
        {
            // 輪郭線は深度を書かないので、Skybox描画後に合成する。
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_outlinePass == null)
        {
            Debug.LogError("[Error] OutlineRenderPassが初期化されていません。");
            return;
        }

        Material composite = _silhouetteComposite;
        if (composite != null)
            composite.SetColor("_SilhouetteColor", _silhouetteColor);

        _outlinePass.Setup(_outlineLayer, composite, _outlineTargetMasks);
        renderer.EnqueuePass(_outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_silhouetteComposite);
        _silhouetteComposite = null;
        for (int i = 0; i < _outlineTargetMasks.Length; i++)
        {
            CoreUtils.Destroy(_outlineTargetMasks[i]);
            _outlineTargetMasks[i] = null;
        }
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
        private readonly ShaderTagId _silhouetteShaderTag = new("ToonSilhouetteMask");
        private Material _composite;
        private Material[] _targetMasks;
        private static readonly int WideMaskId = Shader.PropertyToID("_OutlineWideMask");
        private static readonly int HasWideMaskId = Shader.PropertyToID("_OutlineHasWideMask");
        private readonly List<Renderer> _targetRenderers = new();
        private readonly List<Material> _sourceMaterials = new();

        private class TargetMaskPassData
        {
            public readonly List<(Renderer renderer, Material mask, int submesh)> draws = new();
        }

        private class PassData
        {
            public RendererListHandle rendererList;
        }

        private class CompositePassData
        {
            public TextureHandle mask;
            public TextureHandle wideMask;
            public Material material;
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

            RecordGeometry(renderGraph, "Toon Outline Pass", resourceData.activeColorTexture,
                resourceData.activeDepthTexture, rendererListParams);

            if (_composite == null)
                return;

            drawingSettings.SetShaderPassName(0, _silhouetteShaderTag);
            rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            RecordSilhouette(renderGraph, resourceData, rendererListParams, cameraData.camera);
        }

        private static void RecordGeometry(RenderGraph renderGraph, string passName, TextureHandle color,
            TextureHandle depth, RendererListParams rendererListParams)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);
            passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(color, 0);
            builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                context.cmd.DrawRendererList(data.rendererList));
        }

        private void RecordSilhouette(RenderGraph renderGraph, UniversalResourceData resources,
            RendererListParams rendererListParams, Camera camera)
        {
            TextureDesc maskDescriptor = renderGraph.GetTextureDesc(resources.activeColorTexture);
            maskDescriptor.name = "Toon Silhouette Mask";
            maskDescriptor.format = GraphicsFormat.R8G8B8A8_UNorm;
            maskDescriptor.clearBuffer = true;
            maskDescriptor.clearColor = Color.clear;
            maskDescriptor.bindTextureMS = false;
            TextureHandle mask = renderGraph.CreateTexture(maskDescriptor);

            RecordGeometry(renderGraph, "Draw Toon Silhouette Mask", mask,
                resources.activeDepthTexture, rendererListParams);
            RecordOutlineTargets(renderGraph, mask, resources.activeDepthTexture, camera, false);

            TextureHandle wideMask = default;
            if (_targetMasks[0] != null && OutlineTarget.HasWideTargets())
            {
                maskDescriptor.name = "Wide Outline Target Mask";
                wideMask = renderGraph.CreateTexture(maskDescriptor);
                RecordOutlineTargets(renderGraph, wideMask, resources.activeDepthTexture, camera, true);
            }

            using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                "Composite Toon Silhouette", out var passData);
            passData.mask = mask;
            passData.wideMask = wideMask;
            passData.material = _composite;
            builder.UseTexture(mask, AccessFlags.Read);
            if (wideMask.IsValid())
                builder.UseTexture(wideMask, AccessFlags.Read);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
            builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
            {
                bool hasWideMask = data.wideMask.IsValid();
                context.cmd.SetGlobalFloat(HasWideMaskId, hasWideMask ? 1.0f : 0.0f);
                if (hasWideMask)
                    context.cmd.SetGlobalTexture(WideMaskId, data.wideMask);
                Blitter.BlitTexture(context.cmd, data.mask, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        private void RecordOutlineTargets(RenderGraph renderGraph, TextureHandle mask, TextureHandle depth,
            Camera camera, bool wide)
        {
            if (_targetMasks[0] == null || !OutlineTarget.HasActiveTargets)
                return;

            using var builder = renderGraph.AddRasterRenderPass<TargetMaskPassData>(
                "Draw Outline Targets", out var passData);
            passData.draws.Clear();
            foreach (OutlineTarget target in OutlineTarget.RegisteredTargets)
            {
                if (target == null || !target.EnableOutline || (target.WidthPixels > 4) != wide)
                    continue;

                Transform targetRoot = target.TargetRoot;
                if (targetRoot == null)
                    continue;

                Material maskMaterial = _targetMasks[(target.WidthPixels - 1) % _targetMasks.Length];
                targetRoot.GetComponentsInChildren(false, _targetRenderers);
                foreach (Renderer renderer in _targetRenderers)
                {
                    if (!renderer.enabled || renderer.forceRenderingOff ||
                        renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                        (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0)
                        continue;

#if UNITY_EDITOR
                    if (camera.cameraType == CameraType.SceneView &&
                        UnityEditor.SceneVisibilityManager.instance.IsHidden(renderer.gameObject))
                        continue;
#endif

                    Mesh mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter)
                            ? filter.sharedMesh
                            : null;
                    if (mesh == null)
                        continue;

                    renderer.GetSharedMaterials(_sourceMaterials);
                    int count = Mathf.Min(mesh.subMeshCount, _sourceMaterials.Count);
                    for (int submesh = 0; submesh < count; submesh++)
                    {
                        if (_sourceMaterials[submesh] != null)
                            passData.draws.Add((renderer, maskMaterial, submesh));
                    }
                }
            }
            _targetRenderers.Clear();
            _sourceMaterials.Clear();

            builder.SetRenderAttachment(mask, 0, wide ? AccessFlags.Write : AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
            builder.SetRenderFunc((TargetMaskPassData data, RasterGraphContext context) =>
            {
                foreach (var draw in data.draws)
                    context.cmd.DrawRenderer(draw.renderer, draw.mask, draw.submesh, 0);
            });
        }

        // ==================================================
        // ----- Custom Methods -----
        // ==================================================

        public void Setup(LayerMask layer, Material composite, Material[] targetMasks)
        {
            _layerMask = layer;
            _composite = composite;
            _targetMasks = targetMasks;
            requiresIntermediateTexture = composite != null;
        }
    }
}
