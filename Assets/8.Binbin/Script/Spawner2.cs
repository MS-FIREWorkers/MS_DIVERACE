using UnityEngine;
using System.Collections;

public class Spawner2 : MonoBehaviour
{
    public GameObject prefab;

    [Header("Random Spawn")]
    public float minSpawnTime = 0.5f;
    public float maxSpawnTime = 3f;

    public float lifeTime = 5f;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            Spawn();

            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
        }
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
