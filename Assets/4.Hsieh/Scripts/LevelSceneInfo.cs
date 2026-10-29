using UnityEngine;

public class LevelSceneInfo : MonoBehaviour
{
    [SerializeField] private Transform topPoint;       // 上端
    [SerializeField] private Transform bottomPoint;    // 下端

    public Transform TopPoint => topPoint;
    public Transform BottomPoint => bottomPoint;

    // TopPointとBottomPointが設定されているか確認
    public bool IsValid()
    {
        return topPoint != null && bottomPoint != null;
    }
}