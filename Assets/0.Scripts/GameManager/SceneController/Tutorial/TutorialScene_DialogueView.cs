/*
    TutorialScene_DialogueView
    20261104  hanaue sho
    チュートリアルシーンでの会話パートの会話ボックスの表示非表示の管理
 */
using UnityEngine;

public class TutorialScene_DialogueView : MonoBehaviour
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
