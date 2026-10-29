/*
    PlayerState_Base
    20261004  hanaue sho
    PlayerCharacter のステートパターン基底クラス
 */
public abstract class PlayerState_Base
{
    protected Player_CharacterController _controller;

    public PlayerState_Base(Player_CharacterController controller)
    {
        _controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}


/*
public class PlayerState_xxx : PlayerState_Base
{
    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public PlayerState_xxx(Player_CharacterController controller) : base(controller)
    {
    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter() { }
    public override void Update() { }
    public override void Exit() { }
}


 */