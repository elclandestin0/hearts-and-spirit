using UnityEngine;

public class SeedCollector : MonoBehaviour
{
    public int seedsCollected = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Seed"))
        {
            seedsCollected++;
            Destroy(other.gameObject);
            Debug.Log("Seed collected! Total: " + seedsCollected);
        }
    }
}
