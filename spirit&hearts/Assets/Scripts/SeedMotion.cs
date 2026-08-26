using UnityEngine;

[DisallowMultipleComponent]
public class SeedMotion : MonoBehaviour
{
    [Header("Motion")]
    public Vector3 Velocity;               // single source of truth
    [SerializeField] private float drag = 0.0f;   // optional, set to taste (0 keeps your feel)
    [SerializeField] private bool debugLogs = false;

    // If another script wants to suspend integration (e.g., during cutscenes)
    public bool integrationEnabled = true;

    void Update()
    {
        if (!integrationEnabled) return;

        // integrate
        transform.position += Velocity * Time.deltaTime;

        if (drag > 0f)
        {
            float d = Mathf.Clamp01(drag * Time.deltaTime);
            Velocity *= (1f - d);
        }

        if (debugLogs)
            GameLog.Log($"[SeedMotion] pos={transform.position} vel={Velocity} speed={Velocity.magnitude:F2}");
    }

    public void SetVelocity(Vector3 v) => Velocity = v;
    public void AddImpulse(Vector3 dv) => Velocity += dv;

    // Safe slerp toward a direction (keeps speed)
    public void SteerTowardDir(Vector3 desiredDir, float blend)
    {
        float speed = Velocity.magnitude;
        Vector3 curDir = speed > 1e-6f ? Velocity.normalized : desiredDir;
        Vector3 newDir = Vector3.Slerp(curDir, desiredDir, Mathf.Clamp01(blend));
        Velocity = newDir * speed;
    }

    public void SetSpeed(float newSpeed)
    {
        Vector3 dir = Velocity.sqrMagnitude > 1e-6f ? Velocity.normalized : transform.forward;
        Velocity = dir * Mathf.Max(0f, newSpeed);
    }
}
