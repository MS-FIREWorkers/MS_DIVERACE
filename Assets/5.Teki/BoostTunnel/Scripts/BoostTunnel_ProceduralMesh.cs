/*
    BoostTunnel_ProceduralMesh.cs
    20261124  Teki
    加速トンネル用のメッシュをコードで生成する（モデル制作前のβ版用）
    ・筒の軸はローカル Z。-Z 側が入口、+Z 側が出口（= BoostRing_Controller の加速方向 transform.forward）
    ・PanelWall : 内壁の発光パネル（内向き、UV.x = 円周 / UV.y = 入口 0 → 出口 1）
    ・Frame     : 外枠。数個の円弧ブロックに分割し、隙間を作って回転が見えるようにする
    ・CoreDisc  : 中心の光の円盤（UV は平面投影）
 */
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BoostTunnel_ProceduralMesh : MonoBehaviour
{
    // ==================================================
    // ----- Enum -----
    // ==================================================
    public enum MeshShape
    {
        None,
        PanelWall,
        Frame,
        CoreDisc,
    }

    // ==================================================
    // ----- Inspector Fields -----
    // ==================================================
    [Header("Shape")]
    [Tooltip("生成するメッシュの種類")]
    [SerializeField] private MeshShape _shape = MeshShape.PanelWall;

    [Header("Size")]
    [Tooltip("内側の半径（PanelWall / CoreDisc はこの半径を使う）")]
    [Min(0.01f)]
    [SerializeField] private float _innerRadius = 3.0f;

    [Tooltip("外側の半径（Frame のみ）")]
    [Min(0.01f)]
    [SerializeField] private float _outerRadius = 3.8f;

    [Tooltip("筒の長さ（ローカル Z 方向）")]
    [Min(0.01f)]
    [SerializeField] private float _length = 3.0f;

    [Header("Detail")]
    [Tooltip("円周方向の分割数（1 周あたり）")]
    [Range(8, 256)]
    [SerializeField] private int _radialSegments = 96;

    [Tooltip("Frame を何個のブロックに分割するか")]
    [Range(1, 32)]
    [SerializeField] private int _frameBlockCount = 8;

    [Tooltip("Frame ブロック間の隙間（度）")]
    [Range(0.0f, 20.0f)]
    [SerializeField] private float _frameGapAngle = 4.0f;

    // ==================================================
    // ----- Private Fields -----
    // ==================================================
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private bool _isDirty = true;

    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Vector3> _normals = new List<Vector3>();
    private readonly List<Vector2> _uvs = new List<Vector2>();
    private readonly List<int> _triangles = new List<int>();

    // ==================================================
    // ----- Property -----
    // ==================================================
    public MeshShape Shape => _shape;

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
        if (_outerRadius < _innerRadius + 0.01f)
        {
            _outerRadius = _innerRadius + 0.01f;
        }
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
            _mesh.name = "BoostTunnel_" + _shape;
            _mesh.hideFlags = HideFlags.DontSave;
        }

        _vertices.Clear();
        _normals.Clear();
        _uvs.Clear();
        _triangles.Clear();

        switch (_shape)
        {
            case MeshShape.PanelWall:
                BuildPanelWall();
                break;
            case MeshShape.Frame:
                BuildFrame();
                break;
            case MeshShape.CoreDisc:
                BuildCoreDisc();
                break;
        }

        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetNormals(_normals);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();

        _meshFilter.sharedMesh = _mesh;
    }

    // 内壁パネル：内向きの円筒面
    private void BuildPanelWall()
    {
        float z0 = -_length * 0.5f;
        float z1 = _length * 0.5f;

        for (int i = 0; i < _radialSegments; i++)
        {
            float u0 = (float)i / _radialSegments;
            float u1 = (float)(i + 1) / _radialSegments;
            float a0 = u0 * Mathf.PI * 2.0f;
            float a1 = u1 * Mathf.PI * 2.0f;

            Vector3 d0 = Direction(a0);
            Vector3 d1 = Direction(a1);

            AddQuad(
                d0 * _innerRadius + Vector3.forward * z0,
                d1 * _innerRadius + Vector3.forward * z0,
                d1 * _innerRadius + Vector3.forward * z1,
                d0 * _innerRadius + Vector3.forward * z1,
                -d0, -d1, -d1, -d0,
                new Vector2(u0, 0.0f), new Vector2(u1, 0.0f), new Vector2(u1, 1.0f), new Vector2(u0, 1.0f),
                -Direction((a0 + a1) * 0.5f));
        }
    }

    // 外枠：隙間のある円弧ブロックを並べる
    private void BuildFrame()
    {
        float z0 = -_length * 0.5f;
        float z1 = _length * 0.5f;
        float blockAngle = 360.0f / _frameBlockCount;
        float gap = Mathf.Min(_frameGapAngle, blockAngle * 0.9f);

        for (int b = 0; b < _frameBlockCount; b++)
        {
            float startDeg = b * blockAngle + gap * 0.5f;
            float endDeg = (b + 1) * blockAngle - gap * 0.5f;
            BuildFrameBlock(startDeg * Mathf.Deg2Rad, endDeg * Mathf.Deg2Rad, z0, z1);
        }
    }

    private void BuildFrameBlock(float startRad, float endRad, float z0, float z1)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(_radialSegments * (endRad - startRad) / (Mathf.PI * 2.0f)));
        float rIn = _innerRadius;
        float rOut = _outerRadius;

        for (int i = 0; i < steps; i++)
        {
            float a0 = Mathf.Lerp(startRad, endRad, (float)i / steps);
            float a1 = Mathf.Lerp(startRad, endRad, (float)(i + 1) / steps);
            float u0 = a0 / (Mathf.PI * 2.0f);
            float u1 = a1 / (Mathf.PI * 2.0f);
            Vector3 d0 = Direction(a0);
            Vector3 d1 = Direction(a1);
            Vector3 mid = Direction((a0 + a1) * 0.5f);
            Vector3 front = Vector3.forward * z0;
            Vector3 back = Vector3.forward * z1;

            // 外側面
            AddQuad(d0 * rOut + front, d1 * rOut + front, d1 * rOut + back, d0 * rOut + back,
                d0, d1, d1, d0,
                new Vector2(u0, 0.0f), new Vector2(u1, 0.0f), new Vector2(u1, 1.0f), new Vector2(u0, 1.0f),
                mid);

            // 内側面
            AddQuad(d0 * rIn + front, d1 * rIn + front, d1 * rIn + back, d0 * rIn + back,
                -d0, -d1, -d1, -d0,
                new Vector2(u0, 0.0f), new Vector2(u1, 0.0f), new Vector2(u1, 1.0f), new Vector2(u0, 1.0f),
                -mid);

            // 入口側のふた（-Z 向き）
            AddQuad(d0 * rIn + front, d1 * rIn + front, d1 * rOut + front, d0 * rOut + front,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                new Vector2(u0, 0.0f), new Vector2(u1, 0.0f), new Vector2(u1, 1.0f), new Vector2(u0, 1.0f),
                Vector3.back);

            // 出口側のふた（+Z 向き）
            AddQuad(d0 * rIn + back, d1 * rIn + back, d1 * rOut + back, d0 * rOut + back,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                new Vector2(u0, 0.0f), new Vector2(u1, 0.0f), new Vector2(u1, 1.0f), new Vector2(u0, 1.0f),
                Vector3.forward);
        }

        // ブロック両端の断面
        AddBlockEnd(startRad, z0, z1, rIn, rOut, false);
        AddBlockEnd(endRad, z0, z1, rIn, rOut, true);
    }

    private void AddBlockEnd(float angle, float z0, float z1, float rIn, float rOut, bool isEnd)
    {
        Vector3 d = Direction(angle);
        Vector3 tangent = new Vector3(-d.y, d.x, 0.0f);
        Vector3 normal = isEnd ? tangent : -tangent;
        Vector3 front = Vector3.forward * z0;
        Vector3 back = Vector3.forward * z1;

        AddQuad(d * rIn + front, d * rOut + front, d * rOut + back, d * rIn + back,
            normal, normal, normal, normal,
            new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f),
            normal);
    }

    // 中心の円盤：入口側（-Z）を表とする
    private void BuildCoreDisc()
    {
        for (int i = 0; i < _radialSegments; i++)
        {
            float a0 = (float)i / _radialSegments * Mathf.PI * 2.0f;
            float a1 = (float)(i + 1) / _radialSegments * Mathf.PI * 2.0f;
            Vector3 p0 = Direction(a0) * _innerRadius;
            Vector3 p1 = Direction(a1) * _innerRadius;

            AddTriangle(Vector3.zero, p0, p1,
                Vector3.back, Vector3.back, Vector3.back,
                DiscUv(Vector3.zero), DiscUv(p0), DiscUv(p1),
                Vector3.back);
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

    private Vector2 DiscUv(Vector3 p)
    {
        return new Vector2(p.x / _innerRadius * 0.5f + 0.5f, p.y / _innerRadius * 0.5f + 0.5f);
    }

    // 四角形を追加（頂点は外周順）。faceNormal に合わせて巻き順を自動で決める
    private void AddQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3,
        Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3,
        Vector3 faceNormal)
    {
        AddTriangle(p0, p1, p2, n0, n1, n2, uv0, uv1, uv2, faceNormal);
        AddTriangle(p0, p2, p3, n0, n2, n3, uv0, uv2, uv3, faceNormal);
    }

    // 三角形を追加。Unity は Cross(b - a, c - a) の向きが表面になるので、逆なら入れ替える
    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c,
        Vector3 na, Vector3 nb, Vector3 nc,
        Vector2 uva, Vector2 uvb, Vector2 uvc,
        Vector3 faceNormal)
    {
        bool shouldFlip = Vector3.Dot(Vector3.Cross(b - a, c - a), faceNormal) < 0.0f;
        if (shouldFlip)
        {
            (b, c) = (c, b);
            (nb, nc) = (nc, nb);
            (uvb, uvc) = (uvc, uvb);
        }

        int start = _vertices.Count;
        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _normals.Add(na);
        _normals.Add(nb);
        _normals.Add(nc);
        _uvs.Add(uva);
        _uvs.Add(uvb);
        _uvs.Add(uvc);
        _triangles.Add(start);
        _triangles.Add(start + 1);
        _triangles.Add(start + 2);
    }
}
