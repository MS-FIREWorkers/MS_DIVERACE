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
    // ----- Propaty -----
    // --------------------------------------------------
    [Header("Look Ahead")]
    [Tooltip("プレイヤーよりどこまで先を注視点にするか")]
    [SerializeField] private float _horizontalLookAhead = 3.0f;
    [SerializeField] private float _verticalLookAhead = 8.0f;

    [Header("Smooth")]
    [Tooltip("補完時間")]
    [SerializeField] private float _positionSmoothTime = 0.15f;
    [SerializeField] private float _rotationSmoothSpeed = 8.0f;

    // --------------------------------------------------
    // ----- DeadZone -----
    // --------------------------------------------------
    [Header("DeadZone")]
    [Tooltip("デッドゾーン")]
    [SerializeField] private float _horizontalDeadZoneX = 2.0f;
    [SerializeField] private float _horizontalDeadZoneZ = 2.0f;
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

        UpdateFollowPivot(targetData.Position, posture);

        // ----- CameraDistance -----
        float cameraDistance = CalculateCameraDistance(targetData.Velocity);

        // ----- CameraPosition -----
        Vector3 desiredPosition = CalculateCameraPosition(cameraDistance);

        // ----- LookTarget -----
        Vector3 lookTarget = CalculateLookTarget(targetData.Position, posture);

        // ----- Position -----
        UpdatePosition(desiredPosition, deltaTime);
        
        // ----- Rotate -----
        UpdateRotation(lookTarget, targetData.Forward, deltaTime);

        // ----- FOV -----
        CurrentBaseFov = CalculateBaseFov(targetData.Velocity);

        // Debug
        _debugPlayerPosition = targetData.Position;
        _debugLookTarget = lookTarget;

    }

    // --------------------------------------------------
    // ----- Initialize -----
    // --------------------------------------------------
    private void Initialize(CameraTargetData targetData)
    {
        _followPivotPosition = targetData.Position;
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
    private Vector3 CalculateLookTarget(Vector3 playerPosition, float posture)
    {
        float lookAhead = Mathf.Lerp(_horizontalLookAhead, _verticalLookAhead, posture);

        return playerPosition - Vector3.up * lookAhead;
    }
    private float CalculateCameraDistance(Vector3 velocity)
    {
        // 落下速度
        float fallSpeed = Mathf.Max(0.0f, -velocity.y);

        // 速度を０～１に変換
        float speedRatio = Mathf.InverseLerp(_approachStartFallSpeed, _approachMaxFallSpeed, fallSpeed);

        // 最終距離
        float distance = Mathf.Lerp(_maxCameraDistance, _minCameraDistance, speedRatio);

        return distance;
    }
    private float CalculateBaseFov(Vector3 velocity)
    {
        // 落下速度（正の値）
        float fallSpeed = Mathf.Max(0.0f, -velocity.y);

        // 落下速度を 0~1 に変換
        float speedRatio = Mathf.InverseLerp(_fovStartFallSpeed, _fovMaxFallSpeed, fallSpeed);

        // FOV を補間
        return Mathf.Lerp(_minFov, _maxFov, speedRatio);
    }


}
