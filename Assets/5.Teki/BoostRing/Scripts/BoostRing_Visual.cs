/*
    BoostRing_Visual.cs
    20261125  Teki
    加速リングの見た目の制御
    ・子の BoostRing_Layer をそれぞれの回転速度で回す
    ・プレイヤーが近づくと明るさ・回転速度を上げる
    ・プレイヤーが通過した瞬間にフラッシュさせる
    ・BoostRing_Controller と同じオブジェクト（トリガーを持つルート）に付ける
 */
using UnityEngine;

public class BoostRing_Visual : MonoBehaviour
{
    // ==================================================
    // ----- Inspector Fields -----
    // ==================================================
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

    private BoostRing_Layer[] _layers;
    private Renderer[] _layerRenderers;
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

        _layers = GetComponentsInChildren<BoostRing_Layer>();
        if (_layers.Length == 0)
        {
            Debug.LogWarning("[Warning] BoostRing_Layer Not Found!", this);
        }

        _layerRenderers = new Renderer[_layers.Length];
        for (int i = 0; i < _layers.Length; i++)
        {
            _layerRenderers[i] = _layers[i].GetComponent<Renderer>();
        }
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

    // 各レイヤーをローカル Z 軸（リングの軸）まわりに回転
    private void UpdateRotation()
    {
        float multiplier = Mathf.Lerp(1.0f, _approachRotationMultiplier, _approachRate)
                         + _flash * _flashRotationMultiplier;
        float deltaTime = Time.deltaTime;

        foreach (BoostRing_Layer layer in _layers)
        {
            if (layer == null || Mathf.Approximately(layer.RotationSpeed, 0.0f))
            {
                continue;
            }
            layer.transform.Rotate(0.0f, 0.0f, layer.RotationSpeed * multiplier * deltaTime, Space.Self);
        }
    }

    private void ApplyMaterialParameters()
    {
        float intensity = Mathf.Lerp(_idleIntensity, _approachIntensity, _approachRate);

        foreach (Renderer layerRenderer in _layerRenderers)
        {
            if (layerRenderer == null)
            {
                continue;
            }
            layerRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(IntensityId, intensity);
            _propertyBlock.SetFloat(FlashId, _flash);
            layerRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
