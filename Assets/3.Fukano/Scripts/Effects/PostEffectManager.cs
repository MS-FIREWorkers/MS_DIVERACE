// ==================================================
// File: PostEffectManager.cs
// Created: 2026/11/11
// Author: Fukano
// Summary: Singletonと実行時ポストエフェクト設定の管理
// ==================================================

using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================
// PostEffectManager
// ==================================================

[DisallowMultipleComponent]
public partial class PostEffectManager : MonoBehaviour
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
    private PostEffectRuntimeController _runtimeController;

    private readonly List<CustomPostEffect> _runtimeEffects = new();
    private readonly Dictionary<Type, List<CustomPostEffect>> _effectLookup = new();

    // ----- Default Effect IDs -----

    private const string DefaultOverlayId = "__MS41_DefaultOverlay";
    private const string DefaultFogId = "__MS41_DefaultFog";
    private const string DefaultGaussianBlurId = "__MS41_DefaultGaussianBlur";
    private const string DefaultLensDistortionId = "__MS41_DefaultLensDistortion";
    private const string DefaultRadialBlurId = "__MS41_DefaultRadialBlur";

    // ----- Public Properties -----

    public CustomPostProcessSettings RuntimeSettings => _runtimeSettings;
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

        // ----- Stop Runtime Effects -----

        _runtimeController?.Dispose();
        _runtimeController = null;

        // ----- Release Runtime -----

        ReleaseRuntime();

        Instance = null;
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    private void Initialize()
    {
        if (_sourceSettings == null)
        {
            Debug.LogError("[Error] PostEffectManager: Source Settings is not assigned.");
            return;
        }

        _runtimeSettings = Instantiate(_sourceSettings);
        _runtimeSettings.name = _sourceSettings.name + "_Runtime";
        _runtimeSettings.hideFlags = HideFlags.DontSave;

        foreach (var entry in _runtimeSettings.Effects)
        {
            if (entry == null || entry.effect == null)
                continue;

            CustomPostEffect runtimeEffect = Instantiate(entry.effect);
            runtimeEffect.name = entry.effect.name + "_Runtime";
            runtimeEffect.hideFlags = HideFlags.DontSave;

            entry.effect = runtimeEffect;

            _runtimeEffects.Add(runtimeEffect);

            Type effectType = runtimeEffect.GetType();

            if (!_effectLookup.TryGetValue(effectType, out List<CustomPostEffect> effects))
            {
                effects = new List<CustomPostEffect>();
                _effectLookup.Add(effectType, effects);
            }

            effects.Add(runtimeEffect);

            // ----- Disable Migrated Effects -----

            if (runtimeEffect is FogEffect ||
                runtimeEffect is OverlayEffect ||
                runtimeEffect is GaussianBlurEffect ||
                runtimeEffect is LensDistortionEffect ||
                runtimeEffect is RadialBlurEffect)
            {
                runtimeEffect.SetEnabled(false);
            }
        }

        // ----- Initialize Runtime Controller -----

        _runtimeController = new PostEffectRuntimeController(
            this,
            _runtimeSettings
        );

        Debug.Log("[Info] PostEffectManager: Initialized.");
    }

    private static bool TryGetManager(out PostEffectManager manager)
    {
        manager = Instance;

        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning("[Warning] PostEffectManager: Manager is not initialized.");
            return false;
        }

        return true;
    }

    private T GetEffectInternal<T>() where T : CustomPostEffect
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

    private void ReleaseRuntime()
    {
        // ----- Clear Effect Lookup -----

        _effectLookup.Clear();

        // ----- Destroy Source Runtime Effects -----

        foreach (CustomPostEffect effect in _runtimeEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        _runtimeEffects.Clear();

        // ----- Destroy Runtime Settings -----

        if (_runtimeSettings != null)
        {
            Destroy(_runtimeSettings);
            _runtimeSettings = null;
        }
    }

}
