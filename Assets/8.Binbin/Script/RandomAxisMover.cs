/*
    RandomAxisMover.cs
    20261125  hanaue sho
    一定範囲内のランダムな座標に繰り返し移動するスクリプト
    起動時の座標を基準にする
 */
using UnityEngine;


public class RandomAxisMover : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Parameters -----
    // --------------------------------------------------
    public enum MoveAxis
    {
        X,
        Y,
        Z
    }
    [Header("移動軸")]
    [SerializeField] private MoveAxis _moveAxis = MoveAxis.X;
    [Header("最小値、最大値")]
    [SerializeField] private float _minOffset = -5.0f;
    [SerializeField] private float _maxOffset = 5.0f;
    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 3.0f;
    [Header("ランダムシード値")]
    [SerializeField] private int _seed = 12345;

    // --------------------------------------------------
    // ----- Runtime -----
    // --------------------------------------------------
    private Vector3 _initialPosition;
    private Vector3 _targetPosition;
    private System.Random _random;

    // ==================================================
    // ----- UnityEvents -----
    // ==================================================
    private void Start()
    {
        // ランダムシード値生成
        _random = new System.Random(_seed);

        _initialPosition = transform.position;
        SetRandomTargetPosition();
    }
    private void Update()
    {
        // 目標座標へ移動
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);

        // 目標座標に到達したら次の目標座標を設定
        if (transform.position == _targetPosition)
        {
            SetRandomTargetPosition();
        }
    }
    private void OnValidate()
    {
        _maxOffset = Mathf.Max(_minOffset, _maxOffset);
        _moveSpeed = Mathf.Max(0.0f, _moveSpeed);
    }

    // ==================================================
    // ----- 目標座標の設定 -----
    // ==================================================
    private void SetRandomTargetPosition()
    {
        float offset = Mathf.Lerp(_minOffset, _maxOffset, (float)_random.NextDouble());

        _targetPosition = _initialPosition;

        switch (_moveAxis)
        {
            case MoveAxis.X:
                _targetPosition.x += offset;
                break;

            case MoveAxis.Y:
                _targetPosition.y += offset;
                break;

            case MoveAxis.Z:
                _targetPosition.z += offset;
                break;
        }
    }
}