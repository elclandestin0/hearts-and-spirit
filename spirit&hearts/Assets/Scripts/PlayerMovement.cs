using UnityEngine;

public class HandFlapMovement : MonoBehaviour
{
    [Header("References")]
    public Transform leftHand, rightHand, head; // Assign these manually or via script

    [Header("Flap Settings")]
    public float flapStrength = 4f;
    public float forwardPropulsionStrength = 10f;
    public float gravityCompensation = 3f;

    [Header("Glide Settings")]
    public float glideStrength = 2f;
    public float glideRotationSpeed = 45f;
    public float descentSpeed = 0.5f;
    public float minHandSpread = 1.0f;

    [Header("Movement Settings")]
    public float maxSpeed = 5f;
    public float turnStrength = 3f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool isGliding = false;
    private bool isFlapping = false;
    private bool isGrounded = false; // Implement later if needed

    void Start()
    {
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // --- Calculate hand deltas and posture ---
        Vector3 leftHandDelta = (leftHand.position - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (rightHand.position - prevRightPos) / Time.deltaTime;
        Vector3 handDirection = (rightHand.position - leftHand.position).normalized;
        float handDistance = Vector3.Distance(leftHand.position, rightHand.position);

        isFlapping = (leftHandDelta.y < -1f && rightHandDelta.y < -1f);
        isGliding = !isFlapping && !isGrounded && velocity.magnitude > 0.1f;
        Debug.Log($"State - Flapping: {isFlapping}, Gliding: {isGliding}");

        if (isFlapping)
        {
            // ✅ Calculate dynamic flap direction using posture (hands + head)
            Vector3 headToLeft = leftHand.position - head.position;
            Vector3 headToRight = rightHand.position - head.position;
            Vector3 postureNormal = Vector3.Cross(headToRight, headToLeft).normalized;
            Vector3 flapDirection = Vector3.Lerp(postureNormal, transform.forward, 0.2f).normalized;

            // ✅ Add upward + forward force based on posture
            velocity += flapDirection * flapStrength;
            velocity.y = Mathf.Max(velocity.y + gravityCompensation, velocity.y);
            velocity += transform.forward * forwardPropulsionStrength;

            Debug.Log($"Flapping - FlapDirection: {flapDirection}, Forward: {transform.forward}");
        }
        else if (isGliding)
        {
            if (handDistance > minHandSpread)
            {
                // ✅ Arm tilt for rotation
                Vector3 localHandDirection = transform.InverseTransformDirection(handDirection);
                float tiltAngle = Vector3.SignedAngle(localHandDirection, Vector3.right, Vector3.forward);

                if (Mathf.Abs(tiltAngle) > 25f)
                {
                    float rotationAmount = Mathf.Sign(tiltAngle) * glideRotationSpeed * Time.deltaTime;
                    transform.Rotate(Vector3.up, rotationAmount);
                    Debug.Log($"Gliding Turn - Tilt: {tiltAngle:F2}°, Rotate: {rotationAmount:F2}°");
                }

                // ✅ Apply forward glide
                velocity -= -(transform.forward) * glideStrength * Time.deltaTime;
            }
            else
            {
                Debug.Log($"Hands too close for glide control. Distance: {handDistance:F2}");
            }
        }

        // Clamp speed
        Vector3 preClampVelocity = velocity;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        if (preClampVelocity != velocity)
        {
            Debug.Log($"Speed limited from {preClampVelocity.magnitude:F2} to {velocity.magnitude:F2}");
        }

        // ✅ Stall descent when velocity too low
        if (velocity.magnitude < 0.3f)
        {
            float fallSpeed = descentSpeed * 0.5f * Time.deltaTime;
            transform.position += Vector3.down * fallSpeed;
            Debug.Log($"Stalling - descending manually at {fallSpeed:F2}");
        }

        // ✅ Redirect horizontal velocity to match facing direction
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float horizontalSpeed = horizontal.magnitude;
        Vector3 redirected = transform.forward * horizontalSpeed;
        velocity = new Vector3(redirected.x, velocity.y, redirected.z);

        // ✅ Apply movement
        transform.position += velocity * Time.deltaTime;
        // 🔍 Visualize the movement vector
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 2f, Color.green, 0f, false);

        Debug.Log($"Velocity Applied: {velocity}");

        // ✅ Air resistance
        velocity *= isGliding ? 0.99f : 0.98f;

        // Update hand positions
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
}
