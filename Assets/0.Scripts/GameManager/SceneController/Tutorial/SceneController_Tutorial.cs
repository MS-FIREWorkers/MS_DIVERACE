/*
    SceneController_Tutorial
    20261103  hanaue sho
    チュートリアルシーンを管理するスクリプト
 */
using UnityEngine;

public class SceneController_Tutorial : SceneController_Base
{
    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Tutorial Enter");
    }
    public override void Exit()
    {
        Debug.Log("Tutorial Exit");
    }
}
