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
    private FogEffect.FogMode _fogMode =
        FogEffect.FogMode.Linear;

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
}