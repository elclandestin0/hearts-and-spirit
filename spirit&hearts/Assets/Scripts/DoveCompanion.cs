using System.Collections;
using UnityEngine;

public class DoveCompanion : MonoBehaviour
{
    // == Player variables ==
    [Header("Player Setup")]
    public Transform player;
    public Movement movementScript;

    // == General Flight Settings ==
    [Header("Flight Settings")]
    public float rotationSmoothing = 1f;
    public float currentSpeed = 0.0f;

    [Header("Hovering")]
    public float hoverAmplitude = 0.1f;
    public float hoverFrequency = 1f;
    public float moveDuration = 2f;
    public float waitDuration = 5f;
    public float wanderDistance;
    private Vector3 baseHoverPos;
    private float hoverTimer;
    private bool isHovering = false;
    private Vector3 currentHoverOffset;
    private Coroutine hoverRoutine;
    private bool isHoverIdle = false;

    [Header("Orbit Mode")]
    public float orbitRadius = 5f;
    public float baseOrbitSpeed = 30f;
    public float verticalBobAmplitude = 0.3f;
    public float verticalBobFrequency = 2f;
    public float orbitChillSpeedThreshold = 5f;
    public float currentOrbitRadius;
    public float targetOrbitRadius;
    public float orbitRadiusChangeTimer;
    public float orbitRadiusChangeInterval = 3f;
    private float orbitAngle = 0f;
    private float maxOrbitAngle = 50f;
    private int orbitDirection = 1;
    private bool transitioningToOrbit = false;
    private Vector3 smoothedOrbitOffset = Vector3.zero;
    private Vector3 moveVelocity = Vector3.zero;

    [Header("Follow Mode")]
    public float followDistance = 2f;
    public float followSideOffset = 0.5f;
    public float followVerticalOffset = 0.5f;

    [Header("Obstacle Avoidance")]
    public float dangerRange = 100f;
    public float escapeDistance = 2f;
    public LayerMask obstacleLayers;
    public float escapeDuration = 0.5f;
    [Header("Animation")]
    public Animator animator;

    // Internal variables
    private enum DoveState { Orbiting, Hovering, Following, Escaping, Navigating }
    private DoveState currentState = DoveState.Orbiting;
    private Vector3 escapeTarget;
    private bool isEscaping = false;
    private int flapQueue = 0;
    private bool isFlappingLoop = false;
    private Vector3 liveTargetPosition;
    private Vector3 doveVelocity = Vector3.zero;
    private float lastKnownSpeed;
    private DovinaAudioManager dovinaAudioManager;
    // Chatter
    private bool hasPlayedFastChatter = false;
    private bool hasPlayedSlowChatter = false;

#region Loop
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        dovinaAudioManager = GetComponent<DovinaAudioManager>();
        currentOrbitRadius = orbitRadius;
        targetOrbitRadius = orbitRadius;
        movementScript.OnFlap += HandleFlap;
    }

    void Update()
    {
        Hover();
        TryPlaySpeedChatter();
        switch (currentState)
        {
            case DoveState.Orbiting:
                Orbit();
                break;
            case DoveState.Hovering:
                break;
            case DoveState.Following:
                Follow();
                break;
            case DoveState.Escaping:
                Avoid();
                break;
        }

        animator.SetBool("Gliding", movementScript.isGliding);


        bool shouldMoveToTarget = currentState == DoveState.Orbiting;

        if (shouldMoveToTarget)
        {
            float proximityThreshold = 0.1f;
            float playerSpeed = movementScript.CurrentVelocity.magnitude;
            float distance = Vector3.Distance(transform.position, liveTargetPosition);
            Vector3 moveDir = (liveTargetPosition - transform.position).normalized;

            Vector3 toDove = (transform.position - movementScript.head.position).normalized;
            Vector3 playerForward = movementScript.head.forward;
            float facingDot = Vector3.Dot(playerForward, toDove);
            float attentionFactor = Mathf.InverseLerp(1f, -1f, facingDot);

            float distanceRatio = Mathf.Clamp01(distance / wanderDistance);
            float baseSpeed = Mathf.Lerp(playerSpeed / 1.1f, playerSpeed / 0.9f, distanceRatio);
            float speed = Mathf.Lerp(baseSpeed * 0.75f, baseSpeed * 1.75f, attentionFactor);

            float maxStep = distance / Time.deltaTime;
            speed = Mathf.Min(speed, maxStep);
            lastKnownSpeed = speed;

            transform.position += moveDir * speed * Time.deltaTime;

            // SmoothDamp only when needed
            float smoothingThreshold = 0.12f;
            if (distance > smoothingThreshold)
            {
                transform.position = Vector3.SmoothDamp(transform.position, liveTargetPosition, ref moveVelocity, 2f);
            }
        }


        ObstacleCheck();
    }
#endregion
#region Orbit
    private void Orbit()
    {
        float playerSpeed = movementScript.CurrentVelocity.magnitude;
        float orbitSpeed = baseOrbitSpeed * playerSpeed * 0.5f;
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;
        orbitAngle = Mathf.Clamp(orbitAngle, -maxOrbitAngle, maxOrbitAngle);

        if (orbitAngle >= maxOrbitAngle || orbitAngle <= -maxOrbitAngle)
        {
            orbitDirection *= -1;
        }

        Vector3 right = movementScript.head.right;
        Vector3 forward = movementScript.head.forward;

        orbitRadiusChangeTimer += Time.deltaTime;
        if (orbitRadiusChangeTimer >= orbitRadiusChangeInterval)
        {
            targetOrbitRadius = Random.Range(orbitRadius - 5f, orbitRadius);
            orbitRadiusChangeTimer = 0f;
        }

        currentOrbitRadius = Mathf.Lerp(currentOrbitRadius, targetOrbitRadius, Time.deltaTime * 1f);

        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 targetOrbitOffset = (right * Mathf.Sin(radians) + forward * Mathf.Cos(radians)) * currentOrbitRadius;
        targetOrbitOffset.y += Mathf.Sin(Time.time * verticalBobFrequency) * verticalBobAmplitude;

        smoothedOrbitOffset = Vector3.Lerp(smoothedOrbitOffset, targetOrbitOffset, Time.deltaTime * 2f); // adjust smoothing factor here

        Vector3 orbitCenter = movementScript.head.position;
        Vector3 targetPos = orbitCenter + smoothedOrbitOffset;

        liveTargetPosition = targetPos;

        Vector3 lookPoint = movementScript.head.position + movementScript.head.forward * (wanderDistance * 10f);
        Vector3 direction = (lookPoint - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);
    }

    #endregion
    #region Hovering
    private void BaseChatter()
    {
        var selectedClip = dovinaAudioManager.GetClip("gp_changes/movement/hovering");
        dovinaAudioManager.PlayClip(selectedClip, 0);
    }

    private void TryPlaySpeedChatter()
    {
        float speed = movementScript.CurrentVelocity.magnitude;
        // Fast zone
        if (speed >= 60f)
        {
            if (!hasPlayedFastChatter)
            {
                var clip = dovinaAudioManager.GetClip("gp_changes/speed/fast");
                dovinaAudioManager.PlayClip(clip, 0);
                hasPlayedFastChatter = true;
                hasPlayedSlowChatter = false;
            }
        }
        // Slow zone
        else if (speed <= 50f)
        {
            Debug.Log("Hit slow speed " + speed);
            if (!hasPlayedSlowChatter)
            {
                var clip = dovinaAudioManager.GetClip("gp_changes/speed/slow");
                dovinaAudioManager.PlayClip(clip, 0);
                hasPlayedSlowChatter = true;
                hasPlayedFastChatter = false;
            }
        }
        // Mid-range, reset both
        else
        {
            hasPlayedFastChatter = false;
            hasPlayedSlowChatter = false;
        }
    }

    private void Hover()
    {
        BaseChatter();
        bool isIdle = !movementScript.isGliding;

        if (isIdle && currentState != DoveState.Hovering)
        {
            FlapInfinite();
            currentState = DoveState.Hovering;
            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            hoverRoutine = StartCoroutine(IdleHoverLoop());
        }
        else if (!isIdle && currentState == DoveState.Hovering)
        {
            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            currentState = DoveState.Orbiting;
            hoverRoutine = null;
            isHoverIdle = false;
        }
    }    
    private IEnumerator IdleHoverLoop()
    {
        while (true)
        {
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.5f);
            Vector3 offset = randomDir.normalized * wanderDistance;

            currentHoverOffset = offset;

            // Phase 1: magnitude-based smooth approach
            yield return StartCoroutine(SmoothHoverApproach(offset));

            // Phase 2: idle bobbing
            isHoverIdle = true;
            float timer = 0f;

            while (timer < waitDuration && isHoverIdle && !movementScript.isGliding)
            {
                float offsetY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
                Vector3 baseHover = player.position + currentHoverOffset;
                Vector3 bobTarget = baseHover + new Vector3(0, offsetY, 0);
                liveTargetPosition = bobTarget;
                transform.position = Vector3.SmoothDamp(transform.position, bobTarget, ref doveVelocity, 0.15f);


                Vector3 lookDir = (player.position - movementScript.head.forward * 2f) - transform.position;
                Quaternion rot = Quaternion.LookRotation(lookDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 2f);

                timer += Time.deltaTime;
                yield return null;
            }
            isHoverIdle = false;
        }
    }

    private IEnumerator SmoothHoverApproach(Vector3 offset) 
    {
        float maxCatchupDistance = 1000f;
        float blendDownDuration = 1.0f;

        currentSpeed = lastKnownSpeed;
        float blendTimer = 0f;

        while (!movementScript.isGliding)
        {
            Vector3 targetPos = player.position + offset;
            liveTargetPosition = targetPos;
            float distance = Vector3.Distance(transform.position, targetPos);
            float arrivalThreshold = 50.0f;

            if (distance <= arrivalThreshold)
            {
                yield break;
            }

            float playerSpeed = movementScript.CurrentVelocity.magnitude;
            float maxPlayerSpeed = movementScript.MaxSpeed;

            // Desired hover speed when close (slow and gentle)
            float nearHoverSpeed = maxPlayerSpeed;

            // Distance-based catch-up factor
            float distanceRatio = Mathf.Clamp01(distance / maxCatchupDistance); // 0 when close, 1 when far
            float catchupSpeed = Mathf.Lerp(nearHoverSpeed, maxPlayerSpeed, distanceRatio);

            // Smoothly blend from high speed to gentle hover speed
            if (blendTimer < blendDownDuration)
            {
                float t = blendTimer / blendDownDuration;
                currentSpeed = Mathf.Lerp(currentSpeed, catchupSpeed, t);
                blendTimer += Time.deltaTime;
            }
            else
            {
                currentSpeed = catchupSpeed;
            }

            Vector3 moveDir = (targetPos - transform.position).normalized;
            transform.position += moveDir * currentSpeed * Time.deltaTime;

            // Look toward movement direction
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);

            yield return null;
        }
    }


#endregion
#region Following and avoiding
    private void Follow()
    {
        float playerSpeed = movementScript.CurrentVelocity.magnitude;

        if (playerSpeed <= orbitChillSpeedThreshold)
        {
            currentState = DoveState.Orbiting;
            return;
        }

        Vector3 followOffset =
            movementScript.head.forward * followDistance +
            movementScript.head.right * followSideOffset +
            Vector3.up * followVerticalOffset;

        Vector3 targetPos = movementScript.head.position + followOffset;

        liveTargetPosition = targetPos;
    }
    private void Avoid()
    {
        liveTargetPosition = escapeTarget;
    }
    // Obstacle check and navigating around obstacle
    private void ObstacleCheck()
    {
        if (currentState != DoveState.Orbiting) return;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f; // slight vertical lift to avoid hitting ground immediately
        Vector3 toTarget = (liveTargetPosition - rayOrigin).normalized;

        RaycastHit hit;

        Debug.DrawRay(rayOrigin, toTarget * dangerRange, Color.green);

        if (Physics.SphereCast(rayOrigin, radius: dangerRange, direction: toTarget, out hit, dangerRange, obstacleLayers))
        {
            if (!isEscaping)
            {
                Debug.DrawRay(rayOrigin, toTarget * dangerRange, Color.red);
                StartCoroutine(NavigateAround(hit.normal));
            }
        }
    }
    private IEnumerator NavigateAround(Vector3 hitNormal)
    {
        isEscaping = true;
        currentState = DoveState.Navigating;

        float timer = 0f;
        float navStepInterval = 0.3f;

        while (true)
        {
            // Recalculate liveTargetPosition using orbit center + offset
            Vector3 orbitCenter = movementScript.head.position;
            Vector3 desiredOffset = smoothedOrbitOffset;
            Vector3 desiredTarget = orbitCenter + desiredOffset;

            Vector3 toTarget = desiredTarget - transform.position;

            if (!Physics.Raycast(transform.position, toTarget.normalized, dangerRange, obstacleLayers))
            {
                // Path is now clear
                currentState = DoveState.Orbiting;
                isEscaping = false;
                yield break;
            }

            // Pick a temporary side-step direction (perpendicular to obstacle normal)
            Vector3 sideStep = Vector3.Cross(hitNormal, Vector3.up).normalized * escapeDistance;
            sideStep.y = 0.2f; // small upward bias

            Vector3 stepTarget = transform.position + sideStep;

            // Move toward sidestep
            float stepTime = 0f;
            while (stepTime < navStepInterval)
            {
                Vector3 moveDir = (stepTarget - transform.position).normalized;
                transform.position += moveDir * lastKnownSpeed * Time.deltaTime;

                Quaternion rot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 2f);

                stepTime += Time.deltaTime;
                yield return null;
            }

            timer += navStepInterval;
            if (timer > 3f)
            {
                Debug.LogWarning("[DOVE] Navigation timeout, re-attempting orbit");
                currentState = DoveState.Orbiting;
                isEscaping = false;
                yield break;
            }
        }
    }



    private IEnumerator EscapeAndReturn()
    {
        isEscaping = true;
        DoveState previousState = currentState;
        currentState = DoveState.Escaping;

        yield return new WaitForSeconds(escapeDuration);

        currentState = previousState;
        isEscaping = false;
    }
#endregion
#region Flap region
    private void HandleFlap()
    {
        flapQueue++;
        if (!isFlappingLoop)
            StartCoroutine(PlayQueuedFlaps());
    }

    private IEnumerator PlayQueuedFlaps()
    {
        isFlappingLoop = true;
        while (flapQueue > 0)
        {
            animator.ResetTrigger("Flap");
            animator.SetTrigger("Flap");

            float flapDuration = GetAdjustedClipLength("Flap");
            yield return new WaitForSeconds(flapDuration);
            flapQueue--;
        }

        isFlappingLoop = false;
    }

    private IEnumerator FlapInfinite() 
    {
        while (true)
        {
            animator.ResetTrigger("Flap");
            animator.SetTrigger("Flap");

            float flapDuration = GetAdjustedClipLength("Flap");
            yield return new WaitForSeconds(flapDuration);
        }
    }
#endregion
#region Helpers
    private float GetAdjustedClipLength(string clipName)
    {
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }
        return 1f;
    }
#endregion
    void OnDestroy()
    {
        movementScript.OnFlap -= HandleFlap;
    }
}
