/*
    TutorialScene_GuideView
    20261104  hanaue sho
    チュートリアルシーンの操作パートのガイドの表示を管理するスクリプト
 */
using UnityEngine;

public class TutorialScene_GuidView : MonoBehaviour
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
