using UnityEngine;

// 落下速度に応じた音量・ピッチの変化を担当するScript
// 音量はManagedAudioSourceの補正値として登録する (AudioSource.volumeは直接書き換えない)
// ピッチはこのScriptだけが書き換える
// ループ設定と再生開始は、ManagedAudioSource (AudioDataのloop / playOnStart) に任せる

[RequireComponent(typeof(ManagedAudioSource))]
public class PlayerFallAudioController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("落下速度設定（m/s）")]
    [SerializeField] private float minFallSpeed = 2f;
    [SerializeField] private float maxFallSpeed = 40f;

    [Header("音量設定 (AudioDataの基準音量に対する倍率)")]
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 1f;

    [Header("ピッチ設定")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.3f;

    [Header("音の変化速度")]
    [SerializeField] private float smoothSpeed = 3f;

    private ManagedAudioSource managedAudioSource;
    private AudioSource audioSource;

    private float currentMultiplier;
    private float targetMultiplier;
    private float targetPitch;

    private void Awake()
    {
        managedAudioSource = GetComponent<ManagedAudioSource>();
        audioSource = GetComponent<AudioSource>();

        // 再生開始の時点で無音になるよう、Awakeのうちに補正値を0にしておく
        currentMultiplier = minVolume;
        managedAudioSource.SetVolumeMultiplier(this, currentMultiplier);

        audioSource.pitch = minPitch;
    }

    private void Start()
    {
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        if (playerRigidbody == null)
        {
            Debug.LogError("PlayerFallAudioControllerにRigidbodyが設定されていません");

            enabled = false;
            return;
        }

        // audioSource.pitch = minPitch;

        targetMultiplier = minVolume;
        targetPitch = minPitch;
    }

    // 無効になっても補正値は取り消さない (取り消すと風の音が基準音量まで戻ってしまうため)

    private void Update()
    {
        // Rigidbodyの垂直方向の速度 (落下速度) を取得
        float verticalSpeed = playerRigidbody.linearVelocity.y;

        // 下向きの速度だけを落下速度として扱う
        float fallSpeed = Mathf.Max(0f, -verticalSpeed);

        // 落下速度を0～1に正規化
        float speedRate = Mathf.InverseLerp(minFallSpeed, maxFallSpeed, fallSpeed);

        // 落下速度に応じて音量の倍率を計算
        targetMultiplier = Mathf.Lerp(minVolume, maxVolume, speedRate);

        // 落下速度に応じてピッチを計算
        targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRate);

        // 音量の倍率とピッチを滑らかに変化させる
        currentMultiplier = Mathf.Lerp(currentMultiplier, targetMultiplier,
            Time.deltaTime * smoothSpeed);
        managedAudioSource.SetVolumeMultiplier(this, currentMultiplier);

        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch,
            Time.deltaTime * smoothSpeed);
    }
}
