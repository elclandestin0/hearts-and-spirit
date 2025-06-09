using UnityEngine;

public class PlayerWorldTrackerViewer : MonoBehaviour
{
    public Vector2Int currentZone;

    void Update()
    {
        currentZone = PlayerWorldTracker.CurrentCoord;
    }
}
