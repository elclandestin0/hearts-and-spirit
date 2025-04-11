using UnityEngine;

public static class PhysicsMachine
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
            return velocity; // not a valid glide

        float forwardSpeed = Vector3.Dot(velocity, headForward);
        float liftForce = Mathf.Clamp01(forwardSpeed / maxSpeed) * flapStrength * 0.8f;
        velocity += Vector3.up * liftForce * deltaTime;

        velocity += headForward * glideStrength * deltaTime;

        float diveAngle = Vector3.Angle(headForward, Vector3.down);
        if (diveAngle < 60f)
        {
            float diveIntensity = Mathf.InverseLerp(75f, 15f, diveAngle);
            float diveSpeed = diveIntensity * 20f;
            float diveForward = diveIntensity * 12f;

            velocity += Vector3.down * diveSpeed * deltaTime;
            velocity += headForward * diveForward * deltaTime;
        }

        float descentLimit = Mathf.Lerp(0f, -0.5f, 1f - (forwardSpeed / maxSpeed));
        velocity.y = Mathf.Max(velocity.y, descentLimit);

        return velocity;
    }
}
