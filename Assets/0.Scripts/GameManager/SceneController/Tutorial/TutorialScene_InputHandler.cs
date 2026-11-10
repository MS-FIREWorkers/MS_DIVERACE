/*
    TutorialScene_InputHandler
    20261104  hanaue sho
    チュートリアルシーンでの入力を受けとるスクリプト
    会話送りボタン押下など
 */
using UnityEngine;

public class TutorialScene_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Tutorial _controller;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Tutorial>();

    }
}
