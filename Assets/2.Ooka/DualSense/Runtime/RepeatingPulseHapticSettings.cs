/*
    RepeatingPulseHapticSettings.cs
    繰り返しパルス振動の設定を管理する。
*/

using UnityEngine;

[CreateAssetMenu(fileName = "RepeatingPulseHapticSettings", menuName = "DualSense Haptics/Repeating Pulse")]
public class RepeatingPulseHapticSettings : ScriptableObject
{
    // ==================================================
    // ----- Pulse Settings -----
    // ==================================================

    [Header("パルス設定")]
    [Tooltip("パルス振動の振幅")]
    [Range(0f, 1f)]
    public float _amplitude = 0.15f;

    [Tooltip("振動波形の周波数（Hz）")]
    [Min(1f)]
    public float _frequencyHz = 50f;

    [Tooltip("1回のパルスが継続する時間（秒）")]
    [Min(0.01f)]
    public float _pulseDuration = 0.08f;

    [Tooltip("パルス間の間隔（秒）")]
    [Min(0.01f)]
    public float _interval = 0.35f;
}
