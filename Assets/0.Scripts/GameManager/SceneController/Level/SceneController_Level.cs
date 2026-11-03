/*
    SceneController_Level
    20261103  hanaue sho
    レベルシーンを管理するスクリプト
 */
using UnityEngine;

public class SceneController_Level : SceneController_Base
{
    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Level Enter");
    }
    public override void Exit()
    {
        Debug.Log("Level Exit");
    }
}
