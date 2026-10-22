/*
    BoostRing_Controller
    20261020  hanaue sho
    加速リングのクラス
 */
using UnityEngine;

public struct BoostRequest
{
    public Vector3 Direction;
    public float TargetSpeed;
    public float Acceleration;
    public float Duration;
}


public class BoostRing_Controller : MonoBehaviour
{
    // --------------------------------------------------
    // ----- BoosRingData -----
    // --------------------------------------------------
    [Header("Data")]
    [SerializeField] private BoostRingData _data;
    [SerializeField] private float _effectiveRadius = 2.0f;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        if (_data == null)
        {
            Debug.LogWarning("BoostRingData Not Found !!");
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_data == null)
        {
            return;
        }

        Player_CharacterController player = other.GetComponentInParent<Player_CharacterController>();
        if (player == null)
        {
            return;
        }

        // リング中心からのローカル座標
        Vector3 localPosition = transform.InverseTransformPoint(other.bounds.center);

        // リング平面上の中心距離
        float distance = new Vector2(localPosition.x, localPosition.y).magnitude;

        // 有効範囲
        if (distance > _effectiveRadius)
        {
            return;
        }

        // 0 = 中心、1 = 外周
        float normalizedDistance = Mathf.Clamp01(distance/ _effectiveRadius);

        float strength = Mathf.Max(0.0f, _data.StrengthCurve.Evaluate(normalizedDistance));

        BoostRequest request = new BoostRequest
        {
            Direction = transform.forward,
            TargetSpeed = _data.TragetSpeed * strength,
            Acceleration = _data.Acceleration,
            Duration = _data.Duration
        };

        // リクエスト
        player.ApplyBoost(request);
    }
}
