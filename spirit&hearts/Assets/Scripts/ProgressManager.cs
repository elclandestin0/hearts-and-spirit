using UnityEngine;

public class ProgressManager : MonoBehaviour, ISaveable
{
    public static ProgressManager Instance { get; private set; }

    // Fixed so the singleton's saved state matches itself across sessions.
    // A runtime-created PersistentId would otherwise get a fresh GUID each run.
    private const string SingletonSaveId = "progressmanager00000000000000001";

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

    /// <summary>
    /// Nothing in any scene references this script, so without a bootstrap the
    /// singleton is never created and every ProgressManager.Instance call site
    /// silently no-ops through its null-conditional.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<ProgressManager>() != null) return;

        var go = new GameObject("[ProgressManager]");
        // Order matters: PersistentId and the ISaveable must exist before
        // SaveableEntity's Awake collects them.
        go.AddComponent<PersistentId>().SetIdIfEmpty(SingletonSaveId);
        go.AddComponent<ProgressManager>();
        go.AddComponent<SaveableEntity>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Resolved lazily so a runtime-created instance still finds the rig.
    private Transform RigRoot
    {
        get
        {
            if (rigRoot == null)
            {
                var player = GameObject.FindWithTag("MainPlayer");
                if (player != null) rigRoot = player.transform;
            }
            return rigRoot;
        }
    }

    // Call this right when a light turns on
    public void MarkLastLit(PersistentId light, Transform playerRoot)
    {
        if (light == null || playerRoot == null) return;
        lastLightId = light.Id;
        spawnPos = playerRoot.position;
        spawnRot = playerRoot.rotation;
        GameLog.Log($"[Progress] Checkpoint at light {lastLightId} ({spawnPos}).");
    }

    // Used after Load to place the player at the saved spot
    public void ApplySpawnIfAny()
    {
        if (string.IsNullOrEmpty(lastLightId)) return;

        var root = RigRoot;
        if (root == null)
        {
            Debug.LogWarning("[Progress] No object tagged 'MainPlayer' to respawn.");
            return;
        }

        root.position = spawnPos;
        root.rotation = spawnRot;
    }

    // ---- ISaveable ----
    public object CaptureState() => new State { lastLightId = lastLightId, spawnPos = spawnPos, spawnRot = spawnRot };

    public void RestoreState(object stateObj)
    {
        if (!(stateObj is State s)) return;
        lastLightId = s.lastLightId;
        spawnPos    = s.spawnPos;
        spawnRot    = s.spawnRot;
    }
}
