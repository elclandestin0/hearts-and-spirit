// SpawnManager.cs
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] Transform startSpawnPoint; // assign in scene
    [SerializeField] Transform playerRoot;      // XROrigin or PlayerRig root

    void Awake()
    {
        if (startSpawnPoint != null && playerRoot != null)
        {
            playerRoot.position = startSpawnPoint.position;
            playerRoot.rotation = startSpawnPoint.rotation;
        }
    }
}
