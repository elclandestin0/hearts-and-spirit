using UnityEngine;

public class PlayerWorldTrackerViewer : MonoBehaviour
{
    public Vector2Int currentZone;

    void Update()
    {
        if (PauseState.IsPaused) return;

        currentZone = PlayerWorldTracker.CurrentCoord;
    }
}
