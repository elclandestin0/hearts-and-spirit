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
    public float forwardPropulsionStrength = 3f; // Strength of forward movement during flaps
    public float glideRotationSpeed = 45f; // Degrees per second for rotation while gliding
    private Vector3 velocity = Vector3.zero; // Stores the player's movement speed
    private Vector3 prevLeftPos, prevRightPos; // Stores the previous frame positions of hands    
    private bool isGliding = false;
    private Vector3 flapDirection; // Stores the direction of the flap
    private bool isFlapping = false;
    private bool isGrounded = false; // You'll need to implement ground detection
    public float minHandSpread = 1.0f; // Minimum distance between hands for gliding control


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
        Vector3 handDirection = (rightHand.position - leftHand.position).normalized;
        float handDistance = Vector3.Distance(leftHand.position, rightHand.position);
        Debug.Log("handDistance: " + handDistance);
        
        isFlapping = (leftHandDelta.y < -1f && rightHandDelta.y < -1f);
        isGliding = !isFlapping && !isGrounded && velocity.magnitude > 0.1f;
        Debug.Log($"State - Flapping: {isFlapping}, Gliding: {isGliding}");

        if (isFlapping)
        {
            // Calculate flap direction and forward movement
            Vector3 handPlane = (rightHand.position - leftHand.position).normalized;
            flapDirection = Vector3.Cross(handPlane, Vector3.right);
            
            // Add upward force
            velocity += flapDirection * flapStrength;
            velocity.y = Mathf.Max(velocity.y + gravityCompensation, velocity.y);
            
            // Add forward propulsion in the direction player is facing
            velocity += transform.forward * forwardPropulsionStrength;
            
            Debug.Log($"Flapping - Vertical Force: {flapDirection * flapStrength}, Forward Force: {transform.forward * forwardPropulsionStrength}");
        }
        else if (isGliding)
        {
            // Handle gliding rotation based on arm tilt
            if (handDistance > minHandSpread)
            {
                // Calculate tilt angle of arms (left/right)
                Vector3 idealHorizontalPlane = Vector3.right; // Reference for level arms
                float tiltAngle = Vector3.SignedAngle(handDirection, idealHorizontalPlane, Vector3.forward);
                
                // Apply rotation based on tilt
                float rotationAmount = 0f;
                if (Mathf.Abs(tiltAngle) > 10f) // Add a small deadzone
                {
                    rotationAmount = Mathf.Sign(tiltAngle) * glideRotationSpeed * Time.deltaTime;
                    transform.Rotate(Vector3.up, rotationAmount);
                }
                
                Debug.Log($"Gliding Rotation - Tilt Angle: {tiltAngle:F2}°, Rotation Amount: {rotationAmount:F2}°");
                
                // Normal gliding forward movement
                velocity -= transform.forward * glideStrength * Time.deltaTime;
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

        if (velocity.magnitude < 0.3f)
        {
            float fallSpeed = descentSpeed * 0.5f * Time.deltaTime;
            transform.position += Vector3.down * fallSpeed;
            Debug.Log($"Stalling - descending manually at {fallSpeed:F2}");
        }

        // ✅ Always apply forward movement
        transform.position += velocity * Time.deltaTime;
        Debug.Log($"Current velocity: {velocity}, Position: {transform.position}");

        // Air resistance (less while gliding)
        velocity *= isGliding ? 0.99f : 0.98f;

        // Store hand positions for next frame
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
}