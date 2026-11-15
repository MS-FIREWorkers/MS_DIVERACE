/*
    ObstacleFilm_PlayerTracker
    20261115  hanaue sho
 */
using UnityEngine;

public class ObstacleFilm_PlayerTracker : MonoBehaviour
{
    // ==================================================
    // ----- Shader Properties -----
    // ==================================================
    private static readonly int PlayerPositionID = Shader.PropertyToID("_PlayerPositionWS");
    private static readonly int PlayerValidID = Shader.PropertyToID("_PlayerPositionValid");

    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    private void LateUpdate()
    {
        // プレイヤーの位置を送る
        Shader.SetGlobalVector(PlayerPositionID, transform.position);
        Shader.SetGlobalFloat(PlayerValidID, 1.0f);
    }
    private void OnDisable()
    {
        Shader.SetGlobalFloat(PlayerValidID, 0.0f);
    }
}