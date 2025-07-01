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

        if (other.CompareTag("Light") && seedsCollected > 0)
        {
            LightController light = other.GetComponent<LightController>();
            if (light != null && !light.isLit)
            {
                light.isLit = true;
                seedsCollected--;
                Debug.Log("Seed used to light a source! Seeds remaining: " + seedsCollected);
            }
        }
    }
}
