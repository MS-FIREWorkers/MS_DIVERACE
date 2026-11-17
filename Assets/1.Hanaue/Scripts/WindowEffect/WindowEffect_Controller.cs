
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class WindEffect_Controller : MonoBehaviour
{
    // ==================================================
    // ----- References -----
    // ==================================================
    [Header("References")]
    [SerializeField] private WindEffect_Line _linePrefab;
    private RectTransform _rectTransform;
    private WindEffect_Projection _projection;

    // ==================================================
    // ----- Projection -----
    // ==================================================
    [Header("Projection")]
    [SerializeField] private Vector2 _vanishingPoint = new Vector2(0.5f, 0.5f);
    [SerializeField, Min(0.001f)] private float _focalLength = 100.0f;

    // ==================================================
    // ----- Spawn -----
    // ==================================================
    [Header("Spawn")]
    [SerializeField, Min(0.001f)] private float _spawnInterval = 0.1f;
    [SerializeField] private Vector2 _spawnRadius = new Vector2(5.0f, 10.0f);
    [SerializeField, Min(0.001f)] private float _spawnDepth = 1.0f;

    // ==================================================
    // ----- Movement -----
    // ==================================================
    [Header("Movement")]
    [SerializeField] private Vector2 _speed = new Vector2(5.0f, 10.0f);
    [SerializeField] private float _vanishDepth = 30.0f;

    // ==================================================
    // ----- Appearance -----
    // ==================================================
    [Header("Appearance")]
    [SerializeField] private Vector2 _lineLength = new Vector2(1.0f, 3.0f);
    [SerializeField] private Vector2 _lineWidth = new Vector2(0.01f, 0.03f);
    [SerializeField] private Color _lineColor = Color.white;

    // ==================================================
    // ----- Pool -----
    // ==================================================
    [Header("Pool")]
    [SerializeField, Min(1)] private int _maxLineCount = 50;

    private List<WindEffect_Line> _lines = new List<WindEffect_Line>();

    private float _spawnTimer;

    // ==================================================
    // ----- UnityEvents -----
    // ==================================================
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        // Projection生成
        _projection = new WindEffect_Projection(Vector2.zero, _focalLength);

        UpdateProjection();

        // オブジェクトプール生成
        CreatePool();
    }
    private void Update()
    {
        // Projection更新
        UpdateProjection();

        // 生成タイマー
        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0.0f)
        {
            SpawnLine();
            _spawnTimer = Mathf.Max(_spawnInterval, 0.001f);
        }
    }

    // ==================================================
    // ----- Projection -----
    // ==================================================
    private void UpdateProjection()
    {
        Rect rect = _rectTransform.rect;

        // 正規化座標をUIローカル座標へ変換
        Vector2 point = new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, _vanishingPoint.x),
            Mathf.Lerp(rect.yMin, rect.yMax, _vanishingPoint.y)
        );

        _projection.SetVanishingPoint(point);
        _projection.SetFocalLength(_focalLength);
    }

    // ==================================================
    // ----- Pool -----
    // ==================================================
    private void CreatePool()
    {
        if (_linePrefab == null)
        {
            Debug.LogWarning("[Warning] WindEffect_Line Prefab is not assigned.");
            return;
        }

        for (int i = 0; i < _maxLineCount; i++)
        {
            WindEffect_Line line = Instantiate(_linePrefab, transform);

            line.gameObject.SetActive(false);

            _lines.Add(line);
        }
    }

    // ==================================================
    // ----- Spawn -----
    // ==================================================
    private void SpawnLine()
    {
        WindEffect_Line line = GetAvailableLine();

        if (line == null)
        {
            return;
        }

        // 生成角度
        float angle = Random.Range(0.0f, Mathf.PI * 2.0f);

        // 生成半径
        float minRadius = Mathf.Max(0.0f, Mathf.Min(_spawnRadius.x, _spawnRadius.y));
        float maxRadius = Mathf.Max(minRadius, Mathf.Max(_spawnRadius.x, _spawnRadius.y));

        // 円環の面積に対して均等なランダム分布
        float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));

        // 仮想3D座標
        Vector3 position = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            _spawnDepth
        );

        // 各パラメータ
        float speed = Random.Range(_speed.x, _speed.y);
        float length = Random.Range(_lineLength.x, _lineLength.y);
        float width = Random.Range(_lineWidth.x, _lineWidth.y);

        // 初期化
        line.Initialize(_projection, position, speed, length, width, _vanishDepth, _lineColor);
    }

    // ==================================================
    // ----- Get Available Line -----
    // ==================================================
    private WindEffect_Line GetAvailableLine()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (!_lines[i].gameObject.activeSelf)
            {
                return _lines[i];
            }
        }

        return null;
    }
}
