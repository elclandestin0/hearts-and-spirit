using UnityEngine;

public class NoddingDetector : MonoBehaviour
{
    public Transform head;               // player camera
    public float speedThreshold = 50f;   // degrees per second
    public float angleThreshold = 15f;   // min degrees difference to count
    public float maxNodInterval = 0.6f;  // seconds allowed between down & up

    private float lastPitch;
    private bool goingDown = false;
    private float downTime;
    private float minPitch, maxPitch;

    public int nodCount { get; private set; }

    void Start()
    {
        lastPitch = GetPitch();
    }

    void Update()
    {
        float pitch = GetPitch();
        float delta = (pitch - lastPitch) / Time.deltaTime;
        lastPitch = pitch;

        // detect downward swing
        if (!goingDown && delta < -speedThreshold)
        {
            goingDown = true;
            minPitch = pitch;
            downTime = Time.time;
        }

        // detect upward swing after downward
        if (goingDown && delta > speedThreshold)
        {
            maxPitch = pitch;

            if (Mathf.Abs(maxPitch - minPitch) > angleThreshold &&
                Time.time - downTime < maxNodInterval)
            {
                nodCount++;
                Debug.Log("Nod detected! Total: " + nodCount);
            }

            goingDown = false;
        }
    }

    float GetPitch()
    {
        float pitch = head.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        return pitch;
    }
}
