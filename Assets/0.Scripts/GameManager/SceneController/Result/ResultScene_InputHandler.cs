/*
    ResultScene_InputHandler
    20261104  hanaue sho
    リザルトシーンでの入力を受けとるスクリプト
    タイトルへ戻るボタン押下など
 */
using UnityEngine;

public class ResultScene_InputHandler : MonoBehaviour
{
    // --------------------------------------------------
    // ----- SceneController -----
    // --------------------------------------------------
    private SceneController_Result _controller;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        _controller = GetComponent<SceneController_Result>();

    }
}
