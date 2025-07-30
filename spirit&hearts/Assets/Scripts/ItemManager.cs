using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemManager : MonoBehaviour
{

    [Header("Audio and light")]
    public AudioSource pickupSound;
    public AudioSource lightSound;
    public AmbientLightManager lightManager;

    // Movement section
    [Header("Movement")]
    public Movement movementScript;
    private List<SeedBehavior> attachedSeeds = new List<SeedBehavior>();
    private bool isSeedInFlight = false;

    
    public void RegisterSeed(SeedBehavior seed)
    {
        if (!attachedSeeds.Contains(seed))
            attachedSeeds.Add(seed);
    }

    public void UnregisterSeed(SeedBehavior seed)
    {
        attachedSeeds.Remove(seed);
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

    public void TrySendSeedToLight()
    {
        if (isSeedInFlight || attachedSeeds.Count == 0) return;

        GameObject[] lights = GameObject.FindGameObjectsWithTag("Light");
        Transform closest = null;
        float closestDistance = 100f;

        foreach (var light in lights)
        {
            LightController lc = light.GetComponent<LightController>()
                                ?? light.GetComponentInChildren<LightController>()
                                ?? light.GetComponentInParent<LightController>();
            if (lc == null || lc.isLit) continue;

            float d = Vector3.Distance(transform.position, light.transform.position);
            if (d < closestDistance)
            {
                closest = light.transform;
                closestDistance = d;
            }
        }

        if (closest != null)
        {
            var seedToSend = attachedSeeds[0];
            attachedSeeds.RemoveAt(0);
            seedToSend.SendToLight(closest);
            isSeedInFlight = true;
        }
    }

    public void NotifyLightAvailable()
    {
        isSeedInFlight = false;
    }


}
