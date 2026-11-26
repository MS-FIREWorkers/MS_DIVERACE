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
        // ----- 再接触壁の計測 -----
        _controller.StateMachine.StateWallSide.UpdateReentryBlock(Time.deltaTime);

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
            // イベント発火
            _controller.NotifyObstacleCollision(impactInfo.ImpactType);

            // ----- TopEdgeGraze -----
            if (impactInfo.ImpactType == PlayerImpactType.TopEdgeGraze)
            {
                // 障害物の外側へ抜けるための補正量
                Vector3 correction = _controller.CollisionDetector.CalculateTopEdgeGrazeCorrection(impactInfo, displace);

                // 移動予定量に補正を加える
                Vector3 correctedDisplacement = displace + correction;

                // 速度を変更せずに移動
                _controller.Movement.ApplyMovement(correctedDisplacement);
                return;
            }

            // ----- Side → WallSide -----
            if (impactInfo.ImpactType == PlayerImpactType.Side)
            {
                PlayerState_WallSide wallSideState = _controller.StateMachine.StateWallSide;

                if (wallSideState.CanEnter(impactInfo))
                {
                    _controller.Movement.CancelBoost();

                    wallSideState.SetImpactInfo(impactInfo);
                    _controller.StateMachine.ChangeState(wallSideState);
                    return;
                }
            }

            // ----- Damage -----
            _controller.Movement.CancelBoost();

            _controller.StateMachine.StateDamage.SetImpactInfo(impactInfo);
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateDamage);
            
            return;
        }

        // WallSide 判定
        if (_controller.CollisionDetector.TryGetWallSideCandidate(out PlayerImpactInfo wallImpactInfo))
        {
            PlayerState_WallSide wallSideState = _controller.StateMachine.StateWallSide;

            if (wallSideState.CanEnter(wallImpactInfo))
            {
                // 壁面情報を渡す
                wallSideState.SetImpactInfo(wallImpactInfo);

                // WallSideへ遷移
                _controller.StateMachine.ChangeState(wallSideState);
                return;
            }
        }


        // ----- 通常移動 -----
        _controller.Movement.ApplyMovement(displace);

        // ----- Animation -----
        _controller.Animation.RotateTo(postureInput);
        _controller.Animation.SetPose(postureInput);
    }
    public override void Exit() { }

}
