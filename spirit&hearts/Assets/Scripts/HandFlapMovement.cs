using UnityEngine;

public class HandFlapMovement : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [Header("Physics Controls")]
    [SerializeField] private float gravity = 4f; // m/s²
    

    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;

    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 0.35f;
    private readonly float forwardPropulsionStrength = 0.5f;
    private readonly float glideStrength = 2.5f;
    private readonly float maxSpeed = 7f;
    private readonly float minHandSpread = 1.0f;
    private readonly float glideRotationSpeed = 40f; // kept for future UX toggles

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool isGliding = false;
    private bool isFlapping = false;
    private bool isGrounded = false;

    void Start()
    {
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // 🔁 Override rotation input if ghost is active, with any flight record
        Quaternion headRot = head.rotation;
        Quaternion leftRot = leftHand.rotation;
        Quaternion rightRot = rightHand.rotation;
       

        // 🔁 Override position input if ghost is active
        Vector3 headPos =  head.position;
        Vector3 headFwd = head.forward;
        Vector3 leftHandPos = leftHand.position;
        Vector3 rightHandPos = rightHand.position;

        // ───── Motion + Posture Setup ─────
        Vector3 leftHandDelta = (leftHandPos - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (rightHandPos - prevRightPos) / Time.deltaTime;
        Vector3 handDirection = (rightHandPos - leftHandPos).normalized;
        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);

        isFlapping = (leftHandDelta.y < -1f && rightHandDelta.y < -1f);
        isGliding = !isFlapping && !isGrounded && velocity.magnitude > 0.1f;

        if (isFlapping)
        {
            // 🐦 Flap = Lift + Thrust from posture
            Vector3 headToLeft = leftHandPos - headPos;
            Vector3 headToRight = rightHandPos - headPos;
            Vector3 postureNormal = Vector3.Cross(headToRight, headToLeft).normalized;

            Vector3 upwardLift = Vector3.up * flapStrength;
            Vector3 forwardThrust = headFwd * forwardPropulsionStrength;

            velocity += upwardLift;
            velocity += forwardThrust;

            Debug.DrawLine(headPos, headPos + postureNormal * 2f, Color.green, 0f, false); // Flap direction
        }
        else if (isGliding)
        {
            if (handDistance > minHandSpread)
            {
                // 🪲 Simulate lift from forward velocity
                float forwardSpeed = Vector3.Dot(velocity, headFwd);
                float liftForce = Mathf.Clamp01(forwardSpeed / maxSpeed) * flapStrength * 0.8f;
                velocity += Vector3.up * liftForce * Time.deltaTime;

                // Glide push forward
                Vector3 forwardGlide = headFwd * glideStrength;
                velocity += forwardGlide * Time.deltaTime;

                // 🦅 Stronger eagle dive based on look angle
                float diveAngle = Vector3.Angle(headFwd, Vector3.down);
                if (diveAngle < 60f)
                {
                    float diveIntensity = Mathf.InverseLerp(75f, 15f, diveAngle);
                    float diveSpeed = diveIntensity * 20f;
                    float diveForward = diveIntensity * 12f;

                    velocity += Vector3.down * diveSpeed * Time.deltaTime;
                    velocity += headFwd * diveForward * Time.deltaTime;

                    Debug.Log($"[DIVE] Angle: {diveAngle:F1}°, Intensity: {diveIntensity:F2}, Down: {diveSpeed:F2}, Forward: {diveForward:F2}");
                }

                // Descent limit based on forward speed
                float verticalDescentCap = Mathf.Lerp(0f, -0.5f, 1f - (forwardSpeed / maxSpeed));
                velocity.y = Mathf.Max(velocity.y, verticalDescentCap);

                // 🔄 [Future Feature] Glide turning by hand tilt
                /*
                Vector3 localHandDirection = transform.InverseTransformDirection(handDirection);
                float tiltAngle = Vector3.SignedAngle(localHandDirection, Vector3.right, Vector3.forward);

                if (Mathf.Abs(tiltAngle) > 25f)
                {
                    float rotationAmount = Mathf.Sign(tiltAngle) * glideRotationSpeed * Time.deltaTime;
                    transform.Rotate(Vector3.up, rotationAmount);
                    Debug.Log($"[GLIDE TURN] Tilt: {tiltAngle:F2}°, Rotated: {rotationAmount:F2}°");
                }
                */
            }
        }

        // 🤝 Gradually steer toward head.forward
        Vector3 currentHorizontal = new Vector3(velocity.x, 0f, velocity.z);
        float currentSpeed = currentHorizontal.magnitude;
        Vector3 desiredDirection = headFwd.normalized;
        Vector3 smoothForward = Vector3.Lerp(currentHorizontal.normalized, desiredDirection, 0.05f);
        velocity = smoothForward * currentSpeed + Vector3.up * velocity.y;

        // ✈️ Apply movement
        transform.position += velocity * Time.deltaTime;

        // 🌍 Apply constant gravity
        velocity += Vector3.down * gravity * Time.deltaTime;

        // 🌬️ Drag
        velocity *= isGliding ? 0.99f : 0.98f;

        // 🧪 Debug Lines
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 2f, Color.cyan, 0f, false); // Velocity
        Debug.DrawLine(headPos, headPos + headFwd * 2f, Color.white, 0f, false); // Look direction
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.blue, 0f, false); // Lift dir

        // 🔁 Update hands unless ghosting
        prevLeftPos = leftHandPos;
        prevRightPos = rightHandPos;

        // 🎥 Record the final, actual frame data (true physics)
        if (recorder != null && recorder.enabled)
        {
            recorder.RecordFrame(
                headPos, headRot,
                leftHandPos, leftRot,
                rightHandPos, rightRot,
                leftHandDelta, rightHandDelta,
                velocity.magnitude, velocity
            );
        }
    }
}
