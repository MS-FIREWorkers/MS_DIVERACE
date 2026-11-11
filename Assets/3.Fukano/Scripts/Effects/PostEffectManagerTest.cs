// ==================================================
// File: PostEffectManagerTest.cs
// Created: 2026/11/04
// Author: Fukano
// Summary: PostEffectManagerの公開APIを使用した動作確認
// ==================================================

using UnityEngine;
using UnityEngine.InputSystem;

// ==================================================
// PostEffectManagerTest
// ==================================================

[DisallowMultipleComponent]
public class PostEffectManagerTest : MonoBehaviour
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Test Settings -----

    [Header("Fog Test Settings")]
    [Tooltip("FogのFade Inフレーム数")]
    [SerializeField, Min(0)]
    private int _fadeInFrames = 30;

    [Tooltip("Fogの最大強度維持フレーム数")]
    [SerializeField, Min(0)]
    private int _holdFrames = 60;

    [Tooltip("FogのFade Outフレーム数")]
    [SerializeField, Min(0)]
    private int _fadeOutFrames = 30;

    [Tooltip("Fogの色")]
    [SerializeField]
    private Color _fogColor = Color.red;

    [Tooltip("Fogの開始距離")]
    [SerializeField, Min(0.0f)]
    private float _startDistance = 5.0f;

    [Tooltip("Fogの終了距離")]
    [SerializeField, Min(0.0f)]
    private float _endDistance = 30.0f;

    [Tooltip("Fogの密度")]
    [SerializeField, Range(0.0f, 0.2f)]
    private float _density = 0.03f;

    [Tooltip("Fogの最大強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _intensity = 0.8f;

    [Tooltip("Fogの計算方式")]
    [SerializeField]
    private FogEffect.FogMode _fogMode =        FogEffect.FogMode.Linear;

    // ==================================================
    // Overlay Test Settings
    // ==================================================

    [Header("Overlay Test Settings")]
    [Tooltip("Fade Inのフレーム数")]
    [SerializeField, Min(0)]
    private int _overlayFadeInFrames = 30;

    [Tooltip("Holdのフレーム数")]
    [SerializeField, Min(0)]
    private int _overlayHoldFrames = 60;

    [Tooltip("Fade Outのフレーム数")]
    [SerializeField, Min(0)]
    private int _overlayFadeOutFrames = 30;

    [Tooltip("Overlayの色")]
    [SerializeField]
    private Color _overlayColor = Color.red;

    [Tooltip("Overlayの最大強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _overlayIntensity = 0.8f;

    [Tooltip("Overlayの合成方法")]
    [SerializeField]
    private OverlayEffect.BlendMode _overlayBlendMode =        OverlayEffect.BlendMode.Normal;

    // ==================================================
    // Gaussian Blur Test Settings
    // ==================================================

    [Header("Gaussian Blur Test Settings")]
    [Tooltip("Fade Inのフレーム数")]
    [SerializeField, Min(0)]
    private int _blurFadeInFrames = 30;

    [Tooltip("Holdのフレーム数")]
    [SerializeField, Min(0)]
    private int _blurHoldFrames = 60;

    [Tooltip("Fade Outのフレーム数")]
    [SerializeField, Min(0)]
    private int _blurFadeOutFrames = 30;

    [Tooltip("ぼかし半径")]
    [SerializeField, Range(0.0f, 10.0f)]
    private float _blurRadius = 4.0f;

    [Tooltip("最大ぼかし強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _blurStrength = 0.8f;

    [Tooltip("ぼかしの反復回数")]
    [SerializeField, Range(1, 8)]
    private int _blurIterations = 2;


    // ==================================================
    // Lens Distortion Test Settings
    // ==================================================

    [Header("Lens Distortion Test Settings")]
    [Tooltip("Fade Inのフレーム数")]
    [SerializeField, Min(0)]
    private int _lensFadeInFrames = 30;

    [Tooltip("Holdのフレーム数")]
    [SerializeField, Min(0)]
    private int _lensHoldFrames = 60;

    [Tooltip("Fade Outのフレーム数")]
    [SerializeField, Min(0)]
    private int _lensFadeOutFrames = 30;

    [Tooltip("歪みの強度")]
    [SerializeField, Range(-1.0f, 1.0f)]
    private float _lensIntensity = 0.5f;

    [Tooltip("歪みの中心座標")]
    [SerializeField]
    private Vector2 _lensCenter = new Vector2(0.5f, 0.5f);

    [Header("Lens Distortion Range Settings")]
    [Tooltip("UVの拡大縮小")]
    [SerializeField, Range(0.1f, 2.0f)]
    private float _lensScale = 1.0f;

    [Tooltip("歪みを適用する範囲")]
    [SerializeField]
    private LensDistortionEffect.RangeMode _lensRangeMode =
        LensDistortionEffect.RangeMode.Full;

    [Tooltip("適用範囲の半径")]
    [SerializeField, Range(0.0f, 2.0f)]
    private float _lensRangeRadius = 0.5f;

    [Tooltip("Ringモードの内側半径")]
    [SerializeField, Range(0.0f, 2.0f)]
    private float _lensInnerRadius = 0.2f;

    [Tooltip("境界を滑らかにする幅")]
    [SerializeField, Range(0.0f, 0.5f)]
    private float _lensEdgeSoftness = 0.05f;

    // ==================================================
    // Radial Blur Test Settings
    // ==================================================

    [Header("Radial Blur Test Settings")]
    [Tooltip("Fade Inのフレーム数")]
    [SerializeField, Min(0)]
    private int _radialFadeInFrames = 30;

    [Tooltip("Holdのフレーム数")]
    [SerializeField, Min(0)]
    private int _radialHoldFrames = 60;

    [Tooltip("Fade Outのフレーム数")]
    [SerializeField, Min(0)]
    private int _radialFadeOutFrames = 30;

    [Tooltip("ラジアルブラーの最大強度")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _radialIntensity = 0.7f;

    [Tooltip("ブラーの中心座標")]
    [SerializeField]
    private Vector2 _radialCenter = new Vector2(0.5f, 0.5f);

    [Tooltip("中心付近のブラーを適用しない半径")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float _radialInnerRadius = 0.2f;

    // ----- Radial Blur Additional Settings -----

    [Tooltip("ブラーの適用終了半径")]
    [SerializeField, Range(0.0f, 2.0f)]
    private float _radialOuterRadius = 1.0f;

    [Tooltip("サンプリング回数")]
    [SerializeField, Range(4, 32)]
    private int _radialSampleCount = 12;

    [Tooltip("境界の滑らかさ")]
    [SerializeField, Range(0.001f, 0.5f)]
    private float _radialFalloff = 0.15f;

    // ==================================================
    // Unity Functions
    // ==================================================

    private void Start()
    {
        Debug.Log(
            "[Info] PostEffectManagerTest: " +
            "Initialized. F = Play Fog, G = Stop Fog."
        );
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // ----- Fog Play -----

        if (keyboard.fKey.wasPressedThisFrame)
        {
            TestPlayFog();
        }

        // ----- Fog Stop -----

        if (keyboard.gKey.wasPressedThisFrame)
        {
            TestStopFog();
        }

        // ----- Overlay Play -----
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            TestPlayOverlay();
        }

        // ----- Overlay Stop -----
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            PostEffectManager.StopOverlay();
        }

        // ----- Gaussian Blur Test -----

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            TestPlayGaussianBlur();
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            PostEffectManager.StopGaussianBlur();
        }

        // ----- Lens Distortion Test -----

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            TestPlayLensDistortion();
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            PostEffectManager.StopLensDistortion();
        }

        // ----- Radial Blur Test -----

        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            TestPlayRadialBlur();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            PostEffectManager.StopRadialBlur();
        }
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Fog Test -----

    private void TestPlayFog()
    {
        //Debug.Log("[Test] PostEffectManager: PlayFog");

        PostEffectManager.PlayFog(
            _fadeInFrames,
            _holdFrames,
            _fadeOutFrames,
            _fogColor,
            _startDistance,
            _endDistance,
            _density,
            _intensity,
            _fogMode
        );
    }

    private void TestStopFog()
    {
        //Debug.Log("[Test] PostEffectManager: StopFog");

        PostEffectManager.StopFog();
    }

    // ==================================================
    // Overlay Test
    // ==================================================

    // ----- Play Overlay -----

    private void TestPlayOverlay()
    {
        Debug.Log(
            "[Test] PostEffectManager: PlayOverlay"
        );

        PostEffectManager.PlayOverlay(
            _overlayFadeInFrames,
            _overlayHoldFrames,
            _overlayFadeOutFrames,
            _overlayColor,
            _overlayIntensity,
            _overlayBlendMode
        );
    }

    // ==================================================
    // Gaussian Blur Test
    // ==================================================

    // ----- Play Gaussian Blur -----

    private void TestPlayGaussianBlur()
    {
        Debug.Log(
            "[Test] PostEffectManager: PlayGaussianBlur"
        );

        PostEffectManager.PlayGaussianBlur(
            _blurFadeInFrames,
            _blurHoldFrames,
            _blurFadeOutFrames,
            _blurRadius,
            _blurStrength,
            _blurIterations
        );
    }

    // ==================================================
    // Lens Distortion Test
    // ==================================================

    // ----- Play Lens Distortion -----

    private void TestPlayLensDistortion()
    {
        Debug.Log(
            "[Test] PostEffectManager: PlayLensDistortion"
        );

        PostEffectManager.PlayLensDistortion(
            _lensFadeInFrames,
            _lensHoldFrames,
            _lensFadeOutFrames,
            _lensIntensity,
            _lensCenter,
            _lensScale,
            _lensRangeMode,
            _lensRangeRadius,
            _lensInnerRadius,
            _lensEdgeSoftness
        );
    }

    // ==================================================
    // Radial Blur Test
    // ==================================================

    // ----- Play Radial Blur -----

    private void TestPlayRadialBlur()
    {
        Debug.Log(
            "[Test] PostEffectManager: PlayRadialBlur"
        );

        PostEffectManager.PlayRadialBlur(
            _radialFadeInFrames,
            _radialHoldFrames,
            _radialFadeOutFrames,
            _radialIntensity,
            _radialCenter,
            _radialInnerRadius,
            _radialOuterRadius,
            _radialSampleCount,
            _radialFalloff
        );
    }
}