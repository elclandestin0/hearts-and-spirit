using System.Collections;
using UnityEngine;

public class DoveOnPlayer : MonoBehaviour
{
    // Player variables
    [Header("Player variables")]
    public Transform player;
    public Movement movementScript;

    // Hover variables
    [Header("Hover variables")]
    public float moveDuration = 2f;
    public float waitDuration = 5f;
    public float hoverAmplitude = 0.1f;
    public float hoverFrequency = 1f;
    private bool isMoving = false;
    private bool wasFlyingLastFrame = false;
    private Vector3 baseHoverPos;
    private float hoverTimer;

    // Flight flocking variables
    [Header("Flight flocking variables")]
    public float orbitRadius = 1.5f;
    public float baseOrbitSpeed = 30f; // degrees per second
    public float orbitSwitchInterval = 5f; // seconds
    public float verticalBobAmplitude = 0.3f;
    public float verticalBobFrequency = 2f;
    public float positionSmoothing = 5f;
    public float rotationSmoothing = 3f;
    
    private float orbitAngle = 0f;
    private int orbitDirection = 1; // 1 or -1
    private float orbitSwitchTimer = 0f;

    void Start()
    {
        baseHoverPos = transform.localPosition;
        StartCoroutine(WanderLoop());
    }

    void Update()
    {
        bool isFlying = movementScript.isGliding || movementScript.isFlapping;

        if (wasFlyingLastFrame && !isFlying)
        {
            baseHoverPos = transform.localPosition;
            hoverTimer = 0f;
        }
        wasFlyingLastFrame = isFlying;

        if (!isFlying)
        {
            HoverMode();
        }
        else
        {
            FlockingFlightMode();
        }
    }

    private void HoverMode()
    {
        if (!isMoving)
        {
            hoverTimer += Time.deltaTime;
            float hoverOffset = Mathf.Sin(hoverTimer * hoverFrequency) * hoverAmplitude;
            transform.localPosition = new Vector3(baseHoverPos.x, baseHoverPos.y + hoverOffset, baseHoverPos.z);

            // Glance at player
            Vector3 forwardDir = transform.forward;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            Vector3 glanceDir = Vector3.Slerp(forwardDir, toPlayer, 0.4f);
            Quaternion glanceRotation = Quaternion.LookRotation(glanceDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, glanceRotation, Time.deltaTime * 5f);
        }
    }

    private void FlockingFlightMode()
    {
        // Update orbit switching
        // orbitSwitchTimer += Time.deltaTime;
        // if (orbitSwitchTimer >= orbitSwitchInterval)
        // {
        //     orbitDirection *= -1; // Switch direction
        //     orbitSwitchTimer = 0f;
        // }

        // Orbit based on player speed
        float playerSpeed = movementScript.CurrentVelocity.magnitude;
        float orbitSpeed = baseOrbitSpeed + playerSpeed * 1f;

        // Remove comment to test randomizing the direction
        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;

        // Reverse direction at the edges
        if (orbitAngle >= 135f)
        {
            orbitAngle = 135f;
            orbitDirection = -1;
        }
        else if (orbitAngle <= -45f)
        {
            orbitAngle = -45f;
            orbitDirection = 1;
        }

        // Calculate orbit position
        float radians = orbitAngle * Mathf.Deg2Rad;

        // Build dynamic orbit axes
        Vector3 right = movementScript.head.right;
        Vector3 forward = movementScript.head.forward;

        // Compute orbit offset relative to head orientation
        Vector3 orbitOffset = (right * Mathf.Sin(radians) + forward * Mathf.Cos(radians)) * orbitRadius;

        // Add vertical bobbing
        orbitOffset.y += Mathf.Sin(Time.time * verticalBobFrequency) * verticalBobAmplitude;

        // Calculate world-space target
        Vector3 orbitCenter = movementScript.head.position;
        Vector3 targetWorldPos = orbitCenter + orbitOffset;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * positionSmoothing);

        // Smooth rotation facing forward
        Vector3 forwardDir = Vector3.Slerp(transform.forward, movementScript.head.forward, 0.5f);
        if (forwardDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(forwardDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);
        }
    }

    IEnumerator WanderLoop()
    {
        while (true)
        {
            if (!movementScript.isGliding && !movementScript.isFlapping && !isMoving)
            {
                Vector3 playerPos = player.position;
                Vector3 dovePos = transform.localPosition;
                float distance = Vector3.Distance(Vector3.zero, dovePos); // local space

                // Random local-space direction
                Vector3 randomDir = Random.onUnitSphere;
                randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.3f);
                Vector3 targetLocalPos = randomDir.normalized * distance;

                Quaternion lookDir = Quaternion.LookRotation(player.TransformPoint(targetLocalPos) - transform.position);
                yield return StartCoroutine(MoveToPosition(targetLocalPos, lookDir, moveDuration));

                baseHoverPos = transform.localPosition;
                hoverTimer = 0f;

                yield return new WaitForSeconds(waitDuration);
            }

            yield return null;
        }
    }

    IEnumerator MoveToPosition(Vector3 targetLocalPos, Quaternion targetRot, float duration)
    {
        isMoving = true;
        Vector3 startLocalPos = transform.localPosition;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        isMoving = false;
    }
}
