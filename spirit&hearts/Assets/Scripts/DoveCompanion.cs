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
    public float rotationSmoothing = 3f;

    [Header("Hovering")]
    public float hoverAmplitude = 0.1f;
    public float hoverFrequency = 1f;
    public float moveDuration = 2f;
    public float waitDuration = 5f;
    public float wanderDistance = 10f;
    private Vector3 baseHoverPos;
    private float hoverTimer;
    private bool isHovering = false;
    private Vector3 currentHoverOffset;
    private Coroutine hoverRoutine;
    private bool isHoverIdle = false;

    [Header("Orbit Mode")]
    public float orbitRadius = 1.5f;
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

    [Header("Follow Mode")]
    public float followDistance = 2f;
    public float followSideOffset = 0.5f;
    public float followVerticalOffset = 0.5f;

    [Header("Obstacle Avoidance")]
    public float dangerRange = 10f;
    public float escapeDistance = 2f;
    public LayerMask obstacleLayers;
    public float escapeDuration = 0.5f;

    private enum DoveState { Orbiting, Hovering, Following, Escaping }
    private DoveState currentState = DoveState.Orbiting;

    [Header("Animation")]
    public Animator animator;
    private Vector3 escapeTarget;
    private bool isEscaping = false;
    private int flapQueue = 0;
    private bool isFlappingLoop = false;

    private Vector3 liveTargetPosition;
    private Vector3 doveVelocity = Vector3.zero;

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

        if (!isFlappingLoop)
        {
            animator.SetBool("Gliding", movementScript.isGliding);
        }

        transform.position = Vector3.SmoothDamp(transform.position, liveTargetPosition, ref doveVelocity, movementScript.isGliding || movementScript.isFlapping ? 1.0f : 0.3f);
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
        Vector3 orbitOffset = (right * Mathf.Sin(radians) + forward * Mathf.Cos(radians)) * Random.Range(orbitRadius - 2f, orbitRadius);
        orbitOffset.y += Mathf.Sin(Time.time * verticalBobFrequency) * verticalBobAmplitude;

        Vector3 orbitCenter = movementScript.head.position;
        Vector3 targetPos = orbitCenter + orbitOffset;

        liveTargetPosition = targetPos;
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

            yield return StartCoroutine(SmoothHoverApproach(offset));

            isHoverIdle = true;
            float timer = 0f;

            while (timer < waitDuration && isHoverIdle && (!movementScript.isGliding || !movementScript.isFlapping))
            {
                float offsetY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
                Vector3 baseHover = player.position + currentHoverOffset;
                liveTargetPosition = baseHover + new Vector3(0, offsetY, 0);

                Vector3 lookDir = (player.position + movementScript.head.forward * 2f) - transform.position;
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
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 targetPos = player.position + offset;
        float distanceToOffset = Vector3.Distance(transform.position, targetPos);
        float offsetThreshold = 0.2f;

        while (t < 1f && (!movementScript.isGliding || !movementScript.isFlapping) && distanceToOffset < offsetThreshold)
        {
            t += Time.deltaTime / moveDuration;
            liveTargetPosition = Vector3.Lerp(start, targetPos, t);

            Quaternion targetRot = Quaternion.LookRotation((targetPos - transform.position).normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);
            
            targetPos = Vector3.Lerp(start, targetPos, t);
            distanceToOffset = Vector3.Distance(transform.position, targetPos);

            yield return null;
        }
    }
    private IEnumerator TransitionToOrbitCoroutine()
    {
        float timer = 0f;
        float maxDuration = 1.5f;
        Vector3 startPos = transform.position;

        float playerSpeed = movementScript.CurrentVelocity.magnitude;
        float adjustedDuration = Mathf.Max(maxDuration, 1f / (playerSpeed + 0.1f));

        float orbitSpeed = baseOrbitSpeed * playerSpeed * 0.5f;
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;
        orbitAngle = Mathf.Clamp(orbitAngle, -30f, 30f);
        if (orbitAngle >= 30 || orbitAngle <= -30) orbitDirection *= -1;

        Vector3 right = movementScript.head.right;
        Vector3 forward = movementScript.head.forward;
        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = (right * Mathf.Sin(radians) + forward * Mathf.Cos(radians)) * orbitRadius;
        orbitOffset.y += Mathf.Sin(Time.time * verticalBobFrequency) * verticalBobAmplitude;
        Vector3 targetPos = movementScript.head.position + orbitOffset;

        Debug.Log("[HOVER->ORBIT] Transitioning to orbit");

        bool reachedTarget = false;

        while (timer < 6.0f)
        {
            if (!movementScript.isGliding)
            {
                Debug.Log("[ORBIT->HOVER] Gliding cancelled — return to Hover.");
                transitioningToOrbit = false;
                currentState = DoveState.Hovering;
                yield break;
            }

            liveTargetPosition = Vector3.SmoothDamp(transform.position, targetPos, ref doveVelocity, 5f);

            Quaternion targetRot = Quaternion.LookRotation((targetPos - transform.position).normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);

            timer += Time.deltaTime;

            // Optional: consider checking distance here to end early
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                reachedTarget = true;
                break;
            }

            yield return null;
        }

        transitioningToOrbit = false;

        if (movementScript.isGliding && reachedTarget)
        {
            currentState = DoveState.Orbiting;
            Debug.Log("[HOVER->ORBIT] Orbiting.");
        }
        else
        {
            currentState = DoveState.Hovering;
            Debug.Log("[ORBIT CANCELLED] Didn't reach orbit in time or glide stopped.");
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

        while (flapQueue > 0)
        {
            animator.ResetTrigger("Flap");
            animator.SetBool("Gliding", false);
            animator.SetTrigger("Flap");

            float flapDuration = GetAdjustedClipLength("Flap");
            yield return new WaitForSeconds(flapDuration);

            flapQueue--;
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
