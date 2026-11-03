using UnityEngine;

// 落下速度に応じた音量・ピッチの変化を担当するScript

[RequireComponent(typeof(AudioSource))]
public class PlayerFallAudioController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("落下速度設定（m/s）")]
    [SerializeField] private float minFallSpeed = 2f;
    [SerializeField] private float maxFallSpeed = 40f;

    [Header("音量設定")]
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 1f;

    [Header("ピッチ設定")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.3f;

    [Header("音の変化速度")]
    [SerializeField] private float smoothSpeed = 3f;

    private AudioSource audioSource;

    private float targetVolume;
    private float targetPitch;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

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

        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.pitch = minPitch;

        targetVolume = 0f;
        targetPitch = minPitch;

        audioSource.Play();
    }

    private void Update()
    {
        // Rigidbodyの垂直方向の速度 (落下速度) を取得
        float verticalSpeed = playerRigidbody.linearVelocity.y;

        // 下向きの速度だけを落下速度として扱う
        float fallSpeed = Mathf.Max(0f, -verticalSpeed);

        // 落下速度を0～1に正規化
        float speedRate = Mathf.InverseLerp(minFallSpeed, maxFallSpeed, fallSpeed);

        // 落下速度に応じて音量を計算
        targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRate);

        // 落下速度に応じてピッチを計算
        targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRate);

        // 音量とピッチを滑らかに変化させる
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume,
            Time.deltaTime * smoothSpeed);

        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch,
            Time.deltaTime * smoothSpeed);
    }
}
