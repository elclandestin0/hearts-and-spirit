using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
public class Movement : MonoBehaviour
{
    [Header("XR Input Actions")]
    public InputActionReference leftGrip;
    public InputActionReference rightGrip;
    public InputActionReference rightStickAction;

    [Header("XR Transform References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;
    public Transform rigRoot; // The object you want to rotate (usually XROrigin or a parent of the camera)
    [Header("Physics Controls")]
    [SerializeField] private float gravity = 9.8f; // m/s²
    [SerializeField] private float glideTime = 0f;
    public float diveAngle = 0f;

    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;
    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1f;
    private readonly float glideStrength = 1f;
    private readonly float maxSpeed = 30f;
    private readonly float maxDiveSpeed = 120f;
    private readonly float minHandSpread = 1.0f;
    private float snapAngle = 45f;
    private float turnThreshold = 0.8f;
    private float turnCooldown = 0.5f;

    private float turnCooldownTimer = 0f;
    private bool canSnapTurn => turnCooldownTimer <= 0f;

    // private readonly float glideRotationSpeed = 40f; // kept for future UX toggles
    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;
    // To-do: use later
    // private bool isGrounded = false;
    [Header("Debug variables")]
    public bool isGliding = false;
    public bool isFlapping = false;
    private bool isHovering = false;

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
    private bool wasMovingDownLastFrame = false;

    // Bounce
    private bool recentlyBounced = false;
    private float bounceTimer = 0f;
    private float bounceDuration = 1f;
    private float bounceDampFactor = 0.9f;
    private bool inputLockedDuringBounce => recentlyBounced && bounceTimer > 0f;

    void Start()
    {
        // Save initial world-space hand positions for motion delta
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
    }

    void Update()
    {
        // CheckSurfaceImpact();

        // Handle bounce recovery (loss of control)
        if (inputLockedDuringBounce)
        {
            bounceTimer -= Time.deltaTime;

            velocity *= 0.98f;

            // Move character while in bounce phase
            ApplyMovement();
            ApplyDrag();
            DrawDebugLines(); // optional visuals

            // End bounce recovery
            if (bounceTimer <= 0f)
            {
                recentlyBounced = false;
            }

            return;
        }
        DetectControllerInput();
        // Normal flight update
        UpdateDeltaValues();
        UpdateDiveAngle();
        HandleFlapDetection();
        HandleGlideLogic();
        ApplyGravityIfNeeded();
        ApplyMovement();
        ApplyDrag();

        SavePreviousFramePositions();
        RecordMotion();
        DrawDebugLines();
        CapSpeed();
    }


    private Vector3 currentLeftRel, currentRightRel;
    private Vector3 leftHandDelta, rightHandDelta;
    private Quaternion leftRot, rightRot;
    private Quaternion headRot;
    private Vector3 headFwd, headPos, headDown;

    private void DetectControllerInput()
    {
        float leftGripValue = leftGrip != null ? leftGrip.action.ReadValue<float>() : 0f;
        float rightGripValue = rightGrip != null ? rightGrip.action.ReadValue<float>() : 0f;

        bool leftHeld = leftGripValue > 0.5f;
        bool rightHeld = rightGripValue > 0.5f;

        isHovering = leftHeld && rightHeld;

        if (isHovering)
        {
            Debug.Log("🕊️ Hover Mode Activated!");
        }

        DetectSnapTurn();
    }

    private void DetectSnapTurn()
    { 
        if (rightStickAction != null)
        {
            Vector2 rightStick = rightStickAction.action.ReadValue<Vector2>();
            if (canSnapTurn)
            {
                if (rightStick.x > turnThreshold)
                {
                    rigRoot.Rotate(Vector3.up, snapAngle);
                    turnCooldownTimer = turnCooldown;
                }
                else if (rightStick.x < -turnThreshold)
                {
                    rigRoot.Rotate(Vector3.up, -snapAngle);
                    turnCooldownTimer = turnCooldown;
                }
            }
        }

        if (turnCooldownTimer > 0f)
        {
            turnCooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateDeltaValues()
    {
        currentLeftRel = leftHand.position;
        currentRightRel = rightHand.position;
        leftHandDelta = (currentLeftRel - prevLeftPos) / Time.deltaTime;
        rightHandDelta = (currentRightRel - prevRightPos) / Time.deltaTime;

        headPos = head.position;
        headFwd = head.forward;
        headDown = -head.up;
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
        // ✅ Ensure both hand objects are assigned and active
        if (leftHand == null || rightHand == null) return;
        if (!leftHand.gameObject.activeInHierarchy || !rightHand.gameObject.activeInHierarchy)
        {
            isGliding = false;
            return;
        }
        float leftDown = -leftVelocity.SmoothedVelocity.y;
        float rightDown = -rightVelocity.SmoothedVelocity.y;

        float avgDownSpeed = (leftDown + rightDown) / 2f * 0.5f;
        float minFlapThreshold = 0.5f;
        float maxFlapVelocity = 10f;
        bool isMovingDown = avgDownSpeed > minFlapThreshold && avgDownSpeed < maxFlapVelocity;

        // Only flap if player just started moving down this frame
        bool justStartedMovingDown = isMovingDown && !wasMovingDownLastFrame;

        bool enoughTimePassed = Time.time - lastFlapTime >= 0.2f;
        float flapMagnitude = 2.0f;
        float speed = velocity.magnitude;
        float flapStrengthMultiplier = Mathf.Lerp(1f, 4f, Mathf.InverseLerp(30f, maxDiveSpeed, speed));

        // Super weak flaps when hovering
        if (isHovering)
        {
            flapStrengthMultiplier *= 0.2f; // Very weak flaps
        }

        // Calculate flap every 0.2 seconds
        if (justStartedMovingDown && enoughTimePassed)
        {
            velocity += flapStrengthMultiplier * FlightPhysics.CalculateFlapVelocity(
                head.forward,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );
            glideTime = 0f;
            lastFlapTime = Time.time;
            OnFlap?.Invoke();
        }

        wasMovingDownLastFrame = isMovingDown;
    }
    private void HandleGlideLogic()
    {
        // ✅ Ensure both hand objects are assigned and active
        if (leftHand == null || rightHand == null) return;
        if (!leftHand.gameObject.activeInHierarchy || !rightHand.gameObject.activeInHierarchy)
        {
            isGliding = false;
            return;
        }
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
            ref diveAngle,
            recentlyBounced,
            bounceTimer
        );

        if (isHovering)
        {
            float currentSpeed = velocity.magnitude;
            float targetSpeed = Mathf.Min(currentSpeed, 3f); // never raise speed
            float smoothedSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 4f); // adjust 4f for faster/slower smoothing

            velocity = velocity.normalized * smoothedSpeed;
        }
    }
    private void ApplyGravityIfNeeded()
    {
        if (isGliding)
        {
            // Slower descent when fast
            float speedFactor = Mathf.InverseLerp(0f, maxDiveSpeed, velocity.magnitude);
            float gravityScale = Mathf.Lerp(1.0f, 0.4f, speedFactor);

            velocity += Vector3.down * gravity * gravityScale * Time.deltaTime;

            // 🌀 Optional: smooth lift when fast
            ApplyAirPocketEffect();
        }
        else
        {
            float blendSpeed = (recentlyBounced && bounceTimer > 0f) ? 0.2f : 1.5f;
            Vector3 blendedDir = Vector3.Slerp(velocity.normalized, headFwd.normalized, Time.deltaTime * 0.2f);
            velocity = blendedDir * velocity.magnitude;
            // velocity += Vector3.down * (gravity / 3) * Time.deltaTime * 1.5f;
        }
    }

    private void ApplyAirPocketEffect()
    {
        float speedFactor = Mathf.InverseLerp(1f, maxDiveSpeed, velocity.magnitude);
        float liftBonus = Mathf.Lerp(0f, 3f, speedFactor);
        velocity += Vector3.up * liftBonus * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        transform.position += velocity * Time.deltaTime;
    }

    private void ApplyDrag()
    {
        // Higher speed → slower drag
        float speedFactor = Mathf.InverseLerp(0f, maxDiveSpeed, velocity.magnitude);
        float dragStrength = Mathf.Lerp(0.995f, 0.99f, speedFactor); // stronger flaps decay slower
        velocity *= dragStrength;
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
                10f, velocity
            );
        }
    }
    private void DrawDebugLines()
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
    private void CheckSurfaceImpact()
    {
        // Bounce check
        if (recentlyBounced)
        {
            bounceTimer -= Time.deltaTime;
            if (bounceTimer <= 0f)
            {
                recentlyBounced = false;
            }
        }

        Vector3 rayOrigin = head.position;
        float rayLength = 0.5f;

        // Forward ray
        Debug.DrawRay(rayOrigin, headFwd * rayLength, Color.cyan, 0f, false);
        Debug.DrawRay(rayOrigin, headDown * rayLength, Color.cyan, 0f, false);

        RaycastHit hit;
        bool forwardHit = Physics.Raycast(rayOrigin, headFwd, out RaycastHit forward, rayLength);
        bool downwardsHit = Physics.Raycast(rayOrigin, headDown, out RaycastHit downward, rayLength);
        hit = forwardHit ? forward : downward;

        if (forwardHit || downwardsHit)
        {
            Debug.Log("Hit: " + hit.collider.name);

            Vector3 impactNormal = hit.normal;
            float speed = velocity.magnitude;

            float approachDot = Vector3.Dot(velocity.normalized, -impactNormal);
            if (approachDot > 0.5f)
            {
                Vector3 bounce = impactNormal * speed * 2f;
                velocity = bounce;

                Debug.DrawRay(hit.point, bounce, Color.green, 1f);

                // Trigger slow blending
                recentlyBounced = true;
                bounceTimer = bounceDuration;
            }
        }
    }

}
