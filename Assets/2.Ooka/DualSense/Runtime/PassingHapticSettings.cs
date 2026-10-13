/*
    PassingHapticSettings.cs
    障害物などの通過時に使用するハプティクスの設定を管理する。
*/

using UnityEngine;

[CreateAssetMenu(fileName = "PassingHapticSettings", menuName = "DualSense Haptics/Passing")]
public class PassingHapticSettings : ScriptableObject
{
    // ==================================================
    // ----- Passing Settings -----
    // ==================================================

    [Header("通過振動設定")]
    [Tooltip("通過振動の振幅")]
    [Range(0f, 1f)]
    public float _amplitude = 0.25f;

    [Tooltip("振動波形の周波数（Hz）")]
    [Min(1f)]
    public float _frequencyHz = 50f;

    [Tooltip("通過振動の継続時間（秒）")]
    [Min(0.01f)]
    public float _duration = 0.6f;

    [Tooltip("副方向の振動に適用する重み")]
    [Range(0f, 1f)]
    public float _secondaryWeight = 0.35f;
}
