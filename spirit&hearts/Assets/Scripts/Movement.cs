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
    public float diveAngle = 0f;
    
    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;
    
    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1.43f;
    private readonly float glideStrength = 2.5f;
    private readonly float maxSpeed = 30f;
    private readonly float maxDiveSpeed = 80f;
    private readonly float minHandSpread = 1.0f;
    // private readonly float glideRotationSpeed = 40f; // kept for future UX toggles
    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    private bool wasMovingDown = false; // Track previous frame's downward motion
    // To-do: use later
    // private bool isGrounded = false;
    [Header("Debug variables")]
    public bool isGliding = false;
    public bool isFlapping = false;

    // Publicly accessible variables for reference
    public Vector3 CurrentVelocity => velocity;
    public float MaxSpeed => maxDiveSpeed;

    // What the flap?
    public delegate void FlapEvent();
    public event FlapEvent OnFlap;
    private float lastFlapTime = -1f;
    [SerializeField] private float minFlapInterval = 1.2f;

    void Start()
    {
        // Save initial world-space hand positions for motion delta
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
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
        Vector3 currentLeftRel = leftHand.position;
        Vector3 currentRightRel = rightHand.position;

        Vector3 leftHandDelta = (currentLeftRel - prevLeftPos) / Time.deltaTime;
        Vector3 rightHandDelta = (currentRightRel - prevRightPos) / Time.deltaTime;

        // Calculate dive angle
        diveAngle = Vector3.Angle(headFwd, Vector3.down);

        // 📏 Local space hand distance for posture
        float handDistance = Vector3.Distance(currentLeftRel, currentRightRel);
        bool wingsOutstretched = handDistance > minHandSpread;
        
        // Thresholds
        float minFlapThreshold = 1f;
        float maxFlapVelocity = 10f;

        // Downward speed
        float leftSpeed = -leftHandDelta.y;
        float rightSpeed = -rightHandDelta.y;
        float avgDownSpeed = (leftSpeed + rightSpeed) / 2f;

        // Flap zone: hands not behind head + above chest height
        bool handsInFront = Vector3.Dot(leftHand.position - head.position, head.forward) > -0.2f &&
                            Vector3.Dot(rightHand.position - head.position, head.forward) > -0.2f;

        bool handsHighEnough = leftHand.position.y > head.position.y * 0.9f &&
                            rightHand.position.y > head.position.y * 0.9f;

        // Track flap motion state
        bool isMovingDown = avgDownSpeed > minFlapThreshold && avgDownSpeed < maxFlapVelocity;

        // Detect the start of a flap motion
        bool isFlappingPosture = handsHighEnough && 
            isMovingDown && 
            !wasMovingDown; // Only trigger on the start of downward motion
        
        float flapMagnitude = 10f;
        bool canFlap = Time.time - lastFlapTime >= minFlapInterval;

        // Add space held down for more than 1 second = activate isGliding to true
        if (isFlappingPosture && canFlap)
        {
            velocity += FlightPhysics.CalculateFlapVelocity(
                head.forward,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );

            glideTime = 0f;
            lastFlapTime = Time.time;  // ✅ Mark this flap
            OnFlap?.Invoke();
        }

        // Update previous frame's motion state
        wasMovingDown = isMovingDown;

        // 🪂 Glide posture logic
        bool inGlidePosture = wingsOutstretched;

        if (inGlidePosture)
        {
            // Are both hands behind the head?
            Vector3 leftToHead = leftHand.position - head.position;
            Vector3 rightToHead = rightHand.position - head.position;

            float leftDot = Vector3.Dot(leftToHead, head.forward);
            float rightDot = Vector3.Dot(rightToHead, head.forward);

            bool leftBehind = leftDot < -0.2f;
            bool rightBehind = rightDot < -0.2f;

            bool isManualDivePose = leftBehind && rightBehind;


            velocity = FlightPhysics.CalculateGlideVelocity(
                velocity,
                headFwd,
                glideStrength,
                maxSpeed,
                maxDiveSpeed,
                Time.deltaTime,
                true,
                ref glideTime,
                ref diveAngle
            );
        }

        // If no longer gliding, take the last magnitude and move in the blended direction
        else 
        {
            Vector3 blendedDir = Vector3.Slerp(velocity.normalized, headFwd.normalized, Time.deltaTime * 1.5f);
            velocity = blendedDir * velocity.magnitude;
        }

        velocity += (isGliding || inGlidePosture) ? Vector3.down * gravity * Time.deltaTime : Vector3.zero;

        // ✈️ Apply movement
        transform.position += velocity * Time.deltaTime;

        // 🌬️ Apply drag
        velocity *= 0.995f;

        // 🧪 Debug
        Debug.DrawLine(head.position, head.position + velocity.normalized * 5f, Color.cyan, 0f, false);
        Debug.DrawLine(head.position, head.position + headFwd * 3f, Color.red, 0f, false);

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
