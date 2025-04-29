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

    // == Orbit Mode Settings ==
    [Header("Orbit Mode")]
    public float orbitRadius = 1.5f;
    public float baseOrbitSpeed = 30f;
    public float verticalBobAmplitude = 0.3f;
    public float verticalBobFrequency = 2f;
    public float orbitChillSpeedThreshold = 5f;

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

    [Header("Debug variables")]
    public float orbitAngle = 0f;
    public int orbitDirection = 1;

    private Vector3 escapeTarget;
    private bool isEscaping = false;

    void Update()
    {
        switch (currentState)
        {
            case DoveState.Orbiting:
                Debug.Log("orbiting");
                Orbit();
                break;
            case DoveState.Following:
                Debug.Log("following");
                Follow();
                break;
            case DoveState.Escaping:
                Debug.Log("escaping");
                Avoid();
                break;
        }

        ObstacleCheck();
    }

    private void Orbit()
    {
        float playerSpeed = movementScript.CurrentVelocity.magnitude;

        // Switch to Follow Mode if player is fast
        if (playerSpeed > orbitChillSpeedThreshold)
        {
            currentState = DoveState.Following;
            return;
        }

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
        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = (right * Mathf.Sin(radians) + forward * Mathf.Cos(radians)) * orbitRadius;
        orbitOffset.y += Mathf.Sin(Time.time * verticalBobFrequency) * verticalBobAmplitude;

        Vector3 orbitCenter = movementScript.head.position;
        Vector3 targetPos = orbitCenter + orbitOffset;

        // Move and rotate
        MoveTowards(targetPos, movementScript.head.forward);
    }

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
            -movementScript.head.forward * followDistance +
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
}
