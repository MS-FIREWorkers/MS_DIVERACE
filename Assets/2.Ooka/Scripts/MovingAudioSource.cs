using UnityEngine;

// 音源の位置を移動させる検証用Script

public class MovingAudioSource : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("音源の移動開始位置")] [SerializeField] private Vector3 startPosition = new Vector3(-20f, 0f, 0f);
    [Tooltip("音源の移動終了位置")] [SerializeField] private Vector3 endPosition = new Vector3(20f, 0f, 0f);
    [Tooltip("移動速度（Unityのワールド単位/秒）")] [SerializeField] private float speed = 5f;

    private void Start()
    {
        // 音源を開始位置に配置する
        transform.position = startPosition;
    }

    private void Update()
    {
        // 終了位置に向かって一定速度で移動する
        transform.position = Vector3.MoveTowards(transform.position, endPosition, speed * Time.deltaTime);
    }
}
