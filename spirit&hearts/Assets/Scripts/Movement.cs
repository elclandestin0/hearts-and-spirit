using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;

// TO-DO: We need to divide this script into multiple scripts.
// Non-movement, glide movement and diving and everything else here.
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
    [SerializeField] private float glideStrength = 4.0f;
    [SerializeField] private float diveAcceleratorSmoothness = 2.5f;
    [SerializeField] private float sphereRadius = 2.5f;
    [SerializeField] private float sphereCastDistance = 1.0f;
    [SerializeField] private LayerMask impactLayer;
    public float diveAngle = 0f;
    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;

    // 🔒 Script-controlled flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1f;
    private readonly float maxSpeed = 30f;
    private readonly float maxDiveSpeed = 200f;
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
    public bool isHovering = false;

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

    // Dive->Lift 
    private bool wasDiving = false;
    private float lastDiveEndTime = -10f;
    private float lastDiveTime = -1f;
    private float postDiveLiftBoostDuration = 0f;
    private float diveStartTime = -1f;
    private float diveEndTime = -1f;
    private float lastRecordedDiveSpeed = 0f;
    private Vector3 lastDiveForward = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private AudioSource flapAudioSource;
    [SerializeField] private AudioSource diveAudioSource;
    [SerializeField] private AudioSource bounceAudioSource;
    [SerializeField] private AudioClip flapClip;
    [SerializeField] private AudioClip bounceClip;
    [SerializeField] private float targetVolumeDive;
    [SerializeField] private float targetVolumeGlide;

    [Header("Speed Boost")]
    // Speed Boost
    private bool isSpeedBoosted = false;
    private float speedBoostStartTime = -10f;
    private float speedBoostDuration = 3f;
    private float speedBoostFadeDuration = 2f;
    private float speedBoostMagnitude = 1.0f;
    private Vector3 speedBoostDirection = Vector3.zero;
    private float boostDecayStartTime = -1f;
    private bool wasBoostedRecently = false;
    [SerializeField] private float boostDecayDuration = 2.0f;

    // Wind stuff
    private Vector3 lastKnownWindDir = Vector3.forward;
    private bool wasInWindZoneLastFrame = false;
    private float lastWindExitTime = -10f;
    private float windExitBlendDuration = 2.5f;
    // Hover speed
    public float maxHoverSpeed = 10.0f;

    // Wind zones
    private SplineWindZone[] zones;

    // State trackers for audio playback
    [SerializeField] private DovinaAudioManager dovinaAudioManager;
    private bool wasHovering = false;
    private bool hasPlayedHoverTransition = false;
    private bool wasGliding = false;
    private bool hasPlayedGlideTransition = false;

    void Start()
    {
        // Save initial world-space hand positions for motion delta
        head = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>();
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
        zones = FindObjectsOfType<SplineWindZone>();
    }

    void Update()
    {
        foreach (var zone in zones)
        {
            velocity += zone.GetWindForceAtPosition(transform.position) * Time.deltaTime;
        }

        CheckSurfaceImpact();
        if (inputLockedDuringBounce)
        {
            bounceTimer -= Time.deltaTime;

            velocity *= 0.98f;

            ApplyMovement();
            ApplyDrag();
            DrawDebugLines();
            if (bounceTimer <= 0f)
            {
                recentlyBounced = false;
            }
            return;
        }

        // Reactivate later
        // DetectControllerInput();

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
        UpdateSpeedBoost();
        CapSpeed();
        UpdateFlightAudio();

        // Really should make a method for the below..
        if (Input.GetKey(KeyCode.N))
        {
            isHovering = true;
        }

        else
        {
            isHovering = false;
        }

        HandleStateTransition(isHovering, ref wasHovering, ref hasPlayedHoverTransition, "gp_changes/movement/toHovering", 0);
        HandleStateTransition(isGliding, ref wasGliding, ref hasPlayedGlideTransition, "gp_changes/movement/toGliding", 0);
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
            // DO stuff
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
        bool isCurrentlyDiving = diveAngle < 60f && isGliding && velocity.magnitude > 5f;

        if (isCurrentlyDiving)
        {
            // PlayDive();

            if (!wasDiving)
            {
                diveStartTime = Time.time;
                lastRecordedDiveSpeed = velocity.magnitude;
            }
        }

        // Detect transition from dive → climb
        if (wasDiving && !isCurrentlyDiving)
        {
            diveEndTime = Time.time;
            float diveDuration = diveEndTime - diveStartTime;
            float diveSpeedFactor = Mathf.InverseLerp(10f, maxDiveSpeed, velocity.magnitude);

            float easedDuration = Mathf.SmoothStep(0.6f, 1.0f, Mathf.InverseLerp(1f, 3f, diveDuration));
            float boostDurationRaw = diveDuration * easedDuration * diveSpeedFactor * 2.0f;
            postDiveLiftBoostDuration = Mathf.Clamp(boostDurationRaw, 1.5f, 7.5f);

            lastDiveEndTime = Time.time;
            glideTime = 0f;

            lastDiveForward = head.forward; // ✅ Store direction at dive exit
            // StopDive();
        }

        wasDiving = isCurrentlyDiving;
    }

    private void HandleFlapDetection()
    {
        // Priority goes to debugging first on the laptop.
        float flapMagnitude = 2.0f;
        float speed = velocity.magnitude;
        float flapStrengthMultiplier = Mathf.Lerp(1f, 4f, Mathf.InverseLerp(1f, maxDiveSpeed, speed));

        // Enough time passed
        bool enoughTimePassed = Time.time - lastFlapTime >= 0.665f;
        if (Input.GetKeyDown(KeyCode.F) && enoughTimePassed)
        {
            velocity += flapStrengthMultiplier * FlightPhysics.CalculateFlapVelocity(
                head.forward,
                flapMagnitude,
                flapStrength,
                forwardPropulsionStrength
            );
            Debug.Log("Flappity flap.");
            glideTime = 0f;
            lastFlapTime = Time.time;
            OnFlap?.Invoke();
            PlayFlap();
        }

        // ✅ Ensure both hand objects are assigned and active
        if (leftHand == null || rightHand == null || leftVelocity == null || rightVelocity == null) return;
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

        // Super weak flaps when hovering
        if (isHovering)
        {
            flapStrengthMultiplier *= 1.0f; // Very weak flaps
        }

        // Calculate flap every 0.2 seconds
        if ((Input.GetKeyDown(KeyCode.F) || justStartedMovingDown) && enoughTimePassed)
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
            PlayFlap();
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

        // if (Input.GetKey(KeyCode.M))
        // {
        //     isGliding = true;
        // }
        // else
        // {
        //     isGliding = false;
        //     return;
        // }
        Vector3 leftToHead = leftHand.position - head.position;
        Vector3 rightToHead = rightHand.position - head.position;

        float leftDot = Vector3.Dot(leftToHead, head.forward);
        float rightDot = Vector3.Dot(rightToHead, head.forward);

        bool leftBehind = leftDot < -0.2f;
        bool rightBehind = rightDot < -0.2f;

        bool isManualDivePose = leftBehind && rightBehind;

        float timeSinceDive = Time.time - lastDiveEndTime;
        velocity = FlightPhysics.CalculateGlideVelocity(
            velocity,
            headFwd,
            glideStrength,
            maxDiveSpeed,
            Time.deltaTime,
            isGliding,
            ref glideTime,
            ref diveAngle,
            recentlyBounced,
            bounceTimer,
            timeSinceDive,
            diveStartTime,
            isSpeedBoosted
        );

        // ✅ Only boost if duration is valid
        if (postDiveLiftBoostDuration > 0f && timeSinceDive < postDiveLiftBoostDuration)
        {
            float liftPercent = 1f - (timeSinceDive / postDiveLiftBoostDuration);

            // Separate raw force values for forward and upward
            float forwardBonus = Mathf.Lerp(2f, 10f, liftPercent);
            float upwardBonus = Mathf.Lerp(1f, 10f, liftPercent);

            float pitchY = head.forward.y;

            // Blended weights instead of hard if/else
            float forwardWeight = Mathf.Clamp01(5 - Mathf.InverseLerp(0.8f, 1.2f, pitchY));
            float climbWeight = Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.7f, pitchY));
            float stallWeight = Mathf.Clamp01(Mathf.InverseLerp(0.7f, 0.9f, pitchY)); // stall fade-in

            float forwardForce = forwardBonus * forwardWeight;
            float upwardForce = upwardBonus * climbWeight * (1f - stallWeight);

            Vector3 forwardBoost = lastDiveForward * forwardForce * Time.deltaTime;
            Vector3 upwardBoost = Vector3.up * upwardForce * Time.deltaTime;

            velocity += forwardBoost + upwardBoost;
        }

        // ✅ Optionally reset boost duration after use
        if (timeSinceDive > postDiveLiftBoostDuration)
        {
            postDiveLiftBoostDuration = 0f;
        }

        if (isHovering)
        {
            float currentSpeed = velocity.magnitude;
            float targetSpeed = Mathf.Min(currentSpeed, maxHoverSpeed); // never raise speed
            float smoothedSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 4f); // adjust 4f for faster/slower smoothing

            velocity = velocity.normalized * smoothedSpeed;
        }
    }
    private void ApplyGravityIfNeeded()
    {
        bool isInWindZone = false;

        foreach (var zone in zones)
        {
            Vector3 wind = zone.GetWindForceAtPosition(transform.position);
            if (wind != Vector3.zero)
            {
                isInWindZone = true;
                lastKnownWindDir = wind.normalized;
                break;
            }
        }

        if (isInWindZone)
        {
            wasInWindZoneLastFrame = true;

            // ✅ Light tangent alignment (damp lateral drift)
            Vector3 tangent = lastKnownWindDir;
            Vector3 lateral = Vector3.ProjectOnPlane(velocity, tangent);
            float lateralDampingStrength = 1.0f; // Tune this as needed
            velocity -= lateral * Time.deltaTime * lateralDampingStrength;
        }
        else if (wasInWindZoneLastFrame)
        {
            lastWindExitTime = Time.time;
            wasInWindZoneLastFrame = false;
        }

        float sinceExit = Time.time - lastWindExitTime;
        float windBlendFactor = Mathf.Clamp01(sinceExit / windExitBlendDuration);
        bool withinWindTransition = sinceExit < windExitBlendDuration;

        Vector3 glideDir = withinWindTransition
            ? Vector3.Slerp(lastKnownWindDir, headFwd.normalized, windBlendFactor)
            : headFwd.normalized;

        if (!isHovering)
        {
            Vector3 gravityDirection = Vector3.down * 0.75f + head.forward.normalized * 0.25f;
            gravityDirection.Normalize();

            velocity += gravityDirection * gravity * Time.deltaTime;
        }

        // Optional — revisit later
        // velocity = Vector3.Slerp(velocity, glideDir * velocity.magnitude, Time.deltaTime * 1.5f);
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
        float dragStrength = Mathf.Lerp(0.998f, 0.995f, speedFactor); // stronger flaps decay slower
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
        Debug.DrawLine(head.position, head.position + velocity.normalized * 50f, Color.cyan, 0f, false);
        Debug.DrawLine(head.position, head.position + headFwd * 3f * 30f, Color.red, 0f, false);

        // Gravity direction
        Vector3 gravityDirection = Vector3.down * 0.75f + head.forward.normalized * 0.25f;
        gravityDirection.Normalize();
        Debug.DrawLine(head.position, head.position + gravityDirection * 20f, Color.blue, 0f, false);
    }

    // Speed Boost
    public void ActivateSpeedBoost()
    {
        isSpeedBoosted = true;
        speedBoostStartTime = Time.time;
        speedBoostDirection = head.forward.normalized; // starting point
    }
    
    private void UpdateSpeedBoost()
    {
        if (!isSpeedBoosted) return;

        float elapsed = Time.time - speedBoostStartTime;

        // Smoothly update the boost direction toward where the head is facing
        speedBoostDirection = Vector3.Slerp(speedBoostDirection, head.forward.normalized, Time.deltaTime * 1.5f);

        if (elapsed < speedBoostDuration)
        {
            // Maintain constant speed boost in current direction
            velocity += speedBoostDirection * speedBoostMagnitude;
        }
        else if (elapsed < speedBoostDuration + speedBoostFadeDuration)
        {
            float fadeElapsed = elapsed - speedBoostDuration;
            float fadePercent = 1f - (fadeElapsed / speedBoostFadeDuration);
            float fadedSpeed = speedBoostMagnitude * fadePercent;

            velocity += speedBoostDirection * fadedSpeed;
        }

        else
        {
            isSpeedBoosted = false;
            wasBoostedRecently = true;
            boostDecayStartTime = Time.time;
        }

    }

    private void CapSpeed()
    {
        // Gliding has it's own cap speed in FlightPhysics, no need to Cap speed twice, sailor
        if (isSpeedBoosted) return;
        float speed = velocity.magnitude;

        if (speed > maxDiveSpeed)
        {
            // Gradually reduce speed toward maxDiveSpeed
            float decaySpeed = 2.5f;
            float newSpeed = Mathf.Lerp(speed, maxDiveSpeed, Time.deltaTime * decaySpeed);
            velocity = velocity.normalized * newSpeed;
        }
    }

    private void CheckSurfaceImpact()
    {
        if (recentlyBounced)
        {
            bounceTimer -= Time.deltaTime;
            if (bounceTimer <= 0f)
                recentlyBounced = false;
            return;
        }

        if (velocity.sqrMagnitude < 0.0001f) return; // Optional: remove entirely
        Vector3 origin = head.position;
        Vector3 direction = velocity.normalized;
        float dynamicDistance = Mathf.Max(0.5f, velocity.magnitude * Time.deltaTime * 2f); // dynamic cast distance

        Debug.DrawRay(origin, direction * dynamicDistance, Color.red);

        if (Physics.SphereCast(origin, sphereRadius, direction, out RaycastHit hit, dynamicDistance, impactLayer, QueryTriggerInteraction.Ignore))
        {
            // Bounce sound effect
            PlayBounce();
            float speed = velocity.magnitude;
            float approachDot = Vector3.Dot(direction, -hit.normal);

            if (approachDot > 0.2f) // more forgiving threshold
            {
                // Calculate bounce blend from 1.25 to 2.5 based on speed
                float bounceBlend = Mathf.Lerp(1.25f, 2.5f, velocity.magnitude);

                // Apply the dynamic bounce
                Vector3 bounce = hit.normal * speed * bounceBlend;
                velocity += bounce;
                
                recentlyBounced = true;
                bounceTimer = bounceDuration;

                Debug.DrawLine(origin, hit.point, Color.green, 1f);

                // Dovina audio
                dovinaAudioManager.PlayPriority("gp_changes/bouncing", 0, 0, 999);
            }
        }
    }

    private void PlayFlap()
    {
        if (flapAudioSource == null || flapClip == null) return;

        flapAudioSource.Stop(); // ensure it's reset
        flapAudioSource.clip = flapClip;
        flapAudioSource.time = 0.2f;
        flapAudioSource.Play();

        StartCoroutine(StopAudioAfter(flapAudioSource, 0.665f)); // 0.85 - 0.185
    }

    private void PlayBounce()
    {
        if (bounceAudioSource == null || bounceClip == null) return;

        bounceAudioSource.Stop(); // ensure it's reset
        bounceAudioSource.clip = bounceClip;
        bounceAudioSource.time = 0.075f;
        bounceAudioSource.Play();
    }

    private IEnumerator StopAudioAfter(AudioSource source, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (source.isPlaying)
        {
            source.Stop();
        }
    }

    private void UpdateFlightAudio()
    {
        if (diveAudioSource == null)
            return;

        float speed = velocity.magnitude;
        targetVolumeGlide = Mathf.InverseLerp(0f, maxDiveSpeed, speed);
        diveAudioSource.volume = targetVolumeGlide * 0.65f;

        if (!diveAudioSource.isPlaying)
            diveAudioSource.Play();
    }

    // Gizmos
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 origin = head.position;
        Vector3 direction = velocity.normalized;
        float dynamicDistance = Mathf.Max(0.5f, velocity.magnitude * Time.deltaTime * 2f);
        Vector3 endPoint = origin + direction * dynamicDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, sphereRadius);      // starting sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(endPoint, sphereRadius);    // end of cast sphere
    }

    // Movement Audio
    private void HandleStateTransition(bool currentState, ref bool previousState, ref bool hasPlayedFlag, string category, int clipIndex)
    {
        // Special case: continuously check for hovering condition
        if (category == "gp_changes/movement/toHovering")
        {
            if (currentState && !hasPlayedFlag)
            {
                if (velocity.magnitude <= 30f)
                {
                    dovinaAudioManager.PlayPriority(category, 1, 0, 999);
                    hasPlayedFlag = true;
                }
            }
            else if (!currentState)
            {
                hasPlayedFlag = false;
            }
        }
        else
        {
            // Regular state-based triggering
            if (currentState && !previousState && !hasPlayedFlag)
            {
                float randomThreshold = Mathf.Lerp(0.15f, 0.25f, Random.value);
                if (Random.value <= randomThreshold)
                {
                    dovinaAudioManager.PlayPriority(category, 1, 0, 999);
                    hasPlayedFlag = true;
                }
            }
            else if (!currentState && previousState)
            {
                hasPlayedFlag = false;
            }
        }
        previousState = currentState;
    }
}