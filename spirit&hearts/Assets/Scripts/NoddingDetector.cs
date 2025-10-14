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
        if (!head)
        {
            var cam = Camera.main;
            if (cam != null) head = cam.transform;
        }
    }

    void Start()
    {
        if (!head)
        {
            enabled = false;
            return;
        }
        lastPitch = GetPitch();
    }

    void Update()
    {
        if (PauseState.IsPaused) return;
        if (!head) return;

        float pitch = GetPitch();
        float delta = (pitch - lastPitch) / Time.deltaTime;
        lastPitch = pitch;

        // start downward motion
        if (!goingDown && delta < -speedThreshold)
        {
            goingDown = true;
            minPitch = pitch;
            downTime = Time.time;
        }

        // detect reversal (going up again)
        if (goingDown && delta > speedThreshold)
        {
            maxPitch = pitch;
            float amplitude = Mathf.Abs(maxPitch - minPitch);
            float elapsed = Time.time - downTime;


            if (amplitude > angleThreshold && elapsed < maxNodInterval)
            {
                hub?.RaiseNod();
            }

            goingDown = false;
        }
    }

    float GetPitch()
    {
        Vector3 fwd = head.forward;
        // Convert to pitch angle in degrees (-90 down, +90 up)
        float pitch = Mathf.Asin(fwd.y) * Mathf.Rad2Deg;
        return pitch;
    }
}
