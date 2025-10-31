using UnityEngine;
using TMPro;

public class SeedInventory : MonoBehaviour
{
    [Header("Seed Inventory")]
    [SerializeField] private int seedCount = 0;
    public TMP_Text subtitleUI;

    public void AddSeed()
    {
        seedCount++;
        subtitleUI.text = "Seeds: " + seedCount;
        // Optionally play pickup sound or trigger animation here
    }

    public bool RemoveSeed()
    {
        if (seedCount > 0)
        {
            seedCount--;
            subtitleUI.text = "Seeds: " + seedCount;
            Debug.Log("Seed removed. Remaining: " + seedCount);
            if (seedCount == 0) 
            {
                subtitleUI.text = "Find some seeds!";
            }
            return true;
        }

        Debug.LogWarning("Tried to remove seed, but inventory is empty.");
        return false;
    }

    public int GetSeedCount()
    {
        return seedCount;
    }
}
