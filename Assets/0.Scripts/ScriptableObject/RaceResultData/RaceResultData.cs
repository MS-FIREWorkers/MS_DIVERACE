/*
    RaceResultData
    20261126  hanaue sho
    リザルトに渡す情報
    現状は計測時間のみ
 */
using UnityEngine;

[CreateAssetMenu(menuName = "Game/RaceResultData")]
public class RaceResultData : ScriptableObject
{
    // --------------------------------------------------
    // ----- RaceTime -----
    // --------------------------------------------------
    public float RaceTime { get; private set; }
    public void SetResult(float raceTime)
    {
        RaceTime = raceTime;
    }
    public void ResetResult()
    {
        RaceTime = 0.0f;
    }
}