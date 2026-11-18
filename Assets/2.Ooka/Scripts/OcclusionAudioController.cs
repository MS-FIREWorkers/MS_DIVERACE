using UnityEngine;

// 壁などの障害物による音量・音質の変化を担当するScript
// 音量はManagedAudioSourceの補正値として登録する (AudioSource.volumeは直接書き換えない)

[RequireComponent(typeof(ManagedAudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class OcclusionAudioController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform listener;

    [Header("遮蔽判定")]
    [SerializeField] private LayerMask occlusionLayer;

    [Header("音量設定")]
    [SerializeField, Range(0f, 1f)]
    private float blockedVolumeMultiplier = 0.35f;

    [Header("フィルター設定")]
    [SerializeField] private float openCutoffFrequency = 22000f;
    [SerializeField] private float blockedCutoffFrequency = 1200f;

    [Header("変化速度")]
    [SerializeField] private float smoothSpeed = 5f;

    private ManagedAudioSource managedAudioSource;
    private AudioLowPassFilter lowPassFilter;

    private float currentMultiplier = 1f;
    private bool isOccluded;

    private void Start()
    {
        managedAudioSource = GetComponent<ManagedAudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();

        if (listener == null)
        {
            Debug.LogError("Listenerが設定されていません。");
            enabled = false;
            return;
        }

        lowPassFilter.cutoffFrequency = openCutoffFrequency;
        managedAudioSource.SetVolumeMultiplier(this, currentMultiplier);
    }

    private void OnDisable()
    {
        // 無効になったときは補正を取り消して、音量を元に戻す
        if (managedAudioSource != null)
        {
            managedAudioSource.RemoveVolumeMultiplier(this);
        }
    }

    private void OnEnable()
    {
        // 再度有効になったときは、現在の補正値を登録し直す
        if (managedAudioSource != null)
        {
            managedAudioSource.SetVolumeMultiplier(this, currentMultiplier);
        }
    }

    private void Update()
    {
        Vector3 direction = transform.position - listener.position;

        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            isOccluded = false;
        }
        else
        {
            // Listenerから音源に向けてRaycastによる遮蔽判定
            isOccluded = Physics.Raycast(listener.position, direction.normalized,
                distance, occlusionLayer);
        }

        // 遮蔽状態に応じて目標値 (遮蔽中の音量補正) を決める
        float targetMultiplier = isOccluded ? blockedVolumeMultiplier : 1f;

        // 高音域を抑える
        float targetCutoff = isOccluded ? blockedCutoffFrequency : openCutoffFrequency;

        // 音量補正とカットオフ周波数を滑らかに変化
        currentMultiplier = Mathf.Lerp(currentMultiplier, targetMultiplier,
            Time.deltaTime * smoothSpeed);
        managedAudioSource.SetVolumeMultiplier(this, currentMultiplier);

        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency,
            targetCutoff, Time.deltaTime * smoothSpeed);
    }
}
