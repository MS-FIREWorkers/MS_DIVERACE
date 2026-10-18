/*
    CameraShake_Data
    20261018  hanaue sho
 */
using UnityEngine;

public enum CameraShakePattern
{
    Sine,           // サイン派
    PerlinNoise,    // 不規則な振動
    FovPulse,       // FOV の一時的な変化
}
public enum CameraShakeGroup
{
    Impact,     // 衝突による振動
    Boost,      // 加速時の振動
    Environment // 環境振動
}

[CreateAssetMenu(fileName = "CameraShake_Data", menuName = "Camera/CameraShake_Data")]
public class CameraShake_Data : ScriptableObject
{
    // --------------------------------------------------
    // ----- Common -----
    // --------------------------------------------------
    [Header("Common")]
    [Tooltip("シェイクのグループ")]
    public CameraShakeGroup Group = CameraShakeGroup.Impact;
    [Tooltip("シェイクのパターン")]
    public CameraShakePattern Pattern = CameraShakePattern.Sine;

    // --------------------------------------------------
    // ----- Timing -----
    // --------------------------------------------------
    [Header("Timing")]
    [Tooltip("シェイク全体の再生時間（秒）")]
    [Min(0.0f)] public float Duration = 0.5f;
    [Tooltip("フェードイン時間（秒）")]
    [Min(0.0f)] public float FadeIn = 0.05f;
    [Tooltip("フェードアウト時間（秒）")]
    [Min(0.0f)] public float FadeOut = 0.2f;

    // --------------------------------------------------
    // ----- Transform -----
    // --------------------------------------------------
    [Header("Position")]
    [Tooltip("各軸の位置振幅（ｍ）")]
    public Vector3 PositionAmplitude = Vector3.zero;
    [Tooltip("各軸の振幅周波数（Hz）")]
    public Vector3 PositionFrequency = Vector3.zero;

    [Header("Rotation")]
    [Tooltip("各軸の回転振幅（度）")]
    public Vector3 RotationAmplitude = Vector3.zero;
    [Tooltip("各軸の振幅周波数（Hz）")]
    public Vector3 RotationFrequency = Vector3.zero;

    // --------------------------------------------------
    // ----- FOV -----
    // --------------------------------------------------
    [Header("FOV")]
    [Tooltip("FOV の振幅（度）")]
    public float FovAmplitude = 0.0f;
    [Tooltip("各軸の振幅周波数（Hz）")]
    public float FovFrequency = 0.0f;

    // --------------------------------------------------
    // ----- FOV Pulse -----
    // --------------------------------------------------
    [Header("FOV Pulse")]
    [Tooltip("指定 FOV まで増加する時間")]
    [Min(0.0f)]public float FovAttackTime = 0.1f;
    [Tooltip("FOV を維持する時間")]
    [Min(0.0f)]public float FovHoldTime = 0.1f;
    [Tooltip("元の FOV に戻す時間")]
    [Min(0.0f)]public float FovReleaseTime = 0.3f;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public float TotalDuration
    {
        get 
        { 
            if (Pattern == CameraShakePattern.FovPulse)
            {
                return FovAttackTime + FovHoldTime + FovReleaseTime;
            }

            return Duration;
        }
    }

}
