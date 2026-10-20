/*
    PlayerState_Damage
    20261010  hanaue sho
    障害物に触れたステート
 */
using UnityEngine;

public class PlayerState_Damage : PlayerState_Base
{
    // --------------------------------------------------
    // ----- ControlData -----
    // --------------------------------------------------
    private readonly PlayerMovement_ControlData _controlData;

    // --------------------------------------------------
    // ----- PlayerImpactInfo -----
    // --------------------------------------------------
    private PlayerImpactInfo _impactInfo;

    // --------------------------------------------------
    // ----- ElapsedTime Propaty -----
    // --------------------------------------------------
    private float _elapsedTime = 0.0f;
    private float _damageDuration = 0.5f;


    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public PlayerState_Damage(Player_CharacterController controller, PlayerMovement_ControlData controlData) : base(controller)
    {
        _controlData = controlData;
    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        // 初期化
        _elapsedTime = 0.0f;

        // 速度を与える
        switch (_impactInfo.ImpactType)
        {
            case PlayerImpactType.Top:
                //Debug.Log("Top");
                CalculateVelocityOnTop();
                break;
            case PlayerImpactType.Side:
                //Debug.Log("Side");
                CalculateVelocityOnSide();
                break;
        }
    }
    public override void Update() 
    {
        // ----- 時間計測 -----
        UpdateDamageTime(Time.deltaTime);

        // ----- 入力を取得 -----
        Vector2 moveInput = _controller.InputHandler.MoveInput;
        float postureInput = _controller.InputHandler.PoseInput;
        float rotateInput = _controller.InputHandler.RotateInput;

        // ----- 速度更新 -----
        _controller.Movement.UpdateVelocity(moveInput, postureInput, _controlData, Time.deltaTime);
        _controller.Movement.RotateYaw(rotateInput, _controlData, Time.deltaTime);
        _controller.CollisionDetector.ResolvePenetration();

        // ----- 移動予定量 -----
        Vector3 displace = _controller.Movement.GetDisplacement(Time.deltaTime);

        // ----- 衝突判定 -----
        if (_controller.CollisionDetector.CheckCollision(displace, out PlayerImpactInfo impactInfo))
        {
            if (impactInfo.ImpactType == PlayerImpactType.Top)
            {
                _controller.StateMachine.StateDamage.SetImpactInfo(impactInfo);
            }
            else if (impactInfo.ImpactType == PlayerImpactType.Side)
            {
                _controller.StateMachine.StateDamage.SetImpactInfo(impactInfo);
            }
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateDamage);
            return;
        }

        // ----- 通常移動 -----
        _controller.Movement.ApplyMovement(displace);

        // ----- Animation -----
        _controller.Animation.RotateTo(postureInput);
    }
    public override void Exit() { }


    // --------------------------------------------------
    // ----- Setter -----
    // --------------------------------------------------
    public void SetImpactInfo(PlayerImpactInfo impactInfo)
    {
        _impactInfo = impactInfo;
    }

    // --------------------------------------------------
    // ----- CalculateVelocity -----
    // --------------------------------------------------
    private void CalculateVelocityOnTop()
    {
        // 真上へ返すだけ
        _controller.Movement.SetVelocity(new Vector3(0.0f, 40.0f, 0.0f));
    }
    private void CalculateVelocityOnSide()
    {
        // 衝突のNormal方向を軸に、現在速度を回転
        Vector3 currentVelocity = _controller.Movement.CurrentVelocity;

        // 水平速度だけ取り出す
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0.0f, currentVelocity.z);

        // 法線を水平化
        Vector3 horizontalNormal = _impactInfo.Normal;
        horizontalNormal.y = 0.0f;
        if (horizontalNormal.sqrMagnitude > Mathf.Epsilon)
        {
            horizontalNormal.Normalize();
        }

        // 法線を基準に水平速度を反射
        Vector3 reflectionHorizontalVelocity = Vector3.Reflect(horizontalVelocity, horizontalNormal);

        // 垂直速度は減衰
        float verticalVelocity = currentVelocity.y * 0.5f;

        // 合成
        Vector3 reflectedVelocity = new Vector3(reflectionHorizontalVelocity.x, verticalVelocity, reflectionHorizontalVelocity.z);

        _controller.Movement.SetVelocity(reflectedVelocity);
    }

    // --------------------------------------------------
    // ----- UpdateTime -----
    // --------------------------------------------------
    private void UpdateDamageTime(float deltaTime)
    {
        _elapsedTime += deltaTime;
        if (_elapsedTime > _damageDuration)
        {
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateFreeFall);
        }
    }
}
