using UnityEngine;

public class AsteroidBeltSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform worldCenter;        // planet center
    [SerializeField] private GameObject[] rockPrefabs;     // assign your 5 rock prefabs
    [SerializeField] private int asteroidCount = 80;

    [Header("Belt Placement (XZ plane, orbit around Z axis)")]
    [Tooltip("Base radius of the asteroid belt from world center.")]
    [SerializeField] private float baseRadius = 7000f;

    [Tooltip("Random extra radius applied only at spawn (static spread).")]
    [SerializeField] private float spawnRadiusJitter = 120f;

    [Header("Orbit Speed (deg/s)")]
    [Tooltip("Slower than seeds for a drifting look.")]
    [SerializeField] private float minAngularSpeedDeg = 0.1f;  // very slow
    [SerializeField] private float maxAngularSpeedDeg = 0.2f;  // very slow

    [Tooltip("Randomize clockwise / counter-clockwise around Z.")]
    [SerializeField] private bool randomizeWinding = true;

    [Header("Spawn Height Variety")]
    [Tooltip("Fixed Y offset assigned at spawn so not all rocks are on the same Y.")]
    [SerializeField] private Vector2 spawnYJitterRange = new Vector2(-4000f, 4000f);

    [Header("Rock Rotation Options")]
    [Tooltip("Optional self-spin ONLY around local Z (no yaw).")]
    [SerializeField] private Vector2 selfSpinZ_DegPerSec = new Vector2(0f, 0f); // e.g., (2, 10)
    [Tooltip("Force yaw (rotation around world Y) to zero each frame.")]
    [SerializeField] private bool lockYawZero = true;

    [ContextMenu("Spawn Now")]
    public void SpawnNow()
    {
        if (!worldCenter)
        {
            Debug.LogWarning("[AsteroidBeltSpawner] Assign worldCenter.");
            return;
        }
        if (rockPrefabs == null || rockPrefabs.Length == 0)
        {
            Debug.LogWarning("[AsteroidBeltSpawner] Assign rockPrefabs (ideally 5 variants).");
            return;
        }

        // Fixed basis for a ring in the XY plane (orbit normal = +Z)
        Vector3 t1 = Vector3.right;  // X
        Vector3 t2 = Vector3.up;     // Y

        for (int i = 0; i < asteroidCount; i++)
        {
            var prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            if (!prefab) continue;

            // Even-ish angular distribution with a touch of jitter
            float baseAngle = (i / (float)asteroidCount) * Mathf.PI * 2f;
            float angleJitter = Random.Range(-6f, 6f) * Mathf.Deg2Rad;
            float theta = baseAngle + angleJitter;

            // Static spawn radius spread
            float r0 = baseRadius + Random.Range(-spawnRadiusJitter, spawnRadiusJitter);

            // Compute spawn position on XY ring
            Vector3 posOnRing = worldCenter.position + (Mathf.Cos(theta) * t1 + Mathf.Sin(theta) * t2) * r0;

            // Give each asteroid a fixed Y offset so heights differ (no Y motion later)
            float fixedY = Random.Range(spawnYJitterRange.x, spawnYJitterRange.y);
            posOnRing.y += fixedY;

            // Spawn the rock
            var go = Instantiate(prefab, posOnRing, Random.rotationUniform);

            // Attach and configure orbit driver
            var orbit = go.AddComponent<AsteroidOrbit>();
            orbit.worldCenter = worldCenter;

            // Force Z-axis orbit behavior (ring in XY)
            orbit.baseRadius = baseRadius;
            orbit.spawnRadiusOffset = r0 - baseRadius;

            float ang = Random.Range(minAngularSpeedDeg, maxAngularSpeedDeg);
            if (randomizeWinding && Random.value < 0.5f) ang = -ang;
            orbit.angularSpeedDeg = ang;

            orbit.initialAngleDeg = theta * Mathf.Rad2Deg;
            orbit.phaseJitterDeg = Random.Range(0f, 360f);

            // Fixed height
            orbit.fixedYOffset = fixedY;

            // Visual tweaks
            orbit.selfSpinX_DegPerSec = 0f; // disable X spin
            orbit.selfSpinZ_DegPerSec = (selfSpinZ_DegPerSec.x == selfSpinZ_DegPerSec.y)
                ? selfSpinZ_DegPerSec.x
                : Random.Range(selfSpinZ_DegPerSec.x, selfSpinZ_DegPerSec.y);
            orbit.lockYawZero = lockYawZero;
        }

        Debug.Log($"[AsteroidBeltSpawner] Spawned {asteroidCount} asteroids around Z-axis ring (R≈{baseRadius}).");
    }

    private void Start()
    {
        SpawnNow();
    }

    private void OnDrawGizmosSelected()
    {
        if (!worldCenter) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);

        // Draw belt ring in XY plane at base radius (preview)
        Vector3 center = worldCenter.position;
        Vector3 t1 = Vector3.right;  // X
        Vector3 t2 = Vector3.up;     // Y

        int seg = 128;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = center + (Mathf.Cos(a) * t1 + Mathf.Sin(a) * t2) * baseRadius;
            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
