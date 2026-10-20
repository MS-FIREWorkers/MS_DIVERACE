using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 編集モードでも動作し、必要なコンポーネントを自動的に付与する。
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class FloorGapFiller : MonoBehaviour
{
    // 生成する床の範囲と厚さ（ローカル座標）。
    [Header("Floor Area (Local Coordinates)")]

    // 床を生成するX座標の範囲。
    [Tooltip("床を生成するX座標の範囲を指定します。例：-800～800")]
    [SerializeField] private Vector2 xRange = new Vector2(-800f, 800f);

    // 床を生成するZ座標の範囲。
    [Tooltip("床を生成するZ座標の範囲を指定します。例：-800～800")]
    [SerializeField] private Vector2 zRange = new Vector2(-800f, 800f);

    // 床の中心となるY座標。
    [Tooltip("床の中心のY座標を指定します。厚さはこの位置を中心に上下へ広がります。")]
    [SerializeField] private float centerY = -300f;

    // 床の厚さ。
    [Tooltip("床の厚さを指定します。0より大きい値を設定してください。")]
    [Min(0.01f)]
    [SerializeField] private float thickness = 1f;

    // テクスチャの繰り返し密度。
    [Tooltip("UV座標の倍率を指定します。値が大きいほどテクスチャの繰り返し回数が増えます。")]
    [Min(0.001f)]
    [SerializeField] private float uvScale = 0.02f;

    // 穴あき床のRenderer。これらが占める矩形範囲には床を追加しない。
    [Header("Existing Floors With Holes")]

    [Tooltip("既に穴が開いている床のMeshRendererを登録します。登録した床の矩形範囲を避けて、新しい床を生成します。")]
    [SerializeField] private Renderer[] existingFloors;

    // 編集モードで既存の床を移動した際、自動的に再生成するか。
    [Header("Editor")]

    [Tooltip("有効にすると、編集モードで床の位置や設定が変更された際に、自動的にMeshを再生成します。")]
    [SerializeField] private bool autoRebuildInEditor = true;

    // 小数誤差による重複座標や極小領域を無視するための許容値。
    private const float Epsilon = 0.001f;

    // 前回生成したMeshと再生成状態を管理する。
    private Mesh generatedMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private int previousHash = int.MinValue;
    private bool needsRebuild = true;
    private bool isRebuilding;

    // 既存の床が占有するX/Z方向の長方形領域。
    private struct RectArea
    {
        public float left, right, back, front;

        public RectArea(float l, float r, float b, float f)
        {
            left = l;
            right = r;
            back = b;
            front = f;
        }

        // 指定した座標が既存の床の領域に含まれるか判定する。
        public bool Contains(float x, float z)
        {
            return x >= left - Epsilon && x <= right + Epsilon &&
                   z >= back - Epsilon && z <= front + Epsilon;
        }
    }

    // 起動時またはオブジェクト有効化時に必要に応じて床を生成する。
    private void OnEnable()
    {
        CacheComponents();
        needsRebuild = true;

        if (Application.isPlaying || autoRebuildInEditor)
            RebuildFloor();
    }

    // Inspectorで値が変更されたら、次回の更新で再生成させる。
    private void OnValidate()
    {
        needsRebuild = true;
    }

    // 編集モードのみ、既存床の位置や設定が変化したか確認する。
    private void Update()
    {
        if (Application.isPlaying || !autoRebuildInEditor || isRebuilding)
            return;

        int hash = CalculateSettingsHash();

        if (needsRebuild || hash != previousHash)
            RebuildFloor();
    }

    // 生成Meshの参照を外し、不要なMeshを破棄する。
    private void OnDestroy()
    {
        if (generatedMesh == null) return;

        if (meshCollider != null && meshCollider.sharedMesh == generatedMesh)
            meshCollider.sharedMesh = null;

        if (meshFilter != null && meshFilter.sharedMesh == generatedMesh)
            meshFilter.sharedMesh = null;

        DestroyMesh(generatedMesh);
        generatedMesh = null;
    }

    // MeshFilterとMeshColliderの参照を取得する。
    private void CacheComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshCollider == null)
            meshCollider = GetComponent<MeshCollider>();
    }

    // 既存の穴あき床を避け、残りの範囲を単一Meshで埋める。
    // コンポーネントのメニューから手動実行することもできる。
    [ContextMenu("Rebuild Floor")]
    public void RebuildFloor()
    {
        if (isRebuilding) return;

        isRebuilding = true;
        CacheComponents();

        try
        {
            // 床の幅・奥行き・厚さが有効か確認する。
            if (xRange.x >= xRange.y || zRange.x >= zRange.y || thickness <= 0f)
            {
                Debug.LogError("Invalid floor range or thickness.", this);
                return;
            }

            // 外周と既存床の端を、メッシュ分割用の座標として収集する。
            List<float> xs = new List<float> { xRange.x, xRange.y };
            List<float> zs = new List<float> { zRange.x, zRange.y };
            List<RectArea> reserved = new List<RectArea>();

            if (existingFloors != null)
            {
                foreach (Renderer source in existingFloors)
                {
                    if (source == null) continue;

                    if (source.gameObject == gameObject)
                    {
                        Debug.LogWarning("Do not reference the generated floor itself.", this);
                        continue;
                    }

                    // ワールド空間のBoundsをローカル座標へ変換する。
                    // Boundsは軸平行の矩形であり、穴そのものの形状は判定しない。
                    Bounds b = source.bounds;
                    Vector3 a = transform.InverseTransformPoint(b.min);
                    Vector3 c = transform.InverseTransformPoint(b.max);

                    float left = Mathf.Clamp(Mathf.Min(a.x, c.x), xRange.x, xRange.y);
                    float right = Mathf.Clamp(Mathf.Max(a.x, c.x), xRange.x, xRange.y);
                    float back = Mathf.Clamp(Mathf.Min(a.z, c.z), zRange.x, zRange.y);
                    float front = Mathf.Clamp(Mathf.Max(a.z, c.z), zRange.x, zRange.y);

                    if (right - left <= Epsilon || front - back <= Epsilon)
                    {
                        Debug.LogWarning($"{source.name} is outside the floor area.", source);
                        continue;
                    }

                    // 床の高さが一致しない場合は警告する（生成処理は続行）。
                    float sourceY = transform.InverseTransformPoint(b.center).y;

                    if (Mathf.Abs(sourceY - centerY) > thickness * 0.5f + Epsilon)
                        Debug.LogWarning($"{source.name} Y does not match the generated floor.", source);

                    // 占有領域とその四辺の座標を記録する。
                    reserved.Add(new RectArea(left, right, back, front));
                    xs.Add(left); xs.Add(right);
                    zs.Add(back); zs.Add(front);
                }
            }

            // 参照床がない場合、古い生成Meshを解除して終了する。
            if (reserved.Count == 0)
            {
                Debug.LogWarning("Assign existing floor renderers before generating gaps.", this);

                meshCollider.sharedMesh = null;
                meshFilter.sharedMesh = null;

                if (generatedMesh != null)
                    DestroyMesh(generatedMesh);

                generatedMesh = null;
                needsRebuild = false;
                previousHash = CalculateSettingsHash();
                return;
            }

            // 重複した境界座標をまとめ、X/Z方向の区画を作成する。
            xs = SortUnique(xs);
            zs = SortUnique(zs);

            int width = xs.Count - 1;
            int depth = zs.Count - 1;
            bool[,] filled = new bool[width, depth];
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    // 各区画の中心が既存の床に含まれるか調べる。
                    float centerX = (xs[x] + xs[x + 1]) * 0.5f;
                    float centerZ = (zs[z] + zs[z + 1]) * 0.5f;
                    bool occupied = false;

                    foreach (RectArea area in reserved)
                    {
                        if (!area.Contains(centerX, centerZ)) continue;
                        occupied = true;
                        break;
                    }

                    // 既存床の外側にある区画だけ、新しい床を生成する。
                    filled[x, z] = !occupied;

                    if (!occupied)
                        count++;
                }
            }

            if (count == 0)
            {
                Debug.LogWarning(
                    "No floor space remains. Check existing floor bounds and scales.", this);
            }

            // Meshを構成する頂点、三角形のインデックス、UVを格納する。
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // 床の上面・下面のローカルY座標。
            float top = centerY + thickness * 0.5f;
            float bottom = centerY - thickness * 0.5f;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (!filled[x, z]) continue;

                    float x0 = xs[x], x1 = xs[x + 1];
                    float z0 = zs[z], z1 = zs[z + 1];

                    // 各区画の上面と下面を作成する。
                    AddQuad(verts, tris, uvs,
                        new Vector3(x0, top, z0), new Vector3(x0, top, z1),
                        new Vector3(x1, top, z1), new Vector3(x1, top, z0));

                    AddQuad(verts, tris, uvs,
                        new Vector3(x1, bottom, z0), new Vector3(x1, bottom, z1),
                        new Vector3(x0, bottom, z1), new Vector3(x0, bottom, z0));

                    // 外周、または床のない区画に接する辺だけ側面を作成する。
                    if (x == 0 || !filled[x - 1, z])
                        AddQuad(verts, tris, uvs,
                            new Vector3(x0, bottom, z0), new Vector3(x0, bottom, z1),
                            new Vector3(x0, top, z1), new Vector3(x0, top, z0));

                    if (x == width - 1 || !filled[x + 1, z])
                        AddQuad(verts, tris, uvs,
                            new Vector3(x1, bottom, z1), new Vector3(x1, bottom, z0),
                            new Vector3(x1, top, z0), new Vector3(x1, top, z1));

                    if (z == 0 || !filled[x, z - 1])
                        AddQuad(verts, tris, uvs,
                            new Vector3(x1, bottom, z0), new Vector3(x0, bottom, z0),
                            new Vector3(x0, top, z0), new Vector3(x1, top, z0));

                    if (z == depth - 1 || !filled[x, z + 1])
                        AddQuad(verts, tris, uvs,
                            new Vector3(x0, bottom, z1), new Vector3(x1, bottom, z1),
                            new Vector3(x1, top, z1), new Vector3(x0, top, z1));
                }
            }

            // 作成した頂点情報をUnityのMeshに反映する。
            Mesh mesh = new Mesh { name = "Generated_Floor_Gaps" };

            if (verts.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // 描画用Meshと衝突用Meshを同じ形状へ更新する。
            // Convexを無効にし、凹形状を含む衝突判定を維持する。
            meshCollider.sharedMesh = null;

            Mesh old = generatedMesh;
            generatedMesh = mesh;

            meshFilter.sharedMesh = mesh;
            meshCollider.convex = false;
            meshCollider.sharedMesh = mesh;

            if (old != null)
                DestroyMesh(old);

            // 再生成完了時の設定を記録し、無駄な再生成を防ぐ。
            needsRebuild = false;
            previousHash = CalculateSettingsHash();
        }
        // エラーが発生しても再生成中フラグを解除する。
        finally
        {
            isRebuilding = false;
        }
    }

    // 4頂点から2枚の三角形を作り、四角形1枚をMeshへ追加する。
    private void AddQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = verts.Count;

        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);

        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);

        // X/Z座標をUVへ変換する（テクスチャの繰り返し密度を指定）。
        uvs.Add(new Vector2(a.x, a.z) * uvScale);
        uvs.Add(new Vector2(b.x, b.z) * uvScale);
        uvs.Add(new Vector2(c.x, c.z) * uvScale);
        uvs.Add(new Vector2(d.x, d.z) * uvScale);
    }

    // 境界座標を昇順に並べ、ほぼ同じ値を1つにまとめる。
    private static List<float> SortUnique(List<float> values)
    {
        values.Sort();
        List<float> unique = new List<float>();

        foreach (float v in values)
        {
            if (unique.Count == 0 || Mathf.Abs(v - unique[unique.Count - 1]) > Epsilon)
                unique.Add(v);
        }

        return unique;
    }

    // 設定値と既存床のBoundsから変更検出用のハッシュ値を作成する。
    private int CalculateSettingsHash()
    {
        unchecked
        {
            int h = 17;

            h = h * 31 + xRange.GetHashCode();
            h = h * 31 + zRange.GetHashCode();
            h = h * 31 + centerY.GetHashCode();
            h = h * 31 + thickness.GetHashCode();
            h = h * 31 + uvScale.GetHashCode();

            if (existingFloors != null)
            {
                foreach (Renderer source in existingFloors)
                {
                    h = h * 31 + (source == null ? 0 : source.GetInstanceID());

                    if (source == null) continue;

                    Bounds b = source.bounds;
                    h = h * 31 + b.center.GetHashCode();
                    h = h * 31 + b.size.GetHashCode();
                }
            }

            return h;
        }
    }

    // 再生中と編集モードで適切なMesh破棄処理を使い分ける。
    private static void DestroyMesh(Mesh mesh)
    {
        if (mesh == null) return;

        if (Application.isPlaying)
            Destroy(mesh);
        else
            DestroyImmediate(mesh);
    }
}