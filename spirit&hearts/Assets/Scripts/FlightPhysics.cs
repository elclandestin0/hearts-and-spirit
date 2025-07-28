using UnityEngine;

public static class FlightPhysics
{
    public static Vector3 CalculateFlapVelocity(
    Vector3 headForward,
    float flapMagnitude,
    float flapStrength = 1f,
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
        float maxDiveSpeed,
        float deltaTime,
        bool isManualDivePose,
        ref float glideTime,
        ref float diveAngle,
        bool recentlyBounced,
        float bounceTimer,
        float timeSinceDiveEnd,
        float diveStartTime,
        bool isSpeedBoosted
    )
    {
        Vector3 velocity = currentVelocity;

        Vector3 currentDir = velocity.normalized;
        float currentSpeed = velocity.magnitude;

        // ✨ New logic: if we just exited a dive, blend slower
        float diveBlendMod = Mathf.Lerp(1.5f, 3.0f, Mathf.Clamp01(timeSinceDiveEnd / 2f));
        float blendSpeed = diveBlendMod;

        Vector3 blendedDir = Vector3.Slerp(currentDir, headForward.normalized, deltaTime * blendSpeed);
        float blendedSpeed = Mathf.Lerp(currentSpeed, currentSpeed + glideStrength, deltaTime * blendSpeed);

        velocity = blendedDir * blendedSpeed;

        // 🌬️ Decaying glide push
        float glideDecay = 1f - (glideTime * 0.05f);
        float currentGlideStrength = glideStrength * glideDecay;

        // 🕊️ Decaying lift that eventually loses to gravity
        float forwardSpeed = Vector3.Dot(velocity, headForward);
        float lift = Mathf.Clamp01(forwardSpeed / maxDiveSpeed);
        float upAngle = Vector3.Angle(headForward, Vector3.up);
        float liftFactor = Mathf.InverseLerp(90f, 10f, upAngle);

        // 🧮 Lift decay modifier based on glide time, stronger pull down over time
        float liftDecay = 1f - Mathf.Pow(glideTime, 1.2f) * 0.04f; // nonlinear decay
        float liftPower = lift * liftFactor * liftDecay * deltaTime;

        if (!isManualDivePose)
        {
            velocity += blendedDir * currentGlideStrength * deltaTime;
            velocity += Vector3.up * liftPower;
        }

        // 🪂 Dive-enhanced glide (smooth, embedded in glide)
        if (diveAngle < 60f && isManualDivePose)
        {
            float rawDive = Mathf.InverseLerp(60f, 10f, diveAngle);
            float easedDive = Mathf.Pow(rawDive, 1.1f); // subtle nonlinear ramp
            float diveIntensity = Mathf.Lerp(0.1f, 1f, rawDive);
            float diveSpeedBoost = diveIntensity * maxDiveSpeed;

            float diveTime = Time.time - diveStartTime;
            float diveRamp = Mathf.SmoothStep(0f, 1f, diveTime / 2f);

            // Boost is *extra glide force*, not overriding anything
            // float divePush = diveSpeedBoost * diveRamp * deltaTime * 0.5f;
            float divePush = diveSpeedBoost * deltaTime * 0.5f;
            // Add dive push in the glide direction
            velocity += headForward.normalized * divePush;

            // Slightly counteract glideTime to extend strong glide
            glideTime = Mathf.Max(0f, glideTime - deltaTime * 3f);
        }
        else
        {
            glideTime += deltaTime;
        }


        // Cap speed no cap
        float speed = velocity.magnitude;
        if (speed > maxDiveSpeed && !isSpeedBoosted)
        {
            // Gradually reduce speed toward maxDiveSpeed
            float decaySpeed = 2.5f; // adjust for how quickly you want it to settle
            float newSpeed = Mathf.Lerp(speed, maxDiveSpeed, Time.deltaTime * decaySpeed);
            velocity = velocity.normalized * newSpeed;
        }

        return velocity;
    }
}