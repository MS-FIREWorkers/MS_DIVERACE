/*
    SceneController_Result
    20261103  hanaue sho
    リザルトシーンを管理するスクリプト
    Update の処理は基本的に StateMachine にまとめる。
 */
using UnityEngine;

public class SceneController_Result : SceneController_Base
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    private ResultScene_StateMachine _stateMachine;
    private ResultScene_PresentationController _presentationController;
    private ResultScene_InputHandler _inputHandler;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public ResultScene_StateMachine StateMachine => _stateMachine;
    public ResultScene_PresentationController PresentationController => _presentationController;
    public ResultScene_InputHandler InputHandler => _inputHandler;

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
