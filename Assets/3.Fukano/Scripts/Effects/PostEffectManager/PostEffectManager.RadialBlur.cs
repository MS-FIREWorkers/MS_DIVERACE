// ==================================================
// File: PostEffectManager.RadialBlur.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: RadialBlurの公開APIと実行時操作
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

    public static void PlayRadialBlur(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float innerRadius = 0.1f,
        float outerRadius = 1.0f,
        int sampleCount = 12,
        float falloff = 0.15f)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartRadialBlurEffectInternal(
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            intensity,
            center,
            innerRadius,
            outerRadius,
            sampleCount,
            falloff
        );
    }

    public static void StopRadialBlur()
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopRadialBlurEffectInternal();
    }


    // ==================================================
    // Radial Blur By ID Public API
    // ==================================================

    // ----- Play Radial Blur By ID -----

    public static void PlayRadialBlur(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float innerRadius = 0.1f,
        float outerRadius = 1.0f,
        int sampleCount = 12,
        float falloff = 0.15f)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartRadialBlurByIdInternal(
            effectId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            intensity,
            center,
            innerRadius,
            outerRadius,
            sampleCount,
            falloff
        );
    }

    // ----- Stop Radial Blur By ID -----

    public static void StopRadialBlur(string effectId)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopRadialBlurByIdInternal(effectId);
    }



    // ==================================================
    // Radial Blur Effect Control
    // ==================================================

    // ----- Start Default Radial Blur -----

    private void StartRadialBlurEffectInternal(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float innerRadius,
        float outerRadius,
        int sampleCount,
        float falloff)
    {
        StartRadialBlurByIdInternal(
            DefaultRadialBlurId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            intensity,
            center,
            innerRadius,
            outerRadius,
            sampleCount,
            falloff
        );
    }

    // ----- Stop Default Radial Blur -----

    private void StopRadialBlurEffectInternal()
    {
        StopRadialBlurByIdInternal(DefaultRadialBlurId);
    }


    // ==================================================
    // Multiple Radial Blur Control
    // ==================================================

    // ----- Start Radial Blur By ID -----

    private void StartRadialBlurByIdInternal(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float intensity,
        Vector2 center,
        float innerRadius,
        float outerRadius,
        int sampleCount,
        float falloff)
    {
        // ----- Get Source Effect -----

        RadialBlurEffect sourceEffect = GetEffectInternal<RadialBlurEffect>();

        if (sourceEffect == null || _runtimeController == null)
            return;

        // ----- Play Runtime Effect -----

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
                effect.SetRadialBlurParameters(
                    center,
                    innerRadius,
                    outerRadius,
                    sampleCount,
                    falloff
                );
            },

            // ----- Apply Intensity -----

            (effect, value) =>
            {
                effect.SetIntensity(value);
            }
        );
    }

    // ----- Stop Radial Blur By ID -----

    private void StopRadialBlurByIdInternal(string effectId)
    {
        _runtimeController?.Stop<RadialBlurEffect>(effectId);
    }


}
