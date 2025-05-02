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
    // To-do: use later
    // private bool isGrounded = false;
    [Header("Debug variables")]
    public bool isGliding = false;
    public bool isFlapping = false;

    // Publicly accessible variables for reference
    public Vector3 CurrentVelocity => velocity;
    public float MaxSpeed => maxDiveSpeed;
    public delegate void FlapEvent();
    public event FlapEvent OnFlap;
    // Logger variable(s)
    private static readonly Logger diveLogger = new Logger(Debug.unityLogger.logHandler);

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

        // 📏 Local space hand distance for posture
        float handDistance = Vector3.Distance(currentLeftRel, currentRightRel);
        bool wingsOutstretched = handDistance > minHandSpread;

        // 🐦 Flap detection
        float leftSpeed = -leftHandDelta.y;
        float rightSpeed = -rightHandDelta.y;

        float minFlapThreshold = 1.5f;
        bool isFlappingPosture = leftSpeed > minFlapThreshold && rightSpeed > minFlapThreshold;
        float flapMagnitude = isFlappingPosture
            ? Mathf.Clamp01((leftSpeed + rightSpeed) / 2f / 5f)
            : 0f;

        // 🖐️ Simulated Flap (Debugging without VR)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isFlapping = true;
            isFlappingPosture = true;
            flapMagnitude = Random.Range(3.0f, 4.0f);
        }

        // isGliding boolean control
        if (Input.GetKey(KeyCode.M))
        {
            isGliding = true;
        }

        else 
        {
            isGliding = false;
        }

        // Add space held down for more than 1 second = activate isGliding to true

        if (isFlapping)
        {
            velocity += FlightPhysics.CalculateFlapVelocity(
                headFwd,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );

            glideTime = 0f;
            
            // Fire the flap event
            OnFlap?.Invoke();
            isFlapping = false;
            isFlappingPosture = false;
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

        velocity += isGliding ? Vector3.down * gravity * Time.deltaTime : Vector3.zero;

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
