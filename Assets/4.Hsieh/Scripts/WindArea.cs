
/*
    WindArea.cs
    20261022  作成者：要記入
    範囲・風力・風向・パーティクル・境界線を一括管理する。
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoxCollider))]
public class WindArea : MonoBehaviour
{
    // ==================================================
    // ----- Area Settings -----
    // ==================================================

    [Header("風エリアの範囲")]
    [Tooltip("オブジェクトの中心を基準にした風エリアの大きさ。X・Y・Zを自由に調整できる。")]
    [SerializeField] private Vector3 _areaSize = new Vector3(100f, 80f, 30f);

    // ==================================================
    // ----- Wind Settings -----
    // ==================================================

    [Header("風向・風力")]
    [Tooltip("ローカル座標で設定する風向。X=左右、Y=上下、Z=前後。")]
    [SerializeField] private Vector3 _windDirection = Vector3.right;

    [Tooltip("風がプレイヤーに与える加速度。")]
    [Min(0f)]
    [SerializeField] private float _windAcceleration = 45f;

    [Tooltip("風向方向の最大速度。0の場合は制限なし。")]
    [Min(0f)]
    [SerializeField] private float _maxWindSpeed = 30f;

    [Header("突風")]
    [Tooltip("風力が周期的に変化するかどうか。")]
    [SerializeField] private bool _isGust = false;

    [Tooltip("突風の周期（Hz）。")]
    [Min(0.01f)]
    [SerializeField] private float _gustFrequency = 1f;

    [Tooltip("突風が最も弱いときの倍率。")]
    [Range(0f, 1f)]
    [SerializeField] private float _minGustMultiplier = 0.4f;

    // ==================================================
    // ----- Particle Settings -----
    // ==================================================

    [Header("風パーティクル")]
    [Tooltip("風パーティクルを表示する。")]
    [SerializeField] private bool _showWindParticles = true;

    [Tooltip("風パーティクル用マテリアル。未設定の場合はParticleSystemの既定マテリアルを使用する。")]
    [SerializeField] private Material _windMaterial;

    [Tooltip("単位体積あたりのパーティクル発生密度。範囲が広いほど発生量が増える。")]
    [Min(0f)]
    [SerializeField] private float _particleDensity = 0.001f;

    [Tooltip("1秒あたりの最大発生数。")]
    [Min(1f)]
    [SerializeField] private float _maxEmissionRate = 120f;

    [Tooltip("パーティクルの大きさ。")]
    [Min(0.01f)]
    [SerializeField] private float _particleSize = 0.8f;

    [Tooltip("パーティクルの寿命（秒）。")]
    [Min(0.05f)]
    [SerializeField] private float _particleLifetime = 0.8f;

    [Tooltip("風力を視覚エフェクトの移動速度に変換する倍率。")]
    [Min(0f)]
    [SerializeField] private float _visualSpeedRatio = 0.5f;

    [Header("範囲の表示")]
    [Tooltip("Sceneビューに風エリアの外枠を表示する。")]
    [SerializeField] private bool _showBoundsInScene = true;

    [Tooltip("Gameビューにも風エリアの外枠を表示する。デバッグ用。")]
    [SerializeField] private bool _showBoundsInGame = true;

    [Tooltip("Gameビューで表示する境界線の太さ。")]
    [Min(0.001f)]
    [SerializeField] private float _boundaryLineWidth = 0.2f;

    [Tooltip("境界線の色。")]
    [SerializeField] private Color _boundaryColor = Color.cyan;

    [Tooltip("境界線用マテリアル。未設定の場合はURP Unlitを自動生成する。")]
    [SerializeField] private Material _boundaryMaterial;

    // ==================================================
    // ----- Runtime Fields -----
    // ==================================================

    private BoxCollider _boxCollider;
    private ParticleSystem _windParticle;
    private Material _generatedBoundaryMaterial;

    private readonly Dictionary<Rigidbody, int> _overlapCounts =
        new Dictionary<Rigidbody, int>();

    private readonly HashSet<Rigidbody> _appliedThisStep =
        new HashSet<Rigidbody>();

    private static readonly Dictionary<Rigidbody, HashSet<WindArea>> _activeAreas =
        new Dictionary<Rigidbody, HashSet<WindArea>>();

    private readonly LineRenderer[] _boundaryLines = new LineRenderer[12];

    private Vector3 _lastAreaSize;
    private Vector3 _lastDirection;
    private float _lastAcceleration;
    private float _lastDensity;
    private float _lastEmissionRate;
    private float _lastParticleSize;
    private float _lastLifetime;
    private float _lastVisualSpeedRatio;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticData()
    {
        _activeAreas.Clear();
    }

    private void Reset()
    {
        ValidateArea();
    }


    private void OnValidate()
    {
        _areaSize.x = Mathf.Max(0.1f, _areaSize.x);
        _areaSize.y = Mathf.Max(0.1f, _areaSize.y);
        _areaSize.z = Mathf.Max(0.1f, _areaSize.z);

#if UNITY_EDITOR
    // Inspector変更後、Editorのメインスレッドで同期する。
    UnityEditor.EditorApplication.delayCall -= SyncAreaInEditor;
    UnityEditor.EditorApplication.delayCall += SyncAreaInEditor;
#endif
    }

#if UNITY_EDITOR
// ==================================================
// ----- Editor Area Synchronization -----
// ==================================================

private void SyncAreaInEditor()
{
    if (this == null || Application.isPlaying) return;

    // BoxColliderの大きさを同期する。
    ValidateArea();

    Transform particleTransform = transform.Find("Particle_Wind");
    if (particleTransform == null) return;

    // パーティクルの中心を風エリアの中心に合わせる。
    particleTransform.localPosition = Vector3.zero;
    particleTransform.localRotation = Quaternion.identity;
    particleTransform.localScale = Vector3.one;

    ParticleSystem particle = particleTransform.GetComponent<ParticleSystem>();
    if (particle == null) return;

    // パーティクルの発生範囲を同期する。
    var shape = particle.shape;
    shape.enabled = true;
    shape.shapeType = ParticleSystemShapeType.Box;
    shape.position = Vector3.zero;
    shape.rotation = Vector3.zero;
    shape.scale = _areaSize;
}
#endif


    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();

        if (_boxCollider == null)
        {
            Debug.LogError("[Error] WindArea BoxCollider not found.", this);
            enabled = false;
            return;
        }

        ValidateArea();
        InitializeParticles();
        UpdateParticleSettings();
        InitializeBoundary();

        if (_windDirection.sqrMagnitude < 0.0001f)
            Debug.LogWarning("[Warning] Wind direction is zero.", this);
    }

    private void OnEnable()
    {
        if (_windParticle != null && _showWindParticles)
            _windParticle.Play();
    }

    private void Update()
    {
        if (_boxCollider == null) return;

        // Inspectorの変更を実行中にも反映する。
        if (_lastAreaSize != _areaSize ||
            _lastDirection != _windDirection ||
            !Mathf.Approximately(_lastAcceleration, _windAcceleration) ||
            !Mathf.Approximately(_lastDensity, _particleDensity) ||
            !Mathf.Approximately(_lastEmissionRate, _maxEmissionRate) ||
            !Mathf.Approximately(_lastParticleSize, _particleSize) ||
            !Mathf.Approximately(_lastLifetime, _particleLifetime) ||
            !Mathf.Approximately(_lastVisualSpeedRatio, _visualSpeedRatio))
        {
            ValidateArea();
            UpdateParticleSettings();
            UpdateBoundaryPositions();
        }

        if (_windParticle != null)
        {
            if (_showWindParticles && !_windParticle.isPlaying)
                _windParticle.Play();
            else if (!_showWindParticles && _windParticle.isPlaying)
                _windParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        UpdateBoundaryVisibility();
    }

    private void FixedUpdate()
    {
        // 同じRigidbodyに複数Colliderがあっても、1物理更新につき1回だけ風力を加える。
        _appliedThisStep.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = GetTargetRigidbody(other);
        if (body == null) return;

        if (_overlapCounts.TryGetValue(body, out int count))
            _overlapCounts[body] = count + 1;
        else
        {
            _overlapCounts.Add(body, 1);
            RegisterActiveArea(body);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody body = GetTargetRigidbody(other);
        if (body == null) return;

        if (!_overlapCounts.ContainsKey(body))
        {
            _overlapCounts.Add(body, 1);
            RegisterActiveArea(body);
        }

        if (!_appliedThisStep.Add(body)) return;

        Vector3 direction = GetWorldDirection();
        if (direction.sqrMagnitude < 0.0001f) return;

        float speed = Vector3.Dot(body.linearVelocity, direction);
        if (_maxWindSpeed > 0f && speed >= _maxWindSpeed) return;

        float strength = _windAcceleration * GetGustMultiplier();
        body.AddForce(direction * strength, ForceMode.Acceleration);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !_overlapCounts.TryGetValue(body, out int count))
            return;

        if (count > 1)
        {
            _overlapCounts[body] = count - 1;
            return;
        }

        _overlapCounts.Remove(body);
        UnregisterActiveArea(body);
    }

    private void OnDisable()
    {
        foreach (Rigidbody body in _overlapCounts.Keys)
            UnregisterActiveArea(body);

        _overlapCounts.Clear();
        _appliedThisStep.Clear();

        if (_windParticle != null)
            _windParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (_generatedBoundaryMaterial != null)
            Destroy(_generatedBoundaryMaterial);
    }

    // ==================================================
    // ----- Area Settings -----
    // ==================================================

    private void ValidateArea()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        box.center = Vector3.zero;
        box.size = _areaSize;
        box.isTrigger = true;
    }

    public Vector3 GetWorldDirection()
    {
        if (_windDirection.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return transform.TransformDirection(_windDirection.normalized).normalized;
    }

    private float GetGustMultiplier()
    {
        if (!_isGust) return 1f;

        float wave = (Mathf.Sin(Time.time * _gustFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
        return Mathf.Lerp(_minGustMultiplier, 1f, wave);
    }

    private Rigidbody GetTargetRigidbody(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || body.isKinematic) return null;

        // テストプレイヤーだけに風を適用する。
        return body.GetComponent<TestPlayerController>() != null ? body : null;
    }

    // ==================================================
    // ----- Wind Registration -----
    // ==================================================

    private void RegisterActiveArea(Rigidbody body)
    {
        if (!_activeAreas.TryGetValue(body, out HashSet<WindArea> areas))
        {
            areas = new HashSet<WindArea>();
            _activeAreas.Add(body, areas);
        }

        areas.Add(this);
    }

    private void UnregisterActiveArea(Rigidbody body)
    {
        if (!_activeAreas.TryGetValue(body, out HashSet<WindArea> areas))
            return;

        areas.Remove(this);

        if (areas.Count == 0)
            _activeAreas.Remove(body);
    }

    public static bool IsInsideWind(Rigidbody body)
    {
        return body != null &&
               _activeAreas.TryGetValue(body, out HashSet<WindArea> areas) &&
               areas.Count > 0;
    }

    public static bool HasDownwardWind(Rigidbody body)
    {
        if (body == null || !_activeAreas.TryGetValue(body, out HashSet<WindArea> areas))
            return false;

        foreach (WindArea area in areas)
        {
            if (area != null && area.isActiveAndEnabled &&
                Vector3.Dot(area.GetWorldDirection(), Vector3.down) > 0.1f)
                return true;
        }

        return false;
    }

    // ==================================================
    // ----- Particle Initialization -----
    // ==================================================

    private void InitializeParticles()
    {
        Transform child = transform.Find("Particle_Wind");

        if (child == null)
        {
            GameObject obj = new GameObject("Particle_Wind");
            child = obj.transform;
            child.SetParent(transform, false);
        }

        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        _windParticle = child.GetComponent<ParticleSystem>();

        if (_windParticle == null)
            _windParticle = child.gameObject.AddComponent<ParticleSystem>();

        _windParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _windParticle.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startSpeed = 0f;
        main.startColor = new Color(0.65f, 0.9f, 1f, 0.65f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var shape = _windParticle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;

        var emission = _windParticle.emission;
        emission.enabled = true;

        var colorModule = _windParticle.colorOverLifetime;
        colorModule.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.8f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorModule.color = new ParticleSystem.MinMaxGradient(gradient);

        var velocity = _windParticle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;

        ParticleSystemRenderer particleRenderer =
            _windParticle.GetComponent<ParticleSystemRenderer>();

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.velocityScale = 0.1f;
        particleRenderer.lengthScale = 3f;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        if (_windMaterial != null)
            particleRenderer.sharedMaterial = _windMaterial;
    }

    private void UpdateParticleSettings()
    {
        if (_windParticle == null) return;

        // 範囲と発生量を同時に更新する。
        var shape = _windParticle.shape;
        shape.scale = _areaSize;

        float volume = _areaSize.x * _areaSize.y * _areaSize.z;
        float emissionRate = Mathf.Min(
            volume * _particleDensity, _maxEmissionRate
        );

        var emission = _windParticle.emission;
        emission.rateOverTime = emissionRate;

        var main = _windParticle.main;
        main.startSize = _particleSize;
        main.startLifetime = _particleLifetime;
        main.maxParticles = Mathf.Clamp(
            Mathf.CeilToInt(emissionRate * _particleLifetime * 2f),
            32, 1000
        );

        Vector3 direction = _windDirection.sqrMagnitude > 0.0001f
            ? _windDirection.normalized : Vector3.zero;

        float visualSpeed = _windAcceleration * _visualSpeedRatio;
        Vector3 velocityValue = direction * visualSpeed;

        var velocity = _windParticle.velocityOverLifetime;
        velocity.x = velocityValue.x;
        velocity.y = velocityValue.y;
        velocity.z = velocityValue.z;

        _lastAreaSize = _areaSize;
        _lastDirection = _windDirection;
        _lastAcceleration = _windAcceleration;
        _lastDensity = _particleDensity;
        _lastEmissionRate = _maxEmissionRate;
        _lastParticleSize = _particleSize;
        _lastLifetime = _particleLifetime;
        _lastVisualSpeedRatio = _visualSpeedRatio;
    }

    // ==================================================
    // ----- Runtime Boundary -----
    // ==================================================

    private void InitializeBoundary()
    {
        if (!_showBoundsInGame) return;

        Material material = _boundaryMaterial;

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                Debug.LogWarning("[Warning] URP Unlit Shader not found. Game boundary is disabled.", this);
                return;
            }

            _generatedBoundaryMaterial = new Material(shader);
            _generatedBoundaryMaterial.SetColor("_BaseColor", _boundaryColor);
            material = _generatedBoundaryMaterial;
        }

        for (int i = 0; i < _boundaryLines.Length; i++)
        {
            GameObject lineObject = new GameObject($"Line_Boundary_{i + 1:D2}");
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.sharedMaterial = material;
            line.widthMultiplier = _boundaryLineWidth;
            line.startColor = _boundaryColor;
            line.endColor = _boundaryColor;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            _boundaryLines[i] = line;
        }

        UpdateBoundaryPositions();
    }

    private void UpdateBoundaryPositions()
    {
        Vector3 h = _areaSize * 0.5f;

        Vector3[] corners =
        {
            new Vector3(-h.x, -h.y, -h.z),
            new Vector3( h.x, -h.y, -h.z),
            new Vector3(-h.x,  h.y, -h.z),
            new Vector3( h.x,  h.y, -h.z),
            new Vector3(-h.x, -h.y,  h.z),
            new Vector3( h.x, -h.y,  h.z),
            new Vector3(-h.x,  h.y,  h.z),
            new Vector3( h.x,  h.y,  h.z)
        };

        int[,] edges =
        {
            {0,1}, {1,3}, {3,2}, {2,0},
            {4,5}, {5,7}, {7,6}, {6,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        for (int i = 0; i < _boundaryLines.Length; i++)
        {
            if (_boundaryLines[i] == null) continue;

            _boundaryLines[i].SetPosition(0, corners[edges[i, 0]]);
            _boundaryLines[i].SetPosition(1, corners[edges[i, 1]]);
        }
    }

    private void UpdateBoundaryVisibility()
    {
        if (_showBoundsInGame && _boundaryLines[0] == null)
            InitializeBoundary();

        foreach (LineRenderer line in _boundaryLines)
        {
            if (line == null) continue;

            line.enabled = _showBoundsInGame;
            line.widthMultiplier = _boundaryLineWidth;
        }

        if (_generatedBoundaryMaterial != null)
            _generatedBoundaryMaterial.SetColor("_BaseColor", _boundaryColor);
    }

    // ==================================================
    // ----- Scene Gizmos -----
    // ==================================================

    private void OnDrawGizmos()
    {
        if (!_showBoundsInScene) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, _areaSize);

        Gizmos.matrix = Matrix4x4.identity;

        Vector3 origin = transform.position;
        Vector3 direction = GetWorldDirection();
        Vector3 end = origin + direction * Mathf.Max(5f, _areaSize.magnitude * 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(end, Mathf.Max(0.3f, _areaSize.magnitude * 0.01f));
    }
}
