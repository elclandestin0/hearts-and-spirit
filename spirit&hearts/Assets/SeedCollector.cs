using UnityEngine;

public class SeedCollector : MonoBehaviour
{
    public int seedsCollected = 0;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Lol triggered" + other.gameObject.name);
        if (other.CompareTag("Seed"))
        {
            seedsCollected++;
            Destroy(other.gameObject);
            Debug.Log("Seed collected! Total: " + seedsCollected);
        }

        if (other.CompareTag("Light") && seedsCollected > 0)
        {
            LightController light = other.GetComponent<LightController>();

            if (light == null)
                light = other.GetComponentInParent<LightController>();

            if (light == null)
                light = other.GetComponentInChildren<LightController>();

            if (light != null && !light.isLit)
            {
                light.isLit = true;
                seedsCollected--;
                Debug.Log("Seed used to light a source! Seeds remaining: " + seedsCollected);
            }
            else 
            {
                Debug.Log(light == null ? "Light null" : "Light not null");
                Debug.Log(!light?.isLit ?? false ? "Not lit" : "It's lit");
            }
        }

    }
}
