/*
    LevelScene_State_Countdown
    20261119  hanaue sho
    カウントダウンステート
 */
using UnityEngine;

public class LevelScene_State_Countdown : LevelScene_State_Base
{
    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public LevelScene_State_Countdown(SceneController_Level controller) : base(controller)
    {
        _controller = controller;
    }

    // --------------------------------------------------
    // ----- Timer -----
    // --------------------------------------------------
    private float _elapsedTime;


    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter() 
    {
        _elapsedTime = 0.0f;
        Debug.Log("Countdown Start");
    }
    public override void Update() 
    {
        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= 3.0f)
        {
            _controller.StateMachine.ChangeState(LevelScene_StateMachine.StateType.Racing);
        }
    }
    public override void Exit() { }
}
