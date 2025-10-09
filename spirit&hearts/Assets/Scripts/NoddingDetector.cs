using UnityEngine;

public class NoddingDetector : MonoBehaviour
{
    public Transform head;
    public float speedThreshold = 50f;
    public float angleThreshold = 15f;
    public float maxNodInterval = 0.6f;

    private float lastPitch;
    private bool goingDown = false;
    private float downTime;
    private float minPitch, maxPitch;

    private MovementEventHub hub;

    void Awake()
    {
        hub = GetComponentInParent<MovementEventHub>();
        if (!head) head = head;
    }

    void Start()
    {
        lastPitch = GetPitch();
    }

    void Update()
    {
        if (PauseState.IsPaused) return;

        if (!head) return;

        float pitch = GetPitch();
        float delta = (pitch - lastPitch) / Time.deltaTime;
        lastPitch = pitch;

        if (!goingDown && delta < -speedThreshold)
        {
            goingDown = true;
            minPitch = pitch;
            downTime = Time.time;
        }

        if (goingDown && delta > speedThreshold)
        {
            maxPitch = pitch;

            if (Mathf.Abs(maxPitch - minPitch) > angleThreshold &&
                Time.time - downTime < maxNodInterval)
            {
                hub?.RaiseNod();
            }

            goingDown = false;
        }
    }

    float GetPitch()
    {
        float p = head.localEulerAngles.x;
        if (p > 180f) p -= 360f;
        return p;
    }
}
