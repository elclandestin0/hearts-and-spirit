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
    private Vector3 baseHoverPos;
    private float hoverTimer;
    private bool isHovering = false;
    private Vector3 currentHoverOffset;
    
    [Header("Orbit Mode")]
    public float orbitRadius = 1.5f;
    public float baseOrbitSpeed = 30f;
    public float verticalBobAmplitude = 0.3f;
    public float verticalBobFrequency = 2f;
    public float orbitChillSpeedThreshold = 5f;
    public float currentOrbitRadius;
    public float targetOrbitRadius;
    public float orbitRadiusChangeTimer;
    public float orbitRadiusChangeInterval = 3f; // how often we change target
    private float orbitAngle = 0f;
    private int orbitDirection = 1;

    // == Follow Mode Settings ==
    [Header("Follow Mode")]
    public float followDistance = 2f;
    public float followSideOffset = 0.5f;
    public float followVerticalOffset = 0.5f;

    // == Obstacle Avoidance Settings ==
    [Header("Obstacle Avoidance")]
    public float dangerRange = 10f;
    public float escapeDistance = 2f;
    public LayerMask obstacleLayers;
    public float escapeDuration = 0.5f;

    // == Internal State ==
    private enum DoveState { Orbiting, Following, Escaping }
    private DoveState currentState = DoveState.Orbiting;

    [Header("Animation")]
    public Animator animator;
    // Escape variables
    private Vector3 escapeTarget;
    private bool isEscaping = false;

    // Flapping time
    private int flapQueue = 0;
    private bool isFlappingLoop = false;


    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentOrbitRadius = orbitRadius;
        targetOrbitRadius = orbitRadius;

        // If flap detected from Movement.cs, HandleFlap()
        movementScript.OnFlap += HandleFlap;

        StartCoroutine(WanderLoop());
    }

    void Update()
    {
        switch (currentState)
        {
            case DoveState.Orbiting:
                Orbit();
                break;
            case DoveState.Following:
                Follow();
                break;
            case DoveState.Escaping:
                Avoid();
                break;
        }

        // Animate based on movement state
        if (!isFlappingLoop) 
        {
            Debug.Log("[FLAP] Done flapping, now gliding.");
            animator.SetBool("Gliding", movementScript.isGliding);
        }
        ObstacleCheck();
        HoverIdle();
    }

#region Orbit
    private void Orbit()
    {
        float playerSpeed = movementScript.CurrentVelocity.magnitude;

        // Switch to Follow Mode if player is fast
        // To-do: Unblock later when time to focus on this
        // if (playerSpeed > orbitChillSpeedThreshold)
        // {
        //     currentState = DoveState.Following;
        //     return;
        // }

        // Update orbit angle
        float orbitSpeed = baseOrbitSpeed * playerSpeed;
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;
        orbitAngle = Mathf.Clamp(orbitAngle, -30f, 30f);

        if (orbitAngle >= 30 || orbitAngle <= -30)
        {
            orbitDirection *= -1;
        }


        // Build dynamic local axes
        Vector3 right = movementScript.head.right;
        Vector3 forward = movementScript.head.forward;

        // Calculate orbit offset
        orbitRadiusChangeTimer += Time.deltaTime;
        if (orbitRadiusChangeTimer >= orbitRadiusChangeInterval)
        {
            targetOrbitRadius = Random.Range(orbitRadius - 5f, orbitRadius);
            orbitRadiusChangeTimer = 0f;
        }

        // Smoothly blend current radius toward new target
        currentOrbitRadius = Mathf.Lerp(currentOrbitRadius, targetOrbitRadius, Time.deltaTime * 1f);

        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = (right * Mathf.Sin(radians) + forward * Mathf.Cos(radians)) * Random.Range(orbitRadius - 2f, orbitRadius);
        
        // Vertical Bobbing
        orbitOffset.y += Mathf.Sin(Time.time * verticalBobFrequency) * verticalBobAmplitude;
        
        // Center and targetPos calculation
        Vector3 orbitCenter = movementScript.head.position;
        Vector3 targetPos = orbitCenter + orbitOffset;

        // Move and rotate
        MoveTowards(targetPos, movementScript.head.forward);
    }

    private void MoveTowards(Vector3 targetPos, Vector3 faceDirection)
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSmoothing);

        Vector3 moveDirection = targetPos - transform.position;
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);
        }
    }

#endregion
#region Following and avoiding
    private void Follow()
    {
        float playerSpeed = movementScript.CurrentVelocity.magnitude;

        // Return to orbit mode if slowing down
        if (playerSpeed <= orbitChillSpeedThreshold)
        {
            currentState = DoveState.Orbiting;
            return;
        }

        // Follow target position behind player
        Vector3 followOffset =
            movementScript.head.forward * followDistance +
             movementScript.head.right * followSideOffset +
             Vector3.up * followVerticalOffset;

        Vector3 targetPos = movementScript.head.position + followOffset;

        // Move and rotate
        MoveTowards(targetPos, movementScript.head.forward);
    }
    private void Avoid()
    {
        // Move toward escape target
        MoveTowards(escapeTarget, movementScript.head.forward);
    }

    private void ObstacleCheck()
    {
        RaycastHit hit;
        bool isHit = Physics.Raycast(transform.position, transform.forward, out hit, dangerRange, obstacleLayers);
        // Draw the ray
        if (isHit)
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * dangerRange, Color.red);
            AvoidObstacle(hit);
        }
        else
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * dangerRange, Color.green);
        }
    }

    private void AvoidObstacle(RaycastHit hit)
    {
        // Reflect movement away from obstacle
        Vector3 escapeDir = Vector3.Reflect(transform.forward, hit.normal);
        escapeDir.y = Mathf.Abs(escapeDir.y); // Favor going upward
        escapeTarget = transform.position + escapeDir.normalized * escapeDistance;

        if (!isEscaping)
            StartCoroutine(EscapeAndReturn());
    }

    private IEnumerator EscapeAndReturn()
    {
        isEscaping = true;
        DoveState previousState = currentState;
        currentState = DoveState.Escaping;

        float timer = 0f;
        while (timer < escapeDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Return to previous behavior
        currentState = previousState;
        isEscaping = false;
    }
#endregion
#region Hovering
private IEnumerator WanderLoop()
{
    while (true)
    {
        bool isIdle = !movementScript.isGliding && !movementScript.isFlapping;

        if (isIdle && !isHovering)
        {
            // Choose a persistent local offset
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.3f);
            Vector3 localOffset = randomDir.normalized * 4f;
            currentHoverOffset = localOffset;
            yield return StartCoroutine(HoverToDynamicOffset(localOffset, moveDuration));

            baseHoverPos = transform.position;
            hoverTimer = 0f;

            yield return new WaitForSeconds(waitDuration);
        }

        yield return null;
    }
}
    private void HoverIdle()
    {
        if (isHovering || movementScript.isGliding || movementScript.isFlapping)
            return;

        hoverTimer += Time.deltaTime;
        float offset = Mathf.Sin(hoverTimer * hoverFrequency) * hoverAmplitude;

        Vector3 liveHoverBase = player.position + currentHoverOffset;
        Vector3 hoveredPos = liveHoverBase + new Vector3(0, offset, 0);
        transform.position = hoveredPos;

        // Gently look where the player is looking — from dove's perspective
        Vector3 headFwd = movementScript.head.forward;
        Vector3 fromDoveToLookTarget = (transform.position + headFwd * 2f) - transform.position;
        Vector3 glance = Vector3.Slerp(transform.forward, fromDoveToLookTarget.normalized, 0.4f);
        Quaternion rot = Quaternion.LookRotation(glance);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 2f);
    }
    
    private IEnumerator HoverToDynamicOffset(Vector3 localOffset, float duration)
    {
        isHovering = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;

        while (t < duration)
        {
            float blend = t / duration;

            // Live target position follows the player as they move
            Vector3 dynamicTarget = player.position + localOffset;
            Vector3 pos = Vector3.Lerp(startPos, dynamicTarget, blend);
            Quaternion rot = Quaternion.LookRotation((dynamicTarget - transform.position).normalized);

            transform.position = pos;
            transform.rotation = Quaternion.Slerp(startRot, rot, blend);

            t += Time.deltaTime;
            yield return null;
        }

        isHovering = false;
    }


#endregion

    // Flap region
    private void HandleFlap()
    {
        flapQueue++;
        Debug.Log("[FLAP] HandleFlap()");
        if (!isFlappingLoop)
            StartCoroutine(PlayQueuedFlaps());
    }

    private IEnumerator PlayQueuedFlaps()
    {
        isFlappingLoop = true;

        while (flapQueue > 0)
        {
            Debug.Log("[FLAP] flapQueue: " + flapQueue);

            // Trigger flap
            animator.ResetTrigger("Flap"); // clear previous just in case
            animator.SetBool("Gliding", false);
            animator.SetTrigger("Flap");

            // Wait for animation length (adjusted for speed)
            float flapDuration = GetAdjustedClipLength("Flap");
            yield return new WaitForSeconds(flapDuration);

            flapQueue--;
        }

        isFlappingLoop = false;
    }

    private float GetAdjustedClipLength(string clipName)
    {
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length; // avoid div by 0
            }
        }
        return 1f;
    }

    void OnDestroy()
    {
        movementScript.OnFlap -= HandleFlap;
    }

}
