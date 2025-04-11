using UnityEngine;

public class GhostFlightVisualizer : MonoBehaviour
{
    [Header("Ghost Data")]
    public GhostFlightPlayback ghost;

    [Header("Visual Targets")]
    public Transform headTarget;
    public Transform leftWingTarget;
    public Transform rightWingTarget;

    void LateUpdate()
    {
        if (ghost == null || ghost.CurrentFrame == null)
            return;

        var frame = ghost.CurrentFrame.Value;

        headTarget.position = frame.headPosition;
        headTarget.rotation = frame.headRotation;

        leftWingTarget.position = frame.leftHandPosition;
        leftWingTarget.rotation = frame.leftHandRotation;

        rightWingTarget.position = frame.rightHandPosition;
        rightWingTarget.rotation = frame.rightHandRotation;
    }
}
