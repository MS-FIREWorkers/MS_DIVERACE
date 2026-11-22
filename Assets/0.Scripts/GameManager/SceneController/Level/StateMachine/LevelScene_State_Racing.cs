/*
    LevelScene_State_Racing
    20261119  hanaue sho
    レース中ステート
    レース中からゴールステートへの遷移はLevelScene_GoalTriggerを経由する
 */
using UnityEngine;

public class LevelScene_State_Racing : LevelScene_State_Base
{
    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public LevelScene_State_Racing(SceneController_Level controller) : base(controller)
    {
        _controller = controller;
    }


    // --------------------------------------------------
    // ----- Debug -----
    // --------------------------------------------------
    private float _elapsedTime;

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        // 計測開始
        _controller.Timer.ResetTimer();
        _controller.Timer.StartTimer();

        _controller.Player.StartPlay();
        //Debug.Log("Race Start");
    }
    public override void Update()
    {

    }
    public override void Exit()
    {
        // 計測終了
        _controller.Timer.StopTimer();
    }
}
