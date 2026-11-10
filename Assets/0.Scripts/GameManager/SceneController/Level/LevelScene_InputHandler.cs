/*
    LevelScene_InputHandler
    20261104  hanaue sho
    レベルシーンでの入力を受けとるスクリプト
    ポーズボタン押下など
 */
using UnityEngine;

public class LevelScene_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Level _controller;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Level>();

    }
}
