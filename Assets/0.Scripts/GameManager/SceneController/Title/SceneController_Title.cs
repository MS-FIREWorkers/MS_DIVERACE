/*
    SceneController_Title
    20261103  hanaue sho
    タイトルシーンを管理するスクリプト
 */
using UnityEngine;

public class SceneController_Title : SceneController_Base
{
    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        Debug.Log("Title Enter");
    }
    public override void Exit()
    {
        Debug.Log("Title Exit");
    }

}
