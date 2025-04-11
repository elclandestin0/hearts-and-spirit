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
    private bool isGrounded = false;

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
            velocity += FlightPhysics.CalculateFlapVelocity(
                headFwd,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );
        }

        // 🪂 Apply glide physics
        if (wingsOutstretched && flapMagnitude < 0.05f && velocity.magnitude > 0.1f)
        {
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
        }

        // 🧭 Smooth turn toward look direction
        Vector3 horiz = new Vector3(velocity.x, 0, velocity.z);
        Vector3 forwardBlend = Vector3.Lerp(horiz.normalized, headFwd, 0.05f);
        velocity = forwardBlend * horiz.magnitude + Vector3.up * velocity.y;

        // ✈️ Move player
        transform.position += velocity * Time.deltaTime;

        // 🌍 Apply gravity
        velocity += Vector3.down * gravity * Time.deltaTime;

        // 🌬️ Apply drag
        bool inGlidePosture = wingsOutstretched && flapMagnitude < 0.05f;
        velocity *= inGlidePosture ? 0.99f : 0.98f;

        // 🧪 Debug lines
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 2f, Color.cyan, 0f, false); // Velocity
        Debug.DrawLine(headPos, headPos + headFwd * 2f, Color.white, 0f, false); // Head forward
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 2f, Color.blue, 0f, false); // Upward

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
