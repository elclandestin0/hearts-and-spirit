using UnityEngine;

public class AsteroidOrbit : MonoBehaviour
{
    [Header("Refs")]
    public Transform worldCenter;

    [Header("Orbit (around Z axis)")]
    public float baseRadius = 7000f;
    public float spawnRadiusOffset = 0f;     // static offset from base radius
    public float angularSpeedDeg = 0.1f;     // very slow drift
    public float initialAngleDeg = 0f;       // starting angle in the orbit plane (XY)
    public float phaseJitterDeg = 0f;        // extra per-asteroid offset

    [Header("Height")]
    [Tooltip("Fixed Y offset chosen at spawn so not all rocks share the same Y.")]
    public float fixedYOffset = 0f;          // NO Y oscillation over time

    [Header("Visuals")]
    [Tooltip("Optional self-spin ONLY around local Z.")]
    public float selfSpinZ_DegPerSec = 0f;
    [Tooltip("(Kept for compatibility; unused for Z orbit unless you want it.)")]
    public float selfSpinX_DegPerSec = 0f;   // safe to ignore; spawner may set 0
    [Tooltip("Force yaw (rotation around world Y) to zero each frame.")]
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

        // Angle around Z axis → ring in XY plane
        float angle = (initialAngleDeg + phaseJitterDeg) + angularSpeedDeg * _t;
        float a = angle * Mathf.Deg2Rad;

        float r = baseRadius + spawnRadiusOffset;
        Vector3 center = worldCenter.position;

        // XY ring with fixed Y offset (adds thickness, but no time-based Y motion)
        Vector3 targetPos = center + new Vector3(
            Mathf.Cos(a) * r,
            0f,
            Mathf.Sin(a) * r + fixedYOffset
        );

        transform.position = targetPos;

        // Optional self-spin ONLY around local Z (no Y-axis spin introduced)
        if (selfSpinZ_DegPerSec != 0f)
            transform.Rotate(Vector3.forward, selfSpinZ_DegPerSec * Time.deltaTime, Space.Self);

        // Keep yaw (world Y rotation) zeroed if requested
        if (lockYawZero)
        {
            Vector3 e = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(e.x, 0f, e.z);
        }
    }
}
