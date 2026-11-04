/*
    TitleScene_InputHandler
    20261104  hanaue sho
    タイトルシーンでの入力を受けとるスクリプト
    スタートボタン押下など
 */
using UnityEngine;

public class TitleScene_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Title _controller;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Title>();

    }
}
