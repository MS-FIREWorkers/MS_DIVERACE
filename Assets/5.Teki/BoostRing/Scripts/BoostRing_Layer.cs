/*
    BoostRing_Layer.cs
    20261125  Teki
    加速リングの見た目の 1 レイヤー（メッシュをコードで生成 + 回転速度）
    ・リング面はローカル XY、軸はローカル Z（+Z = BoostRing_Controller の加速方向 transform.forward）
    ・Band    : 円環の帯（レーザー本体 / 破線リング / 目盛り / 円弧）
                UV.x = 円周 0〜1 / UV.y = 内側 0 → 外側 1
    ・Chevron : 円周方向（周回方向）を向いた矢印（>>>）のグループを円周上に並べる
                UV.x = 腕の長さ方向 / UV.y = 腕の太さ方向 / 頂点カラー G = 後ろ → 前の並び順
    ・Panel   : 円周上に浮かぶホログラムパネル（傾き・高さのばらつき付き）
                UV = パネル内 0〜1 / 頂点カラー B = 横 / 縦 ÷ 4
    ・頂点カラー R = パーツごとの乱数（シェーダー側でちらつき・アクセント色に使用）
 */
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BoostRing_Layer : MonoBehaviour
{
    // ==================================================
    // ----- Enum -----
    // ==================================================
    public enum LayerShape
    {
        None,
        Band,
        Chevron,
        Panel,
    }

    // ==================================================
    // ----- Inspector Fields -----
    // ==================================================
    [Header("Shape")]
    [Tooltip("生成するメッシュの種類")]
    [SerializeField] private LayerShape _shape = LayerShape.Band;

    [Tooltip("配置する半径（Band は帯の中心、Chevron は矢印の先端、Panel はパネルの中心）")]
    [Min(0.01f)]
    [SerializeField] private float _radius = 3.0f;

    [Header("Rotation")]
    [Tooltip("ローカル Z 軸まわりの回転速度（度/秒）。マイナスで逆回転")]
    [SerializeField] private float _rotationSpeed = 0.0f;

    [Header("Band")]
    [Tooltip("帯の幅")]
    [Min(0.001f)]
    [SerializeField] private float _bandWidth = 0.6f;

    [Tooltip("円周方向の分割数")]
    [Range(16, 512)]
    [SerializeField] private int _bandSegments = 128;

    [Header("Chevron")]
    [Tooltip("矢印グループの数（円周上に等間隔）")]
    [Range(1, 16)]
    [SerializeField] private int _chevronGroupCount = 4;

    [Tooltip("1 グループあたりの矢印の数")]
    [Range(1, 8)]
    [SerializeField] private int _chevronPerGroup = 3;

    [Tooltip("矢印 1 つの大きさ（腕の長さ）")]
    [Min(0.01f)]
    [SerializeField] private float _chevronSize = 0.45f;

    [Tooltip("矢印同士の間隔（円周方向の距離）")]
    [Min(0.0f)]
    [SerializeField] private float _chevronSpacing = 0.4f;

    [Tooltip("矢印を時計回りに向けるか（向きと同じ方向に回すには回転速度をマイナスにする）")]
    [SerializeField] private bool _isChevronClockwise = false;

    [Tooltip("矢印の線の太さ")]
    [Min(0.001f)]
    [SerializeField] private float _chevronThickness = 0.12f;

    [Header("Panel")]
    [Tooltip("パネルの枚数")]
    [Range(1, 32)]
    [SerializeField] private int _panelCount = 8;

    [Tooltip("パネルの大きさ（X = 円周方向の幅, Y = 高さ）")]
    [SerializeField] private Vector2 _panelSize = new Vector2(1.2f, 0.7f);

    [Tooltip("パネルの傾き（0 = リング面に寝かせる / 90 = 垂直に立てる）")]
    [Range(0.0f, 90.0f)]
    [SerializeField] private float _panelTilt = 35.0f;

    [Tooltip("パネルの高さ（ローカル Z）のばらつき")]
    [Min(0.0f)]
    [SerializeField] private float _panelHeightJitter = 0.5f;

    [Tooltip("パネル配置角度のばらつき（0〜1）")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float _panelAngleJitter = 0.3f;

    [Tooltip("パネルの大きさのばらつき（0〜1）")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float _panelSizeJitter = 0.35f;

    [Tooltip("ランダム配置のシード値")]
    [SerializeField] private int _seed = 1234;

    // ==================================================
    // ----- Private Fields -----
    // ==================================================
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private bool _isDirty = true;

    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Vector2> _uvs = new List<Vector2>();
    private readonly List<Color> _colors = new List<Color>();
    private readonly List<int> _triangles = new List<int>();

    // ==================================================
    // ----- Property -----
    // ==================================================
    public LayerShape Shape => _shape;
    public float RotationSpeed => _rotationSpeed;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    private void OnEnable()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
        {
            Debug.LogError("[Error] MeshFilter Not Found!", this);
            return;
        }

        _isDirty = true;
        Rebuild();
    }

    private void OnValidate()
    {
        // OnValidate 内でメッシュを差し替えると警告が出るので、次の Update で作り直す
        _isDirty = true;
    }

    private void Update()
    {
        if (_isDirty)
        {
            Rebuild();
        }
    }

    private void OnDestroy()
    {
        if (_mesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_mesh);
        }
        else
        {
            DestroyImmediate(_mesh);
        }
    }

    // ==================================================
    // ----- Build -----
    // ==================================================
    private void Rebuild()
    {
        if (_meshFilter == null)
        {
            return;
        }
        _isDirty = false;

        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "BoostRing_" + _shape;
            _mesh.hideFlags = HideFlags.DontSave;
        }

        _vertices.Clear();
        _uvs.Clear();
        _colors.Clear();
        _triangles.Clear();

        switch (_shape)
        {
            case LayerShape.Band:
                BuildBand();
                break;
            case LayerShape.Chevron:
                BuildChevrons();
                break;
            case LayerShape.Panel:
                BuildPanels();
                break;
        }

        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.sharedMesh = _mesh;
    }

    // 円環の帯
    private void BuildBand()
    {
        float inner = Mathf.Max(0.0f, _radius - _bandWidth * 0.5f);
        float outer = _radius + _bandWidth * 0.5f;
        Color color = new Color(0.5f, 0.0f, 0.0f, 1.0f);

        for (int i = 0; i < _bandSegments; i++)
        {
            float u0 = (float)i / _bandSegments;
            float u1 = (float)(i + 1) / _bandSegments;
            Vector3 d0 = Direction(u0 * Mathf.PI * 2.0f);
            Vector3 d1 = Direction(u1 * Mathf.PI * 2.0f);

            AddQuad(
                d0 * inner, d1 * inner, d1 * outer, d0 * outer,
                new Vector2(u0, 0.0f), new Vector2(u1, 0.0f), new Vector2(u1, 1.0f), new Vector2(u0, 1.0f),
                color);
        }
    }

    // 円周方向（周回方向）を向いた矢印のグループ
    private void BuildChevrons()
    {
        // 矢印の向き：反時計回り（+角度方向）= 回転速度プラスと同じ向き
        float directionSign = _isChevronClockwise ? -1.0f : 1.0f;
        float spacingAngle = _chevronSpacing / _radius;

        for (int g = 0; g < _chevronGroupCount; g++)
        {
            float groupAngle = (float)g / _chevronGroupCount * Mathf.PI * 2.0f;

            for (int k = 0; k < _chevronPerGroup; k++)
            {
                // k = 0 が一番後ろ。スイープは後ろ → 前へ流す
                float sequence = (float)k / _chevronPerGroup;
                float random = Hash(g * 31 + k * 7 + _seed);
                Color color = new Color(random, sequence, 0.0f, 1.0f);

                // 各矢印の位置での半径方向・進行方向
                float angle = groupAngle + directionSign * k * spacingAngle;
                Vector3 radial = Direction(angle);
                Vector3 forward = new Vector3(-radial.y, radial.x, 0.0f) * directionSign;

                // 2D 座標 x = 半径方向, y = 進行方向。先端が前、腕が後ろへ開く
                Vector2 tip = new Vector2(_radius, 0.0f);
                Vector2 armEndA = new Vector2(_radius + _chevronSize * 0.8f, -_chevronSize);
                Vector2 armEndB = new Vector2(_radius - _chevronSize * 0.8f, -_chevronSize);

                AddChevronArm(tip, armEndA, radial, forward, color);
                AddChevronArm(tip, armEndB, radial, forward, color);
            }
        }
    }

    // 矢印の腕 1 本（リング面上の 2D 座標 x = 半径方向, y = 接線方向）
    private void AddChevronArm(Vector2 start, Vector2 end, Vector3 radial, Vector3 tangent, Color color)
    {
        Vector2 along = (end - start).normalized;
        Vector2 side = new Vector2(-along.y, along.x) * (_chevronThickness * 0.5f);

        Vector3 p0 = ToRingPlane(start - side, radial, tangent);
        Vector3 p1 = ToRingPlane(end - side, radial, tangent);
        Vector3 p2 = ToRingPlane(end + side, radial, tangent);
        Vector3 p3 = ToRingPlane(start + side, radial, tangent);

        AddQuad(p0, p1, p2, p3,
            new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f),
            color);
    }

    // 円周上に浮かぶパネル
    private void BuildPanels()
    {
        System.Random random = new System.Random(_seed);
        float tiltRad = _panelTilt * Mathf.Deg2Rad;

        for (int i = 0; i < _panelCount; i++)
        {
            float step = Mathf.PI * 2.0f / _panelCount;
            float angle = i * step + ((float)random.NextDouble() - 0.5f) * step * _panelAngleJitter;
            float height = ((float)random.NextDouble() - 0.5f) * 2.0f * _panelHeightJitter;
            float scale = 1.0f + ((float)random.NextDouble() - 0.5f) * 2.0f * _panelSizeJitter;
            float aspect = _panelSize.x / Mathf.Max(_panelSize.y, 0.001f);
            Color color = new Color((float)random.NextDouble(), (float)i / _panelCount, aspect * 0.25f, 1.0f);

            Vector3 radial = Direction(angle);
            Vector3 tangent = new Vector3(-radial.y, radial.x, 0.0f);

            // 傾き 0 = 半径方向に寝かせる / 90 = 入口側（-Z）へ立てる
            Vector3 up = radial * Mathf.Cos(tiltRad) + Vector3.back * Mathf.Sin(tiltRad);
            Vector3 center = radial * _radius + Vector3.forward * height;
            Vector3 halfWidth = tangent * (_panelSize.x * scale * 0.5f);
            Vector3 halfHeight = up * (_panelSize.y * scale * 0.5f);

            AddQuad(
                center - halfWidth - halfHeight,
                center + halfWidth - halfHeight,
                center + halfWidth + halfHeight,
                center - halfWidth + halfHeight,
                new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f),
                color);
        }
    }

    // ==================================================
    // ----- Helper -----
    // ==================================================

    // ローカル XY 平面上の方向（角度はラジアン）
    private static Vector3 Direction(float angle)
    {
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
    }

    private static Vector3 ToRingPlane(Vector2 p, Vector3 radial, Vector3 tangent)
    {
        return radial * p.x + tangent * p.y;
    }

    // 整数から 0〜1 の乱数
    private static float Hash(int n)
    {
        n = (n << 13) ^ n;
        int m = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
        return m / 2147483647.0f;
    }

    // 四角形を追加（シェーダー側は Cull Off なので巻き順は気にしない）
    private void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Color color)
    {
        int start = _vertices.Count;
        _vertices.Add(p0);
        _vertices.Add(p1);
        _vertices.Add(p2);
        _vertices.Add(p3);
        _uvs.Add(uv0);
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);
        for (int i = 0; i < 4; i++)
        {
            _colors.Add(color);
        }

        _triangles.Add(start);
        _triangles.Add(start + 1);
        _triangles.Add(start + 2);
        _triangles.Add(start);
        _triangles.Add(start + 2);
        _triangles.Add(start + 3);
    }
}
