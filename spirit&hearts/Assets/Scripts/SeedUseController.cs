using UnityEngine;

public class SeedUseController : MonoBehaviour
{
    [Header("Refs")]
    public Transform seedHolster;                 // assign in Inspector (a small anchor near chest)
    public GameObject courierPrefab;              // visual seed that flies to lights
    public DovinaAudioManager doveAudio;          // optional
    [SerializeField] private float launchDuration = 1.0f;
    [SerializeField] private float launchSpeed = 500f;
    [SerializeField] private float arriveDistance = 10f;   // “counts as hit” radius
    [SerializeField] private Transform head;
    [SerializeField] private TutorialManager _tutorial;
    [Header("Light Seeking")]
    public string lightTag = "Light";
    private bool _courierActive = false;
    private MovementEventHub _hub;
    private SeedInventory _inv;
    private AmbientLightManager _ambient;
    private bool _isLaunchingForward = false;
    void Awake()
    {
        _inv = GetComponent<SeedInventory>();
        _hub = GetComponent<MovementEventHub>();
        _ambient = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
        if (!seedHolster) seedHolster = transform.Find("SeedHolster");
        if (!doveAudio) doveAudio = GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
    }

    void Update()
    {
        if (PauseState.IsPaused) return;

        if (DebugKeys.Pressed(KeyCode.X))
            LaunchCourierForward();
    }

    /// <summary>Raised on a successful local launch so the net layer can replicate it. (origin, dir)</summary>
    public static event System.Action<Vector3, Vector3> OnLocalSeedLaunched;

    private void LaunchCourierForward()
    {
        if (courierPrefab == null || seedHolster == null) return;
        if (_inv == null || _inv.GetSeedCount() <= 0) return;
        _inv.RemoveSeed();

        Vector3 dir = head.forward;
        SpawnCourier(seedHolster.position, dir);
        OnLocalSeedLaunched?.Invoke(seedHolster.position, dir);
    }

    /// <summary>Spawn a courier that a remote player launched — same prefab, same params, same physics.</summary>
    public void SpawnRemoteCourier(Vector3 origin, Vector3 dir) => SpawnCourier(origin, dir);

    private void SpawnCourier(Vector3 origin, Vector3 launchDir)
    {
        var courierGO = Instantiate(courierPrefab, origin, courierPrefab.transform.rotation);

        var seedBehavior = courierGO.GetComponent<SeedBehavior>();
        if (seedBehavior != null)
        {
            seedBehavior.DisablePickupFor(5f);
            seedBehavior.externallyControlled = true;
        }

        var courier = courierGO.AddComponent<SeedCourier>();
        courier.InitForward(
            dir: launchDir,
            duration: launchDuration,
            speed: launchSpeed,
            arriveDistance: arriveDistance,
            dove: doveAudio,
            ambient: _ambient,
            hub: _hub,
            tutorial: _tutorial,
            playerRoot: transform,
            onBegin: () => { _courierActive = true; },
            onDestroyed: () => { _courierActive = false; }
        );
    }
}