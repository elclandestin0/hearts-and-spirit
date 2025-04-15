using UnityEngine;

public static class FlightPhysics
{
    public static Vector3 CalculateFlapVelocity(Vector3 headForward, float flapMagnitude, float flapStrength = 0.35f, float forwardThrust = 0.5f)
    {
        Vector3 velocity = Vector3.zero;

        velocity += Vector3.up * flapStrength * flapMagnitude;
        velocity += headForward.normalized * forwardThrust * flapMagnitude;

        return velocity;
    }

    public static Vector3 CalculateGlideVelocity(
        Vector3 currentVelocity,
        Vector3 headForward,
        float handDistance,
        float minHandSpread,
        float flapStrength,
        float glideStrength,
        float maxSpeed,
        float deltaTime)
    {
        Vector3 velocity = currentVelocity;

        if (handDistance <= minHandSpread)
            return velocity; // Not gliding if hands are too close

        // ✈️ Base glide lift
        float forwardSpeed = Vector3.Dot(velocity, headForward);
        float liftForce = Mathf.Clamp01(forwardSpeed / maxSpeed) * flapStrength * 0.8f;
        velocity += Vector3.up * liftForce * deltaTime;

        // 🌬️ Forward push from gliding
        velocity += headForward * glideStrength * deltaTime;

        // 🦅 Smooth dive ramp based on angle
        float diveAngle = Vector3.Angle(headForward, Vector3.down);
        Debug.Log("Dive angle: " + diveAngle);

        if (diveAngle < 60f)
        {
            // Dive intensity scales from 0.8 → 1.0 as you approach 10°
            float rawDive = Mathf.InverseLerp(60f, 10f, diveAngle);  // 60° = 0, 10° = 1
            float diveIntensity = Mathf.Lerp(0.8f, 1.0f, rawDive);   // Scale nicely

            // Direction is exactly where you're looking
            Vector3 diveDir = headForward.normalized;

            float diveSpeed = diveIntensity * 100f;
            velocity += diveDir * diveSpeed * deltaTime;

            Debug.Log($"[DIVE] 💥 DiveAngle: {diveAngle:F1}°, Intensity: {diveIntensity:F2}, Speed: {diveSpeed:F1}, Dir: {diveDir}");
        }

        // 🪂 Prevent falling too fast without forward speed
        float descentLimit = Mathf.Lerp(0f, -0.5f, 1f - (forwardSpeed / maxSpeed));
        velocity.y = Mathf.Max(velocity.y, descentLimit);

        return velocity;
    }

}
