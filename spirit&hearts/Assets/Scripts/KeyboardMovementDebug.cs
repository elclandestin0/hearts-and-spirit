using UnityEngine;

public class KeyboardMovementDebug : MonoBehaviour
{
    public Movement movement;

    [Header("Settings")]
    public float pitchSpeed = 45f;
    public float yawSpeed = 60f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Mouse / Touchpad")]
    public bool enableMouseLook = true;
    public float mouseSensitivity = 1.5f;

    void Update()
    {
        if (movement == null || movement.head == null) return;

        Transform head = movement.head;
        Vector3 currentEuler = head.localEulerAngles;

        float pitch = currentEuler.x > 180f ? currentEuler.x - 360f : currentEuler.x;
        float yaw = currentEuler.y > 180f ? currentEuler.y - 360f : currentEuler.y;

        // Keyboard controls
        if (Input.GetKey(KeyCode.W)) pitch -= pitchSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) pitch += pitchSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) yaw -= yawSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) yaw += yawSpeed * Time.deltaTime;

        // Touchpad (Mouse) look
        if (enableMouseLook) // Hold Right-Click or Cmd+click
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX * yawSpeed * Time.deltaTime;
            pitch -= mouseY * pitchSpeed * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        head.localEulerAngles = new Vector3(pitch, yaw, currentEuler.z);
    }
}
