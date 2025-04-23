using UnityEngine;

public static class FlightPhysics
{
    public static Vector3 CalculateFlapVelocity(
    Vector3 headForward,
    float flapMagnitude,
    Vector3 playerPosition,
    Vector3 planetCenter,
    float flapStrength = 0.35f,
    float forwardThrust = 0.5f)
    {
        Vector3 up = (playerPosition - planetCenter).normalized;
        Vector3 tangentForward = Vector3.ProjectOnPlane(headForward, up).normalized;

        Vector3 velocity = Vector3.zero;
        velocity += up * flapStrength * flapMagnitude;
        velocity += tangentForward * forwardThrust * flapMagnitude;

        return velocity;
    }

    public static Vector3 CalculateGlideVelocity(
        Vector3 currentVelocity,
        Vector3 headForward,
        float glideStrength,
        float maxSpeed,
        float maxDiveSpeed,
        float deltaTime,
        bool isManualDivePose,
        ref float glideTime,
        ref float diveAngle,
        Vector3 playerPosition,
        Vector3 planetCenter)
    {
        Vector3 up = (playerPosition - planetCenter).normalized;
        Vector3 tangentForward = Vector3.ProjectOnPlane(headForward, up).normalized;

        Vector3 velocity = currentVelocity;
        Vector3 currentDir = velocity.normalized;
        float currentSpeed = velocity.magnitude;

        Debug.Log($"[GLIDE INIT] currentSpeed: {currentSpeed:F2}, isManualDivePose: {isManualDivePose}, glideTime: {glideTime:F2}");

        // 🔁 Blend current direction toward where the player is looking
        Vector3 blendedDir = Vector3.Slerp(currentDir, tangentForward, deltaTime * 2f);
        float blendedSpeed = Mathf.Lerp(currentSpeed, currentSpeed + glideStrength, deltaTime * 2f);

        velocity = blendedDir * blendedSpeed;

        // 🌬️ Decaying glide push
        float glideDecay = 1f - (glideTime * 0.05f);
        float currentGlideStrength = glideStrength * glideDecay;
        velocity += blendedDir * currentGlideStrength * deltaTime;

        // 🕊️ Decaying lift that eventually loses to gravity
        float forwardSpeed = Vector3.Dot(velocity, tangentForward);
        float lift = Mathf.Clamp01(forwardSpeed / maxSpeed);
        float upAngle = Vector3.Angle(tangentForward, up);
        float liftFactor = Mathf.InverseLerp(90f, 10f, upAngle);

        // 🧮 Lift decay modifier based on glide time, stronger pull down over time
        float liftDecay = (1f - (Mathf.Pow(glideTime, 1.2f) * 0.04f)); // nonlinear decay
        float liftPower = lift * liftFactor * liftDecay * deltaTime;

        Debug.Log($"[LIFT CHECK] FinalLift: {liftPower:F2}, LiftDecay: {liftDecay:F2}, LiftFactor: {liftFactor:F2}");
        velocity += up * liftPower;

        // 🦅 Dive mechanic
        diveAngle = Vector3.Angle(tangentForward, -up);
        if (diveAngle < 60f && isManualDivePose)
        {
            float rawDive = Mathf.InverseLerp(60f, 10f, diveAngle);
            float diveIntensity = Mathf.Lerp(0.001f, 1.0f, rawDive);
            float diveSpeed = diveIntensity * maxDiveSpeed;
            Debug.Log($"[DIVING - Velocity before calculation] Velocity: {velocity:F2}, diveSpeed: {diveSpeed:F2}");
            velocity += tangentForward * diveSpeed * deltaTime;
            Debug.Log($"[DIVE CHECK] RawDive: {rawDive:F2}, DiveIntensity: {diveIntensity:F2}, DiveSpeed: {diveSpeed:F2}, Velocity: {velocity:F2}");

            // 👇 Dive resets glide decay for future lift
            glideTime = Mathf.Max(0f, glideTime - deltaTime * 10f);
        }
        else
        {
            // ⏱️ Accumulate glide time
            glideTime += deltaTime;
        }

        // 🛑 Cap forward speed
        float currentForwardSpeed = Vector3.Dot(velocity, tangentForward);
        float speedLimit = maxDiveSpeed;
        Debug.Log($"[FINAL SPEED CHECK] ForwardSpeed: {currentForwardSpeed:F2}, SpeedLimit: {speedLimit:F2}");

        Debug.Log($"[SPEED CHECK BEFORE CAP] Velocity: {velocity:F2}");
        if (currentForwardSpeed > speedLimit)
        {
            Vector3 forwardDir = tangentForward;
            Vector3 forwardVelocity = forwardDir * currentForwardSpeed;
            Vector3 excess = forwardVelocity - (forwardDir * speedLimit);
            velocity -= excess;
        }
        Debug.Log($"[FINAL VELOCITY] Velocity: {velocity}, Magnitude: {velocity.magnitude:F2}");
        return velocity;
    }
}
