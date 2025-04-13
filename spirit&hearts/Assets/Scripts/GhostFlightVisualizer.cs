using UnityEngine;

public class GhostFlightVisualizer : MonoBehaviour
{
    [Header("Ghost Data")]
    public GhostFlightPlayback ghost;

    [Header("Visual Targets")]
    public Transform headTarget;
    public Transform leftWingTarget;
    public Transform rightWingTarget;

    [Header("Debug Settings")]
    public float velocityLineLength = 3f;

    void LateUpdate()
    {
        if (ghost == null || ghost.CurrentFrame == null)
            return;

        var frame = ghost.CurrentFrame.Value;

        // Update transforms
        headTarget.position = frame.headPosition;
        headTarget.rotation = frame.headRotation;

        leftWingTarget.position = frame.leftHandPosition;
        leftWingTarget.rotation = frame.leftHandRotation;

        rightWingTarget.position = frame.rightHandPosition;
        rightWingTarget.rotation = frame.rightHandRotation;

        // 🔴 Draw original recorded velocity
        Vector3 originalDir = frame.resultingVelocity.normalized * velocityLineLength;
        Debug.DrawLine(frame.headPosition, frame.headPosition + originalDir, Color.red, 0f, false);
        
        // Draw dive line
        Vector3 headForward = frame.headRotation * Vector3.forward;
        DrawDiveDebug(frame.headPosition, headForward);
    }

    void DrawDiveDebug(Vector3 headPos, Vector3 headForward)
    {
        // Always draw the forward direction from the head
        Debug.DrawRay(headPos, headForward * 2f, Color.white);

        // Draw world-down for reference
        Debug.DrawRay(headPos, Vector3.down * 2f, Color.gray);

        // Angle between head forward and world down
        float diveAngle = Vector3.Angle(headForward, Vector3.down);

        // Visualize when you're "diving"
        Color coneColor = diveAngle < 60f ? Color.cyan : Color.yellow;
        Debug.DrawRay(headPos, Quaternion.Euler(60f, 0f, 0f) * Vector3.down * 2f, coneColor);
        Debug.DrawRay(headPos, Quaternion.Euler(-60f, 0f, 0f) * Vector3.down * 2f, coneColor);

        Debug.Log($"[DIVE DEBUG] Angle to down: {diveAngle:F1}° — {(diveAngle < 60f ? "🦅 Diving!" : "🧍 No dive")}");
    }
}
