using UnityEngine;
using TMPro;

public class VelocityDisplay : MonoBehaviour
{
    public Transform target; // Usually the ghost head
    public Vector3 offset = new Vector3(0, 0.3f, 0); // Above the head
    public TextMeshProUGUI textMesh;
    public GhostFlightPlayback ghost; // Reference to playback system

    private Transform cam;

    void Awake()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (ghost == null || textMesh == null || target == null || ghost.CurrentFrame == null)
            return;

        var frame = ghost.CurrentFrame.Value;

        // Position the text
        transform.position = target.position + offset;

        // Make it face the camera
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);

        // Show recorded velocity magnitude
        float speed = frame.resultingVelocity.magnitude;
        textMesh.text = $"{speed:F2} m/s";
    }
}
