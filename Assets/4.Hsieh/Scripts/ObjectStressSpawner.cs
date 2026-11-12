using UnityEngine;

public class ObjectStressSpawner : MonoBehaviour
{
    [Header("Spawn Prefabs")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Amount")]
    [SerializeField] private int spawnCount = 1000;

    [Header("Spawn Area")]
    [SerializeField]
    private Vector3 minPosition =
        new Vector3(-50.0f, -150.0f, -50.0f);

    [SerializeField]
    private Vector3 maxPosition =
        new Vector3(50.0f, 150.0f, 50.0f);

    [Header("Transform")]
    [SerializeField] private bool randomRotation = true;

    [SerializeField]
    private Vector2 randomScale =
        new Vector2(0.8f, 1.2f);

    [Header("Settings")]
    [SerializeField] private bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart)
            GenerateObjects();
    }

    [ContextMenu("Generate Objects")]
    public void GenerateObjects()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("Prefabs are not assigned.");
            return;
        }

        ClearObjects();

        for (int i = 0; i < spawnCount; i++)
            SpawnObject();

        Debug.Log($"Generated : {spawnCount}");
    }

    private void SpawnObject()
    {
        GameObject prefab =
            prefabs[Random.Range(0, prefabs.Length)];

        Vector3 position = new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z)
        );

        GameObject obj =
            Instantiate(prefab, transform);

        obj.transform.localPosition = position;

        if (randomRotation)
            obj.transform.localRotation = Random.rotation;

        float scale =
            Random.Range(randomScale.x, randomScale.y);

        obj.transform.localScale =
            Vector3.one * scale;
    }

    [ContextMenu("Clear Objects")]
    public void ClearObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
}