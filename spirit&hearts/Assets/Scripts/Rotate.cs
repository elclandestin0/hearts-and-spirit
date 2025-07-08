using UnityEngine;

public class Rotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f; // degrees per second
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 2f;

    private float initialY = 0f;

    void Start()
    {
        initialY = transform.localPosition.y;
    }

    void Update()
    {
        // 🌪️ Rotate around world Y
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // 🪩 Bob up and down without freezing other axes
        Vector3 pos = transform.localPosition;
        pos.y = initialY + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = pos;
    }
}
