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

    // Flight variables
    private Vector3 flightOffset;
    private float flightChangeTimer;
    private float flightChangeInterval = 3f;

    void Start()
    {
        baseHoverPos = transform.localPosition;
        StartCoroutine(WanderLoop());
        flightOffset = GetRandomViewOffset();
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
        if (!movementScript.isGliding && !movementScript.isFlapping)
        {
            // Hover mode
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
                transform.rotation = Quaternion.Slerp(transform.rotation, glanceRotation, Time.deltaTime * 1.5f);
            }
        }
        else
        {
            // Flying mode — place dove on a shoulder-view orbit box
            float distance = Mathf.Max(Vector3.Distance(player.position, transform.position), 1.0f);

            // Change offset occasionally
            flightChangeTimer += Time.deltaTime;
            if (flightChangeTimer >= flightChangeInterval)
            {
                flightOffset = GetRandomViewOffset();
                flightChangeTimer = 0f;
                Debug.Log($"[DOVE] New Flight Offset Chosen: {flightOffset}");
            }

            // Project offset into world space from head direction
            Vector3 targetOffset =
                movementScript.head.forward * (flightOffset.z * distance) +
                movementScript.head.right * (flightOffset.x * distance) +
                movementScript.head.up * (flightOffset.y * distance);

            Vector3 targetWorldPos = movementScript.head.position + targetOffset;
            Vector3 localTarget = player.InverseTransformPoint(targetWorldPos);

            transform.localPosition = Vector3.Lerp(transform.localPosition, localTarget, Time.deltaTime * 3f);
            Quaternion targetRot = Quaternion.LookRotation(movementScript.head.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);
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
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        transform.rotation = targetRot;
        isMoving = false;
    }

    // Get random view offset when flying
    Vector3 GetRandomViewOffset()
    {
        float z = 0.7f; // always forward-ish
        float x = Random.Range(-0.5f, 0.5f); // left or right
        float y = Random.Range(-0.3f, 0.3f); // slightly up or down

        return new Vector3(x, y, z);
    }
}
