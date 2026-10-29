
/*
    ObstacleMovement.cs
    20261022  作成者：要記入
    障害物の移動パターンと回転をInspectorから設定する。
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ObstacleMovement : MonoBehaviour
{
    // ==================================================
    // ----- Enum -----
    // ==================================================

    public enum MoveMode
    {
        None,
        PingPong,
        Waypoints,
        Orbit
    }

    public enum OrbitPlane
    {
        XY,
        XZ,
        YZ
    }

    // ==================================================
    // ----- Movement Settings -----
    // ==================================================

    [Header("移動方式")]
    [Tooltip("障害物の移動パターンを設定する。")]
    [SerializeField] private MoveMode _moveMode = MoveMode.PingPong;

    [Tooltip("移動開始までの待機時間（秒）。")]
    [Min(0f)]
    [SerializeField] private float _startDelay = 0f;

    [Header("往復移動")]
    [Tooltip("移動方向。X=左右、Y=上下、Z=前後。")]
    [SerializeField] private Vector3 _moveDirection = Vector3.right;

    [Tooltip("開始位置から移動する最大距離。")]
    [Min(0f)]
    [SerializeField] private float _moveDistance = 5f;

    [Tooltip("移動速度（ローカル単位／秒）。")]
    [Min(0.01f)]
    [SerializeField] private float _moveSpeed = 5f;

    [Tooltip("往復移動の端で減速する。")]
    [SerializeField] private bool _smoothTurn = false;

    // ==================================================
    // ----- Waypoint Settings -----
    // ==================================================

    [Header("経由地点移動")]
    [Tooltip("開始位置からの相対座標。順番に移動する。")]
    [SerializeField]
    private Vector3[] _waypoints =
    {
        Vector3.zero,
        new Vector3(5f, 0f, 0f)
    };

    [Tooltip("最後の地点から最初の地点へ戻る。OFFなら往復する。")]
    [SerializeField] private bool _loopWaypoints = true;

    [Tooltip("各地点に到達した後の待機時間（秒）。")]
    [Min(0f)]
    [SerializeField] private float _waypointWaitTime = 0f;

    // ==================================================
    // ----- Orbit Settings -----
    // ==================================================

    [Header("円運動")]
    [Tooltip("円運動を行う平面。")]
    [SerializeField] private OrbitPlane _orbitPlane = OrbitPlane.XY;

    [Tooltip("円運動の半径。")]
    [Min(0f)]
    [SerializeField] private float _orbitRadius = 5f;

    [Tooltip("円運動の角速度（度／秒）。")]
    [SerializeField] private float _orbitSpeed = 90f;

    // ==================================================
    // ----- Rotation Settings -----
    // ==================================================

    [Header("自転")]
    [Tooltip("障害物を移動しながら自転させる。")]
    [SerializeField] private bool _isRotating = false;

    [Tooltip("各軸の回転速度（度／秒）。")]
    [SerializeField] private Vector3 _rotationSpeed = new Vector3(0f, 90f, 0f);

    // ==================================================
    // ----- Runtime Fields -----
    // ==================================================

    private Rigidbody _rigidbody;
    private Vector3 _startLocalPosition;
    private Quaternion _startLocalRotation;
    private Vector3 _currentLocalPosition;

    private float _elapsedTime;
    private float _delayRemaining;
    private float _waitTimer;
    private int _targetWaypointIndex;
    private int _waypointDirection = 1;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (_rigidbody == null)
        {
            Debug.LogError("[Error] Obstacle Rigidbody not found.", this);
            enabled = false;
            return;
        }

        if (GetComponentInChildren<Collider>() == null)
        {
            Debug.LogWarning("[Warning] Obstacle Collider not found.", this);
        }

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localRotation;
        _currentLocalPosition = _startLocalPosition;
        _delayRemaining = _startDelay;

        if (_moveMode == MoveMode.Waypoints &&
            (_waypoints == null || _waypoints.Length == 0))
        {
            Debug.LogWarning("[Warning] Obstacle waypoints are empty.", this);
        }

        if (_moveMode == MoveMode.PingPong &&
            _moveDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[Warning] Move Direction is zero.", this);
        }
    }

    private void FixedUpdate()
    {
        if (_delayRemaining > 0f)
        {
            _delayRemaining -= Time.fixedDeltaTime;
            return;
        }

        _elapsedTime += Time.fixedDeltaTime;

        // 移動方式に応じて次の座標を計算する。
        switch (_moveMode)
        {
            case MoveMode.PingPong:
                UpdatePingPong();
                break;

            case MoveMode.Waypoints:
                UpdateWaypoints();
                break;

            case MoveMode.Orbit:
                UpdateOrbit();
                break;
        }

        // Rigidbodyを利用して物理演算と同期する。
        if (_moveMode != MoveMode.None)
            _rigidbody.MovePosition(LocalToWorld(_currentLocalPosition));

        if (_isRotating)
        {
            Quaternion localRotation = _startLocalRotation *
                Quaternion.Euler(_rotationSpeed * _elapsedTime);

            Quaternion worldRotation = transform.parent == null
                ? localRotation
                : transform.parent.rotation * localRotation;

            _rigidbody.MoveRotation(worldRotation);
        }
    }

    // ==================================================
    // ----- PingPong Movement -----
    // ==================================================

    private void UpdatePingPong()
    {
        if (_moveDistance <= 0f || _moveDirection.sqrMagnitude < 0.0001f)
            return;

        float distance = Mathf.PingPong(
            _elapsedTime * _moveSpeed, _moveDistance
        );

        float ratio = distance / _moveDistance;

        if (_smoothTurn)
            ratio = Mathf.SmoothStep(0f, 1f, ratio);

        _currentLocalPosition = _startLocalPosition +
            _moveDirection.normalized * (_moveDistance * ratio);
    }

    // ==================================================
    // ----- Waypoint Movement -----
    // ==================================================

    private void UpdateWaypoints()
    {
        if (_waypoints == null || _waypoints.Length == 0)
            return;

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector3 targetPosition = _startLocalPosition +
            _waypoints[_targetWaypointIndex];

        _currentLocalPosition = Vector3.MoveTowards(
            _currentLocalPosition,
            targetPosition,
            _moveSpeed * Time.fixedDeltaTime
        );

        if (Vector3.Distance(_currentLocalPosition, targetPosition) <= 0.001f)
        {
            _waitTimer = _waypointWaitTime;
            UpdateWaypointIndex();
        }
    }

    private void UpdateWaypointIndex()
    {
        int count = _waypoints.Length;
        if (count <= 1) return;

        if (_loopWaypoints)
        {
            _targetWaypointIndex = (_targetWaypointIndex + 1) % count;
            return;
        }

        if (_targetWaypointIndex == count - 1)
            _waypointDirection = -1;
        else if (_targetWaypointIndex == 0)
            _waypointDirection = 1;

        _targetWaypointIndex += _waypointDirection;
    }

    // ==================================================
    // ----- Orbit Movement -----
    // ==================================================

    private void UpdateOrbit()
    {
        float angle = _elapsedTime * _orbitSpeed * Mathf.Deg2Rad;
        _currentLocalPosition = _startLocalPosition + GetOrbitOffset(angle);
    }

    private Vector3 GetOrbitOffset(float angle)
    {
        float x = (Mathf.Cos(angle) - 1f) * _orbitRadius;
        float y = Mathf.Sin(angle) * _orbitRadius;

        switch (_orbitPlane)
        {
            case OrbitPlane.XY:
                return new Vector3(x, y, 0f);

            case OrbitPlane.XZ:
                return new Vector3(x, 0f, y);

            case OrbitPlane.YZ:
                return new Vector3(0f, x, y);
        }

        return Vector3.zero;
    }

    // ==================================================
    // ----- Coordinate Conversion -----
    // ==================================================

    private Vector3 LocalToWorld(Vector3 localPosition)
    {
        return transform.parent == null
            ? localPosition
            : transform.parent.TransformPoint(localPosition);
    }

    // ==================================================
    // ----- Scene Gizmos -----
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying
            ? _startLocalPosition
            : transform.localPosition;

        Gizmos.color = Color.cyan;

        if (_moveMode == MoveMode.PingPong)
        {
            Vector3 direction = _moveDirection.sqrMagnitude > 0.0001f
                ? _moveDirection.normalized
                : Vector3.zero;

            Vector3 start = LocalToWorld(origin);
            Vector3 end = LocalToWorld(origin + direction * _moveDistance);

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.3f);
            Gizmos.DrawWireSphere(end, 0.3f);
        }
        else if (_moveMode == MoveMode.Waypoints && _waypoints != null)
        {
            Vector3 previous = LocalToWorld(origin);

            for (int i = 0; i < _waypoints.Length; i++)
            {
                Vector3 current = LocalToWorld(origin + _waypoints[i]);
                Gizmos.DrawLine(previous, current);
                Gizmos.DrawWireSphere(current, 0.3f);
                previous = current;
            }

            if (_loopWaypoints && _waypoints.Length > 1)
            {
                Gizmos.DrawLine(
                    previous,
                    LocalToWorld(origin + _waypoints[0])
                );
            }
        }
        else if (_moveMode == MoveMode.Orbit)
        {
            const int segments = 40;
            Vector3 previous = LocalToWorld(origin);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 current = LocalToWorld(origin + GetOrbitOffset(angle));

                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
