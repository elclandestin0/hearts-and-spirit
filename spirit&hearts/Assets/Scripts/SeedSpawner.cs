using UnityEngine;
public class SeedSpawner : MonoBehaviour
{
    public GameObject seedPrefab;
    private GameObject spawnedSeed;

    void Start()
    {
        SpawnSeed();
    }

    void SpawnSeed()
    {
        if (spawnedSeed == null && seedPrefab != null)
        {
            spawnedSeed = Instantiate(seedPrefab, transform.position + Vector3.up * 10f, Quaternion.identity);
        }
    }
}
