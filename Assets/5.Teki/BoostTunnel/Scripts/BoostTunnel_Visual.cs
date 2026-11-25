/*
    BoostTunnel_Visual.cs
    20261124  Teki
    加速トンネルの見た目の制御
    ・外枠（Frame）と内壁パネル（PanelWall）を逆方向に回転させる
    ・プレイヤーが近づくと明るさ・回転速度を上げる
    ・プレイヤーが通過した瞬間にフラッシュさせる
    ・BoostRing_Controller と同じオブジェクト（トリガーを持つルート）に付ける
 */
using UnityEngine;

public class BoostTunnel_Visual : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Fields -----
    // ==================================================
    [Header("Rotation")]
    [Tooltip("外枠の回転速度（度/秒）")]
    [SerializeField] private float _frameRotationSpeed = 30.0f;

    [Tooltip("内壁パネルの回転速度（度/秒）。外枠と逆向きにするためマイナス")]
    [SerializeField] private float _panelRotationSpeed = -45.0f;

    [Header("Approach")]
    [Tooltip("この距離からプレイヤーの接近を反映し始める")]
    [Min(0.01f)]
    [SerializeField] private float _approachDistance = 40.0f;

    [Tooltip("待機中の明るさ")]
    [Min(0.0f)]
    [SerializeField] private float _idleIntensity = 1.0f;

    [Tooltip("最接近時の明るさ")]
    [Min(0.0f)]
    [SerializeField] private float _approachIntensity = 1.8f;

    [Tooltip("最接近時の回転速度の倍率")]
    [Min(0.0f)]
    [SerializeField] private float _approachRotationMultiplier = 2.5f;

    [Header("Pass Flash")]
    [Tooltip("通過フラッシュが消える速さ（1/秒）")]
    [Min(0.01f)]
    [SerializeField] private float _flashDecaySpeed = 2.5f;

    [Tooltip("通過フラッシュ中に追加される回転速度の倍率")]
    [Min(0.0f)]
    [SerializeField] private float _flashRotationMultiplier = 4.0f;

    // ==================================================
    // ----- Private Fields -----
    // ==================================================
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int FlashId = Shader.PropertyToID("_Flash");

    private Transform _frameTransform;
    private Transform _panelTransform;
    private Renderer[] _effectRenderers;
    private Transform _playerTransform;
    private MaterialPropertyBlock _propertyBlock;

    private float _approachRate;
    private float _flash;

    // ==================================================
    // ----- Unity Events -----
    // ==================================================
    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        CollectParts();
    }

    private void Start()
    {
        Player_CharacterController player = FindAnyObjectByType<Player_CharacterController>();
        if (player == null)
        {
            Debug.LogWarning("[Warning] Player_CharacterController Not Found! 接近演出は無効になります", this);
            return;
        }
        _playerTransform = player.transform;
    }

    private void Update()
    {
        UpdateApproach();
        UpdateFlash();
        UpdateRotation();
        ApplyMaterialParameters();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Player_CharacterController>() == null)
        {
            return;
        }
        PlayPassEffect();
    }

    // ==================================================
    // ----- Public Function -----
    // ==================================================

    // 通過演出を再生（外部からも呼べる）
    public void PlayPassEffect()
    {
        _flash = 1.0f;
    }

    // ==================================================
    // ----- Setup -----
    // ==================================================
    private void CollectParts()
    {
        BoostTunnel_ProceduralMesh[] parts = GetComponentsInChildren<BoostTunnel_ProceduralMesh>();
        int effectCount = 0;
        Renderer[] effects = new Renderer[parts.Length];

        foreach (BoostTunnel_ProceduralMesh part in parts)
        {
            switch (part.Shape)
            {
                case BoostTunnel_ProceduralMesh.MeshShape.Frame:
                    _frameTransform = part.transform;
                    break;
                case BoostTunnel_ProceduralMesh.MeshShape.PanelWall:
                    _panelTransform = part.transform;
                    effects[effectCount++] = part.GetComponent<Renderer>();
                    break;
                case BoostTunnel_ProceduralMesh.MeshShape.CoreDisc:
                    effects[effectCount++] = part.GetComponent<Renderer>();
                    break;
            }
        }

        _effectRenderers = new Renderer[effectCount];
        System.Array.Copy(effects, _effectRenderers, effectCount);

        if (_frameTransform == null)
        {
            Debug.LogWarning("[Warning] Frame (BoostTunnel_ProceduralMesh) Not Found!", this);
        }
        if (_panelTransform == null)
        {
            Debug.LogWarning("[Warning] PanelWall (BoostTunnel_ProceduralMesh) Not Found!", this);
        }
    }

    // ==================================================
    // ----- Update -----
    // ==================================================

    // プレイヤーとの距離から接近度（0 = 遠い / 1 = 最接近）を求める
    private void UpdateApproach()
    {
        if (_playerTransform == null)
        {
            _approachRate = 0.0f;
            return;
        }

        float distance = Vector3.Distance(_playerTransform.position, transform.position);
        float rate = 1.0f - Mathf.Clamp01(distance / _approachDistance);
        _approachRate = rate * rate * (3.0f - 2.0f * rate);
    }

    private void UpdateFlash()
    {
        _flash = Mathf.Max(0.0f, _flash - _flashDecaySpeed * Time.deltaTime);
    }

    // 外枠とパネルをローカル Z 軸（筒の軸）まわりに逆回転
    private void UpdateRotation()
    {
        float multiplier = Mathf.Lerp(1.0f, _approachRotationMultiplier, _approachRate)
                         + _flash * _flashRotationMultiplier;
        float deltaTime = Time.deltaTime;

        if (_frameTransform != null)
        {
            _frameTransform.Rotate(0.0f, 0.0f, _frameRotationSpeed * multiplier * deltaTime, Space.Self);
        }
        if (_panelTransform != null)
        {
            _panelTransform.Rotate(0.0f, 0.0f, _panelRotationSpeed * multiplier * deltaTime, Space.Self);
        }
    }

    private void ApplyMaterialParameters()
    {
        float intensity = Mathf.Lerp(_idleIntensity, _approachIntensity, _approachRate);

        foreach (Renderer effectRenderer in _effectRenderers)
        {
            if (effectRenderer == null)
            {
                continue;
            }
            effectRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(IntensityId, intensity);
            _propertyBlock.SetFloat(FlashId, _flash);
            effectRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
