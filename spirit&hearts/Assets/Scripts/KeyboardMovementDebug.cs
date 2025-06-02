using UnityEngine;

public class TouchpadLook : MonoBehaviour
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

        // Convert angles
        float pitch = currentEuler.x > 180f ? currentEuler.x - 360f : currentEuler.x;
        float yaw = currentEuler.y > 180f ? currentEuler.y - 360f : currentEuler.y;

        // Read mouse/touchpad delta
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Debug.Log(mouseX);

        // Apply movement
        pitch -= mouseY * pitchSpeed * Time.deltaTime;
        yaw += mouseX * yawSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        head.localEulerAngles = new Vector3(pitch, yaw, currentEuler.z);
    }
}
