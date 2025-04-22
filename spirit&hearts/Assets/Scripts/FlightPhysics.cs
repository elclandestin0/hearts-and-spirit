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
    float glideStrength,
    float maxSpeed,
    float maxDiveSpeed,
    float deltaTime,
    bool isManualDivePose,
    ref float glideTime // ⏱️ Track how long they've been gliding
)
    {
        Vector3 velocity = currentVelocity;

        // 🔄 Time accumulates as long as player is gliding
        glideTime += deltaTime;

        // Base direction blending toward where the player is looking
        Vector3 blendedDir = Vector3.Slerp(velocity.normalized, headForward.normalized, deltaTime * 2f);
        float blendedSpeed = velocity.magnitude;

        velocity = blendedDir * blendedSpeed;

        // 🚀 Glide force decreases slightly over time to simulate air resistance
        float glideDecay = Mathf.Clamp01(1f - (glideTime * 0.05f)); // Adjustable decay rate
        float currentGlideStrength = glideStrength * glideDecay;
        velocity += blendedDir * currentGlideStrength * deltaTime;

        // 🕊️ Lift logic based on look angle and forward speed
        float forwardSpeed = Vector3.Dot(velocity, headForward);
        float lift = Mathf.Clamp01(forwardSpeed / maxSpeed);
        float upAngle = Vector3.Angle(headForward, Vector3.up);
        float liftFactor = Mathf.InverseLerp(90f, 10f, upAngle);
        float liftDecay = Mathf.Clamp01(1f - (glideTime * 0.05f)); // Optional separate lift decay
        velocity += Vector3.up * (lift * liftFactor * liftDecay * deltaTime * 8f);

        // 🪂 Optional manual dive
        float diveAngle = Vector3.Angle(headForward, Vector3.down);
        if (diveAngle < 60f && isManualDivePose)
        {
            float rawDive = Mathf.InverseLerp(60f, 10f, diveAngle);
            float diveIntensity = Mathf.Lerp(0.8f, 1.0f, rawDive);
            float diveSpeed = diveIntensity * maxDiveSpeed;
            velocity += headForward.normalized * diveSpeed * deltaTime;
        }

        // 🛑 Forward speed cap
        float currentForwardSpeed = Vector3.Dot(velocity, headForward);
        float speedLimit = isManualDivePose ? maxDiveSpeed : maxSpeed;
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
