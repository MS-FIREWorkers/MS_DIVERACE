// ==================================================
// File: PostEffectManagerTest.cs
// Created: 2026/11/04
// Author: Fukano
// Summary: PostEffectManagerの公開APIを使用した動作確認
// ==================================================

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// ==================================================
// PostEffectManagerTest
// ==================================================

[DisallowMultipleComponent]
public class PostEffectManagerTest : MonoBehaviour
{
    // ==================================================
    // Variables
    // ==================================================

    // ----- Runtime -----

    private Coroutine _testSequence;

    // ----- Inspector Groups -----

    [Header("Test Configuration")]
    [SerializeField]
    private KeySettings _keySettings = new();

    [SerializeField]
    private EffectSettings _effectSettings = new();

    [System.Serializable]
    private class KeySettings
    {
        [SerializeField]
        public FogKeys fogKeys = new();

        [SerializeField]
        public OverlayKeys overlayKeys = new();

        [SerializeField]
        public GaussianBlurKeys gaussianBlurKeys = new();

        [SerializeField]
        public LensDistortionKeys lensDistortionKeys = new();

        [SerializeField]
        public RadialBlurKeys radialBlurKeys = new();

        [SerializeField]
        public NamedOverlayKeys namedOverlayKeys = new();

        [SerializeField]
        public CombinedKeys combinedKeys = new();

    }

    [System.Serializable]
    private class EffectSettings
    {
        [SerializeField]
        public FogSettings fogSettings = new();

        [SerializeField]
        public OverlaySettings overlaySettings = new();

        [SerializeField]
        public GaussianBlurSettings gaussianBlurSettings = new();

        [SerializeField]
        public LensDistortionSettings lensDistortionSettings = new();

        [SerializeField]
        public RadialBlurSettings radialBlurSettings = new();

    }

    // ==================================================
    // Serializable Inspector Groups
    // ==================================================

    [System.Serializable]
    private class FogSettings
    {
        [Tooltip("FogのFade Inフレーム数")]
        [SerializeField, Min(0)]
        public int fadeInFrames = 30;

        [Tooltip("Fogの最大強度維持フレーム数")]
        [SerializeField, Min(0)]
        public int holdFrames = 60;

        [Tooltip("FogのFade Outフレーム数")]
        [SerializeField, Min(0)]
        public int fadeOutFrames = 30;

        [Tooltip("Fogの色")]
        [SerializeField]
        public Color fogColor = Color.red;

        [Tooltip("Fogの開始距離")]
        [SerializeField, Min(0.0f)]
        public float startDistance = 5.0f;

        [Tooltip("Fogの終了距離")]
        [SerializeField, Min(0.0f)]
        public float endDistance = 30.0f;

        [Tooltip("Fogの密度")]
        [SerializeField, Range(0.0f, 0.2f)]
        public float density = 0.03f;

        [Tooltip("Fogの最大強度")]
        [SerializeField, Range(0.0f, 1.0f)]
        public float intensity = 0.8f;

        [Tooltip("Fogの計算方式")]
        [SerializeField]
        public FogEffect.FogMode fogMode = FogEffect.FogMode.Linear;

    }

    [System.Serializable]
    private class OverlaySettings
    {
        [Tooltip("Fade Inのフレーム数")]
        [SerializeField, Min(0)]
        public int overlayFadeInFrames = 30;

        [Tooltip("Holdのフレーム数")]
        [SerializeField, Min(0)]
        public int overlayHoldFrames = 60;

        [Tooltip("Fade Outのフレーム数")]
        [SerializeField, Min(0)]
        public int overlayFadeOutFrames = 30;

        [Tooltip("Overlayの色")]
        [SerializeField]
        public Color overlayColor = Color.red;

        [Tooltip("Overlayの最大強度")]
        [SerializeField, Range(0.0f, 1.0f)]
        public float overlayIntensity = 0.8f;

        [Tooltip("Overlayの合成方法")]
        [SerializeField]
        public OverlayEffect.BlendMode overlayBlendMode = OverlayEffect.BlendMode.Normal;

    }

    [System.Serializable]
    private class GaussianBlurSettings
    {
        [Tooltip("Fade Inのフレーム数")]
        [SerializeField, Min(0)]
        public int blurFadeInFrames = 30;

        [Tooltip("Holdのフレーム数")]
        [SerializeField, Min(0)]
        public int blurHoldFrames = 60;

        [Tooltip("Fade Outのフレーム数")]
        [SerializeField, Min(0)]
        public int blurFadeOutFrames = 30;

        [Tooltip("ぼかし半径")]
        [SerializeField, Range(0.0f, 10.0f)]
        public float blurRadius = 4.0f;

        [Tooltip("最大ぼかし強度")]
        [SerializeField, Range(0.0f, 1.0f)]
        public float blurStrength = 0.8f;

        [Tooltip("ぼかしの反復回数")]
        [SerializeField, Range(1, 8)]
        public int blurIterations = 2;

    }

    [System.Serializable]
    private class LensDistortionSettings
    {
        [Tooltip("Fade Inのフレーム数")]
        [SerializeField, Min(0)]
        public int lensFadeInFrames = 30;

        [Tooltip("Holdのフレーム数")]
        [SerializeField, Min(0)]
        public int lensHoldFrames = 60;

        [Tooltip("Fade Outのフレーム数")]
        [SerializeField, Min(0)]
        public int lensFadeOutFrames = 30;

        [Tooltip("歪みの強度")]
        [SerializeField, Range(-1.0f, 1.0f)]
        public float lensIntensity = 0.5f;

        [Tooltip("歪みの中心座標")]
        [SerializeField]
        public Vector2 lensCenter = new Vector2(0.5f, 0.5f);

        [Tooltip("UVの拡大縮小")]
        [SerializeField, Range(0.1f, 2.0f)]
        public float lensScale = 1.0f;

        [Tooltip("歪みを適用する範囲")]
        [SerializeField]
        public LensDistortionEffect.RangeMode lensRangeMode = LensDistortionEffect.RangeMode.Full;

        [Tooltip("適用範囲の半径")]
        [SerializeField, Range(0.0f, 2.0f)]
        public float lensRangeRadius = 0.5f;

        [Tooltip("Ringモードの内側半径")]
        [SerializeField, Range(0.0f, 2.0f)]
        public float lensInnerRadius = 0.2f;

        [Tooltip("境界を滑らかにする幅")]
        [SerializeField, Range(0.0f, 0.5f)]
        public float lensEdgeSoftness = 0.05f;

    }

    [System.Serializable]
    private class RadialBlurSettings
    {
        [Tooltip("Fade Inのフレーム数")]
        [SerializeField, Min(0)]
        public int radialFadeInFrames = 30;

        [Tooltip("Holdのフレーム数")]
        [SerializeField, Min(0)]
        public int radialHoldFrames = 60;

        [Tooltip("Fade Outのフレーム数")]
        [SerializeField, Min(0)]
        public int radialFadeOutFrames = 30;

        [Tooltip("ラジアルブラーの最大強度")]
        [SerializeField, Range(0.0f, 1.0f)]
        public float radialIntensity = 0.7f;

        [Tooltip("ブラーの中心座標")]
        [SerializeField]
        public Vector2 radialCenter = new Vector2(0.5f, 0.5f);

        [Tooltip("中心付近のブラーを適用しない半径")]
        [SerializeField, Range(0.0f, 1.0f)]
        public float radialInnerRadius = 0.2f;

        [Tooltip("ブラーの適用終了半径")]
        [SerializeField, Range(0.0f, 2.0f)]
        public float radialOuterRadius = 1.0f;

        [Tooltip("サンプリング回数")]
        [SerializeField, Range(4, 32)]
        public int radialSampleCount = 12;

        [Tooltip("境界の滑らかさ")]
        [SerializeField, Range(0.001f, 0.5f)]
        public float radialFalloff = 0.15f;

    }

    [System.Serializable]
    private class FogKeys
    {
        [Tooltip("playFogKey の操作キー")]
        [SerializeField]
        public Key playFogKey = Key.F;

        [Tooltip("stopFogKey の操作キー")]
        [SerializeField]
        public Key stopFogKey = Key.G;

    }

    [System.Serializable]
    private class OverlayKeys
    {
        [Tooltip("playOverlayKey の操作キー")]
        [SerializeField]
        public Key playOverlayKey = Key.H;

        [Tooltip("stopOverlayKey の操作キー")]
        [SerializeField]
        public Key stopOverlayKey = Key.J;

    }

    [System.Serializable]
    private class GaussianBlurKeys
    {
        [Tooltip("playGaussianBlurKey の操作キー")]
        [SerializeField]
        public Key playGaussianBlurKey = Key.K;

        [Tooltip("stopGaussianBlurKey の操作キー")]
        [SerializeField]
        public Key stopGaussianBlurKey = Key.L;

    }

    [System.Serializable]
    private class LensDistortionKeys
    {
        [Tooltip("playLensDistortionKey の操作キー")]
        [SerializeField]
        public Key playLensDistortionKey = Key.N;

        [Tooltip("stopLensDistortionKey の操作キー")]
        [SerializeField]
        public Key stopLensDistortionKey = Key.B;

    }

    [System.Serializable]
    private class RadialBlurKeys
    {
        [Tooltip("playRadialBlurKey の操作キー")]
        [SerializeField]
        public Key playRadialBlurKey = Key.V;

        [Tooltip("stopRadialBlurKey の操作キー")]
        [SerializeField]
        public Key stopRadialBlurKey = Key.C;

    }

    [System.Serializable]
    private class NamedOverlayKeys
    {
        [Tooltip("playDamageOverlayKey の操作キー")]
        [SerializeField]
        public Key playDamageOverlayKey = Key.Digit1;

        [Tooltip("playWarningOverlayKey の操作キー")]
        [SerializeField]
        public Key playWarningOverlayKey = Key.Digit2;

        [Tooltip("stopDamageOverlayKey の操作キー")]
        [SerializeField]
        public Key stopDamageOverlayKey = Key.Digit3;

        [Tooltip("stopWarningOverlayKey の操作キー")]
        [SerializeField]
        public Key stopWarningOverlayKey = Key.Digit4;

    }

    [System.Serializable]
    private class CombinedKeys
    {
        [Tooltip("playAllKey の操作キー")]
        [SerializeField]
        public Key playAllKey = Key.T;

        [Tooltip("stopAllKey の操作キー")]
        [SerializeField]
        public Key stopAllKey = Key.Y;

        [Tooltip("startSequenceKey の操作キー")]
        [SerializeField]
        public Key startSequenceKey = Key.U;

        [Tooltip("cancelSequenceKey の操作キー")]
        [SerializeField]
        public Key cancelSequenceKey = Key.I;

    }
    // ==================================================
    // Unity Functions
    // ==================================================

    private void Start()
    {
        Debug.Log("[Info] [PostEffectManager] テストキーはInspectorの各Keys項目から変更できます。");
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ----- Fog -----
        if (IsKeyPressed(keyboard, _keySettings.fogKeys.playFogKey)) TestPlayFog();
        if (IsKeyPressed(keyboard, _keySettings.fogKeys.stopFogKey)) TestStopFog();
        // ----- Overlay -----
        if (IsKeyPressed(keyboard, _keySettings.overlayKeys.playOverlayKey)) TestPlayOverlay();
        if (IsKeyPressed(keyboard, _keySettings.overlayKeys.stopOverlayKey)) PostEffectManager.StopOverlay();
        // ----- Gaussian Blur -----
        if (IsKeyPressed(keyboard, _keySettings.gaussianBlurKeys.playGaussianBlurKey)) TestPlayGaussianBlur();
        if (IsKeyPressed(keyboard, _keySettings.gaussianBlurKeys.stopGaussianBlurKey)) PostEffectManager.StopGaussianBlur();
        // ----- Lens Distortion -----
        if (IsKeyPressed(keyboard, _keySettings.lensDistortionKeys.playLensDistortionKey)) TestPlayLensDistortion();
        if (IsKeyPressed(keyboard, _keySettings.lensDistortionKeys.stopLensDistortionKey)) PostEffectManager.StopLensDistortion();
        // ----- Radial Blur -----
        if (IsKeyPressed(keyboard, _keySettings.radialBlurKeys.playRadialBlurKey)) TestPlayRadialBlur();
        if (IsKeyPressed(keyboard, _keySettings.radialBlurKeys.stopRadialBlurKey)) PostEffectManager.StopRadialBlur();
        // ----- Named Overlay -----
        if (IsKeyPressed(keyboard, _keySettings.namedOverlayKeys.playDamageOverlayKey)) TestPlayDamageOverlay();
        if (IsKeyPressed(keyboard, _keySettings.namedOverlayKeys.playWarningOverlayKey)) TestPlayWarningOverlay();
        if (IsKeyPressed(keyboard, _keySettings.namedOverlayKeys.stopDamageOverlayKey)) PostEffectManager.StopOverlay("Damage");
        if (IsKeyPressed(keyboard, _keySettings.namedOverlayKeys.stopWarningOverlayKey)) PostEffectManager.StopOverlay("Warning");
        // ----- Combined Tests -----
        if (IsKeyPressed(keyboard, _keySettings.combinedKeys.playAllKey)) TestPlayAll();
        if (IsKeyPressed(keyboard, _keySettings.combinedKeys.stopAllKey)) TestStopAll();
        if (IsKeyPressed(keyboard, _keySettings.combinedKeys.startSequenceKey)) TestStartSequence();
        if (IsKeyPressed(keyboard, _keySettings.combinedKeys.cancelSequenceKey)) TestCancelSequence();
    }

    // ==================================================
    // Custom Functions
    // ==================================================

    // ----- Input Helpers -----

    private bool IsKeyPressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None) return false;

        KeyControl control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    // ----- Fog Test -----

    public void TestPlayFog()
    {
        Debug.Log("[Info] PostEffectManager: PlayFog");

        PostEffectManager.PlayFog(
            _effectSettings.fogSettings.fadeInFrames,          // Fade In：効果が最大になるまでのフレーム数
            _effectSettings.fogSettings.holdFrames,            // Hold：最大強度を維持するフレーム数
            _effectSettings.fogSettings.fadeOutFrames,         // Fade Out：効果を解除するまでのフレーム数
            _effectSettings.fogSettings.fogColor,              // Fog Color：霧の色
            _effectSettings.fogSettings.startDistance,         // Start Distance：霧がかかり始める距離
            _effectSettings.fogSettings.endDistance,           // End Distance：霧が最大になる距離（Linear）
            _effectSettings.fogSettings.density,               // Density：霧の密度（指数系モードで使用）
            _effectSettings.fogSettings.intensity,             // Intensity：霧の最大強度（0～1）
            _effectSettings.fogSettings.fogMode                // Fog Mode：Linear / Exp / Exp2 の計算方式
        );
    }

    public void TestStopFog()
    {
        Debug.Log("[Info] PostEffectManager: StopFog");

        PostEffectManager.StopFog();
    }

    // ==================================================
    // Overlay Test
    // ==================================================

    // ----- Play Overlay -----

    public void TestPlayOverlay()
    {
        Debug.Log("[Info] PostEffectManager: PlayOverlay");

        PostEffectManager.PlayOverlay(
            _effectSettings.overlaySettings.overlayFadeInFrames,       // Fade In：色の適用を強めるフレーム数
            _effectSettings.overlaySettings.overlayHoldFrames,         // Hold：最大強度を維持するフレーム数
            _effectSettings.overlaySettings.overlayFadeOutFrames,      // Fade Out：色の適用を弱めるフレーム数
            _effectSettings.overlaySettings.overlayColor,              // Color：画面に重ねる色
            _effectSettings.overlaySettings.overlayIntensity,          // Intensity：色の最大適用強度（0～1）
            _effectSettings.overlaySettings.overlayBlendMode           // Blend Mode：Normal / Additive / Multiply の合成方式
        );
    }

    // ==================================================
    // Gaussian Blur Test
    // ==================================================

    // ----- Play Gaussian Blur -----

    public void TestPlayGaussianBlur()
    {
        Debug.Log("[Info] PostEffectManager: PlayGaussianBlur");

        PostEffectManager.PlayGaussianBlur(
            _effectSettings.gaussianBlurSettings.blurFadeInFrames,              // Fade In：30フレームで徐々に適用 
            _effectSettings.gaussianBlurSettings.blurHoldFrames,                // Hold：最大強度を60フレーム維持
            _effectSettings.gaussianBlurSettings.blurFadeOutFrames,             // Fade Out：30フレームで徐々に解除
            _effectSettings.gaussianBlurSettings.blurRadius,                    // Radius : ぼかし半径
            _effectSettings.gaussianBlurSettings.blurStrength,                  // Strength：ぼかしの最大強度（0～1）
            _effectSettings.gaussianBlurSettings.blurIterations                 // Iterations：ぼかし処理の反復回数
        );
    }

    // ==================================================
    // Lens Distortion Test
    // ==================================================

    // ----- Play Lens Distortion -----

    public void TestPlayLensDistortion()
    {
        Debug.Log("[Info] PostEffectManager: PlayLensDistortion");

        PostEffectManager.PlayLensDistortion(
            _effectSettings.lensDistortionSettings.lensFadeInFrames,                  // Fade In：歪みを強めるフレーム数
            _effectSettings.lensDistortionSettings.lensHoldFrames,                    // Hold：最大強度を維持するフレーム数
            _effectSettings.lensDistortionSettings.lensFadeOutFrames,                 // Fade Out：歪みを弱めるフレーム数
            _effectSettings.lensDistortionSettings.lensIntensity,                     // Intensity：歪みの強さ（正負で歪み方向が変化）
            _effectSettings.lensDistortionSettings.lensCenter,                        // Center：歪みの中心UV座標（0.5, 0.5が中央）
            _effectSettings.lensDistortionSettings.lensScale,                         // Scale：UVの拡大・縮小倍率
            _effectSettings.lensDistortionSettings.lensRangeMode,                     // Range Mode：歪みを適用する範囲の方式
            _effectSettings.lensDistortionSettings.lensRangeRadius,                   // Range Radius：適用範囲の半径
            _effectSettings.lensDistortionSettings.lensInnerRadius,                   // Inner Radius：Ringモードの内側半径
            _effectSettings.lensDistortionSettings.lensEdgeSoftness                   // Edge Softness：適用範囲の境界の滑らかさ
        );
    }

    // ==================================================
    // Radial Blur Test
    // ==================================================

    // ----- Play Radial Blur -----

    public void TestPlayRadialBlur()
    {
        Debug.Log("[Info] PostEffectManager: PlayRadialBlur");

        PostEffectManager.PlayRadialBlur(
            _effectSettings.radialBlurSettings.radialFadeInFrames,            // Fade In：放射状ぼかしを強めるフレーム数
            _effectSettings.radialBlurSettings.radialHoldFrames,              // Hold：最大強度を維持するフレーム数
            _effectSettings.radialBlurSettings.radialFadeOutFrames,           // Fade Out：放射状ぼかしを弱めるフレーム数
            _effectSettings.radialBlurSettings.radialIntensity,               // Intensity：放射状ぼかしの最大強度
            _effectSettings.radialBlurSettings.radialCenter,                  // Center：放射状ぼかしの中心UV座標
            _effectSettings.radialBlurSettings.radialInnerRadius,             // Inner Radius：中心付近で効果を適用しない半径
            _effectSettings.radialBlurSettings.radialOuterRadius,             // Outer Radius：効果を適用する外側の半径
            _effectSettings.radialBlurSettings.radialSampleCount,             // Sample Count：ぼかしのサンプリング回数
            _effectSettings.radialBlurSettings.radialFalloff                  // Falloff：適用範囲の境界の滑らかさ
        );
    }

    // ==================================================
    // Additional API Usage Examples
    // ==================================================

    // ----- Named Overlay Examples -----

    private void TestPlayDamageOverlay()
    {
        // ID付き再生。同じ種類のエフェクトを用途別に扱う例。
        // 引数: ID, FadeIn, Hold, FadeOut, Color, Intensity
        PostEffectManager.PlayOverlay("Damage", 10, 300, 30, Color.red, 0.8f);
        Debug.Log("[Info] PlayOverlay ID=Damage");
    }

    private void TestPlayWarningOverlay()
    {
        PostEffectManager.PlayOverlay("Warning", 30, 300, 30, Color.yellow, 0.5f);
        Debug.Log("[Info] PlayOverlay ID=Warning");
    }

    // ----- Combined Examples -----

    public void TestPlayAll()
    {
        // 別種類のエフェクトを同じフレームに要求する例。
        // 同時描画の結果は、Renderer Featureの設定や実装にも依存する。
        TestPlayFog();
        TestPlayOverlay();
        TestPlayGaussianBlur();
        TestPlayLensDistortion();
        TestPlayRadialBlur();
        Debug.Log("[Info] Requested all effects");
    }

    public void TestStopAll()
    {
        // 停止APIを種類別に呼ぶ例。
        PostEffectManager.StopFog();
        PostEffectManager.StopOverlay();
        PostEffectManager.StopOverlay("Damage");
        PostEffectManager.StopOverlay("Warning");
        PostEffectManager.StopGaussianBlur();
        PostEffectManager.StopLensDistortion();
        PostEffectManager.StopRadialBlur();
        Debug.Log("[Info] Requested stop for all test effects");
    }

    // ----- Coroutine Sequence Example -----

    public void TestStartSequence()
    {
        // 二重起動を防ぐ。Coroutineの停止はエフェクト停止とは別。
        TestCancelSequence();
        _testSequence = StartCoroutine(RunSequence());
    }

    public void TestCancelSequence()
    {
        if (_testSequence == null) return;
        StopCoroutine(_testSequence);
        _testSequence = null;
        Debug.Log("[Info] Sequence cancelled (active effects are not stopped)");
    }

    private IEnumerator RunSequence()
    {
        // APIの呼び出し順と複数種類の重なりを確認する例。
        // WaitForSecondsは秒単位。Playの時間指定はフレーム数。
        Debug.Log("[Info] Sequence 1/4: Fog");
        TestPlayFog();
        yield return new WaitForSeconds(1f);

        Debug.Log("[Info] Sequence 2/4: Overlay + Fog");
        TestPlayOverlay();
        yield return new WaitForSeconds(1f);

        Debug.Log("[Info] Sequence 3/4: Blur + Lens");
        TestPlayGaussianBlur();
        TestPlayLensDistortion();
        yield return new WaitForSeconds(1f);

        Debug.Log("[Info] Sequence 4/4: Radial Blur");
        TestPlayRadialBlur();
        yield return new WaitForSeconds(1f);

        TestStopAll();
        _testSequence = null;
        Debug.Log("[Info] Sequence completed");
    }

}