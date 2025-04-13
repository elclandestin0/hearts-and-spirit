using System.Collections.Generic;
using UnityEngine;

public class GhostFlightPlayback : MonoBehaviour
{
    [System.Serializable]
    public struct FlightFrame
    {
        public Vector3 headPosition;
        public Quaternion headRotation;

        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 leftHandVelocity;

        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
        public Vector3 rightHandVelocity;

        public float flapMagnitude;
        public Vector3 resultingVelocity;
    }


    [Header("Ghost Settings")]
    [SerializeField] private TextAsset flightDataFile; // drag in a JSON file here
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool loop = true;

    public Vector3 headPos { get; private set; }
    public Vector3 leftHandPos { get; private set; }
    public Vector3 rightHandPos { get; private set; }
    public Quaternion headRot { get; private set; }
    public Quaternion leftHandRot { get; private set; }
    public Quaternion rightHandRot { get; private set; }


    private List<FlightFrame> frames = new List<FlightFrame>();
    private float playbackTime = 0f;
    private float frameRate = 60f;
    public int frameIndex = 0;

    void Start()
    {
        LoadFlightData();
    }

    void Update()
    {
        if (frames.Count == 0) return;

        playbackTime += Time.deltaTime * playbackSpeed;
        frameIndex = Mathf.FloorToInt(playbackTime * frameRate);

        if (frameIndex >= frames.Count)
        {
            if (loop)
            {
                playbackTime = 0f;
                frameIndex = 0;
            }
            else
            {
                frameIndex = frames.Count - 1;
            }
        }

        FlightFrame frame = frames[frameIndex];

        headPos = frame.headPosition;
        headRot = frame.headRotation;

        leftHandPos = frame.leftHandPosition;
        leftHandRot = frame.leftHandRotation;

        rightHandPos = frame.rightHandPosition;
        rightHandRot = frame.rightHandRotation;
    }

    private void LoadFlightData()
    {
        if (flightDataFile == null)
        {
            Debug.LogWarning("No flight data file assigned.");
            return;
        }

        string json = flightDataFile.text;
        GhostFlightRecording recording = JsonUtility.FromJson<GhostFlightRecording>(json);
        frames = new List<FlightFrame>(recording.frames);
        Debug.Log($"Loaded {frames.Count} ghost frames.");
    }

    [System.Serializable]
    private class GhostFlightRecording
    {
        public FlightFrame[] frames;
    }

    public FlightFrame? CurrentFrame
    {
        get
        {
            if (frames.Count == 0 || frameIndex >= frames.Count)
                return null;

            return frames[frameIndex];
        }
    }

}
