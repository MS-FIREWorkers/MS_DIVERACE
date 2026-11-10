/*
    LevelScene_PauseWindowHandler
    20261104  hanaue sho
    レベルシーンでポーズウィンドウに指示を出すスクリプト
    ポーズボタンを押した際にこのスクリプトを経由してポーズウィンドウを有効化、無効化を行う
 */
using UnityEngine;

public class LevelScene_PauseWindowHandler : MonoBehaviour
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
