/*
    Barrier_FrustumGenerator
    20261126  hanaue sho
    漏斗状のメッシュと当たり判定を作るスクリプト
 */
using UnityEngine;

public class Barrier_FrustumGenerator : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Reference -----
    // --------------------------------------------------
    [Header("Reference")]
    [SerializeField] private MeshFilter _modelMeshFilter;
    [SerializeField] private Transform _collidersRoot;

    // --------------------------------------------------
    // ----- Parameter -----
    // --------------------------------------------------
    [Header("Barrier Parameter")]
    [SerializeField, Min(3)] private int _segmentCount = 64;
    [SerializeField, Min(1)] private int _verticalSegments = 8;

    [SerializeField, Min(0.1f)] private float _topRadius = 50.0f;
    [SerializeField, Min(0.1f)] private float _bottomRadius = 30.0f;
    [SerializeField, Min(0.1f)] private float _height = 50.0f;
    [SerializeField, Min(0.01f)] private float _thickness = 1.0f;

    // Collider同士の継ぎ目を埋める
    [SerializeField, Min(0.0f)] private float _overlap = 0.05f;

    // --------------------------------------------------
    // ----- Generated Mesh -----
    // --------------------------------------------------
    [Header("Generated Mesh")]
    [SerializeField] private Mesh _generatedMesh;

    // --------------------------------------------------
    // ----- Generate -----
    // --------------------------------------------------
    [ContextMenu("Generate Barrier")]
    public void GenerateBarrier()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (_collidersRoot == null)
        {
            _collidersRoot = transform.Find("Colliders");
        }

        if (_modelMeshFilter == null)
        {
            Transform model = transform.Find("Model");
            if (model != null)
            {
                _modelMeshFilter = model.GetComponent<MeshFilter>();
            }
        }

        if (_collidersRoot == null || _modelMeshFilter == null)
        {
            Debug.LogError("[Error] Barrier References Not Found !!");
            return;
        }

        if (_bottomRadius >= _topRadius)
        {
            Debug.LogWarning("[Warning] Bottom Radius must be smaller than Top Radius.");
            return;
        }

        GenerateMesh();
        GenerateColliders();
    }


    // --------------------------------------------------
    // ----- Generate Mesh -----
    // --------------------------------------------------
    private void GenerateMesh()
    {
        int count = Mathf.Max(3, _segmentCount);

        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uv = new Vector2[count * 2];
        int[] triangles = new int[count * 6];

        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2.0f * i / count;

            Vector3 direction = new Vector3(Mathf.Sin(angle), 0.0f, Mathf.Cos(angle));

            // 上端
            vertices[i] = direction * _topRadius;

            // 下端
            vertices[i + count] = direction * _bottomRadius + Vector3.down * _height;

            float u = (float)i / count;

            uv[i] = new Vector2(u, 1.0f);
            uv[i + count] = new Vector2(u, 0.0f);
        }

        // 内側から見えるように三角形を構築
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int index = i * 6;

            triangles[index + 0] = i;
            triangles[index + 1] = next;
            triangles[index + 2] = i + count;

            triangles[index + 3] = next;
            triangles[index + 4] = next + count;
            triangles[index + 5] = i + count;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Barrier_Frustum_Mesh";

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // ----- MeshをAssetとして保存 -----
#if UNITY_EDITOR
        mesh = SaveMeshAsset(mesh);
#endif

        // ----- Modelへ適用 -----
        _modelMeshFilter.sharedMesh = mesh;
    }


    // --------------------------------------------------
    // ----- Generate Colliders -----
    // --------------------------------------------------
    private void GenerateColliders()
    {
        ClearColliders();

        int count = Mathf.Max(3, _segmentCount);
        int layers = Mathf.Max(1, _verticalSegments);

        float angleStep = Mathf.PI * 2.0f / count;
        float halfAngle = Mathf.PI / count;

        float layerHeight = _height / layers;

        // 高さに対する半径の変化量
        float slope = (_topRadius - _bottomRadius) / _height;

        for (int y = 0; y < layers; y++)
        {
            float topT = (float)y / layers;
            float bottomT = (float)(y + 1) / layers;

            float radiusTop = Mathf.Lerp(_topRadius, _bottomRadius, topT);
            float radiusBottom = Mathf.Lerp(_topRadius, _bottomRadius, bottomT);

            float radiusMiddle = (radiusTop + radiusBottom) * 0.5f;
            float positionY = -(y + 0.5f) * layerHeight;

            // 上端側の幅を基準にして隙間を抑える
            float segmentWidth = 2.0f * radiusTop * Mathf.Tan(halfAngle) + _overlap;

            // 斜面に沿った長さ
            float segmentHeight = layerHeight * Mathf.Sqrt(1.0f + slope * slope) + _overlap;

            for (int i = 0; i < count; i++)
            {
                float angle = angleStep * i;

                Vector3 radialDirection = new Vector3(Mathf.Sin(angle), 0.0f, Mathf.Cos(angle));

                // 斜面の外向き法線
                Vector3 normal = (radialDirection - Vector3.up * slope).normalized;

                // 斜面に沿った上方向
                Vector3 slopeUp = (Vector3.up + radialDirection * slope).normalized;

                Vector3 localPosition = radialDirection * radiusMiddle;
                localPosition.y = positionY;

                // Colliderの内側の面を基準に厚みを外側へ
                localPosition += normal * (_thickness * 0.5f);

                Quaternion localRotation = Quaternion.LookRotation(normal, slopeUp);

                GameObject colliderObject = new GameObject("Collider_" + y.ToString("D2") + "_" + i.ToString("D3"));

                colliderObject.transform.SetParent(_collidersRoot, false);
                colliderObject.transform.localPosition = localPosition;
                colliderObject.transform.localRotation = localRotation;
                colliderObject.transform.localScale = Vector3.one;

                colliderObject.tag = "Barrier";

                BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = new Vector3(segmentWidth, segmentHeight, _thickness);
            }
        }

        Debug.Log("[Barrier] Generated Colliders : " + count * layers);
    }


    // --------------------------------------------------
    // ----- Clear Colliders -----
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

            if (child.name.StartsWith("Collider_"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }



    // --------------------------------------------------
    // ----- Save Mesh Asset -----
    // --------------------------------------------------
#if UNITY_EDITOR
    private Mesh SaveMeshAsset(Mesh mesh)
    {
        const string folderPath = "Assets/0.Models/Common/Barrier";

        // フォルダがなければ作成
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/0.Models/Common", "Barrier");
        }

        // ----- 既存Meshの更新 -----
        if (_generatedMesh != null && UnityEditor.AssetDatabase.Contains(_generatedMesh))
        {
            UnityEditor.EditorUtility.CopySerialized(mesh, _generatedMesh);
            UnityEditor.EditorUtility.SetDirty(_generatedMesh);

            DestroyImmediate(mesh);

            UnityEditor.AssetDatabase.SaveAssets();

            return _generatedMesh;
        }

        // ----- 新規Meshアセット作成 -----
        string assetPath = folderPath + "/" + gameObject.name + "_Mesh.asset";

        // 同名アセットが存在する場合は別名にする
        assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetPath);

        UnityEditor.AssetDatabase.CreateAsset(mesh, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();

        _generatedMesh = mesh;

        // Generatorの参照変更をシーンに記録
        UnityEditor.EditorUtility.SetDirty(this);

        return _generatedMesh;
    }
#endif

}
