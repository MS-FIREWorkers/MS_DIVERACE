/*
    SceneController_Result
    20261103  hanaue sho
    リザルトシーンを管理するスクリプト
 */
using UnityEngine;

public class SceneController_Result : SceneController_Base
{
    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Result Enter");
    }
    public override void Exit()
    {
        Debug.Log("Result Exit");
    }
}
