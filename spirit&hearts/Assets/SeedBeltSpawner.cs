using UnityEngine;

public class SeedBeltSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform worldCenter;          // your planet center GO
    [SerializeField] private GameObject seedPrefab;          // prefab with SeedBehavior + SeedMotion
    [SerializeField] private int seedCount = 50;

    [Header("Spherical Cap Placement")]
    [Tooltip("Seeds will be placed on a sphere of this radius around worldCenter.")]
    [SerializeField] private float shellRadius = 6000f;

    [Tooltip("Only points with Y >= worldCenter.y + capYMin will spawn (top spherical cap).")]
    [SerializeField] private float capYMin = 1500f;

    [Tooltip("Random radial jitter added to shellRadius for variety (±).")]
    [SerializeField] private float radialJitter = 40f;

    [Tooltip("Random tangential heading jitter in degrees.")]
    [SerializeField] private float headingJitterDeg = 10f;

    [Header("Initial Orbit Velocity")]
    [Tooltip("Speed used when spawned near inner radius behaviour.")]
    [SerializeField] private float orbitSpeedInner = 14f;

    [Tooltip("Speed used when spawned near outer radius behaviour.")]
    [SerializeField] private float orbitSpeedOuter = 7f;

    [Tooltip("Small random speed variance added to each seed.")]
    [SerializeField] private float speedVariance = 2f;

    [Header("Seed Behaviour")]
    [Tooltip("Immediately push each seed into WorldGravity so it rides your world shell logic.")]
    [SerializeField] private bool forceWorldGravityOnSpawn = true;

    [Tooltip("Optional: randomize clockwise/counter-clockwise winding.")]
    [SerializeField] private bool randomizeWinding = true;

    [ContextMenu("Spawn Now")]
    public void SpawnNow()
    {
        if (!worldCenter || !seedPrefab)
        {
            Debug.LogWarning("[SeedBeltSpawner] Assign worldCenter and seedPrefab.");
            return;
        }

        // clamp cap so it's valid on the sphere
        float clampedCapY = Mathf.Clamp(capYMin, -shellRadius + 0.001f, shellRadius - 0.001f);
        float thetaMax = Mathf.Acos(clampedCapY / shellRadius); // 0 at 'north pole', π at 'south'

        for (int i = 0; i < seedCount; i++)
        {
            // --- sample a point uniformly on spherical cap (θ ∈ [0, θMax], φ ∈ [0, 2π)) ---
            float u = Random.value;
            float cosTheta = Mathf.Lerp(1f, Mathf.Cos(thetaMax), u); // uniform on cap
            float theta = Mathf.Acos(cosTheta);
            float phi = Random.Range(0f, Mathf.PI * 2f);

            // base unit direction from center to point
            Vector3 dir =
                new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Cos(theta),
                    Mathf.Sin(theta) * Mathf.Sin(phi)
                );

            // position on shell with slight radius jitter
            float r = shellRadius + Random.Range(-radialJitter, radialJitter);
            Vector3 pos = worldCenter.position + dir * r;

            // instantiate
            GameObject go = Instantiate(seedPrefab, pos, Quaternion.identity);

            // ensure SeedMotion / SeedBehavior
            var motion = go.GetComponent<SeedMotion>();
            var beh = go.GetComponent<SeedBehavior>();
            if (!motion || !beh)
            {
                Debug.LogWarning("[SeedBeltSpawner] Seed prefab missing SeedMotion or SeedBehavior.");
                continue;
            }

            // OUTWARD unit (away from center) at spawn
            Vector3 rOutHat = (pos - worldCenter.position).normalized;

            // choose tangential direction (eastward around the sphere)
            Vector3 upRef = Vector3.up; // swap for your planet-up if you have one
            Vector3 tangent = Vector3.Cross(upRef, rOutHat).normalized;

            // keep a consistent winding, or randomize if requested
            if (randomizeWinding && Random.value < 0.5f) tangent = -tangent;

            // small heading jitter around the tangent for variety
            if (headingJitterDeg > 0f)
            {
                float jitterRad = headingJitterDeg * Mathf.Deg2Rad;
                // tilt a little toward +/- the great-circle normal
                Vector3 normal = Vector3.Cross(rOutHat, tangent).normalized;
                tangent = Vector3.Normalize(
                    Quaternion.AngleAxis(Random.Range(-headingJitterDeg, headingJitterDeg), normal) * tangent
                );
            }

            // pick initial tangential speed (between your inner/outer styles)
            float speed01 = Mathf.InverseLerp(shellRadius - radialJitter, shellRadius + radialJitter, r);
            float tanSpeed = Mathf.Lerp(orbitSpeedInner, orbitSpeedOuter, speed01)
                             + Random.Range(-speedVariance, speedVariance);

            // tiny radial bias to settle toward your shell logic (optional)
            float vRadOut = 0f;

            // compose initial velocity and apply
            Vector3 v0 = tangent * Mathf.Max(0.1f, tanSpeed) + rOutHat * vRadOut;
            motion.SetVelocity(v0);

            // face velocity
            if (v0.sqrMagnitude > 1e-6f)
                go.transform.rotation = Quaternion.LookRotation(v0.normalized, Vector3.up);

            // hand into WorldGravity immediately so your oscillation logic takes over
            if (forceWorldGravityOnSpawn)
            {
                beh.BeginWorldGravity(worldCenter);
            }
        }

        GameLog.Log($"[SeedBeltSpawner] Spawned {seedCount} seeds on spherical cap (R={shellRadius}, y≥center+{capYMin}).");
    }

    private void Start()
    {
        SpawnNow();
    }

    // Visualize the cap & shell in scene view
    private void OnDrawGizmosSelected()
    {
        if (!worldCenter) return;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.4f);
        Gizmos.DrawWireSphere(worldCenter.position, shellRadius);

        // draw a ring approximating the cap boundary
        float clampedCapY = Mathf.Clamp(capYMin, -shellRadius + 0.001f, shellRadius - 0.001f);
        float y = worldCenter.position.y + clampedCapY;
        float ringRadius = Mathf.Sqrt(Mathf.Max(0f, shellRadius * shellRadius - clampedCapY * clampedCapY));

        // draw ring
        int seg = 64;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = new Vector3(
                worldCenter.position.x + ringRadius * Mathf.Cos(a),
                y,
                worldCenter.position.z + ringRadius * Mathf.Sin(a)
            );
            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
