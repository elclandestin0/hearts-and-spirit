using UnityEngine;

public class SeedInventory : MonoBehaviour
{
    [Header("Seed Inventory")]
    [SerializeField] private int seedCount = 0;

    public void AddSeed()
    {
        seedCount++;
        Debug.Log("Seed added. Total: " + seedCount);
        // Optionally play pickup sound or trigger animation here
    }

    public bool RemoveSeed()
    {
        if (seedCount > 0)
        {
            seedCount--;
            Debug.Log("Seed removed. Remaining: " + seedCount);
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
