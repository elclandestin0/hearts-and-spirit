using UnityEngine;

public class SeedUseController : MonoBehaviour
{
    [Header("Refs")]
    public Transform seedHolster;                 // assign in Inspector (a small anchor near chest)
    public GameObject courierPrefab;              // visual seed that flies to lights
    public DovinaAudioManager doveAudio;          // optional

    [Header("Light Seeking")]
    public float lightSeekRadius = 60f;
    public string lightTag = "Light";

    // internal kk 
    private bool _courierActive = false;
    private MovementEventHub _hub;
    private SeedInventory _inv;
    private AmbientLightManager _ambient;
    private TutorialManager _tutorial;

    // kw
    void Awake()
    {
        _inv = GetComponent<SeedInventory>();
        _hub = GetComponent<MovementEventHub>();
        _ambient = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
        _tutorial = GetComponent<TutorialManager>();
        if (!seedHolster) seedHolster = transform.Find("SeedHolster");
        if (!doveAudio) doveAudio = GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
    }

    void Update()
    {
        Debug.Log("Courier active? " + _courierActive);
        Debug.Log("Inventory? " + _inv == null + " Seed Count? + " + _inv.GetSeedCount());
        Debug.Log("Seed Count? " + _inv.GetSeedCount());
        if (_courierActive) return;
        if (_inv == null || _inv.GetSeedCount() <= 0) return;
        Debug.Log("Courier not active. Inventory not null. We got " + _inv.GetSeedCount() + " seed.");

        // Find a nearby, unlit light
        GameObject[] lights = GameObject.FindGameObjectsWithTag(lightTag);
        Transform target = null;
        float best = float.PositiveInfinity;

        foreach (var light in lights)
        {
            var lc = light.GetComponent<LightController>() 
                  ?? light.GetComponentInParent<LightController>() 
                  ?? light.GetComponentInChildren<LightController>();
            if (lc == null || lc.isLit) continue;

            float d = Vector3.Distance(transform.position, light.transform.position);
            if (d < lightSeekRadius && d < best)
            {
                best = d;
                target = light.transform;
            }
        }

        if (target != null)
            LaunchCourierTo(target);
    }

    private void LaunchCourierTo(Transform targetLight)
    {
        Debug.Log("Found target.");
        if (courierPrefab == null || seedHolster == null) return;
        Debug.Log("Launching courier. Checking seeds.");
        // consume first to make intent clear
        if (_inv.GetSeedCount() <= 0) return;
        Debug.Log("We got a seed babeh.");
        _inv.RemoveSeed();

        var courierGO = Instantiate(courierPrefab, seedHolster.position, seedHolster.rotation);
        var courier = courierGO.AddComponent<SeedCourier>();
        courier.Init(
            targetLight,
            onArrived: () =>
            {
                // light it
                var lc = targetLight.GetComponent<LightController>() 
                      ?? targetLight.GetComponentInParent<LightController>()
                      ?? targetLight.GetComponentInChildren<LightController>();

                if (lc != null && !lc.isLit)
                {
                    lc.isLit = true;
                    if (_tutorial == null || !_tutorial.IsRunning)
                    {
                        doveAudio?.PlayPriority("gp_changes/light", 2, 1, 12);
                        doveAudio?.PlayPriority("parables", 4, 0, 999);
                        _ambient?.UpdateAmbientLight();
                    }

                    _hub?.RaiseLightLit();
                    var pid = lc.GetComponent<PersistentId>();
                    var playerRoot = transform;
                    ProgressManager.Instance?.MarkLastLit(pid, playerRoot);
                    SaveManager.Save("slot1");
                }

                _courierActive = false;
            },
            onBegin: () => { _courierActive = true; },
            onDestroyed: () => { _courierActive = false; }
        );
    }
}
