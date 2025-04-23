using UnityEngine;

public class KeyboardMovementDebug : MonoBehaviour
{
    public Movement movement;

    [Header("Settings")]
    public float pitchSpeed = 45f;
    public float yawSpeed = 60f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    void Update()
    {
        if (movement == null || movement.head == null) return;

        Transform head = movement.head;
        Vector3 currentEuler = head.localEulerAngles;

        // Handle pitch (X axis)
        float pitch = currentEuler.x;
        if (pitch > 180f) pitch -= 360f;

        if (Input.GetKey(KeyCode.W))
            pitch -= pitchSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            pitch += pitchSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Handle yaw (Y axis)
        float yaw = currentEuler.y;
        if (yaw > 180f) yaw -= 360f;

        if (Input.GetKey(KeyCode.A))
            yaw -= yawSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            yaw += yawSpeed * Time.deltaTime;

        // Apply rotation
        head.localEulerAngles = new Vector3(pitch, yaw, currentEuler.z);
    }
}
