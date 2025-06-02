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
    public float transitionSmoothing = 5f;
    public float rotationSmoothing = 1f;

    [Header("Hovering")]
    public float hoverAmplitude = 0.1f;
    public float hoverFrequency = 1f;
    public float moveDuration = 2f;
    public float waitDuration = 5f;
    public float wanderDistance = 5f;
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
    private int orbitDirection = 1;
    private bool transitioningToOrbit = false;
    private Vector3 smoothedOrbitOffset = Vector3.zero;
    private Vector3 moveVelocity = Vector3.zero;

    [Header("Follow Mode")]
    public float followDistance = 2f;
    public float followSideOffset = 0.5f;
    public float followVerticalOffset = 0.5f;

    [Header("Obstacle Avoidance")]
    public float dangerRange = 10f;
    public float escapeDistance = 2f;
    public LayerMask obstacleLayers;
    public float escapeDuration = 0.5f;
    [Header("Animation")]
    public Animator animator;

    // Internal variables
    private enum DoveState { Orbiting, Hovering, Following, Escaping }
    private DoveState currentState = DoveState.Orbiting;
    private Vector3 escapeTarget;
    private bool isEscaping = false;
    private int flapQueue = 0;
    private bool isFlappingLoop = false;
    private Vector3 liveTargetPosition;
    private Vector3 doveVelocity = Vector3.zero;
    private float lastKnownSpeed;

#region Loop
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentOrbitRadius = orbitRadius;
        targetOrbitRadius = orbitRadius;
        movementScript.OnFlap += HandleFlap;
    }

    void Update()
    {
        Hover();

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


        if (currentState != DoveState.Hovering)
        {
            float proximityThreshold = 0.1f;
            float playerSpeed = movementScript.CurrentVelocity.magnitude;
            float distance = Vector3.Distance(transform.position, liveTargetPosition);
            Vector3 moveDir = (liveTargetPosition - transform.position).normalized;

            // If dove is far and in front of the player, make slower
            // If dove is far and behind the player, make faster
            // If dove is near the player, give regular speed
            Vector3 toDove = (transform.position - movementScript.head.position).normalized;
            Vector3 playerForward = movementScript.head.forward;

            // Dot product: 1 = directly in front, -1 = directly behind
            float facingDot = Vector3.Dot(playerForward, toDove);

            // Calculate a weight based on player's attention
            // -1 (behind): faster dove, +1 (front): slower dove
            float attentionFactor = Mathf.InverseLerp(1f, -1f, facingDot); // converts dot range [1,-1] to [0,1]

            // Boost if far, slow if close
            float distanceRatio = Mathf.Clamp01(distance / wanderDistance);

            // Final speed: modulate based on how far dove is AND where it is in relation to player gaze
            float baseSpeed = Mathf.Lerp(playerSpeed / 1.1f, playerSpeed / 0.9f, distanceRatio);
            float speed = Mathf.Lerp(baseSpeed * 0.75f, baseSpeed * 1.75f, attentionFactor);

            // Move dove
            float maxStep = distance / Time.deltaTime;
            speed = Mathf.Min(speed, maxStep);
            lastKnownSpeed = speed;

            transform.position += moveDir * speed * Time.deltaTime;
            
            float smoothingThreshold = 0.12f;
            transform.position = Vector3.SmoothDamp(transform.position, liveTargetPosition, ref moveVelocity, 2f);
        }

        ObstacleCheck();
    }
#endregion
#region Orbit
    private void Orbit()
    {
        Debug.Log("Orbiting");
        float playerSpeed = movementScript.CurrentVelocity.magnitude;
        float orbitSpeed = baseOrbitSpeed * playerSpeed * 0.5f;
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;
        orbitAngle = Mathf.Clamp(orbitAngle, -30f, 30f);

        if (orbitAngle >= 30 || orbitAngle <= -30)
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
    private void Hover()
    {
        bool isIdle = !movementScript.isGliding && !movementScript.isFlapping;

        if (isIdle && currentState != DoveState.Hovering)
        {
            Debug.Log("[HOVER] Entering hover state");
            currentState = DoveState.Hovering;
            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            hoverRoutine = StartCoroutine(IdleHoverLoop());
        }
        else if (!isIdle && currentState == DoveState.Hovering)
        {
            Debug.Log("[HOVER] Exiting hover, transitioning to orbit");
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

            while (timer < waitDuration && isHoverIdle && (!movementScript.isGliding || !movementScript.isFlapping))
            {
                float offsetY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
                Vector3 baseHover = player.position + currentHoverOffset;
                Vector3 bobTarget = baseHover + new Vector3(0, offsetY, 0);

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
        float distanceThreshold = 0.2f;
        float blendTimer = 0f;
        float speedBlendDuration = 0.5f;
        float currentSpeed = 0f;
        
        while (!movementScript.isGliding && !movementScript.isFlapping)
        {
            Vector3 targetPos = player.position + offset;
            float distance = Vector3.Distance(transform.position, targetPos);

            if (distance < distanceThreshold)
            {
                Debug.Log("[HOVER] Reached hover target.");
                yield break;
            }

            float playerSpeed = movementScript.CurrentVelocity.magnitude;
            float maxPlayerSpeed = movementScript.MaxSpeed;

            // Determine how "close" we are to the player
            float distanceRatio = Mathf.Clamp01(wanderDistance / distance);

            // Hover speed scales: max speed when far away, soft slow speed when close
            float targetHoverSpeed = Mathf.Lerp(lastKnownSpeed, maxPlayerSpeed / 10.0f, distanceRatio);

            // Smoothly blend from orbit speed to hover speed over short time
            if (blendTimer < speedBlendDuration)
            {
                float t = blendTimer / speedBlendDuration;
                currentSpeed = Mathf.Lerp(lastKnownSpeed, targetHoverSpeed, t);
                blendTimer += Time.deltaTime;
            }
            else
            {
                currentSpeed = targetHoverSpeed;
            }

            // Move toward target
            Vector3 moveDir = (targetPos - transform.position).normalized;
            transform.position += moveDir * currentSpeed * Time.deltaTime;

            // Look toward movement direction
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);

            yield return null;
        }

        Debug.Log("[HOVER] Aborted approach — player started moving.");
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

    private void ObstacleCheck()
    {
        RaycastHit hit;
        bool isHit = Physics.Raycast(transform.position, transform.forward, out hit, dangerRange, obstacleLayers);
        if (isHit)
        {
            escapeTarget = transform.position + Vector3.Reflect(transform.forward, hit.normal).normalized * escapeDistance;
            escapeTarget.y = Mathf.Max(escapeTarget.y, transform.position.y);

            if (!isEscaping)
                StartCoroutine(EscapeAndReturn());
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
        Debug.Log("Flap Queue: " + flapQueue);
        while (flapQueue > 0)
        {
            animator.ResetTrigger("Flap");
            animator.SetTrigger("Flap");

            float flapDuration = GetAdjustedClipLength("Flap");
            Debug.Log("Waiting for flap: " + flapDuration);
            yield return new WaitForSeconds(flapDuration);
            flapQueue--;
            Debug.Log("Done waiting! Flap queue: " + flapQueue);
        }

        isFlappingLoop = false;
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
