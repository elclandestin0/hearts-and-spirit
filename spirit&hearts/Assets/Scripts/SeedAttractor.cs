using UnityEngine;

public class SeedBehavior : MonoBehaviour
{
    public enum State { Idle, AttractToPlayer, AttachedToPlayer, MoveToLight }
    public State currentState = State.Idle;
    [HideInInspector] public bool externallyControlled = false;

    private Transform player;
    private Transform currentLightTarget;
    [Header("Attraction Settings")]
    [SerializeField] private float attractionRadius;
    [SerializeField] private float attachDistance;
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private Transform seedHolster;
    [Header("Light Seeking Settings")]
    [SerializeField] private float lightSeekRadius;
    [SerializeField] private string lightTag = "Light";
    [SerializeField] private AmbientLightManager lightManager;
    [SerializeField] private MovementEventHub _hub;
    private LightController light;
    private DovinaAudioManager dovinaAudioManager;
    private TutorialManager tutorialManager;
    private float pickupDisabledUntil = 0f;
    void Start()
    {
        player = GameObject.FindWithTag("MainPlayer")?.transform;
        seedHolster = GameObject.FindWithTag("Player").transform.Find("SeedHolster");
        lightManager = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
        dovinaAudioManager = GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
        _hub = GameObject.FindWithTag("MainPlayer")?.GetComponent<MovementEventHub>();
        tutorialManager = GameObject.FindWithTag("MainPlayer")?.GetComponent<TutorialManager>();
    }

    void Update()
    {
        if (externallyControlled) return;
        if (PauseState.IsPaused) return;
        if (player == null) return;

        switch (currentState)
        {
            case State.Idle:
                CheckForPlayerProximity();
                break;

            case State.AttractToPlayer:
                float distance = Vector3.Distance(transform.position, player.position);
                // If C is no longer held OR we left the radius, stop attracting
                if (!Input.GetKey(KeyCode.C) || distance >= attractionRadius)
                {
                    currentState = State.Idle;
                    break;
                }

                MoveToward(seedHolster);
                CheckForAttachment();
                break;


            case State.AttachedToPlayer:
                SearchForNearbyLight();
                break;

            case State.MoveToLight:
                if (currentLightTarget != null)
                {
                    MoveToward(currentLightTarget);

                    float d = Vector3.Distance(transform.position, currentLightTarget.position);
                    if (d <= 10f)
                    {
                        // *** your existing lighting block (unchanged) ***
                        LightController light = currentLightTarget.GetComponent<LightController>()
                                                ?? currentLightTarget.GetComponentInParent<LightController>()
                                                ?? currentLightTarget.GetComponentInChildren<LightController>();
                        if (light != null && !light.isLit)
                        {
                            light.isLit = true;

                            var _player = GameObject.FindWithTag("Player");
                            _player.GetComponent<ItemManager>().PlayLightSound();
                            if (!tutorialManager.IsRunning)
                            {
                                dovinaAudioManager.PlayPriority("gp_changes/light", 2, 1, 12);
                                dovinaAudioManager.PlayPriority("parables", 4, 0, 999);
                                lightManager.UpdateAmbientLight();
                            }

                            _hub.RaiseLightLit();
                            var pid = light.GetComponent<PersistentId>();
                            var playerRoot = GameObject.FindWithTag("MainPlayer").transform;
                            ProgressManager.Instance?.MarkLastLit(pid, playerRoot);
                            SaveManager.Save("slot1");
                            Destroy(this.gameObject);
                        }
                    }
                }
                break;
        }
    }

    public void BeginMoveToLight(Transform target)
    {
        if (target == null) return;
        transform.SetParent(null);
        currentLightTarget = target;
        currentState = State.MoveToLight;
    }

    public bool BeginSeekNearestUnlit(float searchRadius = 300f)
    {
        Transform nearest = FindNearestUnlitLight(searchRadius);
        if (nearest == null) return false;
        BeginMoveToLight(nearest);
        return true;
    }

    private Transform FindNearestUnlitLight(float radius)
    {
        GameObject[] lights = GameObject.FindGameObjectsWithTag(lightTag);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var go in lights)
        {
            var lc = go.GetComponent<LightController>()
                  ?? go.GetComponentInParent<LightController>()
                  ?? go.GetComponentInChildren<LightController>();
            if (lc == null || lc.isLit) continue;

            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d <= radius && d < bestDist)
            {
                best = go.transform;
                bestDist = d;
            }
        }
        return best;
    }

    public void DisablePickupFor(float seconds)
    {
        pickupDisabledUntil = Time.time + seconds;
    }

    private void CheckForAttachment()
    {
        if (Time.time < pickupDisabledUntil) return;
        float distance = Vector3.Distance(transform.position, player.position);
        var inv = player.GetComponent<SeedInventory>();

        if (distance <= attachDistance)
        {
            inv ??= player.gameObject.AddComponent<SeedInventory>();
            inv.AddSeed();

            var _player = GameObject.FindGameObjectWithTag("Player");
            var items = _player.GetComponent<ItemManager>();
            Debug.Log("About to play pick-up sound.");
            items.PlayPickUpSound();
            var dove = dovinaAudioManager ?? GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
            dove?.PlayPriority("gp_changes/seed", 2, 2, 14);

            // 2) Fire the tutorial/event hub
            _hub.RaiseSeedPicked();

            // 3) REMOVE the world pickup object (don't keep it in holster)
            Destroy(gameObject);
        }
    }

    private void CheckForPlayerProximity()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < attractionRadius && Input.GetKey(KeyCode.C))
        {
            currentState = State.AttractToPlayer;
        }
        else
        {
            currentState = State.Idle;
        }
    }


    private void SearchForNearbyLight()
    {
        GameObject[] lightSources = GameObject.FindGameObjectsWithTag(lightTag);
        transform.rotation = Quaternion.Euler(-90, 0f, 0f);
        Transform closest = null;
        foreach (GameObject light in lightSources)
        {
            float d = Vector3.Distance(transform.position, light.transform.position);
            if (d < lightSeekRadius)
            {
                closest = light.transform;
            }
        }

        if (closest != null)
        {
            currentLightTarget = closest;
            transform.SetParent(null);
            currentState = State.MoveToLight;
        }
    }


    private void MoveToward(Transform target)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        float t = Mathf.InverseLerp(attractionRadius, 0f, distance); // closer = faster
        float speed = Mathf.Lerp(minSpeed, maxSpeed, t);

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }
}
