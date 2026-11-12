using UnityEngine;

// 音源とListenerの相対速度に応じた音量補正を担当するScript

[RequireComponent(typeof(AudioSource))]
public class RelativeAudioController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform listener;

    [Header("音量制御")]
    [SerializeField] private float maxRelativeSpeed = 40f;
    [SerializeField] private float approachBoost = 0.5f;
    [SerializeField] private float volumeSmoothSpeed = 5f;

    private AudioSource audioSource;

    private Vector3 previousSourcePosition;
    private Vector3 previousListenerPosition;

    private Vector3 sourceVelocity;
    private Vector3 listenerVelocity;

    private float baseVolume;
    private float targetVolume;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (listener == null)
        {
            Debug.LogError("Listenerが設定されていません");
            enabled = false;
            return;
        }

        previousSourcePosition = transform.position;
        previousListenerPosition = listener.position;

        baseVolume = audioSource.volume;
        targetVolume = baseVolume;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        // 音源とプレイヤーの速度を計算
        sourceVelocity = (transform.position - previousSourcePosition) / deltaTime;
        listenerVelocity = (listener.position - previousListenerPosition) / deltaTime;

        // 次のフレーム用に位置を保存
        previousSourcePosition = transform.position;
        previousListenerPosition = listener.position;

        // 音源からプレイヤーへ向かう方向を計算
        Vector3 directionToSource = (transform.position - listener.position).normalized;

        // 音源とプレイヤーの相対速度を計算
        Vector3 relativeVelocity = sourceVelocity - listenerVelocity;

        // 接近している速度だけを取り出す。距離が増える方向を正、近づく方向を負とする
        float radialSpeed = Vector3.Dot(relativeVelocity, directionToSource);

        // 接近しているときだけ音量を強める
        float approachSpeed = Mathf.Max(0f, -radialSpeed);
        float speedRate = Mathf.Clamp01(approachSpeed / maxRelativeSpeed);

        // 接近速度に応じて音量を強める
        float volumeMultiplier = 1f + speedRate * approachBoost;
        targetVolume = baseVolume * volumeMultiplier;

        // 音量を滑らかに変化させる
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume,
            Time.deltaTime * volumeSmoothSpeed);
    }
}
