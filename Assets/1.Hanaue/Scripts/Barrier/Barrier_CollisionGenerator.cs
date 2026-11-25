/*
    Barrier_CollisionGenerator
    20261126  hanaue sho
    バリアの当たり判定を作るスクリプト
 */
using UnityEngine;

public class Barrier_CollisionGenarator : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Reference -----
    // --------------------------------------------------
    [Header("Reference")]
    [SerializeField] private Transform _collidersRoot;

    // --------------------------------------------------
    // ----- Parameter -----
    // --------------------------------------------------
    [Header("Barrier Parameter")]
    [SerializeField, Min(3)] private int _segmentCount = 64;
    [SerializeField, Min(0.1f)] private float _radius = 50.0f;
    [SerializeField, Min(0.1f)] private float _height = 100.0f;
    [SerializeField, Min(0.01f)] private float _thickness = 1.0f;


    // --------------------------------------------------
    // ----- Generate -----
    // --------------------------------------------------
    [ContextMenu("Generate Colliders")]
    public void GenerateColliders()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[Warning] GenerateColliders is Editor only.");
            return;
        }
        if (_collidersRoot == null)
        {
            _collidersRoot = transform.Find("Colliders");
        }
        if (_collidersRoot == null)
        {
            Debug.LogError("[Error] Colliders Root Not Found !!");
            return;
        }

        // 既存のColliderを削除
        ClearColliders();

        // ----- Parameter -----
        int count = Mathf.Max(3, _segmentCount);

        float angleStep = 360.0f / count;
        float halfAngle = Mathf.PI / count;

        // 円の接線同士が交わる位置までの幅
        float segmentWidth = 2.0f * _radius * Mathf.Tan(halfAngle);

        // ----- Generate -----
        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;

            Vector3 radialDirection = new Vector3(
                Mathf.Sin(angle),
                0.0f,
                Mathf.Cos(angle)
            );

            // 内側の面が指定半径になるように配置
            Vector3 localPosition = radialDirection * (_radius + _thickness * 0.5f);
            localPosition.y = -_height / 2.0f; // 下げる

            Quaternion localRotation = Quaternion.LookRotation(radialDirection, Vector3.up);

            // ----- GameObject -----
            GameObject colliderObject = new GameObject("Collider_" + i.ToString("D3"));

            colliderObject.transform.SetParent(_collidersRoot, false);
            colliderObject.transform.localPosition = localPosition;
            colliderObject.transform.localRotation = localRotation;
            colliderObject.transform.localScale = Vector3.one;

            // ----- Tag -----
            colliderObject.tag = "Barrier";

            // ----- BoxCollider -----
            BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();

            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(
                segmentWidth,
                _height,
                _thickness
            );
        }

        Debug.Log("[Barrier] Generated Colliders : " + count);
    }

    // --------------------------------------------------
    // ----- Clear -----
    // --------------------------------------------------
    [ContextMenu("Clear Colliders")]
    public void ClearColliders()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (_collidersRoot == null)
        {
            _collidersRoot = transform.Find("Colliders");
        }

        if (_collidersRoot == null)
        {
            return;
        }

        for (int i = _collidersRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _collidersRoot.GetChild(i);

            // 自動生成したColliderのみ削除
            if (child.name.StartsWith("Collider_"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}