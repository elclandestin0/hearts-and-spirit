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

    // NEW: hook to the hub
    private MovementEventHub hub;

    void Awake()
    {
        hub = GetComponentInParent<MovementEventHub>(); // find the hub on your rig
        if (!head) head = Camera.main ? Camera.main.transform : head; // safety
    }

    void Start()
    {
        lastPitch = GetPitch();
    }

    void Update()
    {
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
                // tell the system a nod happened
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
