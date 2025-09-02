using UnityEngine;

public class LookDetector : MonoBehaviour
{
    public Transform head;       // player camera
    public Transform target;     // dove (or whatever)
    public float dotThreshold = 0.9f; // 1 = perfect forward, 0.9 = ~25° cone
    private MovementEventHub hub;
    private bool wasLooking;

    void Awake()
    {
        hub = GetComponentInParent<MovementEventHub>();
        if (!head) head = Camera.main ? Camera.main.transform : head;
    }

    void Update()
    {
        if (!head || !target) return;

        Vector3 toTarget = (target.position - head.position).normalized;
        float dot = Vector3.Dot(head.forward, toTarget);
        bool isLooking = dot > dotThreshold;
        if (isLooking)
        {
            hub?.RaiseLookTick(Time.deltaTime);
        }
        else
        {
            hub?.RaiseLookEnd();
        }

        wasLooking = isLooking;
    }
}
