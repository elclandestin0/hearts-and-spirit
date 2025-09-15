using UnityEngine;

public class ProgressManager : MonoBehaviour, ISaveable
{
    public static ProgressManager Instance { get; private set; }

    [System.Serializable]
    private struct State
    {
        public string lastLightId;
        public Vector3 spawnPos;
        public Quaternion spawnRot;
    }

    [SerializeField] private Transform rigRoot; // XROrigin or your player root

    private string lastLightId;
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this right when a light turns on
    public void MarkLastLit(PersistentId light, Transform playerRoot)
    {
        if (light == null || playerRoot == null) return;
        lastLightId = light.Id;
        spawnPos = playerRoot.position;
        spawnRot = playerRoot.rotation;
    }

    // Used after Load to place the player at the saved spot
    public void ApplySpawnIfAny()
    {
        if (rigRoot == null) return;
        if (string.IsNullOrEmpty(lastLightId)) return;

        rigRoot.position = spawnPos;
        rigRoot.rotation = spawnRot;
    }

    // ---- ISaveable ----
    public object CaptureState() => new State { lastLightId = lastLightId, spawnPos = spawnPos, spawnRot = spawnRot };

    public void RestoreState(object stateObj)
    {
        var s = (State)stateObj;
        lastLightId = s.lastLightId;
        spawnPos   = s.spawnPos;
        spawnRot   = s.spawnRot;
    }
}
