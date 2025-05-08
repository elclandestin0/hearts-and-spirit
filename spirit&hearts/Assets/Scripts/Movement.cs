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
    private readonly float maxDiveSpeed = 110f;
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

    // What the flap?
    public delegate void FlapEvent();
    public event FlapEvent OnFlap;
    private float lastFlapTime = -1f;
    [SerializeField] private float minFlapInterval = 1.2f;
    public HandVelocityTracker leftVelocity;
    public HandVelocityTracker rightVelocity;


    void Start()
    {
        // Save initial world-space hand positions for motion delta
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        UpdateDeltaValues();
        UpdateDiveAngle();

        HandleFlapDetection();
        HandleGlideLogic();
        ApplyGravityIfNeeded();
        ApplyMovement();
        ApplyDrag();

        SavePreviousFramePositions();
        RecordMotion();
        DrawTheLines();
        CapSpeed();
    }

    private Vector3 currentLeftRel, currentRightRel;
    private Vector3 leftHandDelta, rightHandDelta;
    private Quaternion leftRot, rightRot;
    private Quaternion headRot;
    private Vector3 headFwd, headPos;

    private void UpdateDeltaValues()
    {
        currentLeftRel = leftHand.position;
        currentRightRel = rightHand.position;
        leftHandDelta = (currentLeftRel - prevLeftPos) / Time.deltaTime;
        rightHandDelta = (currentRightRel - prevRightPos) / Time.deltaTime;

        headPos = head.position;
        headFwd = head.forward;
        headRot = head.rotation;
        leftRot = leftHand.rotation;
        rightRot = rightHand.rotation;
    }

    private void UpdateDiveAngle()
    {
        diveAngle = Vector3.Angle(headFwd, Vector3.down);
    }

    private void HandleFlapDetection()
    {
        float leftDown = -leftVelocity.SmoothedVelocity.y;
        float rightDown = -rightVelocity.SmoothedVelocity.y;
        float avgDownSpeed = (leftDown + rightDown) / 2f * 0.5f; // ✂️ Halved for balance

        Debug.Log("Left hand speed: " + leftDown);
        Debug.Log("Right hand speed: " + rightDown);

        float minFlapThreshold = 1.0f;
        float maxFlapVelocity = 10f;

        bool isMovingDown = avgDownSpeed > minFlapThreshold && avgDownSpeed < maxFlapVelocity;
        bool enoughTimePassed = Time.time - lastFlapTime >= 0.2f;
        float flapMagnitude = 2.0f;

        // return flap multiplier
        float speed = velocity.magnitude;
        float flapStrengthMultiplier = Mathf.Lerp(1f, 4f, Mathf.InverseLerp(30f, maxDiveSpeed, speed));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Flap");
            Vector3 flapFinalCalculation = flapStrengthMultiplier * FlightPhysics.CalculateFlapVelocity(
                head.forward,
                2.0f,
                flapStrength,
                forwardPropulsionStrength
            );
            velocity += flapFinalCalculation;
            Debug.Log("Flap strength " + flapStrengthMultiplier);
            Debug.Log("Flap final calculation " + flapFinalCalculation);
            glideTime = 0f;
            lastFlapTime = Time.time;
        }

        if (!isMovingDown || !enoughTimePassed) return;

        // 🏋️‍♂️ Map down speed → strength multiplier (1x to 3x)
        flapStrengthMultiplier = Mathf.InverseLerp(minFlapThreshold, 2.5f, avgDownSpeed);
        flapMagnitude = Mathf.Lerp(1f, 3f, flapStrengthMultiplier);

        velocity += FlightPhysics.CalculateFlapVelocity(
            head.forward,
            flapMagnitude * flapStrengthMultiplier,
            flapStrength,
            forwardPropulsionStrength
        );

        glideTime = 0f;
        lastFlapTime = Time.time;
        OnFlap?.Invoke();
    }

    private void HandleGlideLogic()
    {
        float handDistance = Vector3.Distance(currentLeftRel, currentRightRel);
        bool wingsOutstretched = handDistance > minHandSpread;
        isGliding = wingsOutstretched;

        if (Input.GetKey(KeyCode.M))
        {
            isGliding = true;
        }

        if (!isGliding) return;

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

    private void ApplyGravityIfNeeded()
    {
        if (isGliding)
            velocity += Vector3.down * gravity * Time.deltaTime;
        else
        {
            // Optional: blend toward forward if not gliding
            Vector3 blendedDir = Vector3.Slerp(velocity.normalized, headFwd.normalized, Time.deltaTime * 1.5f);
            velocity = blendedDir * velocity.magnitude;
        }
    }

    private void ApplyMovement()
    {
        transform.position += velocity * Time.deltaTime;
    }

    private void ApplyDrag()
    {
        velocity *= 0.995f;
    }

    private void SavePreviousFramePositions()
    {
        prevLeftPos = currentLeftRel;
        prevRightPos = currentRightRel;
    }

    private void RecordMotion()
    {
        if (recorder != null && recorder.enabled)
        {
            recorder.RecordFrame(
                headPos, headRot,
                currentLeftRel, leftRot,
                currentRightRel, rightRot,
                leftHandDelta, rightHandDelta,
                10f, velocity // You may want to compute actual flapMagnitude
            );
        }
    }

    private void DrawTheLines()
    {
        Debug.DrawLine(head.position, head.position + velocity.normalized * 5f, Color.cyan, 0f, false);
        Debug.DrawLine(head.position, head.position + headFwd * 3f, Color.red, 0f, false);
    }

    private void CapSpeed() 
    {
        // 🛑 Cap forward speed -- Uncomment later
        float currentForwardSpeed = Vector3.Dot(velocity, head.forward);
        float speedLimit = maxDiveSpeed;

        if (currentForwardSpeed > speedLimit)
        {
            Vector3 forwardDir = head.forward.normalized;
            Vector3 forwardVelocity = forwardDir * currentForwardSpeed;
            Vector3 excess = forwardVelocity - (forwardDir * speedLimit);
            velocity -= excess;
        }
    }
}
