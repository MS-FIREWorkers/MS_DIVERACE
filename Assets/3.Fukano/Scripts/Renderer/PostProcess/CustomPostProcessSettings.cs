
// ==================================================
// File: CustomPostProcessSettings.cs
// Created: 2026/10/21
// Author: Fukano
// Summary: ポストエフェクト一覧と実行順序の管理
//          Runtime Effectの追加・削除・並び替えに対応
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

    // ----- Effect Entry -----

    [System.Serializable]
    public class EffectEntry
    {
        [Header("Effect Settings")]
        [Tooltip("このエフェクトを実行するか")]
        public bool isEnabled = true;

        [Tooltip("実行するポストエフェクト")]
        public CustomPostEffect effect;
    }

    // ----- Post Process Settings -----

    [Header("Post Process Settings")]
    [Tooltip("上から順番に実行するエフェクト一覧")]
    [SerializeField]
    private List<EffectEntry> _effects = new();

    // ----- Public Properties -----

    public IReadOnlyList<EffectEntry> Effects => _effects;
    public int EffectCount => _effects.Count;

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Effect State -----

    public bool HasActiveEffects()
    {
        foreach (EffectEntry entry in _effects)
        {
            if (entry == null || !entry.isEnabled)
                continue;

            if (entry.effect != null && entry.effect.IsActive())
            {
                return true;
            }
        }

        return false;
    }

    // ----- Add Effect -----

    public bool AddRuntimeEffect(CustomPostEffect effect, bool isEnabled = true)
    {
        if (!ValidateRuntimeOperation())
            return false;

        if (effect == null)
        {
            Debug.LogError(
                "[Error] CustomPostProcessSettings: " +
                "Cannot add a null effect.");
            return false;
        }

        if (ContainsEffect(effect))
        {
            Debug.LogWarning(
                "[Warning] CustomPostProcessSettings: " +
                "Effect is already registered.");
            return false;
        }

        EffectEntry entry = new EffectEntry
        {
            effect = effect,
            isEnabled = isEnabled
        };

        _effects.Add(entry);

        return true;
    }

    // ----- Remove Effect -----

    public bool RemoveRuntimeEffect(CustomPostEffect effect)
    {
        if (!ValidateRuntimeOperation())
            return false;

        if (effect == null)
            return false;

        for (int i = 0; i < _effects.Count; i++)
        {
            EffectEntry entry = _effects[i];

            if (entry == null || entry.effect != effect)
                continue;

            _effects.RemoveAt(i);

            return true;
        }

        return false;
    }

    // ----- Move Effect -----

    public bool MoveRuntimeEffect(CustomPostEffect effect, int newIndex)
    {
        if (!ValidateRuntimeOperation())
            return false;

        if (effect == null)
            return false;

        int currentIndex = -1;

        for (int i = 0; i < _effects.Count; i++)
        {
            EffectEntry entry = _effects[i];

            if (entry != null && entry.effect == effect)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex < 0)
            return false;

        newIndex = Mathf.Clamp(newIndex, 0, _effects.Count - 1);

        if (currentIndex == newIndex)
            return true;

        EffectEntry targetEntry = _effects[currentIndex];

        _effects.RemoveAt(currentIndex);
        _effects.Insert(newIndex, targetEntry);

        return true;
    }

    // ----- Contains Effect -----

    public bool ContainsEffect(CustomPostEffect effect)
    {
        if (effect == null)
            return false;

        foreach (EffectEntry entry in _effects)
        {
            if (entry != null && entry.effect == effect)
                return true;
        }

        return false;
    }

    // ----- Runtime Validation -----

    private bool ValidateRuntimeOperation()
    {
        if (Application.isPlaying)
            return true;

        Debug.LogWarning(
            "[Warning] CustomPostProcessSettings: " +
            "Runtime operations are only allowed in Play Mode.");

        return false;
    }
}
