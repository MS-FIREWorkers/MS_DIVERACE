
// ==================================================
// File: PostEffectRuntimeController.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: ID付きポストエフェクトの実行時管理
// ==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==================================================
// PostEffectRuntimeController
// ==================================================

public sealed class PostEffectRuntimeController
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Runtime Key -----

    private readonly struct EffectKey : IEquatable<EffectKey>
    {
        public readonly Type EffectType;
        public readonly string EffectId;

        public EffectKey(Type effectType, string effectId)
        {
            EffectType = effectType;
            EffectId = effectId;
        }

        public bool Equals(EffectKey other)
        {
            return EffectType == other.EffectType && EffectId == other.EffectId;
        }

        public override bool Equals(object obj)
        {
            return obj is EffectKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int typeHash = EffectType != null ? EffectType.GetHashCode() : 0;
                int idHash = EffectId != null ? EffectId.GetHashCode() : 0;

                return (typeHash * 397) ^ idHash;
            }
        }
    }

    // ----- Runtime Record -----

    private sealed class EffectRecord
    {
        public CustomPostEffect Effect;
        public Coroutine Coroutine;
    }

    // ----- Dependencies -----

    private readonly MonoBehaviour _coroutineOwner;
    private readonly CustomPostProcessSettings _settings;

    // ----- Runtime Data -----

    private readonly Dictionary<EffectKey, EffectRecord> _activeEffects = new();
    private bool _isDisposed;

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Constructor -----

    public PostEffectRuntimeController(MonoBehaviour coroutineOwner, CustomPostProcessSettings settings)
    {
        _coroutineOwner = coroutineOwner;
        _settings = settings;
    }

    // ----- Play Effect -----

    public bool Play<T>(
        string effectId,
        T sourceEffect,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float maximumIntensity,
        Action<T> setupParameters,
        Action<T, float> applyIntensity)
        where T : CustomPostEffect
    {
        if (!ValidateOperation(effectId))
            return false;

        if (sourceEffect == null || applyIntensity == null)
        {
            Debug.LogError(
                "[Error] PostEffectRuntimeController: " +
                "Source Effect or intensity callback is null.");
            return false;
        }

        EffectKey key = new EffectKey(typeof(T), effectId);

        // ----- Replace Existing ID -----

        StopInternal(key);

        // ----- Create Runtime Instance -----

        T effect = UnityEngine.Object.Instantiate(sourceEffect);

        effect.name = $"{typeof(T).Name}_{effectId}_Runtime";
        effect.hideFlags = HideFlags.DontSave;

        // ----- Setup -----

        try
        {
            setupParameters?.Invoke(effect);
            applyIntensity(effect, 0.0f);
            effect.SetEnabled(true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            UnityEngine.Object.Destroy(effect);
            return false;
        }

        // ----- Register -----

        if (!_settings.AddRuntimeEffect(effect))
        {
            UnityEngine.Object.Destroy(effect);
            return false;
        }

        EffectRecord record = new EffectRecord
        {
            Effect = effect
        };

        _activeEffects.Add(key, record);

        // ----- Start Timeline -----

        // Coroutineは最初のyieldまで同期実行されるため、
        // ハンドル登録前に完了しないよう初回を遅延させる。
        record.Coroutine = _coroutineOwner.StartCoroutine(
            RunTimeline(
                key,
                record,
                fadeInFrames,
                holdFrames,
                fadeOutFrames,
                maximumIntensity,
                value => applyIntensity(effect, value)
            )
        );

        return true;
    }

    // ----- Stop Effect -----

    public bool Stop<T>(string effectId) where T : CustomPostEffect
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(effectId))
            return false;

        EffectKey key = new EffectKey(typeof(T), effectId);

        return StopInternal(key);
    }

    // ----- Stop All Of Type -----

    public int StopAll<T>() where T : CustomPostEffect
    {
        if (_isDisposed)
            return 0;

        List<EffectKey> keys = new();

        foreach (EffectKey key in _activeEffects.Keys)
        {
            if (key.EffectType == typeof(T))
                keys.Add(key);
        }

        foreach (EffectKey key in keys)
        {
            StopInternal(key);
        }

        return keys.Count;
    }

    // ----- Stop All Effects -----

    public void StopAll()
    {
        if (_isDisposed)
            return;

        List<EffectKey> keys = new List<EffectKey>(_activeEffects.Keys);

        foreach (EffectKey key in keys)
        {
            StopInternal(key);
        }
    }

    // ----- Dispose -----

    public void Dispose()
    {
        if (_isDisposed)
            return;

        StopAll();
        _isDisposed = true;
    }

    // ----- Run Timeline -----

    private IEnumerator RunTimeline(
        EffectKey key,
        EffectRecord record,
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float maximumIntensity,
        Action<float> applyIntensity)
    {
        // StartCoroutineの同期実行区間を抜ける。
        yield return null;

        yield return PostEffectTimeline.Play(
            fadeInFrames,
            holdFrames,
            fadeOutFrames,
            maximumIntensity,
            applyIntensity
        );

        Cleanup(key, record);
    }

    // ----- Stop Internal -----

    private bool StopInternal(EffectKey key)
    {
        if (!_activeEffects.TryGetValue(key, out EffectRecord record))
        {
            return false;
        }

        if (record.Coroutine != null)
        {
            _coroutineOwner.StopCoroutine(record.Coroutine);
            record.Coroutine = null;
        }

        Cleanup(key, record);

        return true;
    }

    // ----- Cleanup -----

    private void Cleanup(EffectKey key, EffectRecord record)
    {
        if (!_activeEffects.TryGetValue(key, out EffectRecord current))
        {
            return;
        }

        if (!ReferenceEquals(current, record))
            return;

        _activeEffects.Remove(key);

        CustomPostEffect effect = record.Effect;

        if (effect == null)
            return;

        // ----- Disable -----

        effect.SetEnabled(false);

        // ----- Unregister -----

        if (_settings != null)
        {
            _settings.RemoveRuntimeEffect(effect);
        }

        // ----- Destroy -----

        UnityEngine.Object.Destroy(effect);

        record.Effect = null;
        record.Coroutine = null;
    }

    // ----- Validation -----

    private bool ValidateOperation(string effectId)
    {
        if (_isDisposed || _coroutineOwner == null || _settings == null)
        {
            Debug.LogError(
                "[Error] PostEffectRuntimeController: " +
                "Controller is not available.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(effectId))
        {
            Debug.LogError(
                "[Error] PostEffectRuntimeController: " +
                "Effect ID is empty.");
            return false;
        }

        return true;
    }


    // ==================================================
    // Stop Effects By ID
    // ==================================================

    // ----- Stop Effects By ID -----

    public int StopById(string effectId)
    {
        if (_isDisposed)
            return 0;

        if (string.IsNullOrWhiteSpace(effectId))
        {
            Debug.LogWarning(
                "[Warning] PostEffectRuntimeController: Effect ID is empty."
            );

            return 0;
        }

        // ----- Find Matching Effects -----

        List<EffectKey> targetKeys = new();

        foreach (EffectKey key in _activeEffects.Keys)
        {
            if (key.EffectId == effectId)
            {
                targetKeys.Add(key);
            }
        }

        // ----- Stop Matching Effects -----

        int stoppedCount = 0;

        foreach (EffectKey key in targetKeys)
        {
            if (StopInternal(key))
            {
                stoppedCount++;
            }
        }

        return stoppedCount;
    }


    // ==================================================
    // Effect State
    // ==================================================

    // ----- Is Effect Playing -----

    public bool IsPlaying<T>(string effectId) where T : CustomPostEffect
    {
        if (_isDisposed || string.IsNullOrWhiteSpace(effectId))
            return false;

        EffectKey key = new EffectKey(typeof(T), effectId);
        return _activeEffects.ContainsKey(key);
    }

}
