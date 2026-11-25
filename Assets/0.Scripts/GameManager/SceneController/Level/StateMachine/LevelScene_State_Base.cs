/*
    LevelScene_State_Base
    20261119  hanaue sho
    LevelScene のステートパターン基底クラス
 */

public class LevelScene_State_Base
{
    protected SceneController_Level _controller;

    public LevelScene_State_Base(SceneController_Level controller)
    {
        _controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}

/*

public class LevelScene_State_xxx : LevelScene_State_Base
{
    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public LevelScene_State_xxx(SceneController_Level controller) : base(controller)
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


 */