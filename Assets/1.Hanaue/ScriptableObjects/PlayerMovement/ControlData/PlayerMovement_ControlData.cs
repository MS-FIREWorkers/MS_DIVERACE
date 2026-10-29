/*
    PlayerMovement_ControlData
    20261008  hanaue sho
    プレイヤーキャラクターの操作性のデータ
    ステートによって変える
 */
using UnityEngine;

[CreateAssetMenu( fileName = "PlayerMovement_ControlData", menuName = "Player/Movement Control Data")]
public class PlayerMovement_ControlData : ScriptableObject
{
    // --------------------------------------------------
    // ----- Enable -----
    // --------------------------------------------------
    [Header("Enable")]
    [Tooltip("平行移動の有効化")]
    public bool EnableHorizontalMove = true;
    [Tooltip("姿勢回転の有効化")]
    public bool EnableYawRotation = true;
    [Tooltip("落下の有効化")]
    public bool EnableFall = true;

    // --------------------------------------------------
    // ----- Horizontal Control -----
    // --------------------------------------------------
    [Header("Horizontal Control")]
    [Tooltip("水平移動の倍率")]
    public float HorizontalControlMultiplier = 1.0f;
    [Tooltip("水平移動頭方向の倍率")]
    public float ForwardControlMultiplier = 1.0f;
    [Tooltip("水平移動左右方向の倍率")]
    public float SideControlMultiplier = 1.0f;
    [Tooltip("水平移動足方向の倍率")]
    public float BackControlMultiplier = 1.0f;

    // --------------------------------------------------
    // ----- Fall Control -----
    // --------------------------------------------------
    [Header("Fall Control")]
    [Tooltip("垂直の終端速度の倍率")]
    public float TerminalFallSpeedMultiplier = 1.0f;
}
