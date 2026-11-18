// Empty versions of game classes that Movement.cs references but which have nothing to do with flap physics.
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    // True keeps Movement from reaching for the voice-line manager, which the tests leave unset.
    public bool IsRunning => true;
}

public class DovinaAudioManager : MonoBehaviour
{
    public void PlayPriority(string category, int priority, int startIndex = 0, int endIndexInclusive = -1) { }
}

public class SplineWindZone : MonoBehaviour
{
    public Vector3 GetWindForceAtPosition(Vector3 position) => Vector3.zero;
}

public class GhostFlightRecorder : MonoBehaviour
{
    public void RecordFrame(
        Vector3 headPos, Quaternion headRot,
        Vector3 leftPos, Quaternion leftRot,
        Vector3 rightPos, Quaternion rightRot,
        Vector3 leftVel, Vector3 rightVel,
        float flapMagnitude, Vector3 finalVelocity)
    { }
}
