using UnityEngine;

// 壁などの障害物による音量・音質の変化を担当するScript

[RequireComponent(typeof(AudioSource))]
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

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;

    private float baseVolume;
    private bool isOccluded;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();

        if (listener == null)
        {
            Debug.LogError("Listenerが設定されていません。");
            enabled = false;
            return;
        }

        baseVolume = audioSource.volume;
        lowPassFilter.cutoffFrequency = openCutoffFrequency;
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
        float targetVolume = isOccluded ? baseVolume * blockedVolumeMultiplier : baseVolume;

        // 高音域を抑える
        float targetCutoff = isOccluded ? blockedCutoffFrequency : openCutoffFrequency;

        // 音量とカットオフ周波数を滑らかに変化
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume,
            Time.deltaTime * smoothSpeed);

        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency,
            targetCutoff, Time.deltaTime * smoothSpeed);
    }
}
