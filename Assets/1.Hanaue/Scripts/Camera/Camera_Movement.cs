/*
    Camera_Movement
    20261017  hanaue sho
    カメラの移動を制御する
 */
using UnityEngine;

public class Camera_Movement : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Transform -----
    // --------------------------------------------------
    [Header("Transform")]
    [SerializeField] private Transform _cameraTransform;

    // --------------------------------------------------
    // ----- Property -----
    // --------------------------------------------------
    [Header("Look Ahead")]
    [Tooltip("プレイヤーよりどこまで先を注視点にするか")]
    [SerializeField] private float _horizontalLookAhead = 8.0f;
    [SerializeField] private float _verticalLookAhead = 8.0f;

    [Header("Smooth")]
    [Tooltip("補完時間")]
    [SerializeField] private float _positionSmoothTime = 0.10f;
    [SerializeField] private float _rotationSmoothSpeed = 8.0f;

    // --------------------------------------------------
    // ----- DeadZone -----
    // --------------------------------------------------
    [Header("DeadZone")]
    [Tooltip("デッドゾーン")]
    [SerializeField] private float _horizontalDeadZoneX = 1.5f;
    [SerializeField] private float _horizontalDeadZoneZ = 1.5f;
    [SerializeField] private float _verticalDeadZoneX = 0.5f;
    [SerializeField] private float _verticalDeadZoneZ = 0.5f;

    // --------------------------------------------------
    // ----- CameraApproach -----
    // --------------------------------------------------
    [Header("Distance")]
    [Tooltip("低速時のプレイヤーとの距離")]
    [SerializeField] private float _maxCameraDistance = 9.0f;
    [Tooltip("高速時のプレイヤーとの距離")]
    [SerializeField] private float _minCameraDistance = 3.0f;
    [Header("approach")]
    [Tooltip("カメラが近づく落下速度の閾値")]
    [SerializeField] private float _approachStartFallSpeed = 30.0f;
    [Tooltip("カメラが近づく落下速度の閾値")]
    [SerializeField] private float _approachMaxFallSpeed = 60.0f;

    // --------------------------------------------------
    // ----- FOV -----
    // --------------------------------------------------
    [Header("FOV")]
    [Tooltip("落下速度によるFOVの最小値")]
    [SerializeField] private float _minFov = 65.0f;
    [Tooltip("落下速度によるFOVの最大値")]
    [SerializeField] private float _maxFov = 80.0f;
    [Tooltip("FOVの変化を開始する落下速度")]
    [SerializeField] private float _fovStartFallSpeed = 30.0f;
    [Tooltip("FOVが最大になる落下速度")]
    [SerializeField] private float _fovMaxFallSpeed = 60.0f;

    // --------------------------------------------------
    // ----- Camera Speed Filter -----
    // --------------------------------------------------
    [Header("Camera Speed Filter")]
    [Tooltip("Distanceの加速・減速追従速度")]
    [SerializeField] private float _distanceAccelerationFollowRate = 100.0f;
    [SerializeField] private float _distanceDecelerationFollowRate = 100.0f;
    [Tooltip("FOVの加速・減速追従速度")]
    [SerializeField] private float _fovAccelerationFollowRate = 100.0f;
    [SerializeField] private float _fovDecelerationFollowRate = 100.0f;

    // --------------------------------------------------
    // ----- WallSide Camera -----
    // --------------------------------------------------
    [Header("WallSide Camera")]
    [Tooltip("WallSide中の壁からの距離")]
    [SerializeField] private float _wallSideDistance = 5.0f;
    [Tooltip("WallSide中のキャラクターのローカルZ軸方向の距離")]
    [SerializeField] private float _wallBackDistance = 3.0f;
    [Tooltip("WallSide中のキャラクターのローカルY軸方向の距離")]
    [SerializeField] private float _wallCameraHeight = 5.0f;

    [Tooltip("WallSide中のデッドゾーン")]
    [SerializeField] private float _wallDeadZone = 2.0f;
    [SerializeField] private float _wallDeadZoneOffset = 0.0f;

    [SerializeField] private float _wallLookAhead = 10.0f;
    [SerializeField] private float _wallLookSideOffset = 0.0f;

    [Header("WallSide Transition")]
    [Tooltip("WallSide中への切り替え時間")]
    [SerializeField] private float _wallBlendDuration = 0.4f;

    // --------------------------------------------------
    // ----- Debug -----
    // --------------------------------------------------
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmo = true;
    [SerializeField] private float _debugDownVectorLength = 10.0f;
    private Vector3 _debugPlayerPosition;
    private Vector3 _debugLookTarget;

    // --------------------------------------------------
    // ----- Runtime -----
    // --------------------------------------------------
    private Vector3 _followPivotPosition;
    private Vector3 _positionVelocity;
    private bool _initialized;
    public float CurrentBaseFov { get; private set; }

    private float _distanceFallSpeed;
    private float _fovFallSpeed;

    private float _wallSideBlend;
    private Vector3 _wallFollowPivotPosition;
    private Vector3 _lastWallNormal;
    private Vector3 _lastWallForward;
    private bool _wasWallSide;

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        if (_cameraTransform == null)
        {
            _cameraTransform = transform;
        }
    }
    private void OnDrawGizmos()
    {
        if (!_showDebugGizmo)
        {
            return;
        }

        // ----- Player の真下方向 -----
        Gizmos.color = Color.blue;
        Vector3 downEnd = _debugPlayerPosition + Vector3.down * _debugDownVectorLength;
        Gizmos.DrawLine(_debugPlayerPosition, downEnd);

        // ----- LookTarget -----
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_debugLookTarget, 0.3f);

        // ----- LookTarget -----
        if (_cameraTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_cameraTransform.position, _debugLookTarget);
        }
    }

    // --------------------------------------------------
    // ----- UpdateCamera -----
    // --------------------------------------------------
    public void UpdateCamera(CameraTargetData targetData, float deltaTime)
    {
        if (!_initialized)
        {
            Initialize(targetData);
        }

        float posture = Mathf.Clamp01(targetData.Posture);

        // ----- Normal Follow Pivot -----
        UpdateFollowPivot(targetData.Position, posture);

        // ----- Fall Speed Filter -----
        float actualFallSpeed = Mathf.Max(0.0f, -targetData.Velocity.y);
        float targetDistanceSpeed = Mathf.Min(actualFallSpeed, _approachMaxFallSpeed);
        float targetFovSpeed = Mathf.Min(actualFallSpeed, _fovMaxFallSpeed);
        _distanceFallSpeed = CalculateFilteredFallSpeed(_distanceFallSpeed, targetDistanceSpeed, _distanceAccelerationFollowRate, _distanceDecelerationFollowRate, deltaTime);
        _fovFallSpeed = CalculateFilteredFallSpeed(_fovFallSpeed, targetFovSpeed, _fovAccelerationFollowRate, _fovDecelerationFollowRate, deltaTime);


        // ----- Normal Camera -----
        float cameraDistance = CalculateCameraDistance(_distanceFallSpeed);
        Vector3 normalPosition = CalculateCameraPosition(cameraDistance);
        Vector3 normalLookTarget = CalculateLookTarget(targetData.Position, posture);

        // ----- WallSide Camera -----
        if (targetData.IsWallSide)
        {
            Vector3 normal = targetData.WallNormal;
            normal.y = 0.0f;
            if (normal.sqrMagnitude > Mathf.Epsilon)
            {
                _lastWallNormal = normal.normalized;
            }

            Vector3 forward = Vector3.ProjectOnPlane(targetData.Forward, Vector3.up);
            if (forward.sqrMagnitude > Mathf.Epsilon)
            {
                _lastWallForward = Vector3.ProjectOnPlane(forward.normalized, _lastWallNormal).normalized;
            }

            // WallSideに入った直後だけ初期化
            if (!_wasWallSide)
            {
                _wallFollowPivotPosition = targetData.Position;
            }

            UpdateWallSidePivot(targetData.Position, _lastWallForward);
        }
        else
        {
            // 離脱後もY座標だけは落下に追従
            _wallFollowPivotPosition.y = targetData.Position.y;
        }

        _wasWallSide = targetData.IsWallSide;

        // ----- WallSide Blend -----
        float targetBlend = targetData.IsWallSide ? 1.0f : 0.0f;
        float blendStep = _wallBlendDuration > 0.0f ? deltaTime / _wallBlendDuration : 1.0f;

        _wallSideBlend = Mathf.MoveTowards(_wallSideBlend, targetBlend, blendStep);

        // ----- Camera Position / LookTarget -----
        Vector3 desiredPosition = normalPosition;
        Vector3 lookTarget = normalLookTarget;
        if (_wallSideBlend > 0.0f)
        {
            // 通常カメラとスライドカメラを補間
            Vector3 wallPosition = CalculateWallSideCameraPosition();
            Vector3 wallLookTarget = CalculateWallSideLookTarget(targetData.Position);

            desiredPosition = Vector3.Lerp(normalPosition, wallPosition, _wallSideBlend);
            lookTarget = Vector3.Lerp(normalLookTarget, wallLookTarget, _wallSideBlend);
        }

        // ----- Apply -----
        UpdatePosition(desiredPosition, deltaTime);
        UpdateRotation(lookTarget, targetData.Forward, deltaTime);

        // ----- FOV -----
        CurrentBaseFov = CalculateBaseFov(_fovFallSpeed);


        // ----- Debug -----
        _debugPlayerPosition = targetData.Position;
        _debugLookTarget = lookTarget;
    }

    // --------------------------------------------------
    // ----- Initialize -----
    // --------------------------------------------------
    private void Initialize(CameraTargetData targetData)
    {
        _followPivotPosition = targetData.Position;

        float fallSpeed = Mathf.Max(0.0f, -targetData.Velocity.y);

        _distanceFallSpeed = Mathf.Min(fallSpeed, _approachMaxFallSpeed);

        _fovFallSpeed = Mathf.Min(fallSpeed, _fovMaxFallSpeed);

        _initialized = true;
    }

    // --------------------------------------------------
    // ----- Update -----
    // --------------------------------------------------
    private void UpdateFollowPivot(Vector3 targetPosition, float posture)
    {
        // Y方向は常に追従
        _followPivotPosition.y = targetPosition.y;

        // デッドゾーン計算
        float deadZoneX = Mathf.Lerp(_horizontalDeadZoneX, _verticalDeadZoneX, posture);
        float deadZoneZ = Mathf.Lerp(_horizontalDeadZoneZ, _verticalDeadZoneZ, posture);

        // X
        float differenceX = targetPosition.x - _followPivotPosition.x;
        if (differenceX > deadZoneX)
        {
            _followPivotPosition.x = targetPosition.x - deadZoneX;
        }
        else if ( differenceX < -deadZoneX)
        {
            _followPivotPosition.x = targetPosition.x + deadZoneX;
        }

        // Z
        float differenceZ = targetPosition.z - _followPivotPosition.z;
        if (differenceZ > deadZoneZ)
        {
            _followPivotPosition.z = targetPosition.z - deadZoneZ;
        }
        else if (differenceZ < -deadZoneZ)
        {
            _followPivotPosition.z = targetPosition.z + deadZoneZ;
        }
    }
    private void UpdateWallSidePivot(Vector3 playerPosition, Vector3 wallForward)
    {
        // Y座標は常にプレイヤーへ追従
        _wallFollowPivotPosition.y = playerPosition.y;

        // デッドゾーンの中心をオフセット
        Vector3 targetPosition = playerPosition - wallForward * _wallDeadZoneOffset;

        // 壁に沿う方向の距離
        float difference = Vector3.Dot(targetPosition - _wallFollowPivotPosition, wallForward);

        if (difference > _wallDeadZone)
        {
            _wallFollowPivotPosition += wallForward * (difference - _wallDeadZone);
        }
        else if (difference < -_wallDeadZone)
        {
            _wallFollowPivotPosition += wallForward * (difference + _wallDeadZone);
        }

        // 壁の法線方向は常に追従
        Vector3 normalOffset = Vector3.Project(playerPosition - _wallFollowPivotPosition, _lastWallNormal);
        _wallFollowPivotPosition += normalOffset;
    }
    private void UpdatePosition(Vector3 targetPosition, float deltaTime)
    {
        Vector3 currentPosition = _cameraTransform.position;

        // XZ だけ SmoothDamp
        Vector3 currentHorizontal = new Vector3(currentPosition.x, 0.0f, currentPosition.z);
        Vector3 targetHorizontal = new Vector3(targetPosition.x, 0.0f, targetPosition.z);

        Vector3 smoothHorizontal = Vector3.SmoothDamp(
                                                       currentHorizontal,
                                                       targetHorizontal,
                                                       ref _positionVelocity,
                                                       _positionSmoothTime,
                                                       Mathf.Infinity,
                                                       deltaTime
                                                      );
        // Y は完全一致
        _cameraTransform.position = new Vector3( smoothHorizontal.x, targetPosition.y, smoothHorizontal.z );

    }
    private void UpdateRotation(Vector3 lookTarget, Vector3 screenUp, float deltaTime)
    {
        Vector3 direction = lookTarget - _cameraTransform.position;

        if(direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, screenUp);

        _cameraTransform.rotation = Quaternion.Slerp(
                                                     _cameraTransform.rotation,
                                                     targetRotation,
                                                     1.0f - Mathf.Exp(-_rotationSmoothSpeed * deltaTime)
                                                    );
    }

    // --------------------------------------------------
    // ----- Calculate -----
    // --------------------------------------------------
    private Vector3 CalculateCameraPosition(float distance)
    {
        return _followPivotPosition + Vector3.up * distance;
    }
    private Vector3 CalculateWallSideCameraPosition()
    {
        return _wallFollowPivotPosition
            + _lastWallNormal * _wallSideDistance
            - _lastWallForward * _wallBackDistance
            + Vector3.up * _wallCameraHeight;
    }
    private Vector3 CalculateLookTarget(Vector3 playerPosition, float posture)
    {
        float lookAhead = Mathf.Lerp(_horizontalLookAhead, _verticalLookAhead, posture);

        return playerPosition - Vector3.up * lookAhead;
    }
    private Vector3 CalculateWallSideLookTarget(Vector3 playerPosition)
    {
        return playerPosition
            + _lastWallNormal * _wallLookSideOffset
            + Vector3.down * _wallLookAhead;
    }
    private float CalculateCameraDistance(float fallSpeed)
    {
        // 速度を０～１に変換
        float speedRatio = Mathf.InverseLerp(_approachStartFallSpeed, _approachMaxFallSpeed, fallSpeed);

        // 最終距離
        float distance = Mathf.Lerp(_maxCameraDistance, _minCameraDistance, speedRatio);

        return distance;
    }
    private float CalculateBaseFov(float fallSpeed)
    {
        // 落下速度を 0~1 に変換
        float speedRatio = Mathf.InverseLerp(_fovStartFallSpeed, _fovMaxFallSpeed, fallSpeed);

        // FOV を補間
        return Mathf.Lerp(_minFov, _maxFov, speedRatio);
    }
    private float CalculateFilteredFallSpeed(float currentSpeed, float targetSpeed, float accelerationRate, float decelerationRate, float deltaTime)
    {
        float followRate = targetSpeed > currentSpeed ? accelerationRate : decelerationRate;

        return Mathf.MoveTowards(currentSpeed, targetSpeed, Mathf.Max(0.0f, followRate) * deltaTime);
    }

}
