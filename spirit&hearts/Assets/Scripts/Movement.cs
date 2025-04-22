using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [Header("Physics Controls")]
    [SerializeField] private float gravity = 9.8f; // m/s²
    [SerializeField] private float glideTime = 0f;
    
    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;

    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1.43f;
    private readonly float glideStrength = 5f;
    private readonly float maxSpeed = 30f;
    private readonly float maxDiveSpeed = 80f; // or 100f
    private readonly float minHandSpread = 1.0f;
    // private readonly float glideRotationSpeed = 40f; // kept for future UX toggles
    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    // To-do: use later
    // private bool isGrounded = false;
    [SerializeField] private bool isGliding = false;

    // Publicly accessible variables for reference
    public Vector3 CurrentVelocity => velocity;
    public Transform Head => head;

    // Logger variable(s)
    private static readonly Logger diveLogger = new Logger(Debug.unityLogger.logHandler);



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

        float minFlapThreshold = 1.5f;
        bool isFlapping = leftSpeed > minFlapThreshold && rightSpeed > minFlapThreshold;

        float flapMagnitude = isFlapping
            ? Mathf.Clamp01((leftSpeed + rightSpeed) / 2f / 5f)
            : 0f;

        if (isFlapping)
        {
            velocity += FlightPhysics.CalculateFlapVelocity(
                headFwd,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );

            glideTime = 0f;
        }


        // 🪂 Glide posture logic
        bool inGlidePosture = wingsOutstretched && flapMagnitude < 0.05f;
        if ((inGlidePosture && velocity.magnitude > 0.1f) || isGliding)
        {
            // Are both hands behind the head?
            Vector3 leftToHead = leftHand.position - head.position;
            Vector3 rightToHead = rightHand.position - head.position;

            float leftDot = Vector3.Dot(leftToHead, head.forward);
            float rightDot = Vector3.Dot(rightToHead, head.forward);

            bool leftBehind = leftDot < -0.2f;
            bool rightBehind = rightDot < -0.2f;

            bool isManualDivePose = leftBehind && rightBehind;

            Debug.Log($"[DIVE CHECK] LeftDot: {leftDot:F2}, RightDot: {rightDot:F2}");

            velocity = FlightPhysics.CalculateGlideVelocity(
                velocity,
                headFwd,
                glideStrength,
                maxSpeed,
                maxDiveSpeed,
                Time.deltaTime,
                true,
                ref glideTime
            );
        }

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
