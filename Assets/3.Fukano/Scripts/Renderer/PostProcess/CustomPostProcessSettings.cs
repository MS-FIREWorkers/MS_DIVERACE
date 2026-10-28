// ==================================================
// File: CustomPostProcessSettings.cs
// Created: 2026/10/21
// Author: Fukano
// Summary: ポストエフェクト一覧と実行順序の管理
// ==================================================

using System.Collections.Generic;
using UnityEngine;

// ==================================================
// CustomPostProcessSettings
// ==================================================

[CreateAssetMenu(
    fileName = "CustomPostProcessSettings",
    menuName = "MS41/Rendering/Post Process Settings")]
public class CustomPostProcessSettings : ScriptableObject
{
    // ==================================================
    // Variables
    // ==================================================

    [System.Serializable]
    public class EffectEntry
    {
        [Header("Effect Settings")]
        [Tooltip("このエフェクトを実行するか")]
        public bool isEnabled = true;

        [Tooltip("実行するポストエフェクト")]
        public CustomPostEffect effect;
    }

    [Header("Post Process Settings")]
    [Tooltip("上から順番に実行するエフェクト一覧")]
    [SerializeField]
    private List<EffectEntry> _effects = new();

    public IReadOnlyList<EffectEntry> Effects => _effects;

    // ==================================================
    // Custom Functions
    // ==================================================

    // 実行可能なエフェクトが存在するか
    public bool HasActiveEffects()
    {
        foreach (EffectEntry entry in _effects)
        {
            if (entry == null || !entry.isEnabled)
                continue;

            if (entry.effect != null && entry.effect.IsActive())
                return true;
        }

        return false;
    }
}