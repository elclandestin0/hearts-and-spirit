using UnityEngine;

public class HeadDebug : MonoBehaviour
{
    public Movement movement;

    public enum LookState
    {
        Forward,
        Up,
        Down
    }

    [Header("Simulated Look State")]
    public LookState currentState = LookState.Forward;

    [Header("Settings")]
    public float rotationSpeed = 45f; // degrees per second
    public float upAngle = -60f;
    public float downAngle = 60f;
    public float forwardAngle = 0f;

    private float targetPitch = 0f;

    void Update()
    {
        if (movement == null || movement.Head == null) return;

        switch (currentState)
        {
            case LookState.Up:
                targetPitch = upAngle;
                break;
            case LookState.Down:
                targetPitch = downAngle;
                break;
            case LookState.Forward:
            default:
                targetPitch = forwardAngle;
                break;
        }

        // Apply rotation smoothly
        Transform head = movement.Head;
        Vector3 currentEuler = head.localEulerAngles;

        float currentPitch = currentEuler.x;
        if (currentPitch > 180f) currentPitch -= 360f;

        float newPitch = Mathf.MoveTowards(currentPitch, targetPitch, rotationSpeed * Time.deltaTime);

        head.localEulerAngles = new Vector3(newPitch, currentEuler.y, currentEuler.z);
    }
}
