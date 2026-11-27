/*
    Debug_StaticCreateObstacle.cs
    エディター上で指定範囲内に障害物を一括生成する
*/
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Debug_StaticCreateObstacle : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Obstacle Prefab -----
    // --------------------------------------------------
    [Header("Obstacle Prefab")]
    [SerializeField] private GameObject _obstaclePrefab;

    // --------------------------------------------------
    // ----- Parameters -----
    // --------------------------------------------------
    [Header("Parameters")]
    [Tooltip("下方向への生成範囲")]
    [SerializeField] private float _verticalDistance = 100.0f;

    [Tooltip("XZ平面上の生成半径")]
    [SerializeField] private float _radius = 20.0f;

    [Tooltip("垂直方向の生成間隔")]
    [SerializeField] private float _intervalDistance = 10.0f;

    [Tooltip("1層あたりの生成個数")]
    [SerializeField] private int _obstacleCount = 10;

    // --------------------------------------------------
    // ----- Random -----
    // --------------------------------------------------
    [Header("Random")]
    [SerializeField] private int _seed = 0;

#if UNITY_EDITOR

    // --------------------------------------------------
    // ----- Generate -----
    // --------------------------------------------------
    [ContextMenu("Generate Obstacles")]
    private void GenerateObstacles()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (_obstaclePrefab == null || !PrefabUtility.IsPartOfPrefabAsset(_obstaclePrefab))
        {
            Debug.LogError("Obstacle Prefab が未設定、またはPrefabアセットではありません。");
            return;
        }

        if (_intervalDistance <= 0.0f ||
            _verticalDistance <= 0.0f ||
            _radius < 0.0f ||
            _obstacleCount <= 0)
        {
            Debug.LogError("生成パラメータが不正です。");
            return;
        }

        // Undoグループの設定
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Obstacles");

        // 生成済みの障害物を削除
        DeleteChildren();

        // ランダム状態を退避
        Random.State previousState = Random.state;
        Random.InitState(_seed);

        try
        {
            // 生成する層の数
            int layerCount = Mathf.FloorToInt(_verticalDistance / _intervalDistance);

            // 各層を生成
            for (int i = 1; i <= layerCount; i++)
            {
                float y = transform.position.y - _intervalDistance * i;

                GenerateLayer(y);
            }
        }
        finally
        {
            // ランダム状態を復元
            Random.state = previousState;

            // Undo操作を一つにまとめる
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private void GenerateLayer(float y)
    {
        for (int i = 0; i < _obstacleCount; i++)
        {
            // 円形範囲内のランダム座標
            Vector2 randomPosition = Random.insideUnitCircle * _radius;

            // 生成位置
            Vector3 position = new Vector3(transform.position.x + randomPosition.x, y, transform.position.z + randomPosition.y);

            // Prefabとの接続を維持して生成
            GameObject obstacle = (GameObject)PrefabUtility.InstantiatePrefab(_obstaclePrefab, transform);

            // Transformの設定
            obstacle.transform.position = position;
            obstacle.transform.rotation = _obstaclePrefab.transform.rotation;

            PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle.transform);

            // Undoに登録
            Undo.RegisterCreatedObjectUndo(obstacle, "Create Obstacle");
        }
    }

    // --------------------------------------------------
    // ----- Delete -----
    // --------------------------------------------------
    [ContextMenu("Delete Obstacles")]
    private void DeleteObstacles()
    {
        if (Application.isPlaying)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Delete Obstacles");

        DeleteChildren();

        Undo.CollapseUndoOperations(undoGroup);
    }

    private void DeleteChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }
    }

#endif
}