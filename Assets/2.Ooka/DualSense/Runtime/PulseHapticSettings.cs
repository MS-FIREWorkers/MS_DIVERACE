/*
    PulseHapticSettings.cs
    単発パルス振動の設定を管理する。
*/

using UnityEngine;

[CreateAssetMenu(fileName = "PulseHapticSettings", menuName = "DualSense Haptics/Pulse")]
public class PulseHapticSettings : ScriptableObject
{
    // ==================================================
    // ----- Pulse Settings -----
    // ==================================================

    [Header("パルス設定")]
    [Tooltip("パルス振動の振幅")]
    [Range(0f, 1f)]
    public float _amplitude = 0.25f;

    [Tooltip("振動波形の周波数（Hz）")]
    [Min(1f)]
    public float _frequencyHz = 50f;

    [Tooltip("パルス振動の継続時間（秒）")]
    [Min(0.01f)]
    public float _duration = 0.12f;
}
