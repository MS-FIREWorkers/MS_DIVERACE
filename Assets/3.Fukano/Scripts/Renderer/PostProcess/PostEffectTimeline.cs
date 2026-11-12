
// ==================================================
// File: PostEffectTimeline.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: ポストエフェクト共通のフレーム単位Fade制御
// ==================================================

using System;
using System.Collections;
using UnityEngine;

// ==================================================
// PostEffectTimeline
// ==================================================

public static class PostEffectTimeline
{
    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Play Timeline -----

    public static IEnumerator Play(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float maximumIntensity,
        Action<float> applyIntensity)
    {
        if (applyIntensity == null)
        {
            Debug.LogError(
                "[Error] PostEffectTimeline: " +
                "Intensity callback is null.");
            yield break;
        }

        fadeInFrames = Mathf.Max(0, fadeInFrames);
        holdFrames = Mathf.Max(0, holdFrames);
        fadeOutFrames = Mathf.Max(0, fadeOutFrames);

        // ----- Fade In -----

        for (int frame = 1; frame <= fadeInFrames; frame++)
        {
            float t = (float)frame / fadeInFrames;
            applyIntensity(maximumIntensity * t);
            yield return null;
        }

        // ----- Hold -----

        applyIntensity(maximumIntensity);

        for (int frame = 0; frame < holdFrames; frame++)
        {
            yield return null;
        }

        // ----- Fade Out -----

        for (int frame = 1; frame <= fadeOutFrames; frame++)
        {
            float t = (float)frame / fadeOutFrames;
            applyIntensity(maximumIntensity * (1.0f - t));
            yield return null;
        }

        // ----- Complete -----

        applyIntensity(0.0f);
    }
}
