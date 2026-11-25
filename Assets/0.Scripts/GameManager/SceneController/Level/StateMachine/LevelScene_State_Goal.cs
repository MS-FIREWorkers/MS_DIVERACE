/*
    LevelScene_State_Goal
    20261119  hanaue sho
    ゴールステート
 */
using UnityEngine;

public class LevelScene_State_Goal : LevelScene_State_Base
{
    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public LevelScene_State_Goal(SceneController_Level controller) : base(controller)
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
        _elapsedTime = 0.0f;
        Debug.Log("Goal" + _controller.Timer.ElapsedTime);
    }
    public override void Update() 
    {
        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= 2.0f)
        {
            // シーン遷移
            //Debug.Log("Scene Transition");
            _controller.ChangeSceneResult();
        }
    }
    public override void Exit() { }
}
