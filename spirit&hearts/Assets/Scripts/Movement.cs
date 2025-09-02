using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

// TO-DO: Split into: Input, Glide, Dive, Wind, Audio.
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
    public Transform rigRoot; // usually XROrigin or a parent of the camera

    [Header("Physics Controls")]
    [SerializeField] private float gravity = 9.8f; // m/s²
    [SerializeField] private float glideStrength = 4.0f;
    [SerializeField] private float diveAcceleratorSmoothness = 2.5f;
    [SerializeField] private float sphereRadius = 2.5f;
    [SerializeField] private float sphereCastDistance = 1.0f;
    [SerializeField] private LayerMask impactLayer;
    public float diveAngle = 0f;

    [Header("Recorder")]
    [SerializeField] private GhostFlightRecorder recorder;

    // Flight values
    private readonly float flapStrength = 1f;
    private readonly float forwardPropulsionStrength = 1f;
    private readonly float maxSpeed = 30f;
    private readonly float maxDiveSpeed = 200f;

    private float snapAngle = 45f;
    private float turnThreshold = 0.8f;
    private float turnCooldown = 0.5f;
    private float turnCooldownTimer = 0f;
    private bool canSnapTurn => turnCooldownTimer <= 0f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 prevLeftPos, prevRightPos;

    [Header("Debug variables")]
    public bool isGliding = false;
    public bool isFlapping = false;
    public bool isHovering = false;

    // Public refs
    public Vector3 CurrentVelocity => velocity;
    public float MaxSpeed => maxDiveSpeed;

    // Flap
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
    private bool isSpeedBoosted = false;
    private float speedBoostStartTime = -10f;
    private float speedBoostDuration = 3f;
    private float speedBoostFadeDuration = 2f;
    private float speedBoostMagnitude = 1.0f;
    private Vector3 speedBoostDirection = Vector3.zero;
    private float boostDecayStartTime = -1f;
    private bool wasBoostedRecently = false;
    [SerializeField] private float boostDecayDuration = 2.0f;

    // Wind
    private Vector3 lastKnownWindDir = Vector3.forward;
    private bool wasInWindZoneLastFrame = false;
    private float lastWindExitTime = -10f;
    private float windExitBlendDuration = 2.5f;

    // Hover
    public float maxHoverSpeed = 10.0f;
    [SerializeField] private float minHoverDirSpeed = 0.25f;

    // Wind zones
    private SplineWindZone[] zones;

    // Audio state trackers
    [SerializeField] private DovinaAudioManager dovinaAudioManager;
    private bool wasHovering = false;
    private bool hasPlayedHoverTransition = false;
    private bool wasGliding = false;
    private bool hasPlayedGlideTransition = false;

    // --- Glide/Flap cohesion ---
    [Header("Glide / Flap Cohesion")]
    [SerializeField] private float glideHoldDuration = 0.35f;   // keep gliding after a flap
    [SerializeField] private float strokeHoldDuration = 0.20f;  // grace while arms travel up or down
    [SerializeField] private float strokeSpeedThreshold = 0.8f; // m/s Y on both hands
    private float glideHoldUntil = -1f;

    [SerializeField] private float minHandSpreadEnter = 40f;
    [SerializeField] private float minHandSpreadStay  = 20f;
    [SerializeField] private float spreadSmoothing    = 10f;
    private float smoothedHandSpread = 0f;
    private bool wasGlidingLastFrame = false;

    // Internals
    private float glideTime = 0f;
    private Vector3 currentLeftRel, currentRightRel;
    private Vector3 leftHandDelta, rightHandDelta;
    private Quaternion leftRot, rightRot;
    private Quaternion headRot;
    private Vector3 headFwd, headPos, headDown;

    // Tutorial variables to control movement
    // --- ADDED (top of class fields) ---
    private IMovementPolicyProvider policyProvider;
    private MovementEventHub eventHub;
    private MovementPolicy Policy => policyProvider != null
        ? policyProvider.CurrentPolicy
        : new MovementPolicy { Allowed = (MovementAbility)(-1), GravityEnabled = true };
    private bool Allowed(MovementAbility a) => (Policy.Allowed & a) != 0;

    void Awake()
    {
        policyProvider = GetComponentInParent<IMovementPolicyProvider>();
        eventHub = GetComponent<MovementEventHub>() ?? gameObject.AddComponent<MovementEventHub>();
    }

    void Start()
    {
        head = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>();
        prevLeftPos = leftHand.position;
        prevRightPos = rightHand.position;
        zones = FindObjectsOfType<SplineWindZone>();
    }

    void Update()
    {
        // Still read inputs/poses; but if translate is locked, zero out motion and skip forces.
        bool allowTranslate = Allowed(MovementAbility.Translate);

        if (!allowTranslate)
        {
            velocity = Vector3.zero; // freeze translation
            // Emit "end" events if we were in active states
            if (wasGliding)  { eventHub.RaiseGlideEnd();  wasGliding = false; }
            if (wasHovering) { eventHub.RaiseHoverEnd();  wasHovering = false; }
            if (wasDiving)   { eventHub.RaiseDiveEnd();   wasDiving  = false; }
            // Skip physics & movement when locked:
            DrawDebugLines();
            UpdateFlightAudio();
            return;
        }

        ApplyWindForces();
        CheckSurfaceImpact();
        DetectControllerInput();

        UpdateDeltaValues();
        UpdateDiveAngle();
        HandleFlapDetection();
        HandleGlideLogic();
        HandleHoverLogic();
        ApplyGravityIfNeeded();
        ApplyMovement();
        ApplyDrag();

        SavePreviousFramePositions();
        RecordMotion();
        DrawDebugLines();
        UpdateSpeedBoost();
        CapSpeed();
        UpdateFlightAudio();

        HandleStateTransition(isHovering, ref wasHovering, ref hasPlayedHoverTransition, "gp_changes/movement/toHovering", 0);
        HandleStateTransition(isGliding, ref wasGliding, ref hasPlayedGlideTransition, "gp_changes/movement/toGliding", 0);
    }

    private void DetectControllerInput()
    {
        float leftGripValue  = leftGrip  != null ? leftGrip.action.ReadValue<float>()  : 0f;
        float rightGripValue = rightGrip != null ? rightGrip.action.ReadValue<float>() : 0f;

        bool leftHeld  = leftGripValue  > 0.5f;
        bool rightHeld = rightGripValue > 0.5f;

        isHovering = Allowed(MovementAbility.Hover) && leftHeld && rightHeld;
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
            turnCooldownTimer -= Time.deltaTime;
    }

    private void UpdateDeltaValues()
    {
        currentLeftRel  = leftHand.position;
        currentRightRel = rightHand.position;
        leftHandDelta   = (currentLeftRel  - prevLeftPos)  / Time.deltaTime;
        rightHandDelta  = (currentRightRel - prevRightPos) / Time.deltaTime;

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
            if (!wasDiving)
            {
                diveStartTime = Time.time;
                lastRecordedDiveSpeed = velocity.magnitude;
            }
        }

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

            lastDiveForward = head.forward; // direction at dive exit
        }

        wasDiving = isCurrentlyDiving;
    }

    private void HandleFlapDetection()
    {
        if (!Allowed(MovementAbility.Flap)) { isFlapping = false; return; }
        // Debug flap magnitude
        float flapMagnitude = 2.0f;
        float speed = velocity.magnitude;
        float flapStrengthMultiplier = Mathf.Lerp(1f, 4f, Mathf.InverseLerp(1f, maxDiveSpeed, speed));

        // if (leftHand == null || rightHand == null || leftVelocity == null || rightVelocity == null) return;
        // if (!leftHand.gameObject.activeInHierarchy || !rightHand.gameObject.activeInHierarchy)
        // {
        //     isFlapping = false;
        //     return;
        // }

        // ↓ movement (downward stroke) detection
        float leftDown = -leftVelocity.SmoothedVelocity.y;
        float rightDown = -rightVelocity.SmoothedVelocity.y;

        float avgDownSpeed = (leftDown + rightDown) * 0.5f * 0.5f;
        float minFlapThreshold = 0.5f;
        float maxFlapVelocity = 10f;
        bool isMovingDown = avgDownSpeed > minFlapThreshold && avgDownSpeed < maxFlapVelocity;

        bool justStartedMovingDown = isMovingDown && !wasMovingDownLastFrame;
        bool enoughTimePassed = Time.time - lastFlapTime >= 0.665f;

        // Fire flap
        if ((Input.GetKeyDown(KeyCode.F) || justStartedMovingDown) && enoughTimePassed)
        {
            velocity += flapStrengthMultiplier * FlightPhysics.CalculateFlapVelocity(
                head.forward, flapMagnitude, flapStrength, forwardPropulsionStrength
            );
            glideTime = 0f;
            lastFlapTime = Time.time;
            OnFlap?.Invoke();
            PlayFlap();

            eventHub.RaiseFlap();

            // Latch gliding through the flap
            glideHoldUntil = Mathf.Max(glideHoldUntil, Time.time + glideHoldDuration);
        }

        // Stroke latch (arms moving strongly up OR down keeps glide alive briefly)
        float ly = leftVelocity.SmoothedVelocity.y;
        float ry = rightVelocity.SmoothedVelocity.y;
        bool goingUpFast   =  ly >  strokeSpeedThreshold && ry >  strokeSpeedThreshold;
        bool goingDownFast =  ly < -strokeSpeedThreshold && ry < -strokeSpeedThreshold;
        bool strokeActive  = goingUpFast || goingDownFast;

        if ((isGliding || wasGlidingLastFrame) && strokeActive)
        {
            glideHoldUntil = Mathf.Max(glideHoldUntil, Time.time + strokeHoldDuration);
        }

        wasMovingDownLastFrame = isMovingDown;
    }

    private void HandleGlideLogic()
    {
        Debug.Log("Attempting to glide.");
        if (!Allowed(MovementAbility.Glide)) { 
            if (isGliding || wasGlidingLastFrame) { eventHub.RaiseGlideEnd(); }
            isGliding = false; wasGlidingLastFrame = false; 
            return;
        }
        Debug.Log("Can glide.");
        
        // if (leftHand == null || rightHand == null) return;
        // if (!leftHand.gameObject.activeInHierarchy || !rightHand.gameObject.activeInHierarchy)
        // {
        //     isGliding = false;
        //     return;
        // }

        // --- Hand spread calculation (projected to ignore vertical "squeeze") ---
        Vector3 lPos = currentLeftRel;
        Vector3 rPos = currentRightRel;
        Vector3 wingVec = Vector3.ProjectOnPlane(rPos - lPos, head.up);
        float rawSpread = wingVec.magnitude;

        // Smooth short dips
        smoothedHandSpread = Mathf.Lerp(smoothedHandSpread <= 0 ? rawSpread : smoothedHandSpread,
                                        rawSpread,
                                        Time.deltaTime * spreadSmoothing);

        // Hysteresis: enter vs stay
        float threshold = isGliding ? minHandSpreadStay : minHandSpreadEnter;
        bool wingsOutstretched = smoothedHandSpread > threshold;

        // Grace window after flap / during stroke
        if (Time.time < glideHoldUntil)
            wingsOutstretched = true;

        isGliding = wingsOutstretched;
        Debug.Log("Attempting to glide.");
        if (Input.GetKey(KeyCode.M))
        {
            Debug.Log("Gliding is now true.");
            isGliding = true;
        }

        // If not gliding, don't apply glide forces
        if (!isGliding)
        {
            eventHub.RaiseGlideEnd();
            wasGlidingLastFrame = false;
            return;
        }

        else
        {
            eventHub.RaiseGlideTick(Time.deltaTime); // hub will auto-emit start on first tick
        }

        // Steering direction: in hover keep travel/wind heading; otherwise head
        bool hoverActive = isHovering && isGliding;

        Vector3 hoverDir;
        if (velocity.sqrMagnitude > (minHoverDirSpeed * minHoverDirSpeed))
            hoverDir = velocity.normalized;
        else if (lastKnownWindDir != Vector3.zero)
            hoverDir = lastKnownWindDir;
        else
            hoverDir = headFwd.normalized;

        Vector3 steerDir = hoverActive ? hoverDir : headFwd.normalized;

        float timeSinceDive = Time.time - lastDiveEndTime;

        velocity = FlightPhysics.CalculateGlideVelocity(
            velocity,
            steerDir,               // use chosen steering direction
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

        // Post-dive lift boost
        if (postDiveLiftBoostDuration > 0f && timeSinceDive < postDiveLiftBoostDuration)
        {
            float liftPercent = 1f - (timeSinceDive / postDiveLiftBoostDuration);

            float forwardBonus = Mathf.Lerp(2f, 10f, liftPercent);
            float upwardBonus  = Mathf.Lerp(1f, 10f, liftPercent);

            float pitchY = head.forward.y;

            float forwardWeight = Mathf.Clamp01(5 - Mathf.InverseLerp(0.8f, 1.2f, pitchY));
            float climbWeight   = Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.7f, pitchY));
            float stallWeight   = Mathf.Clamp01(Mathf.InverseLerp(0.7f, 0.9f, pitchY));

            float forwardForce = forwardBonus * forwardWeight;
            float upwardForce  = upwardBonus  * climbWeight * (1f - stallWeight);

            Vector3 forwardBoost = lastDiveForward * forwardForce * Time.deltaTime;
            Vector3 upwardBoost  = Vector3.up    * upwardForce  * Time.deltaTime;

            velocity += forwardBoost + upwardBoost;
        }

        if (timeSinceDive > postDiveLiftBoostDuration)
            postDiveLiftBoostDuration = 0f;

        wasGlidingLastFrame = true;
    }

    private void HandleHoverLogic()
    {
        if (!Allowed(MovementAbility.Hover)) {
            if (isHovering) { eventHub.RaiseHoverEnd(); }
            isHovering = false; 
            return;
        }

        if (isHovering && isGliding)
        {
            eventHub.RaiseHoverTick(Time.deltaTime);
            float currentSpeed  = velocity.magnitude;
            float targetSpeed   = Mathf.Min(currentSpeed, maxHoverSpeed);
            float smoothedSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 4f);
            velocity = velocity.normalized * smoothedSpeed;
        }
        else if (!isHovering && wasHovering)
        {
            eventHub.RaiseHoverEnd();
        }
    }

    private void ApplyGravityIfNeeded()
    {
        if (!Policy.GravityEnabled)
        {
            // If you fire a gravity tick event elsewhere, make sure not to raise it here.
            // Also keep your wind exit bookkeeping if you need it.
            return;
        }
        bool appliedGravityThisFrame = false;
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

            // Damp lateral drift orthogonal to wind
            Vector3 tangent = lastKnownWindDir;
            Vector3 lateral = Vector3.ProjectOnPlane(velocity, tangent);
            float lateralDampingStrength = 1.0f;
            velocity -= lateral * Time.deltaTime * lateralDampingStrength;
        }
        else if (wasInWindZoneLastFrame)
        {
            lastWindExitTime = Time.time;
            wasInWindZoneLastFrame = false;
        }

        if (!isHovering)
        {
            Vector3 gravityDirection = Vector3.down * 0.75f + head.forward.normalized * 0.25f;
            gravityDirection.Normalize();
            velocity += gravityDirection * gravity * Time.deltaTime;

            appliedGravityThisFrame = true;
        }

        if (appliedGravityThisFrame)
            eventHub?.RaiseGravityTick(Time.deltaTime);
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

        Vector3 gravityDirection = Vector3.down * 0.75f + head.forward.normalized * 0.25f;
        gravityDirection.Normalize();
        Debug.DrawLine(head.position, head.position + gravityDirection * 20f, Color.blue, 0f, false);
    }

    // Speed Boost
    public void ActivateSpeedBoost()
    {
        isSpeedBoosted = true;
        speedBoostStartTime = Time.time;
        speedBoostDirection = head.forward.normalized;
    }

    private void UpdateSpeedBoost()
    {
        if (!isSpeedBoosted) return;

        float elapsed = Time.time - speedBoostStartTime;

        // Smoothly update the boost direction toward where the head is facing
        speedBoostDirection = Vector3.Slerp(speedBoostDirection, head.forward.normalized, Time.deltaTime * 1.5f);

        if (elapsed < speedBoostDuration)
        {
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
        if (isSpeedBoosted) return;

        float speed = velocity.magnitude;
        if (speed > maxDiveSpeed)
        {
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

        if (velocity.sqrMagnitude < 0.0001f) return;

        Vector3 origin = head.position;
        Vector3 direction = velocity.normalized;
        float dynamicDistance = Mathf.Max(0.5f, velocity.magnitude * Time.deltaTime * 2f);

        Debug.DrawRay(origin, direction * dynamicDistance, Color.red);

        if (Physics.SphereCast(origin, sphereRadius, direction, out RaycastHit hit, dynamicDistance, impactLayer, QueryTriggerInteraction.Ignore))
        {
            PlayBounce();
            float speed = velocity.magnitude;
            float approachDot = Vector3.Dot(direction, -hit.normal);

            if (approachDot > 0.2f)
            {
                float bounceBlend = Mathf.Lerp(1.25f, 2.5f, velocity.magnitude);
                Vector3 bounce = hit.normal * speed * bounceBlend;
                velocity += bounce;

                recentlyBounced = true;
                bounceTimer = bounceDuration;

                Debug.DrawLine(origin, hit.point, Color.green, 1f);

                dovinaAudioManager.PlayPriority("gp_changes/bouncing", 0, 0, 999);
            }
        }

        if (inputLockedDuringBounce)
        {
            bounceTimer -= Time.deltaTime;
            velocity *= 0.98f;

            ApplyMovement();
            ApplyDrag();
            DrawDebugLines();

            if (bounceTimer <= 0f)
                recentlyBounced = false;

            return;
        }
    }

    private void PlayFlap()
    {
        if (flapAudioSource == null || flapClip == null) return;

        flapAudioSource.Stop();
        flapAudioSource.clip = flapClip;
        flapAudioSource.time = 0.2f;
        flapAudioSource.Play();

        StartCoroutine(StopAudioAfter(flapAudioSource, 0.665f)); // 0.85 - 0.185
    }

    private void PlayBounce()
    {
        if (bounceAudioSource == null || bounceClip == null) return;

        bounceAudioSource.Stop();
        bounceAudioSource.clip = bounceClip;
        bounceAudioSource.time = 0.075f;
        bounceAudioSource.Play();
    }

    private IEnumerator StopAudioAfter(AudioSource source, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (source.isPlaying)
            source.Stop();
    }

    private void UpdateFlightAudio()
    {
        if (diveAudioSource == null) return;

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
        Gizmos.DrawWireSphere(origin, sphereRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(endPoint, sphereRadius);
    }

    private void ApplyWindForces()
    {
        foreach (var zone in zones)
        {
            velocity += zone.GetWindForceAtPosition(transform.position) * Time.deltaTime;
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled) velocity = Vector3.zero;
    }

    // Movement Audio
    private void HandleStateTransition(bool currentState, ref bool previousState, ref bool hasPlayedFlag, string category, int clipIndex)
    {
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
