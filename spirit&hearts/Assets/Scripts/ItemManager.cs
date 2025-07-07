using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public int seedsCollected = 0;

    [Header("Audio and light")]
    public AudioSource pickupSound;
    public AudioSource lightSound;
    public AmbientLightManager lightManager;

    // Movement section
    [Header("Movement")]
    public Movement movementScript;
    private void OnTriggerEnter(Collider other)
    {
        // Level section
        if (other.CompareTag("SpeedBooster"))
        {
            movementScript.ActivateSpeedBoost();
        }

        // Light section
        // if (other.CompareTag("Seed"))
        // {
        //     seedsCollected++;
        //     other.transform.SetParent(transform);
        //     other.gameObject.SetActive(false);
        //     pickupSound?.Play();  // Play pickup sound
        //     Debug.Log("Seed collected! Total: " + seedsCollected);
        // }

        // if (other.CompareTag("Light") && seedsCollected > 0)
        // {
        //     LightController light = other.GetComponent<LightController>()
        //                                     ?? other.GetComponentInParent<LightController>()
        //                                     ?? other.GetComponentInChildren<LightController>();
        //     if (light != null && !light.isLit)
        //     {
        //         light.isLit = true;
        //         seedsCollected--;
        //         lightSound?.Play();  // Play light-up sound
        //         Debug.Log("Seed used to light a source! Seeds remaining: " + seedsCollected);
        //         lightManager.UpdateAmbientLight();
        //     }
        //     else
        //     {
        //         Debug.Log(light == null ? "Light null" : "Light not null");
        //         Debug.Log(!light?.isLit ?? false ? "Not lit" : "It's lit");
        //     }
        // }
    }
}
