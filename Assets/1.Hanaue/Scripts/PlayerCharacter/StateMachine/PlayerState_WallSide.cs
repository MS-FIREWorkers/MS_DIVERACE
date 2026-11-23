/*
    PlayerState_WallSide
    20261122  hanaue sho
    障害物の側面を滑るステート
 */
using UnityEngine;

public class PlayerState_WallSide : PlayerState_Base
{
    // --------------------------------------------------
    // ----- PlayerImpactInfo -----
    // --------------------------------------------------
    private PlayerImpactInfo _impactInfo;

    // --------------------------------------------------
    // ----- ControlData -----
    // --------------------------------------------------
    private readonly PlayerMovement_ControlData _controlData;

    // --------------------------------------------------
    // ----- WallSide Movement Parameter -----
    // --------------------------------------------------
    private float _horizontalDragMultiplier = 0.05f; // 摩擦減衰率
    private float _extraFallAcceleration = 10.0f; // 追加加速度

    // --------------------------------------------------
    // ----- WallSide Condition -----
    // --------------------------------------------------
    private readonly float _maxPosture = 0.7f;
    private readonly float _minSideDot = 0.7f;

    // --------------------------------------------------
    // ----- WallSide Exit Condition -----
    // --------------------------------------------------
    private readonly float _exitPostureThreshold = 0.5f;

    // --------------------------------------------------
    // ----- Input Exit Parameter -----
    // --------------------------------------------------
    private readonly float _exitInputDuration = 0.2f;
    private readonly float _exitInputThreshold = 0.5f;

    private float _exitInputElapsedTime;

    // --------------------------------------------------
    // ----- Exit Parameter -----
    // --------------------------------------------------
    private readonly float _exitSpeed = 5.0f;   // 離脱時最大追加速度
    private readonly float _wallCollisionExitSpeed = 5.0f; // 衝突時追加速度 

    // --------------------------------------------------
    // ----- Slide Bonus Parameter -----
    // --------------------------------------------------
    private readonly float _minSlideTime = 1.0f;
    private readonly float _maxSlideTime = 5.0f;
    private readonly float _maxExitBonusSpeed = 30.0f;

    private float _slideElapsedTime = 0.0f;

    // --------------------------------------------------
    // ----- Wall Contact -----
    // --------------------------------------------------
    private readonly float _targetWallGap = 0.02f;
    private readonly float _maxWallGap = 0.15f;

    private Collider _wallCollider;
    private Vector3 _wallNormal;


    // --------------------------------------------------
    // ----- WallSideExitType -----
    // --------------------------------------------------
    public enum WallSideExitType
    {
        Input,
        Posture,
        Edge,
        Collision,
    }


    // --------------------------------------------------
    // ----- Constructor -----
    // --------------------------------------------------
    public PlayerState_WallSide(Player_CharacterController controller, PlayerMovement_ControlData controlData) : base(controller)
    {
        _controlData = controlData;
    }

    // --------------------------------------------------
    // ----- Lifecycle -----
    // --------------------------------------------------
    public override void Enter()
    {
        _wallCollider = _impactInfo.Collider;

        _wallNormal = _impactInfo.Normal;
        _wallNormal.y = 0.0f;
        _wallNormal.Normalize();

        // 壁に対してキャラクターの角度を補正
        AlignRotationToWall();

        // 壁面法線方向の速度を除去
        _controller.Movement.ConstrainVelocityToWall(_wallNormal);

        _slideElapsedTime = 0.0f;
        _exitInputElapsedTime = 0.0f;

        Debug.Log("WallSide Enter");
    }

    public override void Update()
    {
        // ----- 入力取得 -----
        Vector2 moveInput = _controller.InputHandler.MoveInput;
        float postureInput = _controller.InputHandler.PoseInput;

        // ----- スライド時間計測 -----
        _slideElapsedTime = Mathf.Min(_slideElapsedTime + Time.deltaTime, _maxSlideTime);

        // ----- 姿勢変更による離脱 -----
        if (CheckPostureExit(postureInput))
        {
            LeaveWall(WallSideExitType.Posture);
            return;
        }
        // ----- 入力による離脱 -----
        if (CheckInputExit(moveInput, Time.deltaTime))
        {
            LeaveWall(WallSideExitType.Input);
            return;
        }

        // ----- 壁面接触確認 -----
        if (!UpdateWallContact())
        {
            LeaveWall(WallSideExitType.Edge);
            return;
        }

        // ----- 速度更新 -----
        _controller.Movement.UpdateWallSideVelocity(moveInput, postureInput, _controlData, _wallNormal, _horizontalDragMultiplier, _extraFallAcceleration, Time.deltaTime);

        // ----- 移動予定量 -----
        Vector3 displacement = _controller.Movement.GetDisplacement(Time.deltaTime);

        // ----- 他の障害物との衝突判定 -----
        if (_controller.CollisionDetector.CheckCollision(displacement, out PlayerImpactInfo impactInfo))
        {
            Vector3 additionalVelocity = _wallNormal * _wallCollisionExitSpeed;

            _controller.StateMachine.StateDamage.SetImpactInfo(impactInfo, additionalVelocity);
            _controller.StateMachine.ChangeState(_controller.StateMachine.StateDamage);

            return;
        }

        // ----- 移動 -----
        _controller.Movement.ApplyMovement(displacement);

        // ----- 移動後の接触確認 -----
        if (!UpdateWallContact())
        {
            LeaveWall(WallSideExitType.Edge);
            return;
        }
    }

    public override void Exit()
    {
        _wallCollider = null;
        _wallNormal = Vector3.zero;
        Debug.Log("WallSide Exit");
    }

    // --------------------------------------------------
    // ----- Setter -----
    // --------------------------------------------------
    public void SetImpactInfo(PlayerImpactInfo impactInfo)
    {
        _impactInfo = impactInfo;
    }

    // --------------------------------------------------
    // ----- Check WallSide -----
    // --------------------------------------------------
    public bool CanEnter(PlayerImpactInfo impactInfo)
    {
        // 側面衝突以外は不可
        if (impactInfo.ImpactType != PlayerImpactType.Side)
        {
            return false;
        }

        // 姿勢判定（1=鉛直、0=水平）
        float posture = _controller.InputHandler.PoseInput;
        if (posture < _maxPosture)
        {
            return false;
        }

        // 壁面法線を水平化
        Vector3 wallNormal = impactInfo.Normal;
        wallNormal.y = 0.0f;
        if (wallNormal.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        wallNormal.Normalize();

        // プレイヤーの右方向
        Vector3 playerRight = _controller.transform.right;
        playerRight.y = 0.0f;
        playerRight.Normalize();

        // 左右どちらかの腕が壁に向いているか
        float sideDot = Mathf.Abs(Vector3.Dot(playerRight, wallNormal));
        if (sideDot < _minSideDot)
        {
            return false;
        }

        return true;
    }

    // --------------------------------------------------
    // ----- Align Rotation To Wall -----
    // 進入時の左右関係を維持してYaw角度を補正
    // --------------------------------------------------
    private void AlignRotationToWall()
    {
        Vector3 currentRight = _controller.transform.right;
        currentRight.y = 0.0f;
        currentRight.Normalize();

        // 右腕側と左腕側のどちらが壁に近いか
        float sideDot = Vector3.Dot(currentRight, _wallNormal);

        // 右腕側なら右方向を壁側へ、左腕側なら左方向を壁側へ
        Vector3 targetRight = sideDot < 0.0f ? -_wallNormal : _wallNormal;

        // 右方向から前方向を算出
        Vector3 targetForward = Vector3.Cross(targetRight, Vector3.up);

        _controller.transform.rotation = Quaternion.LookRotation(targetForward, Vector3.up);
    }

    // --------------------------------------------------
    // ----- Update Wall Contact -----
    // 接触判定を行い、壁との距離を補正
    // --------------------------------------------------
    private bool UpdateWallContact()
    {
        bool isContact = _controller.CollisionDetector.CheckWallContact(_wallCollider, _wallNormal, _maxWallGap, out float gap);
        if (!isContact)
        {
            return false;
        }

        _controller.Movement.CorrectWallGap(_wallNormal, gap, _targetWallGap);

        return true;
    }

    // --------------------------------------------------
    // ----- Leave Wall -----
    // --------------------------------------------------
    private void LeaveWall(WallSideExitType exitType)
    {
        // 入力による離脱時のみ外側へ速度を付与
        if (exitType == WallSideExitType.Input)
        {
            _controller.Movement.ApplyWallExitVelocity(_wallNormal, _exitSpeed);
        }

        // ----- 離脱時の追加加速 -----
        if (exitType == WallSideExitType.Input || exitType == WallSideExitType.Edge)
        {
            float bonusSpeed = CalculateExitBonusSpeed();
            _controller.Movement.ApplyImpulse(Vector3.down * bonusSpeed);
        }

        _controller.StateMachine.ChangeState(_controller.StateMachine.StateFreeFall);
    }

    // --------------------------------------------------
    // ----- Check Posture Exit -----
    // --------------------------------------------------
    private bool CheckPostureExit(float postureInput)
    {
        return postureInput <= _exitPostureThreshold;
    }
    // --------------------------------------------------
    // ----- Check Input Exit -----
    // --------------------------------------------------
    private bool CheckInputExit(Vector2 moveInput, float deltaTime)
    {
        // 入力をワールド方向へ変換
        Vector3 inputDirection =_controller.transform.right * moveInput.x +
                                _controller.transform.forward * moveInput.y;

        inputDirection.y = 0.0f;

        // 壁から離れる方向への入力成分
        float outwardInput = Vector3.Dot(inputDirection, _wallNormal);

        if (outwardInput >= _exitInputThreshold)
        {
            _exitInputElapsedTime += deltaTime;
        }
        else
        {
            _exitInputElapsedTime = 0.0f;
        }

        return _exitInputElapsedTime >= _exitInputDuration;
    }

    // --------------------------------------------------
    // ----- Calculate Exit Bonus Speed -----
    // --------------------------------------------------
    private float CalculateExitBonusSpeed()
    {
        if (_maxSlideTime <= 0.0f)
        {
            return _maxExitBonusSpeed;
        }

        float time = Mathf.Max(0.0f, _slideElapsedTime - _minSlideTime);
        
        float ratio = Mathf.Clamp01(time / _maxSlideTime - _minSlideTime);

        return _maxExitBonusSpeed * ratio;
    }
}
