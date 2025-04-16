using UnityEngine;

public static class FlightPhysics
{
    public static Vector3 CalculateFlapVelocity(
    Vector3 headForward,
    float flapMagnitude,
    float flapStrength = 0.35f,
    float forwardThrust = 0.5f
)
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
        float glideStrength,
        float maxSpeed,
        float deltaTime)
    {
        Vector3 velocity = currentVelocity;
        Debug.Log($"[GLIDE] In: Velocity = {currentVelocity}, Mag = {currentVelocity.magnitude}");

        // Directional blend
        Vector3 currentDir = velocity.normalized;
        float currentSpeed = velocity.magnitude;

        // Blend direction + slightly smooth the magnitude (optional, tweakable)
        Vector3 blendedDir = Vector3.Slerp(currentDir, headForward.normalized, deltaTime * 2f);
        float blendedSpeed = Mathf.Lerp(currentSpeed, currentSpeed + glideStrength, deltaTime * 2f); // optional smoothing

        velocity = blendedDir * blendedSpeed;

        // Forward push
        Vector3 glidePush = blendedDir * glideStrength * deltaTime;
        Debug.Log($"[GLIDE] GlidePush = {glidePush}");
        velocity += glidePush;

        // Lift
        float forwardSpeed = Vector3.Dot(velocity, headForward);
        float lift = Mathf.Clamp01(forwardSpeed / maxSpeed);
        float upAngle = Vector3.Angle(headForward, Vector3.up);
        float liftFactor = Mathf.InverseLerp(90f, 10f, upAngle);
        Debug.Log($"[GLIDE] LiftFactor = {liftFactor:F2}, LiftForce = {lift}, FinalLift = {lift * liftFactor * deltaTime * 8f}");
        velocity += Vector3.up * lift * liftFactor * deltaTime * 8f;

        // Final output
        Debug.Log($"[GLIDE] Out: Velocity = {velocity}, Y = {velocity.y:F2}, Z = {velocity.z:F2}");

        // 🦅 Smooth dive ramp based on angle
        float diveAngle = Vector3.Angle(headForward, Vector3.down);
        if (diveAngle < 60f)
        {
            // Dive intensity from 0.8 to 1.0 between 60° and 10°
            float rawDive = Mathf.InverseLerp(60f, 10f, diveAngle);
            float diveIntensity = Mathf.Lerp(0.8f, 1.0f, rawDive);

            // Dive direction is exactly where the player is looking
            Vector3 diveDir = headForward.normalized;

            // Dive force
            float diveSpeed = diveIntensity * 30f;
            velocity += diveDir * diveSpeed * deltaTime;
            Debug.Log($"[DIVE] Angle: {diveAngle:F1}°, Intensity: {diveIntensity:F2}, Speed: {diveSpeed:F1}, Dir: {diveDir}");
        }

        // 🛑 Cap forward speed
        Vector3 forwardDir = headForward.normalized;
        float currentForwardSpeed = Vector3.Dot(velocity, forwardDir);

        if (currentForwardSpeed > maxSpeed)
        {
            // Remove the excess in the forward direction
            Vector3 forwardVelocity = forwardDir * currentForwardSpeed;
            Vector3 excess = forwardVelocity - (forwardDir * maxSpeed);
            velocity -= excess;

            Debug.Log($"[GLIDE] ⚠️ Forward speed capped: {currentForwardSpeed:F2} → {maxSpeed:F2}");
        }

        return velocity;
    }


}
