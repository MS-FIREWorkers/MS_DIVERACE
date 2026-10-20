/*
    PlayerMovement_ParameterData
    20261008  hanaue sho
    プレイヤーキャラクターの基礎パラメータデータ
 */
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerMovement_ParameterData", menuName = "Player/Movement Parameter Data")]
public class PlayerMovement_ParameterData : ScriptableObject
{
    // --------------------------------------------------
    // ----- Fall -----
    // --------------------------------------------------
    [Header("Fall")]
    [Tooltip("重力加速度")]
    public float GravityAcceleration = 30.0f;

    [Tooltip("水平姿勢　終端速度")]
    public float Horizontal_TerminalFallSpeed = 20.0f;
    [Tooltip("鉛直姿勢　終端速度")]
    public float Vertical_TerminalFallSpeed = 50.0f;

    [Tooltip("水平姿勢 落下速度への収束速度")]
    public float Horizontal_FallResponse = 0.5f;
    [Tooltip("鉛直姿勢 落下速度への収束速度")]
    public float Vertical_FallResponse = 0.5f;

    // --------------------------------------------------
    // ----- HorizontalMove -----
    // --------------------------------------------------
    [Header("HorizontalMove")]
    [Tooltip("水平姿勢　頭方向加速度")]
    public float Horizontal_ForwardAcceleration = 20.0f;
    [Tooltip("水平姿勢　左右方向加速度")]
    public float Horizontal_SideAcceleration = 15.0f;
    [Tooltip("水平姿勢　足方向加速度")]
    public float Horizontal_BackAcceleration = 10.0f;
    [Tooltip("鉛直姿勢　頭方向加速度")]
    public float Vertical_ForwardAcceleration = 10.0f;
    [Tooltip("鉛直姿勢　左右方向加速度")]
    public float Vertical_SideAcceleration = 8.0f;
    [Tooltip("鉛直姿勢　足方向加速度")]
    public float Vertical_BackAcceleration = 5.0f;

    // --------------------------------------------------
    // ----- Resistance -----
    // --------------------------------------------------
    [Header("Resistance")]
    [Tooltip("水平姿勢　水平方向抗力")]
    public float Horizontal_HorizontalDrag = 1.0f;
    [Tooltip("鉛直姿勢　水平方向抗力")]
    public float Vertical_HorizontalDrag = 2.0f;
    
    // --------------------------------------------------
    // ----- Rotation -----
    // --------------------------------------------------
    [Header("Rotation")]
    [Tooltip("姿勢回転速度")]
    public float YawSpeed = 90.0f;

}
