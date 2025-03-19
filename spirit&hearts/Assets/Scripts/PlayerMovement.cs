using UnityEngine;

public class HandFlapMovement : MonoBehaviour
{
    public Transform leftHand, rightHand; // Assign these to the left and right hand OVRHand objects
    public float flapStrength = 5f; // How much force is applied when flapping
    public float gravityCompensation = 2f; // Helps counter gravity for smoother flight
    public float maxSpeed = 5f; // Limit movement speed
    private Vector3 velocity = Vector3.zero; // Stores the player's movement speed

    private Vector3 prevLeftPos, prevRightPos; // Stores the previous frame positions of hands

    void Start()
    {
        // Initialize previous positions to current hand positions
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // Calculate vertical movement of each hand
        float leftHandVelocity = (leftHand.position.y - prevLeftPos.y) / Time.deltaTime;
        float rightHandVelocity = (rightHand.position.y - prevRightPos.y) / Time.deltaTime;

        // Detect flapping motion (hands moving downward quickly)
        if (leftHandVelocity < -1f && rightHandVelocity < -1f)
        {
            velocity += Vector3.forward * flapStrength; // Move forward
            velocity.y += gravityCompensation; // Counteract gravity for lift
        }

        // Apply movement with speed limit
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        transform.position += velocity * Time.deltaTime;

        // Slow down gradually (air resistance)
        velocity *= 0.98f;

        // Store hand positions for next frame comparison
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
}