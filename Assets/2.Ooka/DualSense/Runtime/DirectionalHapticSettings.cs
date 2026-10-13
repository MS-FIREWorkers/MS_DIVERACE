/*
    DirectionalHapticSettings.cs
    •ûŒü«‚Ì‚ ‚éƒnƒvƒeƒBƒNƒX‚ÌU•E”gŒ`İ’è‚ğŠÇ—‚·‚éB
*/

using UnityEngine;

[CreateAssetMenu(fileName = "DirectionalHapticSettings", menuName = "DualSense Haptics/Directional")]
public class DirectionalHapticSettings : ScriptableObject
{
    // ==================================================
    // ----- Amplitude Settings -----
    // ==================================================

    [Header("U•İ’è")]
    [Tooltip("U“®‚ÌÅ¬U•")]
    [Range(0f, 1f)]
    public float _minAmplitude = 0.15f;

    [Tooltip("U“®‚ÌÅ‘åU•")]
    [Range(0f, 1f)]
    public float _maxAmplitude = 0.6f;

    // ==================================================
    // ----- Wave Settings -----
    // ==================================================

    [Header("”gŒ`İ’è")]
    [Tooltip("U“®”gŒ`‚Ìü”g”iHzj")]
    [Min(1f)]
    public float _frequencyHz = 50f;

    [Tooltip("•›•ûŒü‚ÌU“®‚É“K—p‚·‚éd‚İ")]
    [Range(0f, 1f)]
    public float _secondaryWeight = 0.35f;
}
