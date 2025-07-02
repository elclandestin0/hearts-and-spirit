using UnityEngine;

public class SeedCollector : MonoBehaviour
{
    public int seedsCollected = 0;

    [Header("Audio")]
    public AudioSource pickupSound;
    public AudioSource lightSound;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Lol triggered " + other.gameObject.name);

        if (other.CompareTag("Seed"))
        {
            seedsCollected++;
            Destroy(other.gameObject);
            pickupSound?.Play();  // Play pickup sound
            Debug.Log("Seed collected! Total: " + seedsCollected);
        }

        if (other.CompareTag("Light") && seedsCollected > 0)
        {
            LightController light = other.GetComponent<LightController>() 
                                  ?? other.GetComponentInParent<LightController>() 
                                  ?? other.GetComponentInChildren<LightController>();

            if (light != null && !light.isLit)
            {
                light.isLit = true;
                seedsCollected--;
                lightSound?.Play();  // Play light-up sound
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
