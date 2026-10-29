/*
    Player_CollisionDetector
    20261007  hanaue sho
 */
using UnityEngine;

public enum PlayerImpactType
{
    None,
    Top,
    TopEdge,
    Side,
}
public struct PlayerImpactInfo
{
    public PlayerImpactType ImpactType;

    public Vector3 Point;
    public Vector3 Normal;

    public Collider Collider;

    public float Distance;

    // TopEdge時の反発方向（水平・正規化済み）
    public Vector3 EdgeDirection;
}

public class Player_CollisionDetector : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Collider -----
    // --------------------------------------------------
    [Header("Collider")]
    [SerializeField] private BoxCollider _boxCollider;

    // --------------------------------------------------
    // ----- Collision Parameter -----
    // --------------------------------------------------
    [Header("Collision Parameter")]
    [SerializeField] private float _skinWidth = 0.02f;
    [SerializeField, Range(-1.0f, 1.0f)] private float _topNormalThreshold = 0.6f;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        if (_boxCollider == null)
        {
            _boxCollider = GetComponentInChildren<BoxCollider>();
        }
        if (_boxCollider == null)
        {
            Debug.LogError("[Error] BoxCollider Not Found !!");
        }
    }

    // --------------------------------------------------
    // ----- Check Collision -----
    // --------------------------------------------------
    public bool CheckCollision(Vector3 displacement, out PlayerImpactInfo impactInfo)
    {
        impactInfo = default;

        float distance = displacement.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 direction = displacement.normalized;

        // BoxCollider の中心をワールド座標へ変換
        Vector3 center = _boxCollider.transform.TransformPoint(_boxCollider.center);

        // Scale 込みの HalfExtents
        Vector3 lossyScale = _boxCollider.transform.lossyScale;
        Vector3 halfExtents = _boxCollider.size * 0.5f;
        halfExtents = Vector3.Scale(halfExtents, new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));

        Quaternion orientation = _boxCollider.transform.rotation;

        // BoxCast
        RaycastHit[] hits = Physics.BoxCastAll(
                                               center,
                                               halfExtents,
                                               direction,
                                               orientation,
                                               distance + _skinWidth,
                                               ~0,
                                               QueryTriggerInteraction.Ignore
                                               );
        RaycastHit? nearestHit = null;
        float nearestDistance = float.MaxValue;
        
        foreach (RaycastHit hit in hits)
        {

            // 自分自身は無視
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!IsTargetCollision(hit.collider))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
            }
        }
        if (!nearestHit.HasValue)
        {
            return false;
        }
        RaycastHit targetHit = nearestHit.Value;

        // 衝突時点のプレイヤーCollider中心
        Vector3 impactCenter = center + direction * targetHit.distance;
        // 衝突タイプと反発方向を取得
        PlayerImpactType impactType = JudgeImpactType(
                                                      targetHit.normal,
                                                      targetHit.collider,
                                                      impactCenter,
                                                      out Vector3 edgeDirection
                                                     );

        // PlayerImpactType の構築
        impactInfo = new PlayerImpactInfo
        {
            ImpactType = impactType,

            Point = targetHit.point,
            Normal = targetHit.normal,

            Collider = targetHit.collider,

            Distance = targetHit.distance,

            EdgeDirection = edgeDirection,
        };

        return true;
    }
    private bool IsTargetCollision(Collider target)
    {
        return
            target.CompareTag("Obstacle") ||
            target.CompareTag("Terrain");
    }
    private PlayerImpactType JudgeImpactType(Vector3 normal, Collider targetCollider, Vector3 impactCenter, out Vector3 edgeDirection)
    {
        edgeDirection = Vector3.zero;

        float upDot = Vector3.Dot(normal.normalized, Vector3.up);
        // 上面以外の衝突
        if (upDot < _topNormalThreshold)
        {
            return PlayerImpactType.Side;
        }
        // BoxColldier 以外は従来の Top 判定
        if (targetCollider is not BoxCollider box)
        {
            return PlayerImpactType.Top;
        }


        // ----- プレイヤーのBoxCollider -----
        Vector3 halfSize = _boxCollider.size * 0.5f;
        Quaternion rotation = _boxCollider.transform.rotation;
        Vector3 scale = _boxCollider.transform.lossyScale;

        // ----- 障害物の範囲 -----
        Bounds targetBounds = box.bounds;

        // 各方向への最大はみ出し量
        float overflowPositiveX = 0.0f;
        float overflowNegativeX = 0.0f;
        float overflowPositiveZ = 0.0f;
        float overflowNegativeZ = 0.0f;

        // プレイヤーの8頂点を確認
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localOffset = new Vector3(halfSize.x * x, halfSize.y * y, halfSize.z * z);

                    Vector3 scaledOffset = Vector3.Scale(localOffset, scale);

                    Vector3 worldVertex = impactCenter + rotation * scaledOffset;

                    // X方向
                    overflowPositiveX = Mathf.Max(overflowPositiveX, worldVertex.x - targetBounds.max.x);

                    overflowNegativeX = Mathf.Max(overflowNegativeX, targetBounds.min.x - worldVertex.x);

                    // Z方向
                    overflowPositiveZ = Mathf.Max(overflowPositiveZ, worldVertex.z - targetBounds.max.z);

                    overflowNegativeZ = Mathf.Max(overflowNegativeZ, targetBounds.min.z - worldVertex.z);
                }
            }
        }

        // ----- 最大はみ出し量 -----
        float maxOverflow = Mathf.Max(
            overflowPositiveX,
            overflowNegativeX,
            overflowPositiveZ,
            overflowNegativeZ
        );

        // 全体が障害物内に収まっている
        if (maxOverflow <= 0.0f)
        {
            return PlayerImpactType.Top;
        }

        // ----- TopEdge -----
        if (maxOverflow == overflowPositiveX)
        {
            edgeDirection = Vector3.right;
        }
        else if (maxOverflow == overflowNegativeX)
        {
            edgeDirection = Vector3.left;
        }
        else if (maxOverflow == overflowPositiveZ)
        {
            edgeDirection = Vector3.forward;
        }
        else
        {
            edgeDirection = Vector3.back;
        }

        return PlayerImpactType.TopEdge;
    }

    // --------------------------------------------------
    // ----- ResolvePenetration -----
    // --------------------------------------------------
    public bool ResolvePenetration()
    {
        // BoxCollider の中心をワールド座標へ変換
        Vector3 center = _boxCollider.transform.TransformPoint(_boxCollider.center);

        // Scale 込みの HalfExtents
        Vector3 lossyScale = _boxCollider.transform.lossyScale;
        Vector3 halfExtents = _boxCollider.size * 0.5f;
        halfExtents = Vector3.Scale(halfExtents, new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
        Quaternion orientation = _boxCollider.transform.rotation;

        // 現在の BoxCollider と重なっている Collider を取得
        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, orientation, ~0, QueryTriggerInteraction.Ignore);

        bool resolved = false;
        foreach (Collider other in overlaps)
        {
            if (other.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!IsTargetCollision(other))
            {
                continue;
            }

            // めり込み量と押し戻し方向を取得
            bool isPenetrating = Physics.ComputePenetration(
                                                            _boxCollider,
                                                            _boxCollider.transform.position,
                                                            _boxCollider.transform.rotation,
                                                            other,
                                                            other.transform.position,
                                                            other.transform.rotation,
                                                            out Vector3 pushDirection,
                                                            out float pushDistance
                                                           );
            if (!isPenetrating)
            {
                continue;
            }

            // 最低限めり込みを解消するだけ押し戻す
            transform.position += pushDirection * (pushDistance + _skinWidth);
            resolved = true;
        }
        return resolved;
    }

}
