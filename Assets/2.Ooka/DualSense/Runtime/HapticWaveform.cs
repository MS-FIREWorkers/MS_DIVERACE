/*
    HapticWaveform.cs
    DIVE RACE の振動波形を定義する
*/

/// <summary>
/// 振動の波形を定義する。
/// </summary>
public enum HapticWaveform
{
    // ==================================================
    // ----- Haptic Waveform -----
    // ==================================================
    Noise,      // ノイズ波（不規則な波形）
    Sine,       // 正弦波
    Triangle,   // 三角波
    Square      // 矩形波（方形波）
}
