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
    }

    public void PlayPickUpSound() 
    {
        if (pickupSound == null) return;
        else 
        {
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
