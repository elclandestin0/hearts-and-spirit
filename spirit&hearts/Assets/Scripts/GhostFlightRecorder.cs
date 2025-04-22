using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GhostFlightRecorder : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;
    private List<GhostFlightPlayback.FlightFrame> frames = new List<GhostFlightPlayback.FlightFrame>();
    private float timer = 0f;
    private float nextRecord = 0f;
    private bool isRecording = false;

    public void BeginRecording()
    {
        frames.Clear();
        timer = 0f;
        nextRecord = 0f;
        isRecording = true;
        Debug.Log("🎙️ Recorder triggered by Movement script.");
    }

    void OnApplicationQuit()
    {
        if (isRecording && frames.Count > 0)
        {
            SaveToFile();
            Debug.Log("💾 Auto-saved ghost flight on quit.");
        }
    }


public void SaveToFile()
{
    string folderPath = Application.dataPath + "/FlightRecords";
    if (!Directory.Exists(folderPath))
    {
        Directory.CreateDirectory(folderPath);
    }

    int fileIndex = 1;
    string filePath;

    // Keep incrementing until we find a new file name
    do
    {
        filePath = Path.Combine(folderPath, $"ghost_flight_{fileIndex:D3}.json");
        fileIndex++;
    } while (File.Exists(filePath));

    GhostFlightContainer container = new GhostFlightContainer();
    container.frames = frames.ToArray();
    string json = JsonUtility.ToJson(container, true);

    File.WriteAllText(filePath, json);
    Debug.Log("💾 Saved ghost flight to: " + filePath);
}


    [System.Serializable]
    private class GhostFlightContainer
    {
        public GhostFlightPlayback.FlightFrame[] frames;
    }

    public void RecordFrame(
        Vector3 headPos, Quaternion headRot,
        Vector3 leftPos, Quaternion leftRot,
        Vector3 rightPos, Quaternion rightRot,
        Vector3 leftVel, Vector3 rightVel,
        float flapMagnitude, Vector3 finalVelocity)
    {
        if (!isRecording) return;

        frames.Add(new GhostFlightPlayback.FlightFrame
        {
            headPosition = headPos,
            headRotation = headRot,

            leftHandPosition = leftPos,
            leftHandRotation = leftRot,
            leftHandVelocity = leftVel,

            rightHandPosition = rightPos,
            rightHandRotation = rightRot,
            rightHandVelocity = rightVel,

            flapMagnitude = flapMagnitude,

            resultingVelocity = finalVelocity
        });
    }

}
