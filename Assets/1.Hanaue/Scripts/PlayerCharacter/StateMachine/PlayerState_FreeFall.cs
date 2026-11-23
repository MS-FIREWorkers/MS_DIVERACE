/*
    PlayerState_FreeFall
    20261004  hanaue sho
 */
using UnityEngine;

public class PlayerState_FreeFall : PlayerState_Base
{
    // --------------------------------------------------
    // ----- ControlData -----
    // --------------------------------------------------
    private readonly PlayerMovement_ControlData _controlData;

    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public PlayerState_FreeFall(Player_CharacterController controller, PlayerMovement_ControlData controlData) : base(controller)
    {
        _controlData = controlData;
    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter() { }
    public override void Update() 
    {
        // ----- 入力を取得 -----
        Vector2 moveInput = _controller.InputHandler.MoveInput;
        float postureInput = _controller.InputHandler.PoseInput;
        float rotateInput = _controller.InputHandler.RotateInput;

        // ----- 速度更新 -----
        _controller.Movement.UpdateVelocity(moveInput, postureInput, _controlData,  Time.deltaTime);
        _controller.Movement.RotateYaw(rotateInput, _controlData, Time.deltaTime);
        _controller.CollisionDetector.ResolvePenetration();

        // ----- 移動予定量 -----
        Vector3 displace = _controller.Movement.GetDisplacement(Time.deltaTime);

        // ----- 衝突判定 -----
        if (_controller.CollisionDetector.CheckCollision(displace, out PlayerImpactInfo impactInfo))
        {
            _controller.Movement.CancelBoost();

            // WallSide の条件を確認
            if (_controller.StateMachine.StateWallSide.CanEnter(impactInfo))
            {
                _controller.StateMachine.StateWallSide.SetImpactInfo(impactInfo);
                _controller.StateMachine.ChangeState(_controller.StateMachine.StateWallSide);
            }
            else
            {
                _controller.StateMachine.StateDamage.SetImpactInfo(impactInfo);
                _controller.StateMachine.ChangeState(_controller.StateMachine.StateDamage);
            }
            return;
        }

        // ----- 通常移動 -----
        _controller.Movement.ApplyMovement(displace);

        // ----- Animation -----
        _controller.Animation.RotateTo(postureInput);
    }
    public override void Exit() { }

}
