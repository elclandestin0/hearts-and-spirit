using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private int seedsCollected = 0;

    [Header("Audio and light")]
    public AudioSource pickupSound;
    public AudioSource lightSound;
    public AmbientLightManager lightManager;

    // Movement section
    [Header("Movement")]
    public Movement movementScript;

    public void AddSeed() 
    {
        seedsCollected++;
    }

    public void RemoveSeed() 
    {
        seedsCollected--;
    }
    private void OnTriggerEnter(Collider other)
    {
        // Level section
        if (other.CompareTag("SpeedBooster"))
        {
            Debug.Log("Whaaa!");
            movementScript.ActivateSpeedBoost();
        }
    }

    public void PlayPickUpSound() 
    {
        Debug.Log("Attempting to play pick up sound...");
        if (pickupSound == null) return;
        else 
        {
            Debug.Log("Blay...");
            pickupSound.Play();
        }
    }

    public void PlayLightSound() 
    {
        if (lightSound == null) return;
        else 
        {
            lightSound.Play();
        }
    }
}
