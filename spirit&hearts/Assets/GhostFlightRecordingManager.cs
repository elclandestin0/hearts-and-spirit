using System.Collections.Generic;
using UnityEngine;

public class GhostRecordingManager : MonoBehaviour
{
    [Header("Recorders to Manage")]
    [SerializeField] private List<GhostFlightRecorder> recorders = new List<GhostFlightRecorder>();

    [Header("Auto-Trigger Options")]
    [SerializeField] private bool autoStartOnPlay = false;
    [SerializeField] private bool autoSaveOnQuit = true;

    private bool isRecording = false;

    public static GhostRecordingManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (autoStartOnPlay)
            StartAllRecordings();
    }

    public void StartAllRecordings()
    {
        if (isRecording) return;

        foreach (var recorder in recorders)
        {
            if (recorder != null)
                recorder.BeginRecording();
        }

        isRecording = true;
        Debug.Log("🟥 Recording started via GhostRecordingManager.");
    }

    public void StopAllRecordings()
    {
        isRecording = false;
        Debug.Log("⏹️ Recording stopped manually.");
    }

    public void SaveAllRecordings()
    {
        foreach (var recorder in recorders)
        {
            if (recorder != null)
                recorder.SaveToFile();
        }

        Debug.Log("💾 All ghost recordings saved.");
    }

    void OnApplicationQuit()
    {
        if (autoSaveOnQuit && isRecording)
        {
            SaveAllRecordings();
        }
    }
}
