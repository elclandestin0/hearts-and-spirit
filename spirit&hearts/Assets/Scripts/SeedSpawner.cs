using UnityEngine;
public class SeedSpawner : MonoBehaviour
{
    public GameObject seedPrefab;
    public Transform spawnPoint;
    private GameObject spawnedSeed;
    void Start()
    {
        SpawnSeed();
    }

    void SpawnSeed()
    {
        if (spawnedSeed == null && seedPrefab != null)
        {
            Quaternion spawnRotation = Quaternion.Euler(-90f, spawnPoint.rotation.eulerAngles.y, spawnPoint.rotation.eulerAngles.z);
            spawnedSeed = Instantiate(seedPrefab, spawnPoint.position, spawnRotation);
        }
    }
}
