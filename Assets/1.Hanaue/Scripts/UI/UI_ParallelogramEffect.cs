
/*
    UI_ParallelogramEffect
    20261117  hanaue sho
    UI_Parallelogram.shader へのエフェクトの指示を送る
 */
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_ParallelogramEffect : MonoBehaviour
{
    // Shader Property
    private static readonly int BorderLightStartTimeID = Shader.PropertyToID("_BorderLightStartTime");
    private static readonly int HighlightStartTimeID = Shader.PropertyToID("_HighlightStartTime");

    private Image _image;
    private Material _materialInstance;

    // ==================================================
    // ----- Awake -----
    // ==================================================
    private void Awake()
    {
        _image = GetComponent<Image>();

        // ボタンごとにマテリアルを独立させる
        if (_image.material == null)
        {
            Debug.LogWarning("[Warning] UI_ParallelogramEffect: Materialが設定されていません。");
            return;
        }

        _materialInstance = new Material(_image.material);
        _image.material = _materialInstance;

        // 初期状態は光を再生しない
        _materialInstance.SetFloat(BorderLightStartTimeID, -1.0f);
        _materialInstance.SetFloat(HighlightStartTimeID, -1.0f);
    }

    // ==================================================
    // ----- Play Border Light -----
    // 枠線を一周する光を再生する
    // ==================================================
    public void PlayBorderLight()
    {
        if (_materialInstance == null)
        {
            return;
        }

        // 再生開始時間をシェーダーへ渡す
        _materialInstance.SetFloat(BorderLightStartTimeID, Time.timeSinceLevelLoad);
    }

    // ==================================================
    // ----- Play Highlight -----
    // 面のハイライトを左から右へ再生する
    // ==================================================
    public void PlayHighlight()
    {
        if (_materialInstance == null)
        {
            return;
        }

        // 再生開始時間をシェーダーへ渡す
        _materialInstance.SetFloat(HighlightStartTimeID, Time.timeSinceLevelLoad);
    }

    // ==================================================
    // ----- On Destroy -----
    // ==================================================
    private void OnDestroy()
    {
        // 生成したマテリアルを破棄
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }
}
