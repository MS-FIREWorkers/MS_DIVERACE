/*
    LevelScene_State_FadeIn
    20261119  hanaue sho
    フェードイン待ちクラス
 */

public class LevelScene_State_FadeIn : LevelScene_State_Base
{
    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public LevelScene_State_FadeIn(SceneController_Level controller) : base(controller)
    {
        _controller = controller;
    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter() { }
    public override void Update() { }
    public override void Exit() { }
}
