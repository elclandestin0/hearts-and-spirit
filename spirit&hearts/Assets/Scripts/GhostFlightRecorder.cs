using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GhostFlightRecorder : MonoBehaviour
{
    [Header("XR References")]
    public Transform leftHand;
    public Transform rightHand;
    public Transform head;

    [Header("Record Controls")]
    public float recordDuration = 60f;
    public float recordRate = 60f;

    private List<GhostFlightPlayback.FlightFrame> frames = new List<GhostFlightPlayback.FlightFrame>();
    private bool startRecord = false;
    private float timer = 0f;
    private float nextRecord = 0f;
    private bool isRecording = false;
    private float autosaveTimer = 0f;
    private float autosaveInterval = 10f;

    void Update()
    {

        if (isRecording)
        {
            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autosaveInterval)
            {
                SaveToFile(); // Overwrite file, or version it
                autosaveTimer = 0f;
            }
        }

    }

    public void BeginRecording()
    {
        frames.Clear();
        timer = 0f;
        nextRecord = 0f;
        isRecording = true;
        Debug.Log("🎙️ Recorder triggered by HandFlapMovement.");
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
        string path = Application.dataPath + "/ghost_flight.json";

        GhostFlightContainer container = new GhostFlightContainer();
        container.frames = frames.ToArray();
        string json = JsonUtility.ToJson(container, true);

        File.WriteAllText(path, json);
        Debug.Log("Saved ghost flight to: " + path);
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
