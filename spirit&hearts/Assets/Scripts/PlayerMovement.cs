using UnityEngine;

public class HandFlapMovement : MonoBehaviour
{
    [Header("Body part reference")]
    public Transform leftHand, rightHand, head; // Assign these manually or via script

    // Flight tuning (locked to script)
    private readonly float flapStrength = 0.75f;
    private readonly float forwardPropulsionStrength = 1.0f;
    private readonly float glideStrength = 2.5f;
    private readonly float glideRotationSpeed = 40f;
    private readonly float maxSpeed = 7f;
    private readonly float minHandSpread = 1.0f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool isGliding = false;
    private bool isFlapping = false;
    private bool isGrounded = false; // Ground detection can be added later

    void Start()
    {
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // --- Hand movement + posture setup ---
        Vector3 leftHandDelta = (leftHand.position - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (rightHand.position - prevRightPos) / Time.deltaTime;
        Vector3 handDirection = (rightHand.position - leftHand.position).normalized;
        float handDistance = Vector3.Distance(leftHand.position, rightHand.position);

        isFlapping = (leftHandDelta.y < -1f && rightHandDelta.y < -1f);
        isGliding = !isFlapping && !isGrounded && velocity.magnitude > 0.1f;

        if (isFlapping)
        {
            // 🐦 FLAP PHYSICS: upward lift + forward thrust
            Vector3 headToLeft = leftHand.position - head.position;
            Vector3 headToRight = rightHand.position - head.position;
            Vector3 postureNormal = Vector3.Cross(headToRight, headToLeft).normalized;

            // 🧭 Draw the posture-based normal (flap direction)
            Debug.DrawLine(head.position, head.position + postureNormal * 10f, Color.green, 0f, false);

            Vector3 upwardLift = Vector3.up * flapStrength;
            Vector3 forwardThrust = head.forward * forwardPropulsionStrength;
            

            velocity += upwardLift;
            velocity += forwardThrust;

            Debug.Log($"[FLAP] Lift: {upwardLift}, Thrust: {forwardThrust}");
        }
        else if (isGliding)
        {
            if (handDistance > minHandSpread)
            {
                // 🌀 Turning from hand tilt
                // Uncomment in case of locomotion setting
                // Need to work on this further. 

                // Vector3 localHandDirection = transform.InverseTransformDirection(handDirection);
                // float tiltAngle = Vector3.SignedAngle(localHandDirection, Vector3.right, Vector3.forward);

                // if (Mathf.Abs(tiltAngle) > 25f)
                // {
                //     float rotationAmount = Mathf.Sign(tiltAngle) * glideRotationSpeed * Time.deltaTime;
                //     transform.Rotate(Vector3.up, rotationAmount);
                //     Debug.Log($"[GLIDE TURN] Tilt: {tiltAngle:F2}°, Rotation: {rotationAmount:F2}°");
                // }

                // 🐦 Glide lift based on forward speed
                float forwardSpeed = Vector3.Dot(velocity, head.forward);
                float liftForce = Mathf.Clamp01(forwardSpeed / maxSpeed) * flapStrength * 0.8f;
                velocity += Vector3.up * liftForce * Time.deltaTime;

                // Forward push
                velocity += head.forward * glideStrength * Time.deltaTime;

                Debug.Log($"[GLIDE] Lift: {liftForce:F2}, Forward: {head.forward * glideStrength * Time.deltaTime}");
            }
        }

        // 🦅 DIVE BOOST when looking steeply down
        float diveAngle = Vector3.Angle(head.forward, Vector3.down);
        if (diveAngle < 60f) // looking downward steeply
        {
            float diveIntensity = Mathf.InverseLerp(60f, 20f, diveAngle); // stronger the steeper
            float diveSpeed = diveIntensity * 10f; // tune this number for power
            velocity += Vector3.down * diveSpeed * Time.deltaTime;

            // Optional: add forward momentum too
            velocity += head.forward * diveIntensity * glideStrength * Time.deltaTime;

            Debug.Log($"[DIVE] Angle: {diveAngle:F1}°, Intensity: {diveIntensity:F2}, DiveSpeed: {diveSpeed:F2}");
        }
        
        // 🔁 Redirect horizontal velocity to current facing direction
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float horizontalSpeed = horizontal.magnitude;
        Vector3 redirected = head.forward * horizontalSpeed;
        velocity = new Vector3(redirected.x, velocity.y, redirected.z);

        // 🛫 Apply movement
        transform.position += velocity * Time.deltaTime;

        // 🪂 Air resistance
        velocity *= isGliding ? 0.99f : 0.98f;

        // Update hand positions
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }
}
