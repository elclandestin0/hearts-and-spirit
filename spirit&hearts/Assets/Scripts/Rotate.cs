using UnityEngine;

public class Rotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f; // degrees per second
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // ✨ Rotate around Y axis (like a PS1 power-up idol waiting for you)
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // 🎈 Add some gentle vertical bobbing for playful life
        float newY = startPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
