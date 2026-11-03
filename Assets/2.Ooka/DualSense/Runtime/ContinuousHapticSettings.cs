/*
    ContinuousHapticSettings.cs
    Œp‘±“I‚ÈƒnƒvƒeƒBƒNƒX‚ÌU•E”gŒ`İ’è‚ğŠÇ—‚·‚éB
*/

using UnityEngine;

[CreateAssetMenu(fileName = "ContinuousHapticSettings", menuName = "DualSense Haptics/Continuous")]
public class ContinuousHapticSettings : ScriptableObject
{
    // ==================================================
    // ----- Amplitude Settings -----
    // ==================================================

    [Header("U•İ’è")]
    [Tooltip("U“®‚ÌÅ¬U•")]
    [Range(0f, 1f)]
    public float _minAmplitude = 0.05f;

    [Tooltip("U“®‚ÌÅ‘åU•")]
    [Range(0f, 1f)]
    public float _maxAmplitude = 0.7f;

    // ==================================================
    // ----- Wave Settings -----
    // ==================================================

    [Header("”gŒ`İ’è")]
    [Tooltip("U“®”gŒ`‚Ìü”g”iHzj")]
    [Min(1f)]
    public float _frequencyHz = 50f;

    [Tooltip("g—p‚·‚éU“®”gŒ`")]
    public HapticWaveform _waveform = HapticWaveform.Noise;

    [Tooltip("ƒmƒCƒY”gŒ`‚ÌƒQƒCƒ“")]
    [Min(0f)]
    public float _noiseGain = 2.5f;
}
