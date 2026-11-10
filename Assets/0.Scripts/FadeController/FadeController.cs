/*
    FadeController
    20261103  hanaue sho
    フェードを管理するスクリプト
    シングルトン化して
    フェードの内部処理を実装。
 */
using UnityEngine;
using System.Collections;

public class FadeController : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Singleton -----
    // --------------------------------------------------
    public static FadeController Instance { get; private set; }

    // --------------------------------------------------
    // ----- FadeProperty -----
    // --------------------------------------------------
    [Header("FadeCanvas")]
    [SerializeField] private CanvasGroup _fadeCanvas;

    // --------------------------------------------------
    // ----- Parameters -----
    // --------------------------------------------------
    [Header("Duration")]
    [SerializeField] private float _fadeDuration = 0.5f;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        // ----- Singleton -----
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Canvas
        if (_fadeCanvas == null)
        {
            _fadeCanvas = GetComponentInChildren<CanvasGroup>(true);
        }
        if (_fadeCanvas == null)
        {
            Debug.LogWarning("[Warning] FadeCanvas Not Found!!");
        }
        else
        {
            // 有効化
            _fadeCanvas.gameObject.SetActive(true);
            // 初期化
            _fadeCanvas.alpha = 0.0f;
            _fadeCanvas.blocksRaycasts = false;
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // ==================================================
    // ----- Fade -----
    // ==================================================
    public IEnumerator FadeIn()
    {
        yield return Fade(1.0f, 0.0f);
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(0.0f, 1.0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        // セーフティ
        if (_fadeCanvas == null)
        {
            yield break;
        }

        _fadeCanvas.blocksRaycasts = true;

        float elapsed = 0.0f;
        float duration = Mathf.Max(0.0f, _fadeDuration);

        _fadeCanvas.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            _fadeCanvas.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        _fadeCanvas.alpha = to;

        // 透明になった場合は入力を通す
        _fadeCanvas.blocksRaycasts = to > 0.0f;
    }

}
