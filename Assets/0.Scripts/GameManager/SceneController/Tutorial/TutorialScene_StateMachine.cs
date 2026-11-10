/*
    TutorialScene_StateMachine
    20261104  hanaue sho
    チュートリアルシーンの状態を管理するスクリプト
    以下は管理する状態フロー
    １．会話パートと操作パートを交互に管理
 */
using UnityEngine;

public class TutorialScene_StateMachine : MonoBehaviour
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
    private void Update()
    {

    }
}
