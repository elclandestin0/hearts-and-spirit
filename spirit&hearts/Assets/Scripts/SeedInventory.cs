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
        if (subtitleUI) subtitleUI.text = "Seeds: " + seedCount;
        // Optionally play pickup sound or trigger animation here
    }

    public bool RemoveSeed()
    {
        if (seedCount > 0)
        {
            seedCount--;
            if (subtitleUI) subtitleUI.text = "Seeds: " + seedCount;
            GameLog.Log("Seed removed. Remaining: " + seedCount);
            if (seedCount == 0 && subtitleUI)
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
