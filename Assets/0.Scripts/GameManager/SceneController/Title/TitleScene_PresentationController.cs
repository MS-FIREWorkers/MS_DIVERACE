/*
    TitleScene_PresentationController
    20261104  hanaue sho
    タイトルシーンでの演出を指示するスクリプト
    ロゴの表示、非表示や演出の再生の指示など
 */
using UnityEngine;

public class TitleScene_PresentationController : MonoBehaviour
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
