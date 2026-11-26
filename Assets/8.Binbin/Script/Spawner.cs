using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public float spawnInterval = 0.5f;
    public float lifeTime = 5f;

    private void Start()
    {
        InvokeRepeating(nameof(Spawn), 0f, spawnInterval);
    }

    private void Spawn()
    {
        GameObject obj = Instantiate(
            prefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(obj, lifeTime);
    }
}