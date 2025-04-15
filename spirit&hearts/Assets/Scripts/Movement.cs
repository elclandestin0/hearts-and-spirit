using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [Header("Physics Controls")]
    [SerializeField] private float gravity = 9.8f; // m/s²
    

    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;

    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1.43f;
    private readonly float glideStrength = 5f;
    private readonly float maxSpeed = 20f;
    private readonly float minHandSpread = 1.0f;
    // private readonly float glideRotationSpeed = 40f; // kept for future UX toggles

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool isGrounded = false;

    public Vector3 CurrentVelocity => velocity;


    void Start()
    {
        // Save initial world-space hand positions for motion delta
        prevLeftPos = leftHand.position - head.position;
        prevRightPos = rightHand.position - head.position;
    }

    void Update()
    {
        // 🎯 Position references
        Vector3 headPos = head.position;
        Vector3 headFwd = head.forward;
        Quaternion headRot = head.rotation;

        Quaternion leftRot = leftHand.rotation;
        Quaternion rightRot = rightHand.rotation;

        // 🔁 Deltas in WORLD space (for flap motion detection)
        Vector3 currentLeftRel = leftHand.position - head.position;
        Vector3 currentRightRel = rightHand.position - head.position;

        Vector3 leftHandDelta = (currentLeftRel - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (currentRightRel - prevRightPos) / Time.deltaTime;

        // 📏 Local space hand distance for posture
        float handDistance = Vector3.Distance(currentLeftRel, currentRightRel);
        bool wingsOutstretched = handDistance > minHandSpread;

        // 🐦 Flap detection
        float leftSpeed = -leftHandDelta.y;
        float rightSpeed = -rightHandDelta.y;

        Debug.Log($"Left speed: {leftSpeed:F2}, Right speed: {rightSpeed:F2}");

        float minFlapThreshold = 1.5f;
        bool isFlapping = leftSpeed > minFlapThreshold && rightSpeed > minFlapThreshold;

        float flapMagnitude = isFlapping
            ? Mathf.Clamp01((leftSpeed + rightSpeed) / 2f / 5f)
            : 0f;

        if (isFlapping)
        {
            Debug.Log("✋ Flapping");
            velocity += FlightPhysics.CalculateFlapVelocity(
                headFwd,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );
        }

        // 🪂 Glide posture logic
        bool inGlidePosture = wingsOutstretched && flapMagnitude < 0.05f;
        if (inGlidePosture && velocity.magnitude > 0.1f)
        {
            Debug.Log("🕊️ Gliding");
            velocity = FlightPhysics.CalculateGlideVelocity(
                velocity,
                headFwd,
                handDistance,
                minHandSpread,
                glideStrength,
                maxSpeed,
                Time.deltaTime
            );

            // 🎯 Smooth forward alignment
            float fallAlignment = Mathf.Clamp01(Vector3.Dot(velocity.normalized, headFwd.normalized)); // 0 = opposite, 1 = aligned
            float blendStrength = Mathf.Lerp(0.05f, 0.5f, fallAlignment); // strong when aligned, weak when diving

            Vector3 blendedDir = Vector3.Slerp(velocity.normalized, headFwd.normalized, Time.deltaTime * blendStrength);
            velocity = blendedDir * velocity.magnitude;
        }
        else
        {
            // 👇 Simulate fall posture
            Vector3 fallDir = Vector3.Slerp(velocity.normalized, Vector3.down, Time.deltaTime * 0.5f);
            velocity = fallDir * velocity.magnitude;
        }

        // 🌍 Apply gravity
        velocity += Vector3.down * gravity * Time.deltaTime;

        // ✈️ Apply movement
        transform.position += velocity * Time.deltaTime;

        // 🌬️ Apply drag
        velocity *= 0.995f;

        // 🧪 Debug
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 2f, Color.cyan, 0f, false);

        // 🔁 Save previous frame world-space hand positions
        prevLeftPos = currentLeftRel;
        prevRightPos = currentRightRel;

        // 🎥 Record    
        if (recorder != null && recorder.enabled)
        {
            recorder.RecordFrame(
                headPos, headRot,
                currentLeftRel, leftRot,
                currentRightRel, rightRot,
                leftHandDelta, rightHandDelta,
                flapMagnitude, velocity
            );
        }
    }

}
