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

        // 🟢 Simulate corrected velocity from posture
        Vector3 correctedVelocity = SimulateVelocityFromFrame(frame);
        Vector3 correctedDir = correctedVelocity.normalized * velocityLineLength;
        Debug.DrawLine(frame.headPosition, frame.headPosition + correctedDir, Color.green, 0f, false);
    }

    Vector3 SimulateVelocityFromFrame(GhostFlightPlayback.FlightFrame frame)
    {
        Vector3 simulated = Vector3.zero;

        Vector3 headFwd = frame.headRotation * Vector3.forward;
        float flapMagnitude = frame.flapMagnitude;

        // Reapply flap-based lift + thrust
        float flapStrength = 0.35f;
        float forwardThrust = 0.5f;

        simulated += Vector3.up * flapStrength * flapMagnitude;
        simulated += headFwd * forwardThrust * flapMagnitude;

        return simulated;
    }
}
