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

        if (diveAngle < 45f) // start easing in
        {
            Debug.Log("Diving: " + diveAngle);
            float diveIntensity = Mathf.InverseLerp(45f, 10f, diveAngle); // 0 to 1 between 45° and 10°
            float diveSpeed = diveIntensity * 30f;     // vertical drop
            float diveForward = diveIntensity * 1f;   // horizontal plunge

            velocity += Vector3.down * diveSpeed * deltaTime;
            velocity += headForward * diveForward * deltaTime;

            Debug.Log($"[DIVE] Angle: {diveAngle:F1}°, Intensity: {diveIntensity:F2}, Down: {diveSpeed:F1}, Forward: {diveForward:F1}");
        }

        // 🪂 Prevent falling too fast without forward speed
        float descentLimit = Mathf.Lerp(0f, -0.5f, 1f - (forwardSpeed / maxSpeed));
        velocity.y = Mathf.Max(velocity.y, descentLimit);

        return velocity;
    }

}
