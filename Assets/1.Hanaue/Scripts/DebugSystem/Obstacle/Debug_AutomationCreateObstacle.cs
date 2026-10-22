
/*
    Debug_AutomationCreateObstacle.cs
    20261020  hanaue sho
    プレイヤーの落下に合わせて障害物を自動生成
    プレイヤーより上の不要な障害物を削除
*/
using System.Collections.Generic;
using UnityEngine;

// 生成モード
public enum ObstacleGenerateMode
{
    Random,
    RandomWithClearArea
}


public class Debug_AutomationCreateObstacle : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Player -----
    // --------------------------------------------------
    [Header("Player")]
    [SerializeField] private Transform _player;

    // --------------------------------------------------
    // ----- Obstacle Prefab -----
    // --------------------------------------------------
    [Header("Obstacle Prefab")]
    [SerializeField] private GameObject _obstaclePrefab;

    // --------------------------------------------------
    // ----- Parameters -----
    // --------------------------------------------------
    [Header("Parameters")]
    [Tooltip("プレイヤーの現在位置から下方向に、何m先まで障害物を生成しておくかを指定")]
    [SerializeField] private float _verticalDistance = 100.0f;

    [Tooltip("障害物をランダム配置するXZ平面上の半径（m）、プレイヤーのXZ座標を中心とした円形範囲内に生成")]
    [SerializeField] private float _radius = 20.0f;

    [Tooltip("障害物を生成する層の垂直方向の間隔（m）。10を指定すると10mごとに障害物の層を生成する")]
    [SerializeField] private float _intervalDistance = 10.0f;

    [Tooltip("1層あたりにランダム生成する障害物の個数")]
    [SerializeField] private int _obstacleCount = 10;

    // --------------------------------------------------
    // ----- Delete -----
    // --------------------------------------------------
    [Header("Delete")]
    [Tooltip("プレイヤーより何m上にある障害物の層を削除するかを指定")]
    [SerializeField] private float _deleteDistance = 30.0f;

    // --------------------------------------------------
    // ----- Generate Mode -----
    // --------------------------------------------------
    [Header("Generate Mode")]
    [SerializeField]
    private ObstacleGenerateMode _generateMode = ObstacleGenerateMode.Random;

    [Tooltip("障害物を配置しない円形領域の半径")]
    [SerializeField]
    private float _clearRadius = 5.0f;

    // --------------------------------------------------
    // ----- Random -----
    // --------------------------------------------------
    [Header("Random")]
    [SerializeField] private int _seed = 0;

    // --------------------------------------------------
    // ----- Runtime -----
    // --------------------------------------------------
    // 次に障害物を生成するY座標
    private float _nextGenerateY;
    // 生成済み障害物の管理
    private readonly Queue<ObstacleLayer> _layers = new();
    // 1層分の障害物
    private class ObstacleLayer
    {
        public float y;
        public List<GameObject> obstacles = new();
    }

    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Start()
    {
        if (_player == null || _obstaclePrefab == null)
        {
            Debug.LogError("Player または Obstacle Prefab が未設定です。");
            enabled = false;
            return;
        }

        if (_intervalDistance <= 0.0f ||
            _verticalDistance <= 0.0f ||
            _obstacleCount <= 0)
        {
            Debug.LogError("生成パラメータが不正です。");
            enabled = false;
            return;
        }

        // ランダムシード設定
        Random.InitState(_seed);

        // 最初の生成位置
        _nextGenerateY = _player.position.y - _intervalDistance;

        // 初期生成
        GenerateObstacles();
    }

    private void Update()
    {
        if (_player == null)
        {
            return;
        }

        // プレイヤーの下方向に障害物を追加
        GenerateObstacles();

        // 上方向の不要な障害物を削除
        DeleteObstacles();
    }

    // --------------------------------------------------
    // ----- Generate -----
    // --------------------------------------------------
    private void GenerateObstacles()
    {
        // 生成する最下端のY座標
        float targetY = _player.position.y - _verticalDistance;

        // 生成範囲に達するまで追加
        while (_nextGenerateY >= targetY)
        {
            GenerateLayer(_nextGenerateY);

            // 次の生成位置を下に移動
            _nextGenerateY -= _intervalDistance;
        }
    }

    private void GenerateLayer(float y)
    {
        ObstacleLayer layer = new ObstacleLayer();
        layer.y = y;

        // 空白領域の中心点を決定
        Vector2 clearCenter = Vector2.zero;

        if (_generateMode == ObstacleGenerateMode.RandomWithClearArea)
        {
            // 空白領域が配置範囲からはみ出さないようにする
            float centerRadius = Mathf.Max(0.0f, _radius - _clearRadius);

            clearCenter = Random.insideUnitCircle * centerRadius;
        }

        for (int i = 0; i < _obstacleCount; i++)
        {
            Vector2 randomPosition;

            // 条件を満たす位置が見つかるまで抽選
            do
            {
                randomPosition = Random.insideUnitCircle * _radius;
            } while ( _generateMode == ObstacleGenerateMode.RandomWithClearArea && Vector2.Distance(randomPosition, clearCenter) < _clearRadius);

            // プレイヤーの現在のXZ座標を中心に配置
            Vector3 position = new Vector3(_player.position.x + randomPosition.x, y, _player.position.z + randomPosition.y);

            // プレファブ生成
            GameObject obstacle = Instantiate(
                _obstaclePrefab,
                position,
                _obstaclePrefab.transform.rotation,
                transform
            );

            layer.obstacles.Add(obstacle);
        }

        _layers.Enqueue(layer);
    }

    // --------------------------------------------------
    // ----- Delete -----
    // --------------------------------------------------
    private void DeleteObstacles()
    {
        // プレイヤーより一定距離上の障害物を削除
        float deleteY = _player.position.y + _deleteDistance;

        while (_layers.Count > 0)
        {
            ObstacleLayer layer = _layers.Peek();

            // まだ削除範囲に達していない
            if (layer.y <= deleteY)
            {
                break;
            }

            // 1層分の障害物を削除
            foreach (GameObject obstacle in layer.obstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle);
                }
            }

            _layers.Dequeue();
        }
    }
}