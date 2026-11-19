using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 編集中も表示する仮組。設定だけを保存し、メッシュやマテリアルは読込時に再生成する。
[ExecuteAlways, DisallowMultipleComponent]
public sealed class SkyshaftPrototype : MonoBehaviour
{
    [Header("Entrance (local XY plane)")]
    [Min(0.1f)] public float entranceRadius = 2f;

    [Tooltip("入口の横・縦の倍率。(1, 1)で円、(1, 2)で縦長。判定範囲は子のBoxColliderで別途調整。")]
    public Vector2 entranceAspect = Vector2.one;

    [Tooltip("1でひし形、2で円・楕円、大きくすると角の丸い四角形に近づきます。")]
    [Range(1f, 8f)] public float shapePower = 2f;

    [Tooltip("輪郭の凹凸の強さ。0でなめらか、値を増やすと洞窟風の輪郭になります。")]
    [Range(0f, 0.4f)] public float edgeDeformation;

    [Tooltip("輪郭の凹凸の数。")]
    [Range(2, 12)] public int edgeWaves = 5;

    [Tooltip("凹凸の位置をずらす角度。")]
    [Range(0f, 360f)] public float edgePhase;
    [Min(0.1f)] public float rayLength = 3f;
    [Min(0.01f)] public float rayWidth = 0.8f;
    [Range(0f, 1f)] public float rayOpacity = 0.55f;

    [Header("Ray counts / depth")]
    [Tooltip("円の面に沿って放射状に広がる光の本数。0で非表示。")]
    [Range(0, 64)] public int rayCount = 8;

    [Tooltip("円の面に垂直な方向へ伸びる光の本数。0で非表示。")]
    [Range(0, 64)] public int depthRayCount = 8;

    [Tooltip("円の法線から外側へ開く角度。0度で垂直、90度で円の面と平行。")]
    [Range(0f, 90f)] public float depthSpreadAngle = 25f;

    [Tooltip("OFFでローカル+Z、ONで-Z方向へ伸ばす。現在のシーンではOFFがMain Camera側。")]
    public bool reverseDepthDirection;

    [Header("Background white plane")]
    [Tooltip("このタグが付いた、有効なCameraを自動取得します。タグはCamera自身のGameObjectに付けてください。")]
    public string cameraTag = "MainCamera";

    [Tooltip("タグに一致する有効なカメラが見つからない場合に使用する、手動指定のカメラ。")]
    public Camera viewCamera;

    [Tooltip("Player Tagで取得できなかった場合に使用する、手動指定のキャラクター。タグ取得を優先します。")]
    public Transform character;

    [Min(0.1f)] public float backgroundDistance = 8f;
    [Min(0.1f)] public float characterClearance = 1f;
    [Range(0f, 1f)] public float previewWhiteout;

    [Header("Playback (Inspector button in Play mode)")]
    [Min(0.01f)] public float fadeIn = 0.2f;
    [Min(0f)] public float hold = 0.1f;
    [Min(0.01f)] public float fadeOut = 1f;

    readonly List<Object> resources = new List<Object>();
    Transform generated, entrance, background;

    readonly List<Transform> rays = new List<Transform>();
    readonly List<Transform> depthRays = new List<Transform>();

    Mesh rayQuad, depthCross;
    Mesh entranceMesh;
    Vector3[] entranceVertices;
    Vector2 previousAspect;
    Vector4 previousShape;
    bool shapeReady;
    Material rayMaterial, whiteMaterial;

    bool playing, completed;
    float elapsed;
    Camera cachedCamera;
    Transform taggedCharacter;
    float nextCharacterSearch;

    // 入口の当たり判定から、再生途中の二重発動を防ぐために参照する。
    public bool IsPlaying => playing;

    void OnEnable() 
    { 
        // シーン読込やコンポーネントの有効化時に、表示用のパーツを準備する。
        Build(); 
    }

    void OnDisable() 
    { 
        // 自動生成したパーツが、再読込のたびに残って増えないように片付ける。
        Clear(); 
    }

    // 一時リソースを管理し、無効化時にまとめて破棄する。
    T Own<T>(T resource) where T : Object
    {
        // シーンの保存対象から外す。一時生成した素材はresourcesに保持して後で破棄する。
        resource.hideFlags = HideFlags.HideAndDontSave;
        resources.Add(resource);
        return resource;
    }

    // URP標準のUnlitを使用。光の筋は加算、背景の白い板は通常の半透明で描画する。
    Material MakeMaterial(bool transparent, bool additive = false)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null) 
            return null;

        var material = Own(new Material(shader));
        // 表裏の両方を表示し、板の裏側へ回っても光が消えないようにする。
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetColor("_BaseColor", Color.white);

        if (transparent)
        {
            // 通常の半透明は背景へ白を混ぜ、加算は背景の色に光の明るさを足す。
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", additive ? 2 : 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            // 半透明の板が深度を書き込んで、後続の描画を隠すのを防ぐ。
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            // 背景用の白い板は光の筋より後に描き、白さで筋も覆えるようにする。
            // 深度テストは有効なので、板より手前の不透明なキャラは残る。
            material.renderQueue = additive ? 3000 : 3100;
        }
        return material;
    }

    Transform MakeObject(string objectName, Transform parent)
    {
        var go = new GameObject(objectName) { hideFlags = HideFlags.HideAndDontSave };
        // ワールド座標を維持せず、親を基準とした初期位置・回転で生成する。
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    Transform MakeMesh(string objectName, Mesh mesh, Material material)
    {
        var t = MakeObject(objectName, generated);
        // 同じ板メッシュやマテリアルを共有し、光の枚数分の複製を避ける。
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        var renderer = t.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        // 発光を模した飾りなので、影や環境光の計算は利用しない。
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return t;
    }

    // 白い円、設定本数の光、背景を覆う板を生成する。
    void Build()
    {
        // 編集中も呼ばれるため、すでに生成済みなら二重に作らない。
        if (generated != null) 
            return;

        var solid = MakeMaterial(false);
        rayMaterial = MakeMaterial(true, true);
        whiteMaterial = MakeMaterial(true);

        if (solid == null) 
        {
            Clear(); return; 
        }

        generated = MakeObject("Generated Skyshaft (temporary)", transform);

        // 原点中心・幅1×高さ1の板。UVは光の画像を板全体へ貼るための座標。
        // 4頂点を2枚の三角形でつなぎ、光の筋と背景用の板で使い回す。
        var quad = Own(new Mesh { name = "Skyshaft quad" });
        quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        quad.RecalculateNormals(); quad.RecalculateBounds();
        rayQuad = quad;

        // 奥行き方向の光は、直交する2枚の板を1メッシュにまとめる。
        // 1枚だけの場合より、横から見たときに薄く消えてしまう角度を減らせる。
        depthCross = Own(new Mesh { name = "Skyshaft crossed ray" });
        depthCross.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0, -0.5f, -0.5f), new Vector3(0, -0.5f, 0.5f),
            new Vector3(0, 0.5f, 0.5f), new Vector3(0, 0.5f, -0.5f) };
        depthCross.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
            Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        depthCross.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6 };
        depthCross.RecalculateNormals(); depthCross.RecalculateBounds();

        // 中心1頂点と外周64頂点を三角形でつなぎ、半径1の円を作る。
        // 実際の半径はUpdateVisuals内のScaleで調整する。
        var disk = Own(new Mesh { name = "Skyshaft disk" });
        entranceMesh = disk;

        var vertices = new Vector3[65];
        entranceVertices = vertices;

        var triangles = new int[64 * 3];

        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            // 最後の外周頂点は最初の外周頂点につなぎ、円を閉じる。
            triangles[i * 3 + 2] = (i + 1) % 64 + 1;
        }

        disk.vertices = vertices; disk.triangles = triangles;
        disk.RecalculateNormals(); disk.RecalculateBounds();

        entrance = MakeMesh("WhiteEntrance", disk, solid);

        // 外部画像を使わず、端が透明で中心が明るい光の画像を一度だけ生成する。
        var texture = Own(new Texture2D(64, 128, TextureFormat.RGBA32, false));
        texture.name = "Skyshaft soft ray";
        texture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[64 * 128];

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                // Sinは両端で0、中央で1になる。縦横の値を掛けて四辺をぼかす。
                // さらに二乗して、板の輪郭が見えにくい細めの光にする。
                float across = Mathf.Sin(x / 63f * Mathf.PI);
                float along = Mathf.Sin(y / 127f * Mathf.PI);
                pixels[y * 64 + x] = new Color(1f, 0.94f, 0.8f, across * across * along * along);
            }
        }

        // GPUへ画像を転送したら、再編集しないCPU側の画像データは解放する。
        texture.SetPixels(pixels); texture.Apply(false, true);

        rayMaterial.SetTexture("_BaseMap", texture);
        background = MakeMesh("BackgroundWhite", quad, whiteMaterial);
        UpdateVisuals();
    }

    void SyncRayCount(List<Transform> group, int count, Mesh mesh, string prefix)
    {
        // 本数が変わった分だけ生成・破棄し、毎フレーム全体を作り直さない。
        // MeshとMaterialは共有するため、本数を増やしても素材は増えない。
        count = Mathf.Clamp(count, 0, 64);
        while (group.Count > count)
        {
            var last = group[group.Count - 1];
            group.RemoveAt(group.Count - 1);

            if (last == null) 
                continue;

            last.gameObject.SetActive(false);
            Dispose(last.gameObject);
        }
        while (group.Count < count)
            group.Add(MakeMesh(prefix + (group.Count + 1), mesh, rayMaterial));
    }

    Vector3 EntranceBoundary(float angle)
    {
        // スーパー楕円で円～角の丸い四角まで変形。符号を保って各軸を累乗する。
        float x = Mathf.Cos(angle);
        float y = Mathf.Sin(angle);
        float exponent = 2f / Mathf.Clamp(shapePower, 1f, 8f);

        // 周期の異なる波を混ぜ、規則的すぎない凹凸を作る。半径は常に正を保つ。
        float phase = edgePhase * Mathf.Deg2Rad;
        int waves = Mathf.Clamp(edgeWaves, 2, 12);
        float ripple = 1f + Mathf.Clamp(edgeDeformation, 0f, 0.4f) *
            (0.7f * Mathf.Sin(angle * waves + phase) + 0.3f * Mathf.Sin(angle * (waves + 1) + phase + 1.3f));

        return new Vector3(Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), exponent) * Mathf.Max(0.05f, entranceAspect.x),
            Mathf.Sign(y) * Mathf.Pow(Mathf.Abs(y), exponent) * Mathf.Max(0.05f, entranceAspect.y), 0) * ripple;
    }

    void UpdateEntranceShape()
    {
        var settings = new Vector4(shapePower, edgeDeformation, edgeWaves, edgePhase);
        // 設定変更時だけ頂点を更新。光の根元も同じ輪郭計算に追従する。
        if (shapeReady && previousAspect == entranceAspect && previousShape == settings) return;
        for (int i = 0; i < 64; i++)
            entranceVertices[i + 1] = EntranceBoundary(i * Mathf.PI * 2f / 64f);
        entranceMesh.vertices = entranceVertices;
        entranceMesh.RecalculateBounds();
        previousAspect = entranceAspect;
        previousShape = settings;
        shapeReady = true;
    }

    Camera ResolveCamera()
    {
        // 取得済みのカメラを再利用。タグ変更・無効化・破棄があれば探し直す。
        if (!string.IsNullOrEmpty(cameraTag) && cachedCamera != null && cachedCamera.isActiveAndEnabled && cachedCamera.tag == cameraTag)
            return cachedCamera;

        cachedCamera = null;

        // 有効なCameraから探すので、同じタグの別オブジェクトを誤取得しない。
        foreach (var candidate in Camera.allCameras)
        {
            if (string.IsNullOrEmpty(cameraTag) || candidate.tag != cameraTag) continue;
            cachedCamera = candidate;
            break;
        }

        // タグ取得が優先。手動指定は、該当する有効なカメラがない場合だけ使う。
        return cachedCamera != null ? cachedCamera :
            (viewCamera != null && viewCamera.isActiveAndEnabled ? viewCamera : null);
    }

    public void SetTaggedCharacter(Transform detectedPlayer)
    {
        // 接触で判別したPlayerは別に保持し、Inspectorの手動指定を上書きしない。
        taggedCharacter = detectedPlayer;
    }

    Transform ResolveCharacter()
    {
        var trigger = GetComponentInChildren<SkyshaftEntranceTrigger>(true);
        string tag = trigger != null ? trigger.playerTag : null;

        if (!string.IsNullOrEmpty(tag) && taggedCharacter != null &&
            taggedCharacter.gameObject.activeInHierarchy && taggedCharacter.tag == tag)
            return taggedCharacter;

        taggedCharacter = null;

        // 手動再生・プレビューでもタグ取得を優先。未生成のPlayerを探す頻度は抑える。
        if (!string.IsNullOrEmpty(tag) && Time.realtimeSinceStartup >= nextCharacterSearch)
        {
            nextCharacterSearch = Time.realtimeSinceStartup + 0.5f;
            try
            {
                var found = GameObject.FindGameObjectWithTag(tag);
                if (found != null) 
                    taggedCharacter = found.transform;
            }
            catch (UnityException)
            {
                // 未登録のタグ名をスクリプトから設定された場合も、手動指定で続行する。
            }
        }
        return taggedCharacter != null ? taggedCharacter : character;
    }

    // 再生中は「白くする → 維持 → 元に戻す」の順に透明度を更新する。
    void LateUpdate()
    {
        if (generated == null) 
            Build();

        if (Application.isPlaying && playing)
        {
            elapsed += Time.deltaTime;
            float attack = Mathf.Max(0.01f, fadeIn);
            float release = Mathf.Max(0.01f, fadeOut);

            // SmoothStepで開始・終了をなだらかにし、直線的な透明度変化を和らげる。
            if (elapsed < attack)
            { 
                previewWhiteout = Mathf.SmoothStep(0, 1, elapsed / attack);
            }
            else
            {
                // 白く覆い終えたら入口と光の筋を消し、背景用の板だけで復帰させる。
                // 維持時間中はSmoothStepへ負の値が入り、0に丸められるため白さ1を保つ。
                completed = true;
                previewWhiteout = 1f - Mathf.SmoothStep(0, 1, (elapsed - attack - hold) / release);
            }

            if (elapsed >= attack + hold + release)
            {
                playing = false;
                previewWhiteout = 0;
            }
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (generated == null) 
            return;

        entrance.localScale = Vector3.one * entranceRadius;
        UpdateEntranceShape();
        entrance.gameObject.SetActive(!completed);

        float time = Application.isPlaying ? Time.time : 0;
        SyncRayCount(rays, rayCount, rayQuad, "LightRay_");
        SyncRayCount(depthRays, depthRayCount, depthCross, "DepthRay_");

        for (int i = 0; i < rays.Count; i++)
        {
            // 本数に合わせて一周を等分。ずらす角度も間隔に比例させ、密集を避ける。
            // 時間と各板の位相をずらし、長さを基準値の60～100%でゆっくり揺らす。
            float step = 360f / rays.Count;
            float angle = i * step + (i % 2 == 0 ? -1f : 1f) * step * (6f / 45f);
            float length = rayLength * (0.8f + 0.2f * Mathf.Sin(time * 1.3f + i * 2.1f));
            var rotation = Quaternion.Euler(0, 0, angle);

            // 板の中心を外側へ移動し、根元が白い円と重なるように配置する。
            var start = EntranceBoundary((angle + 90f) * Mathf.Deg2Rad) * entranceRadius * 0.75f;
            rays[i].localPosition = start + rotation * new Vector3(0, length * 0.5f, -0.02f);
            rays[i].localRotation = rotation;
            rays[i].localScale = new Vector3(rayWidth * (i % 2 == 0 ? 1f : 0.7f), length, 1);
            rays[i].gameObject.SetActive(!completed);
        }

        float spread = depthSpreadAngle * Mathf.Deg2Rad;
        var normal = reverseDepthDirection ? Vector3.back : Vector3.forward;

        for (int i = 0; i < depthRays.Count; i++)
        {
            float angle = (i + 0.5f) * Mathf.PI * 2f / depthRays.Count;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

            // 法線方向と外向き方向を混ぜ、円から手前へ開く円錐状の光にする。
            var direction = normal * Mathf.Cos(spread) + radial * Mathf.Sin(spread);
            float length = rayLength * (0.8f + 0.2f * Mathf.Sin(time * 1.3f + i * 2.1f));

            // 根元を円内で分散させ、全ての筋が同じ一点に重なるのを避ける。
            var start = EntranceBoundary(angle) * entranceRadius * (i % 2 == 0 ? 0.35f : 0.7f) + normal * 0.02f;
            depthRays[i].localPosition = start + direction * length * 0.5f;

            // メッシュの長軸（ローカルY）を、光の進行方向に合わせる。
            depthRays[i].localRotation = Quaternion.FromToRotation(Vector3.up, direction);

            float width = rayWidth * (i % 2 == 0 ? 1f : 0.7f);
            depthRays[i].localScale = new Vector3(width, length, width);
            depthRays[i].gameObject.SetActive(!completed);
        }

        rayMaterial.SetColor("_BaseColor", new Color(1, 1, 1, rayOpacity));

        // タグからカメラを取得。白さ0なら背景用の板を非表示にして描画を省く。
        var camera = ResolveCamera();
        background.gameObject.SetActive(camera != null && previewWhiteout > 0);

        if (camera == null) 
            return;

        // カメラに近すぎて板が切れないように、Near Clip Planeより奥へ置く。
        float distance = Mathf.Max(camera.nearClipPlane + 0.1f, backgroundDistance);

        // キャラの描画範囲より奥へ白い板を置き、キャラを隠さず背景を白くする。
        var targetCharacter = ResolveCharacter();
        if (targetCharacter != null)
        {
            // 内積でカメラの前方向の距離を求める。直線距離ではなく奥行きとして扱う。
            float characterDepth = Vector3.Dot(targetCharacter.position - camera.transform.position, camera.transform.forward);
            foreach (var renderer in targetCharacter.GetComponentsInChildren<Renderer>())
            {
                var b = renderer.bounds;
                var f = camera.transform.forward;

                // 各RendererのBoundsをカメラ方向へ投影し、装備や手足も含む最奥を求める。
                float farDepth = Vector3.Dot(b.center - camera.transform.position, f)
                    + Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), Mathf.Abs(f.y), Mathf.Abs(f.z)));
                characterDepth = Mathf.Max(characterDepth, farDepth);
            }
            // 設定距離を最低値とし、必要ならキャラ最奥＋余白まで板を遠ざける。
            distance = Mathf.Max(distance, characterDepth + characterClearance);
        }

        // カメラへ追従させ、画面と平行な板として配置する。
        background.position = camera.transform.position + camera.transform.forward * distance;
        background.rotation = camera.transform.rotation;

        // 平行投影は表示高さが一定。透視投影は距離と縦画角から表示高さを逆算する。
        float height = camera.orthographic ? camera.orthographicSize * 2f : 2f * distance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);

        // 親の拡大率を補正し、白い板がカメラの画面全体を少し余裕を持って覆うようにする。
        background.localScale = Vector3.one;

        var inherited = background.lossyScale;
        background.localScale = new Vector3(
            height * camera.aspect * 1.1f / Mathf.Max(0.001f, Mathf.Abs(inherited.x)),
            height * 1.1f / Mathf.Max(0.001f, Mathf.Abs(inherited.y)), 1);

        whiteMaterial.SetColor("_BaseColor", new Color(1, 1, 1, previewWhiteout));
    }

    [ContextMenu("Play Whiteout (Play mode)")]
    public void PlayWhiteout()
    {
        if (!Application.isPlaying) 
            return;

        // 経過時間と入口の表示状態を戻し、途中からでも最初から再生できるようにする。
        elapsed = 0; completed = false; playing = true; previewWhiteout = 0;
        nextCharacterSearch = 0;
    }

    [ContextMenu("Reset Preview")]
    public void ResetPreview()
    {
        // 再生を止め、白い板を透明にし、入口と光の筋が見える初期状態へ戻す。
        playing = completed = false;
        previewWhiteout = 0;
        // 一度だけ発動する入口も、範囲へ入り直せば再び試せる状態に戻す。
        foreach (var trigger in GetComponentsInChildren<SkyshaftEntranceTrigger>(true))
            trigger.ResetTrigger();
        UpdateVisuals();
    }

    void Clear()
    {
        // 先に生成ルートごとパーツを破棄し、共有していた素材もまとめて解放する。
        if (generated != null) Dispose(generated.gameObject);
            generated = entrance = background = null;

        rays.Clear();
        depthRays.Clear();
        rayQuad = depthCross = null;
        entranceMesh = null;
        entranceVertices = null;
        shapeReady = false;
        cachedCamera = null;
        taggedCharacter = null;
        nextCharacterSearch = 0;

        foreach (var resource in resources) 
            if (resource != null) 
                Dispose(resource);

        resources.Clear();
    }

    static void Dispose(Object resource)
    {
        // 再生中はフレーム終端で破棄。編集時はフレーム更新を待たず即座に破棄する。
        if (Application.isPlaying) 
            Destroy(resource);
        else 
            DestroyImmediate(resource);
    }
}
