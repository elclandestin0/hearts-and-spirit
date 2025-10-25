using UnityEngine;

public class SeedBehavior : MonoBehaviour
{
    public enum State { Idle, AttractToPlayer, AttachedToPlayer, MoveToLight }
    public State currentState = State.Idle;
    public bool IsHomingToLight => currentState == State.MoveToLight && currentLightTarget != null;
    [HideInInspector] public bool externallyControlled = false;
    [SerializeField] private float minTurnDegPerSec = 80f;   // gentle curl when slow
    [SerializeField] private float turnDegPerSecPerMs = 40f; // extra deg/sec per 1 m/s of speed
    private Transform player;
    private Transform currentLightTarget;
    [Header("Attraction Settings")]
    [SerializeField] private float attractionRadius;
    [SerializeField] private float attachDistance;
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private Transform seedHolster;

    [Header("MoveToLight Homing")]
    // put with your other fields
    [SerializeField] private float moveBlendGain = 6f;
    [SerializeField] private float attractAccel = 100f;
    [SerializeField] private float attractDecel = 20f;
    [SerializeField] private float homingMinKick = 6f;
    [SerializeField] private float homingMinCruise = 30f;
    [Header("MoveToLight Speed By Proximity")]
    [SerializeField] private float homingMaxSpeed = 500f;
    [SerializeField] private float homingAccel = 500f;
    [SerializeField] private float homingCaptureRadius = 200f;
    [SerializeField] private float baseTurnDegPerSec = 10f;
    [SerializeField] private float maxTurnDegPerSec = 30f;
    [SerializeField] private float curveStrengthAt1 = 2.0f;
    [SerializeField] private float curvePerMs = 0.08f;
    [SerializeField] float minInwardSpeedNear = 6f;
    private AnimationCurve homingSpeedByProximity =
        AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("Light Seeking Settings")]
    [SerializeField] private float lightSeekRadius;
    [SerializeField] private string lightTag = "Light";
    [SerializeField] private AmbientLightManager lightManager;
    [SerializeField] private MovementEventHub _hub;
    private LightController light;
    private DovinaAudioManager dovinaAudioManager;
    private TutorialManager tutorialManager;
    private float pickupDisabledUntil = 0f;
    private float outsideTimer = 0f;
    private SeedMotion motion;
    private int orbitSign = +1;

    void Start()
    {
        player = GameObject.FindWithTag("MainPlayer")?.transform;
        seedHolster = GameObject.FindWithTag("Player").transform.Find("SeedHolster");
        lightManager = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
        dovinaAudioManager = GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
        _hub = GameObject.FindWithTag("MainPlayer")?.GetComponent<MovementEventHub>();
        tutorialManager = GameObject.FindWithTag("MainPlayer")?.GetComponent<TutorialManager>();
        motion = GetComponent<SeedMotion>();
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
                float dt = Time.deltaTime;
                bool held = Input.GetKey(KeyCode.C);
                float distance = Vector3.Distance(transform.position, player.position);

                if (!held || distance >= attractionRadius)
                {
                    currentState = State.Idle;
                    break;
                }

                if (held && motion)
                {
                    Vector3 toHolster = seedHolster.position - transform.position;
                    float d = toHolster.magnitude;
                    if (d > 0.001f)
                    {
                        Vector3 targetDir = toHolster / d;
                        Vector3 curDir = (motion.Velocity.sqrMagnitude > 1e-6f) ? motion.Velocity.normalized : targetDir;

                        Vector3 blendedDir = Vector3.Slerp(curDir, targetDir, dt * moveBlendGain);

                        // accelerate while held (cap to maxSpeed)
                        float speedNow = motion.Velocity.magnitude;
                        float speedNext = Mathf.MoveTowards(speedNow, maxSpeed, attractAccel * dt);
                        motion.Velocity = blendedDir * speedNext;
                    }
                    CheckForAttachment();
                }
                else if (motion)
                {
                    float speed = motion.Velocity.magnitude - attractDecel * dt;
                    if (speed <= 0f)
                    {
                        motion.Velocity = Vector3.zero;
                        currentState = State.Idle;
                    }
                    else
                    {
                        Vector3 keepDir = motion.Velocity.sqrMagnitude > 1e-6f ? motion.Velocity.normalized : transform.forward;
                        motion.Velocity = keepDir * speed;
                    }
                }
                break;



            case State.AttachedToPlayer:
                SearchForNearbyLight();
                break;

            case State.MoveToLight:
                {
                    if (currentLightTarget == null || motion == null) break;

                    Vector3 toTarget = currentLightTarget.position - transform.position;
                    float dist = toTarget.magnitude;

                    // outside seek radius timer → Idle fallback
                    if (dist > lightSeekRadius) outsideTimer += Time.deltaTime;
                    else outsideTimer = 0f;

                    if (outsideTimer > 20f)
                    {
                        currentLightTarget = null;
                        currentState = State.Idle;
                        outsideTimer = 0f;
                        motion.Velocity *= 0.5f;      // optional damp
                        return;                       // leave Update() cleanly
                    }

                    // normal homing until we hit the center zone
                    if (dist > 10f)
                    {
                        // toward-light unit
                        Vector3 rHat = toTarget / dist;

                        // current velocity state
                        Vector3 v = motion.Velocity;
                        float speed = v.magnitude;
                        Vector3 vDir = (speed > 1e-6f) ? v / speed : rHat;

                        // proximity: 0 at radius edge → 1 near center
                        float tNear = 1f - Mathf.InverseLerp(0f, lightSeekRadius, dist);

                        // preserve winding
                        Vector3 upRef = Vector3.up; // swap to planet-up if needed
                        float wind = Mathf.Sign(Vector3.Dot(Vector3.Cross(toTarget, v), upRef));
                        if (wind == 0f) wind = 1f;
                        Vector3 swirlDir = Vector3.Cross(rHat, upRef).normalized * wind; // tangential

                        // swirl fades near center, inward pull grows near center
                        float swirlAmt = (curveStrengthAt1 + curvePerMs * speed) * (1f - tNear);
                        float pullBase = 1.5f, pullPerMs = 0.06f;
                        float pullAmt = (pullBase + pullPerMs * speed) * tNear;

                        // desired direction = inward + swirl (with inward bias)
                        Vector3 desiredDir = (rHat * pullAmt + swirlDir * swirlAmt).normalized;
                        float coneBias = Mathf.Lerp(0.15f, 0.7f, tNear);                // keep some "toward" component
                        desiredDir = Vector3.Normalize(Vector3.Lerp(desiredDir, rHat, coneBias));

                        // turn allowance (faster near target)
                        float turnDegSec = Mathf.Lerp(baseTurnDegPerSec, maxTurnDegPerSec, Mathf.Pow(tNear, 0.6f));
                        float maxRadStep = turnDegSec * Mathf.Deg2Rad * Time.deltaTime;
                        Vector3 newDir = Vector3.RotateTowards(vDir, desiredDir, maxRadStep, 0f);

                        // speed increases as we approach
                        float targetSpeed = Mathf.Lerp(homingMinCruise, homingMaxSpeed, tNear);
                        float newSpeed = Mathf.MoveTowards(speed, targetSpeed, homingAccel * Time.deltaTime);

                        // build velocity, then enforce inward convergence
                        Vector3 newV = newDir * newSpeed;

                        // remove any outward radial component
                        float vRad = Vector3.Dot(newV, rHat);               // >0 toward light
                        if (vRad < 0f) newV -= rHat * vRad;

                        // ensure a minimum inward component that ramps up near the center
                        const float minInwardSpeedNear = 8f;                 // tweak or serialize
                        float minInward = Mathf.Lerp(0f, minInwardSpeedNear, tNear);
                        vRad = Vector3.Dot(newV, rHat);
                        if (vRad < minInward) newV += rHat * (minInward - vRad);

                        motion.Velocity = newV;
                    }
                    else
                    {
                        // arrive & light
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
                            return;
                        }
                    }
                    break;
                }
        }
    }

    public void BeginMoveToLight(Transform target)
    {
        if (target == null) return;
        transform.SetParent(null);
        currentLightTarget = target;
        currentState = State.MoveToLight;
        // capture winding sign from current angular momentum
        Vector3 r = target.position - transform.position;
        Vector3 v = (motion ? motion.Velocity : Vector3.zero);
        Vector3 upRef = Vector3.up;
        if (r.sqrMagnitude > 0.0001f && v.sqrMagnitude > 0.0001f)
            orbitSign = (int)Mathf.Sign(Vector3.Dot(Vector3.Cross(r, v), upRef));
        if (orbitSign == 0) orbitSign = +1;

        if (motion && motion.Velocity.sqrMagnitude < 1e-6f)
            motion.Velocity = r.normalized * homingMinKick;
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
