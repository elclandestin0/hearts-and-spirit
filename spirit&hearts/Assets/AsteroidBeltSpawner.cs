using UnityEngine;

public class AsteroidBeltSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform worldCenter;        // planet center
    [SerializeField] private GameObject[] rockPrefabs;     // assign your 5 rock prefabs
    [SerializeField] private int asteroidCount = 90;       // divisible by 3 is ideal (30 per layer)

    [Header("Belt Placement (XY plane, orbit around Z axis)")]
    [Tooltip("Base radius of the asteroid belt from world center.")]
    [SerializeField] private float baseRadius = 7000f;

    [Tooltip("Random extra radius applied only at spawn (static spread).")]
    [SerializeField] private float spawnRadiusJitter = 120f;

    [Header("Orbit Speed (deg/s)")]
    [Tooltip("Slower than seeds for a drifting look.")]
    [SerializeField] private float minAngularSpeedDeg = 0.1f;
    [SerializeField] private float maxAngularSpeedDeg = 0.2f;
    [Tooltip("Randomize clockwise / counter-clockwise around Z.")]
    [SerializeField] private bool randomizeWinding = true;

    [Header("Layer Heights")]
    [Tooltip("Vertical offset above world center for top layer.")]
    [SerializeField] private float topLayerOffset = 3000f;
    [Tooltip("Vertical offset below world center for bottom layer.")]
    [SerializeField] private float bottomLayerOffset = -3000f;

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

        // Divide count into 3 layers
        int layerCount = Mathf.Max(1, asteroidCount / 3);
        int remaining = asteroidCount - (layerCount * 3);
        int midCount = layerCount + (remaining > 0 ? 1 : 0);
        int topCount = layerCount + (remaining > 1 ? 1 : 0);
        int bottomCount = layerCount;

        SpawnLayer(midCount, 0f);                  // mid layer (center)
        SpawnLayer(topCount, topLayerOffset);      // top layer
        SpawnLayer(bottomCount, bottomLayerOffset); // bottom layer
    }

    private void SpawnLayer(int count, float yOffset)
    {
        Vector3 t1 = Vector3.right;  // X
        Vector3 t2 = Vector3.up;     // Y

        for (int i = 0; i < count; i++)
        {
            var prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            if (!prefab) continue;

            // Even-ish angular distribution with a touch of jitter
            float baseAngle = (i / (float)count) * Mathf.PI * 2f;
            float angleJitter = Random.Range(-6f, 6f) * Mathf.Deg2Rad;
            float theta = baseAngle + angleJitter;

            // Static spawn radius spread
            float r0 = baseRadius + Random.Range(-spawnRadiusJitter, spawnRadiusJitter);

            // Compute spawn position on XY ring
            Vector3 posOnRing = worldCenter.position + (Mathf.Cos(theta) * t1 + Mathf.Sin(theta) * t2) * r0;

            // Apply vertical layer offset
            posOnRing.z += yOffset;

            // Spawn the rock
            var go = Instantiate(prefab, posOnRing, Random.rotationUniform);

            // Attach and configure orbit driver
            var orbit = go.AddComponent<AsteroidOrbit>();
            orbit.worldCenter = worldCenter;

            orbit.baseRadius = baseRadius;
            orbit.spawnRadiusOffset = r0 - baseRadius;

            float ang = Random.Range(minAngularSpeedDeg, maxAngularSpeedDeg);
            if (randomizeWinding && Random.value < 0.5f) ang = -ang;
            orbit.angularSpeedDeg = ang;

            orbit.initialAngleDeg = theta * Mathf.Rad2Deg;
            orbit.phaseJitterDeg = Random.Range(0f, 360f);
            orbit.fixedYOffset = yOffset;

            // Visual tweaks
            orbit.selfSpinX_DegPerSec = 0f;
            orbit.selfSpinZ_DegPerSec = (selfSpinZ_DegPerSec.x == selfSpinZ_DegPerSec.y)
                ? selfSpinZ_DegPerSec.x
                : Random.Range(selfSpinZ_DegPerSec.x, selfSpinZ_DegPerSec.y);
            orbit.lockYawZero = lockYawZero;
        }

        Debug.Log($"[AsteroidBeltSpawner] Spawned {count} asteroids at Y offset {yOffset}.");
    }

    private void Start()
    {
        SpawnNow();
    }

    private void OnDrawGizmosSelected()
    {
        if (!worldCenter) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        Vector3 center = worldCenter.position;

        // Draw belt rings for visualization
        float[] yOffsets = { bottomLayerOffset, 0f, topLayerOffset };
        foreach (float yOffset in yOffsets)
        {
            Vector3 t1 = Vector3.right;
            Vector3 t2 = Vector3.up;
            int seg = 128;
            Vector3 prev = Vector3.zero;

            for (int i = 0; i <= seg; i++)
            {
                float a = (i / (float)seg) * Mathf.PI * 2f;
                Vector3 p = center + (Mathf.Cos(a) * t1 + Mathf.Sin(a) * t2) * baseRadius;
                p.z += yOffset;
                if (i > 0) Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
