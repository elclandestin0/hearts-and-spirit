using UnityEngine;

public static class FlightPhysics
{
    public static Vector3 CalculateFlapVelocity(
    Vector3 headForward,
    float flapMagnitude,
    float flapStrength = 0.35f,
    float forwardThrust = 0.5f)
    {
        Vector3 velocity = Vector3.zero;

        velocity += Vector3.up * flapStrength * flapMagnitude;
        velocity += headForward.normalized * forwardThrust * flapMagnitude;

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
        ref float glideTime)
    {
       Vector3 velocity = currentVelocity;

        Vector3 currentDir = velocity.normalized;
        float currentSpeed = velocity.magnitude;

        Debug.Log($"[GLIDE INIT] currentSpeed: {currentSpeed:F2}, isManualDivePose: {isManualDivePose}, glideTime: {glideTime:F2}");
        
        // 🔁 Blend current direction toward where the player is looking
        Vector3 blendedDir = Vector3.Slerp(currentDir, headForward.normalized, deltaTime * 2f);
        float blendedSpeed = Mathf.Lerp(currentSpeed, currentSpeed + glideStrength, deltaTime * 2f);

        velocity = blendedDir * blendedSpeed;

        // 🌬️ Decaying glide push
        float glideDecay = 1f - (glideTime * 0.05f);
        float currentGlideStrength = glideStrength * glideDecay;
        velocity += blendedDir * currentGlideStrength * deltaTime;

        // 🕊️ Decaying lift that eventually loses to gravity
        float forwardSpeed = Vector3.Dot(velocity, headForward);
        float lift = Mathf.Clamp01(forwardSpeed / maxSpeed);
        float upAngle = Vector3.Angle(headForward, Vector3.up);
        float liftFactor = Mathf.InverseLerp(90f, 0f, upAngle);

        // 🧮 Lift decay modifier based on glide time, stronger pull down over time
        float liftDecay = (1f - (Mathf.Pow(glideTime, 1.2f) * 0.04f)); // nonlinear decay
        float liftPower = lift * liftFactor * liftDecay * deltaTime;

        Debug.Log($"[LIFT CHECK] FinalLift: {liftPower:F2}, LiftDecay: {liftDecay:F2}, LiftFactor: {liftFactor:F2}");
        velocity += Vector3.up * liftPower;

        // 🦅 Dive mechanic
        float diveAngle = Vector3.Angle(headForward, Vector3.down);
        if (diveAngle < 60f && isManualDivePose)
        {
            float rawDive = Mathf.InverseLerp(60f, 10f, diveAngle);
            float diveIntensity = Mathf.Lerp(0.8f, 1.0f, rawDive);
            float diveSpeed = diveIntensity * maxDiveSpeed;
            Debug.Log($"[DIVING - Velocity before calculation] Velocity: {velocity:F2}, diveSpeed: {diveSpeed:F2}");
            velocity += headForward.normalized * diveSpeed * deltaTime;
            Debug.Log($"[DIVE CHECK] RawDive: {rawDive:F2}, DiveIntensity: {diveIntensity:F2}, DiveSpeed: {diveSpeed:F2}, Velocity: {velocity:F2}");
            
            // 👇 Dive resets glide decay for future lift
            glideTime = Mathf.Max(0f, glideTime - deltaTime * 5f);
        } 
        else 
        {
            // ⏱️ Accumulate glide time
            glideTime += deltaTime;
        }

        // 🛑 Cap forward speed
        float currentForwardSpeed = Vector3.Dot(velocity, headForward);
        float speedLimit = maxDiveSpeed;
        Debug.Log($"[FINAL SPEED CHECK] ForwardSpeed: {currentForwardSpeed:F2}, SpeedLimit: {speedLimit:F2}");

        Debug.Log($"[SPEED CHECK BEFORE CAP] Velocity: {velocity:F2}");
        if (currentForwardSpeed > speedLimit)
        {
            Vector3 forwardDir = headForward.normalized;
            Vector3 forwardVelocity = forwardDir * currentForwardSpeed;
            Vector3 excess = forwardVelocity - (forwardDir * speedLimit);
            velocity -= excess;
        }
        Debug.Log($"[FINAL VELOCITY] Velocity: {velocity}, Magnitude: {velocity.magnitude:F2}");
        return velocity;
    }
}
