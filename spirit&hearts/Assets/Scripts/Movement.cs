using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [Header("Physics Controls")]
    [SerializeField] private float gravity = -9.8f; // m/s²
    

    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;

    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1.43f;
    private readonly float glideStrength = 5f;
    private readonly float maxSpeed = 20f;
    private readonly float minHandSpread = 1.0f;
    private readonly float glideRotationSpeed = 40f; // kept for future UX toggles

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool isGrounded = false;

    public Vector3 CurrentVelocity => velocity;


    void Start()
    {
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // 🔁 Get positions + rotations
        Vector3 headPos = head.position;
        Vector3 headFwd = head.forward;
        Quaternion headRot = head.rotation;

        Vector3 leftHandPos = leftHand.position;
        Vector3 rightHandPos = rightHand.position;
        Quaternion leftRot = leftHand.rotation;
        Quaternion rightRot = rightHand.rotation;

        // ───── Hand Motion Setup ─────
        Vector3 leftHandDelta = (leftHandPos - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (rightHandPos - prevRightPos) / Time.deltaTime;
        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);

        float flapMagnitude = Mathf.Clamp01(Mathf.Min(-leftHandDelta.y, -rightHandDelta.y));
        bool wingsOutstretched = handDistance > minHandSpread;

        // 🐦 Apply flap velocity
        if (flapMagnitude > 0.05f)
        {
            // TO-DO: 
            // - If grip buttons are held, calculate strength as percentage of magnitude capped to 10
            velocity += FlightPhysics.CalculateFlapVelocity(
                headFwd,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );
        }

        bool inGlidePosture = wingsOutstretched && flapMagnitude < 0.05f;

        // 🪂 Apply glide physics and smooth forward redirection together
        if (inGlidePosture && velocity.magnitude > 0.1f)
        {
            // Apply lift, dive, forward push
            velocity = FlightPhysics.CalculateGlideVelocity(
                velocity,
                headFwd,
                handDistance,
                minHandSpread,
                flapStrength,
                glideStrength,
                maxSpeed,
                Time.deltaTime
            );

            // Blend only in gravity
            Vector3 currentDir = velocity.normalized;
            Vector3 desiredDir = headFwd.normalized;
            float blendAmount = Time.deltaTime * 2f;

            Vector3 blendedDir = Vector3.Slerp(currentDir, desiredDir, blendAmount);
            velocity = blendedDir * velocity.magnitude;

            // 🌍 Apply gravity
            velocity += Vector3.down * gravity * Time.deltaTime;
        }

        // ✈️ Move player
        transform.position += velocity * Time.deltaTime;

        // 🌬️ Apply drag
        velocity *= inGlidePosture ? 0.99f : 0.98f;

        // 🧪 Debug lines
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 2f, Color.cyan, 0f, false); // Velocity

        // 🔁 Save for next frame
        prevLeftPos = leftHandPos;
        prevRightPos = rightHandPos;

        // 🎥 Record
        if (recorder != null && recorder.enabled)
        {
            recorder.RecordFrame(
                headPos, headRot,
                leftHandPos, leftRot,
                rightHandPos, rightRot,
                leftHandDelta, rightHandDelta,
                flapMagnitude, velocity
            );
        }
    }
}
