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
    TopEdgeGraze,
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
    [SerializeField] private BoxCollider _wallSideCollider;

    // --------------------------------------------------
    // ----- Collision Parameter -----
    // --------------------------------------------------
    [Header("Collision Parameter")]
    [SerializeField] private float _skinWidth = 0.02f;
    [SerializeField, Range(-1.0f, 1.0f)] private float _topNormalThreshold = 0.6f;

    [SerializeField, Min(0.0f)] private float _topEdgeGrazeMargin = 0.0f;

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
    public bool CheckCollision(Vector3 displacement, out PlayerImpactInfo impactInfo, Collider ignoreCollider = null)
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
        
        foreach (RaycastHit originalHit in hits)
        {
            // 自分自身は無視
            if (originalHit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!IsTargetCollision(originalHit.collider))
            {
                continue;
            }

            if (originalHit.collider == ignoreCollider)
            {
                Debug.Log("[Debug] Here");
                continue;
            }


            RaycastHit hit = originalHit;

            // ----- 初期重なりの判定 -----
            if (hit.distance <= 0.0001f)
            {
                bool isPenetrating = Physics.ComputePenetration(
                    _boxCollider, _boxCollider.transform.position, _boxCollider.transform.rotation,
                    hit.collider, hit.collider.transform.position, hit.collider.transform.rotation,
                    out Vector3 separationDirection, out float separationDistance
                );

                if (isPenetrating)
                {
                    // BoxCastの仮の法線を、実際の分離方向に置き換える
                    hit.normal = separationDirection;
                }
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

        Debug.Log("[Collision] Target=" + impactInfo.Collider.name
    + " Type=" + impactInfo.ImpactType
    + " Normal=" + impactInfo.Normal
    + " Distance=" + impactInfo.Distance
    + " VelocityY=" + (displacement.y / Time.deltaTime));

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

        // ----- TopEdgeGraze 判定 -----
        // EdgeDirection に対応する障害物の端と、
        // プレイヤー中心との距離を求める
        float insideDistance = 0.0f;

        if (edgeDirection == Vector3.right)
        {
            insideDistance = targetBounds.max.x - impactCenter.x;
        }
        else if (edgeDirection == Vector3.left)
        {
            insideDistance = impactCenter.x - targetBounds.min.x;
        }
        else if (edgeDirection == Vector3.forward)
        {
            insideDistance = targetBounds.max.z - impactCenter.z;
        }
        else if (edgeDirection == Vector3.back)
        {
            insideDistance = impactCenter.z - targetBounds.min.z;
        }

        // 中心が外側、または内側のマージン以内ならGraze
        if (insideDistance <= _topEdgeGrazeMargin)
        {
            Debug.Log("[Debug] TopEdgeGraze");
            return PlayerImpactType.TopEdgeGraze;
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

    // --------------------------------------------------
    // ----- Check Wall Contact -----
    // 指定した壁が横方向に存在するか確認する
    // gap はプレイヤーColliderと壁の間隔
    // --------------------------------------------------
    public bool CheckWallContact(Collider wallCollider, Vector3 wallNormal, float maxGap, out float gap)
    {
        gap = 0.0f;

        if (wallCollider == null)
        {
            return false;
        }

        // 壁の法線は水平として扱う
        wallNormal.y = 0.0f;
        if (wallNormal.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        wallNormal.Normalize();

        // Player Collider
        Vector3 center = _boxCollider.transform.TransformPoint(_boxCollider.center);
        Vector3 scale = _boxCollider.transform.lossyScale;

        Vector3 halfExtents = Vector3.Scale(
                                            _boxCollider.size * 0.5f,
                                            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))
                                           );

        Quaternion orientation = _boxCollider.transform.rotation;

        // 壁から少し離した場所を始点とする
        float probeOffset = 0.25f;
        Vector3 origin = center + wallNormal * probeOffset;

        float castDistance = probeOffset + maxGap + _skinWidth;

        // 壁の方向へBoxCast
        RaycastHit[] hits = Physics.BoxCastAll(origin, halfExtents, -wallNormal, orientation, castDistance, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            // 現在スライドしている壁のみを対象とする
            if (hit.collider != wallCollider)
            {
                continue;
            }

            // 想定している壁面か確認
            if (Vector3.Dot(hit.normal, wallNormal) < 0.9f)
            {
                continue;
            }

            gap = hit.distance - probeOffset;
            return true;
        }

        return false;
    }

    // --------------------------------------------------
    // ----- Calculate TopEdgeGraze Correction -----
    // 移動後のプレイヤーColliderが障害物の外側へ
    // 抜けるために必要な水平補正量を計算
    // --------------------------------------------------
    public Vector3 CalculateTopEdgeGrazeCorrection(PlayerImpactInfo impactInfo, Vector3 displacement)
    {
        if (impactInfo.ImpactType != PlayerImpactType.TopEdgeGraze)
        {
            return Vector3.zero;
        }

        if (impactInfo.Collider == null)
        {
            return Vector3.zero;
        }

        // ----- 障害物の範囲 -----
        Bounds targetBounds = impactInfo.Collider.bounds;

        // ----- 移動後のプレイヤーCollider -----
        Vector3 center = _boxCollider.transform.TransformPoint(_boxCollider.center) + displacement;
        Vector3 halfSize = _boxCollider.size * 0.5f;
        Vector3 scale = _boxCollider.transform.lossyScale;
        Quaternion rotation = _boxCollider.transform.rotation;

        // ----- 頂点の範囲を取得 -----
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localOffset = new Vector3(halfSize.x * x, halfSize.y * y, halfSize.z * z);
                    Vector3 scaledOffset = Vector3.Scale(localOffset, scale);
                    Vector3 vertex = center + rotation * scaledOffset;

                    minX = Mathf.Min(minX, vertex.x);
                    maxX = Mathf.Max(maxX, vertex.x);
                    minZ = Mathf.Min(minZ, vertex.z);
                    maxZ = Mathf.Max(maxZ, vertex.z);
                }
            }
        }

        // ----- 補正量を計算 -----
        Vector3 correction = Vector3.zero;

        if (impactInfo.EdgeDirection == Vector3.right)
        {
            correction.x = Mathf.Max(0.0f, targetBounds.max.x - minX + _skinWidth);
        }
        else if (impactInfo.EdgeDirection == Vector3.left)
        {
            correction.x = -Mathf.Max(0.0f, maxX - targetBounds.min.x + _skinWidth);
        }
        else if (impactInfo.EdgeDirection == Vector3.forward)
        {
            correction.z = Mathf.Max(0.0f, targetBounds.max.z - minZ + _skinWidth);
        }
        else if (impactInfo.EdgeDirection == Vector3.back)
        {
            correction.z = -Mathf.Max(0.0f, maxZ - targetBounds.min.z + _skinWidth);
        }

        return correction;
    }

    // --------------------------------------------------
    // ----- TryGetWallHandContact -----
    // 指定した壁にRaycastして接触情報を取得
    // --------------------------------------------------
    public bool TryGetWallHandContact(Vector3 origin, Vector3 wallNormal, Collider wallCollider, float maxDistance, out RaycastHit hit)
    {
        hit = default;

        if (wallCollider == null)
        {
            return false;
        }
        if (wallNormal.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        wallNormal.Normalize();

        // 壁の方向へRaycast
        Ray ray = new Ray(origin, -wallNormal);

        // 指定した壁だけを判定
        if (!wallCollider.Raycast(ray, out hit, maxDistance))
        {
            return false;
        }

        // 想定している側面か確認
        if (Vector3.Dot(hit.normal, wallNormal) < 0.9f)
        {
            return false;
        }

        return true;
    }

    // --------------------------------------------------
    // ----- Try Get WallSide Candidate -----
    // WallSide用Colliderの範囲内から壁面候補を取得
    // --------------------------------------------------
    public bool TryGetWallSideCandidate(out PlayerImpactInfo impactInfo)
    {
        impactInfo = default;

        if (_wallSideCollider == null)
        {
            return false;
        }

        // ----- WallSide Collider -----
        Transform wallTransform = _wallSideCollider.transform;

        Vector3 center = wallTransform.TransformPoint(_wallSideCollider.center);
        Vector3 scale = wallTransform.lossyScale;

        Vector3 halfExtents = Vector3.Scale(
            _wallSideCollider.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))
        );

        Quaternion orientation = wallTransform.rotation;

        // ----- OverlapBox -----
        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, orientation, ~0, QueryTriggerInteraction.Ignore);

        // ----- Ray Direction -----
        Vector3 right = transform.right;
        right.y = 0.0f;
        if (right.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        right.Normalize();

        Vector3[] directions = { right, -right };

        // Triggerの中心から左右端までの距離
        float rayDistance = halfExtents.x;

        float nearestDistance = float.MaxValue;
        bool found = false;
        foreach (Collider other in overlaps)
        {
            // 自分自身は除外
            if (other.transform.IsChildOf(transform))
            {
                continue;
            }
            // 障害物以外は除外
            if (!IsTargetCollision(other))
            {
                continue;
            }

            // ----- Left / Right Raycast -----
            foreach (Vector3 direction in directions)
            {
                Ray ray = new Ray(center, direction);
                if (!other.Raycast(ray, out RaycastHit hit, rayDistance))
                {
                    continue;
                }

                // 左右方向に向いた側面のみ採用
                float sideDot = Vector3.Dot(hit.normal, -direction);
                if (sideDot < 0.7f)
                {
                    continue;
                }

                // 最も近い候補を採用
                if (hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                found = true;

                impactInfo = new PlayerImpactInfo
                {
                    ImpactType = PlayerImpactType.None,
                    Point = hit.point,
                    Normal = hit.normal,
                    Collider = other,
                    Distance = hit.distance,
                    EdgeDirection = Vector3.zero
                };
            }
        }

        return found;
    }
}
