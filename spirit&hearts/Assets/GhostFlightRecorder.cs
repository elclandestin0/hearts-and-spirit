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

    [SerializeField] private bool startRecord = false;

    private List<GhostFlightPlayback.FlightFrame> frames = new List<GhostFlightPlayback.FlightFrame>();
    private float timer = 0f;
    private float nextRecord = 0f;
    private bool isRecording = false;

    void Start()
    {
        if (!startRecord) return;

        frames.Clear();
        timer = 0f;
        nextRecord = 0f;
        isRecording = true;
        Debug.Log("Recording started.");
    }

    void Update()
    {
        if (!isRecording) return;

        timer += Time.deltaTime;

        if (timer >= nextRecord)
        {
            frames.Add(new GhostFlightPlayback.FlightFrame
            {
                headPosition = head.position,
                headRotation = head.rotation,
                leftHandPosition = leftHand.position,
                leftHandRotation = leftHand.rotation,
                rightHandPosition = rightHand.position,
                rightHandRotation = rightHand.rotation
            });

            nextRecord += 1f / recordRate;
        }

        if (timer >= recordDuration)
        {
            SaveToFile();
            isRecording = false;
            Debug.Log("Ghost flight recording finished.");
        }
    }

    private void SaveToFile()
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
}
