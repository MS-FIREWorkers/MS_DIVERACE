using UnityEngine;

// 音源とListenerの相対速度に応じた音量補正を担当するScript
// 音量はManagedAudioSourceの補正値として登録する (AudioSource.volumeは直接書き換えない)
//
// 接近ブーストで音量を上げるには、AudioDataのvolumeに余裕が必要
// (最終的なAudioSource.volumeは1が上限のため。例: volume 0.6 × 最大1.5倍 = 0.9)

[RequireComponent(typeof(ManagedAudioSource))]
public class RelativeAudioController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform listener;

    [Header("音量制御")]
    [SerializeField] private float maxRelativeSpeed = 40f;
    [SerializeField] private float approachBoost = 0.5f;
    [SerializeField] private float volumeSmoothSpeed = 5f;

    private ManagedAudioSource managedAudioSource;

    private Vector3 previousSourcePosition;
    private Vector3 previousListenerPosition;

    private Vector3 sourceVelocity;
    private Vector3 listenerVelocity;

    private float currentMultiplier = 1f;

    private void Start()
    {
        managedAudioSource = GetComponent<ManagedAudioSource>();

        if (listener == null)
        {
            Debug.LogError("Listenerが設定されていません");
            enabled = false;
            return;
        }

        previousSourcePosition = transform.position;
        previousListenerPosition = listener.position;

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

        // プレイヤーから音源へ向かう方向を計算
        Vector3 directionToSource = (transform.position - listener.position).normalized;

        // 音源とプレイヤーの相対速度を計算
        Vector3 relativeVelocity = sourceVelocity - listenerVelocity;

        // 接近している速度だけを取り出す。距離が増える方向を正、近づく方向を負とする
        float radialSpeed = Vector3.Dot(relativeVelocity, directionToSource);

        // 接近しているときだけ音量を強める
        float approachSpeed = Mathf.Max(0f, -radialSpeed);
        float speedRate = Mathf.Clamp01(approachSpeed / maxRelativeSpeed);

        // 接近速度に応じて音量補正を強める
        float targetMultiplier = 1f + speedRate * approachBoost;

        // 音量補正を滑らかに変化させる
        currentMultiplier = Mathf.Lerp(currentMultiplier, targetMultiplier,
            Time.deltaTime * volumeSmoothSpeed);
        managedAudioSource.SetVolumeMultiplier(this, currentMultiplier);
    }
}
