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
        ref float glideTime,
        ref float diveAngle)
    {
       Vector3 velocity = currentVelocity;

        Vector3 currentDir = velocity.normalized;
        float currentSpeed = velocity.magnitude;

        
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
        float liftFactor = Mathf.InverseLerp(90f, 10f, upAngle);

        // 🧮 Lift decay modifier based on glide time, stronger pull down over time
        float liftDecay = 1f - Mathf.Pow(glideTime, 1.2f) * 0.04f; // nonlinear decay
        float liftPower = lift * liftFactor * liftDecay * deltaTime;

        velocity += Vector3.up * liftPower;

        // 🦅 Dive mechanic
        diveAngle = Vector3.Angle(headForward, Vector3.down);
        if (diveAngle < 50f && isManualDivePose)
        {
            float rawDive = Mathf.InverseLerp(50f, 10f, diveAngle);
            float diveIntensity = Mathf.Lerp(0.001f, 1.0f, rawDive);
            float diveSpeed = diveIntensity * maxDiveSpeed;
            velocity += headForward.normalized * diveSpeed * deltaTime;
            
            // 👇 Dive resets glide decay for future lift
            glideTime = Mathf.Max(0f, glideTime - deltaTime * 10f);
        } 
        else 
        {
            // ⏱️ Accumulate glide time
            glideTime += deltaTime;
        }

        // 🛑 Cap forward speed
        float currentForwardSpeed = Vector3.Dot(velocity, headForward);
        float speedLimit = maxDiveSpeed;

        if (currentForwardSpeed > speedLimit)
        {
            Vector3 forwardDir = headForward.normalized;
            Vector3 forwardVelocity = forwardDir * currentForwardSpeed;
            Vector3 excess = forwardVelocity - (forwardDir * speedLimit);
            velocity -= excess;
        }
        return velocity;
    }
}
// +447305934810