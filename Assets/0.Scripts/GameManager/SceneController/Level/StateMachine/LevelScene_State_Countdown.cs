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
    private const float COUNTDOWN_TIME = 3.0f;
    private float _elapsedTime;


    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter() 
    {
        _elapsedTime = 0.0f;
        Debug.Log("Countdown Start");

        // カウントダウン表示
        _controller.CountdownText.gameObject.SetActive(true);
        _controller.CountdownText.SetText("{0}", COUNTDOWN_TIME);
    }
    public override void Update() 
    {
        _elapsedTime += Time.deltaTime;


        if (_elapsedTime >= COUNTDOWN_TIME)
        {
            _controller.StateMachine.ChangeState(LevelScene_StateMachine.StateType.Racing);
            return;
        }

        // 残り時間を整数に変換
        int count = Mathf.CeilToInt(COUNTDOWN_TIME - _elapsedTime);
        // テキスト更新
        _controller.CountdownText.SetText("{0}", count);
    }
    public override void Exit()
    {
        // カウントダウン非表示
        _controller.CountdownText.gameObject.SetActive(false);
    }
}
