/*
    LevelScene_GoalTrigger
    20261122  hanaue sho
    ゴールを通知するスクリプト
    ゴールのコライダーにつける
 */
using UnityEngine;

public class LevelScene_GoalTrigger : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    [SerializeField] private SceneController_Level _controller;


    // --------------------------------------------------
    // ----- UnityEvents -----
    // --------------------------------------------------
    private void Awake()
    {
        _controller = FindFirstObjectByType<SceneController_Level>();
    }
    private void OnTriggerEnter(Collider other)
    {

        Debug.Log("[Debug] OnTriggerEnter");

        // プレイヤーキャラクター判定
        Player_CharacterController player = other.GetComponentInParent<Player_CharacterController>();
        if (player == null)
        {
            return;
        }

        // 登録されているプレイヤーと一致するか確認
        if (player != _controller.Player)
        {
            return;
        }

        // ゴール通知
        _controller.OnPlayerGoal();
    }
}
