using UnityEngine;

public class SeedCollector : MonoBehaviour
{
    public int seedsCollected = 0;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered " + other.gameObject.name);
        if (other.CompareTag("Seed"))
        {
            Debug.Log("with seed ");
            seedsCollected++;
            Destroy(other.gameObject);
            Debug.Log("Seed collected! Total: " + seedsCollected);
        }
    }
}
