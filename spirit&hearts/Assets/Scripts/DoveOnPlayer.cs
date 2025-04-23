using System.Collections;
using UnityEngine;

public class DoveOnPlayer : MonoBehaviour
{
    // Player variables
    [Header("Player variables")]
    public Transform player;
    public Movement movementScript;

    // Header variables
    [Header("Hover variables")]
    public float moveDuration = 2f;
    public float waitDuration = 5f;
    public float hoverAmplitude = 0.1f;
    public float hoverFrequency = 1f;
    private bool isMoving = false;
    private Vector3 baseHoverPos;
    private Quaternion baseHoverRot;
    private float hoverTimer;

    // Flight variables
    [Header("Flight variables")]
    private Vector3 flightOffset;
    private float flightChangeTimer;
    private float flightChangeInterval = 3f;
    void Start()
    {
        baseHoverPos = transform.position;
        baseHoverRot = transform.rotation;
        StartCoroutine(WanderLoop());
        flightOffset = GetRandomViewOffset();
    }

    void Update()
    {
        if (!movementScript.isGliding && !movementScript.isFlapping)
        {
            // Hover mode
            if (!isMoving)
            {
                hoverTimer += Time.deltaTime;
                float hoverOffset = Mathf.Sin(hoverTimer * hoverFrequency) * hoverAmplitude;
                transform.position = new Vector3(baseHoverPos.x, baseHoverPos.y + hoverOffset, baseHoverPos.z);

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

            Vector3 targetPos = movementScript.head.position + targetOffset;
            Debug.Log($"[DOVE] Target World Pos: {targetPos}, Distance from Player: {distance}");

            // Smooth move and rotation
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);
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
                Vector3 dovePos = transform.position;
                float distance = Vector3.Distance(playerPos, dovePos);

                // Random horizontal direction
                Vector3 randomDir = Random.onUnitSphere;
                randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.3f);
                Vector3 targetPos = playerPos + randomDir.normalized * distance;

                Quaternion lookDir = Quaternion.LookRotation((targetPos - dovePos).normalized);
                yield return StartCoroutine(MoveToPosition(targetPos, lookDir, moveDuration));

                baseHoverPos = transform.position;
                hoverTimer = 0f;

                yield return new WaitForSeconds(waitDuration);
            }
            yield return null;
        }
    }

    IEnumerator MoveToPosition(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
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
