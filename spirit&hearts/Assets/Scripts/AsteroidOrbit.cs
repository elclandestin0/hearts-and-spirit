using UnityEngine;

public class AsteroidOrbit : MonoBehaviour
{
    [Header("Refs")]
    public Transform worldCenter;

    [Header("Orbit (around Z axis)")]
    public float baseRadius = 7000f;
    public float spawnRadiusOffset = 0f;
    public float angularSpeedDeg = 0.1f;
    public float initialAngleDeg = 0f;
    public float phaseJitterDeg = 0f;

    [Header("Layer Offset (vertical offset relative to world center)")]
    public float fixedYOffset = 0f;

    [Header("Visuals")]
    public float selfSpinZ_DegPerSec = 0f;
    public float selfSpinX_DegPerSec = 0f;
    public bool lockYawZero = true;

    private float _t;

    private void Start()
    {
        if (!worldCenter)
        {
            Debug.LogWarning("[AsteroidOrbit] No worldCenter set. Disabling.");
            enabled = false; return;
        }
    }

    private void Update()
    {
        _t += Time.deltaTime;

        float angle = (initialAngleDeg + phaseJitterDeg) + angularSpeedDeg * _t;
        float a = angle * Mathf.Deg2Rad;

        float r = baseRadius + spawnRadiusOffset;
        Vector3 center = worldCenter.position;

        // Orbit in XY plane, with Z offset per layer
        Vector3 targetPos = center + new Vector3(
            Mathf.Cos(a) * r,
            fixedYOffset,
            Mathf.Sin(a) * r
        );

        transform.position = targetPos;

        if (selfSpinZ_DegPerSec != 0f)
            transform.Rotate(Vector3.forward, selfSpinZ_DegPerSec * Time.deltaTime, Space.Self);

        if (lockYawZero)
        {
            Vector3 e = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(e.x, 0f, e.z);
        }
    }
}
