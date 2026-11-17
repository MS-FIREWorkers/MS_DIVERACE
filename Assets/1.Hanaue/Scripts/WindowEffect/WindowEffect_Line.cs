/*
    WindowEffect_Line
    20261117  hanaue sho
    ウィンドウエフェクトラインの動き制御
 */
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class WindEffect_Line : MonoBehaviour
{
    // ==================================================
    // ----- Components -----
    // ==================================================
    private RectTransform _rectTransform;
    private Image _image;

    // ==================================================
    // ----- References -----
    // ==================================================
    private WindEffect_Projection _projection;

    // ==================================================
    // ----- Parameters -----
    // ==================================================
    private Vector3 _position;

    private float _speed;
    private float _length;
    private float _width;
    private float _vanishDepth;

    private Color _color;

    private bool _isActive;

    // ==================================================
    // ----- UnityEvents -----
    // ==================================================
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();

        // 線の中心を基準にする
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        _isActive = false;
    }
    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        // 奥行き方向へ移動
        _position.z += _speed * Time.deltaTime;

        // 消滅判定
        if (_position.z >= _vanishDepth)
        {
            Deactivate();
            return;
        }

        // UI描画更新
        UpdateVisual();
    }

    // ==================================================
    // ----- Initialize -----
    // ==================================================
    public void Initialize(
                           WindEffect_Projection projection,
                           Vector3 position,
                           float speed,
                           float length,
                           float width,
                           float vanishDepth,
                           Color color
                          )
    {
        _projection = projection;

        _position = position;

        _speed = speed;
        _length = length;
        _width = width;
        _vanishDepth = vanishDepth;

        _color = color;

        _isActive = true;

        _image.color = _color;

        gameObject.SetActive(true);

        UpdateVisual();
    }

    // ==================================================
    // ----- Update Visual -----
    // ==================================================
    private void UpdateVisual()
    {
        if (_projection == null)
        {
            return;
        }

        // 仮想3D空間での線の両端
        Vector3 startPosition = _position;
        Vector3 endPosition = _position + Vector3.forward * _length;

        // UI座標へ投影
        Vector2 startUI = _projection.Project(startPosition);
        Vector2 endUI = _projection.Project(endPosition);

        // 線の中心
        Vector2 center = (startUI + endUI) * 0.5f;

        // 線の方向
        Vector2 direction = endUI - startUI;

        // UI上の長さ
        float screenLength = direction.magnitude;

        // 奥行きに応じた太さ
        float screenWidth = _width * _projection.GetScale(_position.z);

        // 位置
        _rectTransform.anchoredPosition = center;

        // 回転
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);

        // サイズ
        _rectTransform.sizeDelta = new Vector2(screenLength, screenWidth);
    }

    // ==================================================
    // ----- Deactivate -----
    // ==================================================
    private void Deactivate()
    {
        _isActive = false;

        gameObject.SetActive(false);
    }
}
