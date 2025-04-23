using System.Collections;
using UnityEngine;

public class DoveOnPlayer : MonoBehaviour
{
    public Transform player;
    public float moveDuration = 2f;
    public float waitDuration = 5f;
    public float hoverAmplitude = 0.5f;
    public float hoverFrequency = 1f;

    private bool isMoving = false;
    private Vector3 baseHoverPos;
    private float hoverTimer;

    void Start()
    {
        baseHoverPos = transform.position;
        StartCoroutine(WanderLoop());
    }

    void Update()
    {
        if (!isMoving)
        {
            hoverTimer += Time.deltaTime;
            float hoverOffset = Mathf.Sin(hoverTimer * hoverFrequency) * hoverAmplitude;
            transform.position = new Vector3(baseHoverPos.x, baseHoverPos.y + hoverOffset, baseHoverPos.z);

            // Smoothly rotate back to original rotation when hovering
            Vector3 toPlayer = player.position - transform.position;
            Quaternion playerDirection = Quaternion.LookRotation(toPlayer.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, playerDirection, Time.deltaTime * 1.5f);
        }
    }

    IEnumerator WanderLoop()
    {
        while (true)
        {
            if (!isMoving)
            {
                Vector3 playerPos = player.position;
                Vector3 dovePos = transform.position;
                float distance = Vector3.Distance(playerPos, dovePos);

                // Random direction, mostly horizontal
                Vector3 randomDir = Random.onUnitSphere;
                randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.3f);
                Vector3 targetPos = playerPos + randomDir.normalized * distance;

                // Calculate target rotation
                Vector3 lookDir = (targetPos - dovePos).normalized;
                Quaternion targetRot = Quaternion.LookRotation(lookDir);

                yield return StartCoroutine(MoveToPosition(targetPos, targetRot, moveDuration));

                baseHoverPos = transform.position;
                hoverTimer = 0f;

                yield return new WaitForSeconds(waitDuration);
            }
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
}
