/*
    SceneController_Tutorial
    20261103  hanaue sho
    チュートリアルシーンを管理するスクリプト
    Update の処理は基本的に StateMachine にまとめる。
 */
using UnityEngine;

public class SceneController_Tutorial : SceneController_Base
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    private TutorialScene_StateMachine _stateMachine;
    private TutorialScene_InputHandler _inputHandler;
    private TutorialScene_DialogueView _dialogueView;
    private TutorialScene_GuidView _guidView;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public TutorialScene_StateMachine StateMachine => _stateMachine;
    public TutorialScene_InputHandler InputHandler => _inputHandler;
    public TutorialScene_DialogueView DialogueView => _dialogueView;
    public TutorialScene_GuidView GuidView => _guidView;

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
