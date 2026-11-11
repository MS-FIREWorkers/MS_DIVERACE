// ==================================================
// File: PostEffectManager.Fog.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: Fogの公開APIと実行時操作
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

    public static void PlayFog(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color fogColor,
        float startDistance,
        float endDistance,
        float density,
        float intensity,
        FogEffect.FogMode fogMode = FogEffect.FogMode.Linear)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartFogEffectInternal(
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            fogColor,
            startDistance,
            endDistance,
            density,
            intensity,
            fogMode
        );
    }

    public static void StopFog()
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopFogEffectInternal();
    }


    // ==================================================
    // Fog By ID Public API
    // ==================================================

    // ----- Play Fog By ID -----

    public static void PlayFog(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color fogColor,
        float startDistance,
        float endDistance,
        float density,
        float intensity,
        FogEffect.FogMode fogMode = FogEffect.FogMode.Linear)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StartFogByIdInternal(
            effectId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            fogColor,
            startDistance,
            endDistance,
            density,
            intensity,
            fogMode
        );
    }

    // ----- Stop Fog By ID -----

    public static void StopFog(string effectId)
    {
        if (!TryGetManager(out PostEffectManager manager))
            return;

        manager.StopFogByIdInternal(effectId);
    }


    // ==================================================
    // Fog Effect Control
    // ==================================================

    // ----- Start Default Fog -----

    private void StartFogEffectInternal(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color fogColor,
        float fogStart,
        float fogEnd,
        float fogDensity,
        float intensity,
        FogEffect.FogMode fogMode)
    {
        StartFogByIdInternal(
            DefaultFogId,
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            fogColor,
            fogStart,
            fogEnd,
            fogDensity,
            intensity,
            fogMode
        );
    }

    // ----- Stop Default Fog -----

    private void StopFogEffectInternal()
    {
        StopFogByIdInternal(DefaultFogId);
    }


    // ==================================================
    // Multiple Fog Control
    // ==================================================

    // ----- Start Fog By ID -----

    private void StartFogByIdInternal(
        string effectId,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color fogColor,
        float fogStart,
        float fogEnd,
        float fogDensity,
        float intensity,
        FogEffect.FogMode fogMode)
    {
        FogEffect sourceEffect = GetEffectInternal<FogEffect>();

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
                effect.SetFogParameters(
                    fogMode,
                    fogColor,
                    fogStart,
                    fogEnd,
                    fogDensity
                );
            },

            // ----- Apply Intensity -----

            (effect, value) =>
            {
                effect.SetIntensity(value);
            }
        );
    }

    // ----- Stop Fog By ID -----

    private void StopFogByIdInternal(string effectId)
    {
        _runtimeController?.Stop<FogEffect>(effectId);
    }

}
