// ==================================================
// File: PostEffectManager.cs
// Created: 2026/11/04
// Author: Fukano
// Summary: ポストエフェクトの実行時管理とstatic公開API
// ==================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==================================================
// PostEffectManager
// ==================================================

[DisallowMultipleComponent]
public class PostEffectManager : MonoBehaviour
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Singleton -----

    public static PostEffectManager Instance { get; private set; }

    // ----- Settings -----

    [Header("Post Effect Settings")]
    [Tooltip("元となるポストプロセス設定アセット")]
    [SerializeField]
    private CustomPostProcessSettings _sourceSettings;

    [Tooltip("シーン切り替え時もManagerを維持する")]
    [SerializeField]
    private bool _isPersistent = false;

    // ----- Runtime -----

    private CustomPostProcessSettings _runtimeSettings;

    private readonly List<CustomPostEffect> _runtimeEffects = new();

    private readonly Dictionary<Type, List<CustomPostEffect>>
        _effectLookup = new();

    // ----- Effect Coroutines -----

    private readonly Dictionary<Type, Coroutine>
        _effectCoroutines = new();

    // ----- Public Properties -----

    public CustomPostProcessSettings RuntimeSettings =>
        _runtimeSettings;

    public bool IsInitialized => _runtimeSettings != null;

    // ==================================================
    // Unity Functions
    // ==================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[Warning] PostEffectManager: Duplicate manager detected."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_isPersistent)
        {
            DontDestroyOnLoad(gameObject);
        }

        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        StopAllCoroutines();
        _effectCoroutines.Clear();

        ReleaseRuntime();

        Instance = null;
    }

    // ==================================================
    // Static Public API
    // ==================================================

    // ----- Fog -----

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
    // Custom Functions
    // ==================================================

    // ----- Initialize -----

    private void Initialize()
    {
        if (_sourceSettings == null)
        {
            Debug.LogError(
                "[Error] PostEffectManager: Source Settings is not assigned."
            );

            return;
        }

        _runtimeSettings = Instantiate(_sourceSettings);

        _runtimeSettings.name =
            _sourceSettings.name + "_Runtime";

        _runtimeSettings.hideFlags =
            HideFlags.DontSave;

        foreach (var entry in _runtimeSettings.Effects)
        {
            if (entry == null || entry.effect == null)
                continue;

            CustomPostEffect runtimeEffect =
                Instantiate(entry.effect);

            runtimeEffect.name =
                entry.effect.name + "_Runtime";

            runtimeEffect.hideFlags =
                HideFlags.DontSave;

            entry.effect = runtimeEffect;

            _runtimeEffects.Add(runtimeEffect);

            Type effectType = runtimeEffect.GetType();

            if (!_effectLookup.TryGetValue(
                effectType,
                out List<CustomPostEffect> effects))
            {
                effects = new List<CustomPostEffect>();
                _effectLookup.Add(effectType, effects);
            }

            effects.Add(runtimeEffect);
        }

        Debug.Log("[Info] PostEffectManager: Initialized.");
    }

    // ----- Manager Access -----

    private static bool TryGetManager(
        out PostEffectManager manager)
    {
        manager = Instance;

        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning(
                "[Warning] PostEffectManager: Manager is not initialized."
            );

            return false;
        }

        return true;
    }

    // ----- Effect Access -----

    private T GetEffectInternal<T>()
        where T : CustomPostEffect
    {
        if (!IsInitialized)
            return null;

        if (_effectLookup.TryGetValue(
            typeof(T),
            out List<CustomPostEffect> effects))
        {
            if (effects.Count > 0)
                return effects[0] as T;
        }

        Debug.LogWarning(
            $"[Warning] PostEffectManager: Effect {typeof(T).Name} was not found."
        );

        return null;
    }

    // ==================================================
    // Fog Effect Control
    // ==================================================

    // ----- Start -----

    // ==================================================
    // Fog Effect Control
    // ==================================================

    // ----- Start -----

    private void StartFogEffectInternal(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        Color fogColor,
        float startDistance,
        float endDistance,
        float density,
        float intensity,
        FogEffect.FogMode fogMode)
    {
        FogEffect effect = GetEffectInternal<FogEffect>();

        if (effect == null)
            return;

        // 以前のFog演出を停止
        StopEffectCoroutine<FogEffect>();

        // Runtime Entryを有効化
        if (!SetEffectEntryEnabled<FogEffect>(true))
            return;

        // Fogパラメータ設定
        effect.SetFogParameters(
            fogMode,
            fogColor,
            startDistance,
            endDistance,
            density
        );

        // 初期状態
        effect.SetIntensity(0.0f);
        effect.SetEnabled(true);

        // Coroutine開始
        Coroutine coroutine = StartCoroutine(
            PlayEffect(
                fadeInFrames,
                holdFrames,
                fadeOutFrames,
                Mathf.Clamp01(intensity),
                effect.SetIntensity,
                () =>
                {
                    effect.SetIntensity(0.0f);
                    effect.SetEnabled(false);

                    SetEffectEntryEnabled<FogEffect>(false);
                },
                typeof(FogEffect)
            )
        );

        if (coroutine != null &&
            _effectCoroutines.ContainsKey(typeof(FogEffect)))
        {
            _effectCoroutines[typeof(FogEffect)] = coroutine;
        }
    }

    // ----- Stop -----

    private void StopFogEffectInternal()
{
    StopEffectCoroutine<FogEffect>();

    FogEffect effect = GetEffectInternal<FogEffect>();

    if (effect == null)
        return;

    effect.SetIntensity(0.0f);
    effect.SetEnabled(false);

    SetEffectEntryEnabled<FogEffect>(false);
}

// ==================================================
// Effect Entry Control
// ==================================================

// ----- Set Entry Enabled -----

private bool SetEffectEntryEnabled<T>(bool isEnabled)
        where T : CustomPostEffect
    {
        if (_runtimeSettings == null)
            return false;

        foreach (var entry in _runtimeSettings.Effects)
        {
            if (entry == null || entry.effect == null)
                continue;

            if (entry.effect is not T)
                continue;

            entry.isEnabled = isEnabled;
            return true;
        }

        Debug.LogWarning(
            $"[Warning] PostEffectManager: " +
            $"EffectEntry {typeof(T).Name} was not found."
        );

        return false;
    }

    // ==================================================
    // Effect Timeline
    // ==================================================

    // ----- Play -----

    private IEnumerator PlayEffect(
        int fadeInFrames,
        int holdFrames,
        int fadeOutFrames,
        float maximumIntensity,
        Action<float> applyIntensity,
        Action onComplete,
        Type effectType)
    {
        fadeInFrames = Mathf.Max(0, fadeInFrames);
        holdFrames = Mathf.Max(0, holdFrames);
        fadeOutFrames = Mathf.Max(0, fadeOutFrames);

        _effectCoroutines[effectType] = null;

        // ----- Fade In -----

        if (fadeInFrames > 0)
        {
            for (int frame = 1; frame <= fadeInFrames; frame++)
            {
                float t = (float)frame / fadeInFrames;

                applyIntensity(maximumIntensity * t);

                yield return null;
            }
        }

        applyIntensity(maximumIntensity);

        // ----- Hold -----

        for (int frame = 0; frame < holdFrames; frame++)
        {
            yield return null;
        }

        // ----- Fade Out -----

        if (fadeOutFrames > 0)
        {
            for (int frame = 1; frame <= fadeOutFrames; frame++)
            {
                float t = (float)frame / fadeOutFrames;

                applyIntensity(
                    maximumIntensity * (1.0f - t)
                );

                yield return null;
            }
        }

        // ----- Complete -----

        applyIntensity(0.0f);

        onComplete?.Invoke();

        _effectCoroutines.Remove(effectType);
    }

    // ----- Stop Coroutine -----

    private void StopEffectCoroutine<T>()
        where T : CustomPostEffect
    {
        Type effectType = typeof(T);

        if (!_effectCoroutines.TryGetValue(
            effectType,
            out Coroutine coroutine))
        {
            return;
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        _effectCoroutines.Remove(effectType);
    }

    // ----- Cleanup -----

    private void ReleaseRuntime()
    {
        _effectLookup.Clear();

        foreach (CustomPostEffect effect in _runtimeEffects)
        {
            if (effect != null)
                Destroy(effect);
        }

        _runtimeEffects.Clear();

        if (_runtimeSettings != null)
        {
            Destroy(_runtimeSettings);
            _runtimeSettings = null;
        }
    }
}