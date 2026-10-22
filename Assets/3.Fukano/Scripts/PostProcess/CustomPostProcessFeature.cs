// ==================================================
// File: CustomPostProcessFeature.cs
// Created: 2026/10/21
// Author: MS41
// Summary: ポストプロセス用Renderer Feature
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

    [Header("Post Process Settings")]
    [Tooltip("実行するポストエフェクトの設定アセット")]
    [SerializeField]
    private CustomPostProcessSettings _settings;

    [Header("Rendering Settings")]
    [Tooltip("ポストプロセスの実行タイミング")]
    [SerializeField]
    private RenderPassEvent _renderPassEvent =
        RenderPassEvent.BeforeRenderingPostProcessing;

    private CustomPostProcessPass _postProcessPass;

    // ==================================================
    // Unity Functions
    // ==================================================

    public override void Create()
    {
        _postProcessPass = new CustomPostProcessPass();

        _postProcessPass.renderPassEvent = _renderPassEvent;
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (_settings == null)
            return;

        if (_postProcessPass == null)
            return;

        if (!_settings.HasActiveEffects())
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        _postProcessPass.renderPassEvent = _renderPassEvent;
        _postProcessPass.Setup(_settings);

        renderer.EnqueuePass(_postProcessPass);
    }

    protected override void Dispose(bool disposing)
    {
        _postProcessPass = null;
    }
}