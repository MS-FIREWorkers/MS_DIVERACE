/*
    BoostRingData
    20261020  hanaue sho
    加速リングのデータ管理
 */
using UnityEngine;

[CreateAssetMenu(fileName = "BoostRingData", menuName = "Game/BoostRingData")]
public class BoostRingData : ScriptableObject
{
    [Header("Boost")]
    [Min(0.0f)] public float TragetSpeed = 120.0f;
    [Min(0.0f)] public float Duration = 2.0f;
    [Min(0.0f)] public float Acceleration = 100.0f;

    [Header("Center Bounus")]
    public AnimationCurve StrengthCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.5f);

}
