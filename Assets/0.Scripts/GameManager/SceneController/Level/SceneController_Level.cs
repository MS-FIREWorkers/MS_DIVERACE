/*
    SceneController_Level
    20261103  hanaue sho
    レベルシーンを管理するスクリプト
    Update の処理は基本的に StateMachine にまとめる。
 */
using UnityEngine;

public class SceneController_Level : SceneController_Base
{
    // --------------------------------------------------
    // ----- Components -----
    // --------------------------------------------------
    private LevelScene_StateMachine _stateMachine;
    private LevelScene_PauseWindowHandler _pauseWindowHandler;
    private LevelScene_InputHandler _inputHandler;

    // --------------------------------------------------
    // ----- Public Property -----
    // --------------------------------------------------
    public LevelScene_StateMachine StateMachine => _stateMachine;
    public LevelScene_PauseWindowHandler PauseWindowHandler => _pauseWindowHandler;
    public LevelScene_InputHandler InputHandler => _inputHandler;

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
