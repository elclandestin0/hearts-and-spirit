using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    public bool isEnabled = false;

    void Awake() => Instance = this;

    void Update()
    {
        if (!isEnabled) return;

        // Handle flying mechanics here
    }

    public void EnablePlayer()
    {
        isEnabled = true;
        // Optional: reset player state here if starting new game
    }
}
