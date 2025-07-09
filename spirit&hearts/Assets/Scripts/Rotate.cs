using UnityEngine;

public class Rotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f;
    public float bobAmplitude = 0.1f;
    public float bobFrequency = 2f;

    private Vector3 lastBobbingOffset = Vector3.zero;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        transform.position -= lastBobbingOffset;
        float bobY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        lastBobbingOffset = new Vector3(0f, bobY, 0f);
        transform.position += lastBobbingOffset;
    }
}
