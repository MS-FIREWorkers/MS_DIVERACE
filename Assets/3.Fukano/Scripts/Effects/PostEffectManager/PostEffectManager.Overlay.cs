// ==================================================
// File: PostEffectManager.Overlay.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: Overlayの公開APIと実行時操作
// ==================================================

using UnityEngine;

// ==================================================
// PostEffectManager
// ==================================================

public partial class PostEffectManager
{
    // ==================================================
    // Static Public API
    // ==================================================

    public static void PlayOverlay(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color overlayColor,
        float intensity,
        OverlayEffect.BlendMode blendMode =
            OverlayEffect.BlendMode.Normal)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartOverlayEffectInternal(
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            overlayColor,
            intensity,
            blendMode
        );
    }

    public static void PlayOverlay(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color overlayColor,
        float intensity,
        OverlayEffect.BlendMode blendMode =
            OverlayEffect.BlendMode.Normal)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartOverlayByIdInternal(
            effectId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            overlayColor,
            intensity,
            blendMode
        );
    }

    public static void StopOverlay()
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopOverlayEffectInternal();
    }

    public static void StopOverlay(string effectId)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopOverlayByIdInternal(effectId);
    }

    private void StopOverlayEffectInternal()
    {
        StopOverlayByIdInternal(DefaultOverlayId);
    }

    private void StopOverlayByIdInternal(string effectId)
    {
        _runtimeController?.Stop<OverlayEffect>(effectId);
    }

    // ==================================================
    // Overlay Effect Control
    // ==================================================

    private void StartOverlayEffectInternal(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color overlayColor,
        float intensity,
        OverlayEffect.BlendMode blendMode)
    {
        StartOverlayByIdInternal(
            DefaultOverlayId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            overlayColor,
            intensity,
            blendMode
        );
    }

    private void StartOverlayByIdInternal(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color overlayColor,
        float intensity,
        OverlayEffect.BlendMode blendMode)
    {
        OverlayEffect sourceEffect =
            GetEffectInternal<OverlayEffect>();

        if (sourceEffect == null || _runtimeController == null)
            return;

        _runtimeController.Play(
            effectId,
            sourceEffect,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            Mathf.Clamp01(intensity),

            // ----- Setup Parameters -----

            effect =>
            {
                effect.SetOverlayParameters(
                    overlayColor,
                    blendMode
                );
            },

            // ----- Apply Intensity -----

            (effect, value) =>
            {
                effect.SetIntensity(value);
            }
        );
    }

}
