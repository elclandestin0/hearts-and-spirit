using UnityEngine;

public class HandFlapMovement : MonoBehaviour
{
    public Transform leftHand, rightHand; // Assign these to the left and right hand OVRHand objects
    public float flapStrength = 2f; // How much force is applied when flapping
    public float gravityCompensation = 2f; // Helps counter gravity for smoother flight
    public float maxSpeed = 5f; // Limit movement speed
    public float glideStrength = 2f; // How strong the gliding effect is
    public float turnStrength = 3f; // How sharp the turns are while gliding
    public float descentSpeed = 2f; // How fast we descend when hands are tilted down
    private Vector3 velocity = Vector3.zero; // Stores the player's movement speed
    private Vector3 prevLeftPos, prevRightPos; // Stores the previous frame positions of hands    
    private bool isGliding = false;
    private Vector3 flapDirection; // Stores the direction of the flap
    private bool isFlapping = false;
    private bool isGrounded = false; // You'll need to implement ground detection


    void Start()
    {
        // Initialize previous positions to current hand positions
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
    void Update()
    {
        // Calculate hand movements
        Vector3 leftHandDelta = (leftHand.position - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (rightHand.position - prevRightPos) / Time.deltaTime;

        isFlapping = leftHandDelta.y < -1f && rightHandDelta.y < -1f;
        isGliding = !isFlapping && !isGrounded && velocity.magnitude > 0.1f;
        Debug.Log($"State - Flapping: {isFlapping}, Gliding: {isGliding}");

        if (isFlapping)
        {
            Vector3 handPlane = (rightHand.position - leftHand.position).normalized;
            flapDirection = Vector3.Cross(handPlane, Vector3.right);
            Debug.Log($"Flapping! Direction: {flapDirection}");

            velocity += flapDirection * flapStrength;
            velocity.y = Mathf.Max(velocity.y + gravityCompensation, velocity.y);
        }
        
        else if (isGliding)
            {
                Vector3 handDirection = rightHand.position - leftHand.position;
                float handAngle = Vector3.Angle(handDirection, Vector3.right);
                Vector3 handNormal = Vector3.Cross(handDirection, Vector3.right);
                float pitchAngle = Vector3.Angle(handNormal, Vector3.up);
                Debug.Log($"Gliding - Hand angle: {handAngle:F2}°, Pitch: {pitchAngle:F2}°");

                if (Mathf.Abs(handAngle - 180f) < 30f)
                {
                    // Apply forward glide
                    velocity += transform.forward * glideStrength * Time.deltaTime;

                    // ✅ NEW: Always allow slow descent while gliding
                    velocity += Vector3.down * descentSpeed * Time.deltaTime;

                    // Optional: add descent boost when pitched downward
                    if (pitchAngle > 100f)
                    {
                        velocity += Vector3.down * descentSpeed * 0.5f * Time.deltaTime;
                        Debug.Log($"Descending faster - Pitch angle: {pitchAngle:F2}°");
                    }

                    // Handle turning based on banking
                    float bankAngle = (handAngle - 180f);
                    Vector3 turnDirection = transform.right * bankAngle * turnStrength * Time.deltaTime;
                    velocity += turnDirection;
                    Debug.Log($"Gliding - Bank angle: {bankAngle:F2}°, Turn direction: {turnDirection}");
                }
                else
                {
                    Debug.Log("Improper hand position while gliding");
                }
            }

        // ✅ REMOVED: Block that prevented downward movement
        // if (velocity.y < 0) { velocity.y = 0f; }

        // Clamp speed
        Vector3 preClampVelocity = velocity;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        if (preClampVelocity != velocity)
        {
            Debug.Log($"Speed limited from {preClampVelocity.magnitude:F2} to {velocity.magnitude:F2}");
        }

        if (velocity.magnitude < 0.3f)
        {
            float fallSpeed = descentSpeed * 0.5f * Time.deltaTime;
            transform.position += Vector3.down * fallSpeed;
            Debug.Log($"Stalling - descending manually at {fallSpeed:F2}");
        }

        // ✅ Always apply forward movement
        transform.position += velocity * Time.deltaTime;
        Debug.Log($"Current velocity: {velocity}, Position: {transform.position}");

        // Air resistance
        float previousSpeed = velocity.magnitude;
        velocity *= isGliding ? 0.99f : 0.98f;
        Debug.Log($"Air resistance - Speed change: {previousSpeed:F2} -> {velocity.magnitude:F2}");

        // Update hand positions
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
}