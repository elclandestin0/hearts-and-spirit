using UnityEngine;

public class SeedBehavior : MonoBehaviour
{
    public enum State { Idle, AttractToPlayer, AttachedToPlayer, MoveToLight, WorldGravity }
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
    [Header("Light Capture Aggression")]
    [SerializeField] private float commitFrac = 0.1f;
    [SerializeField] private float closeCaptureRadius = 25f;  // "thrown just a little away"
    [SerializeField] private float farAggression = 0.35f; // how weak convergence is when far
    [SerializeField] private float nearAggression = 1.0f;  // full aggression when close
    private bool enteredClose = false;
    private float moveToLightEnterDist = 0f;
    private float moveToLightEnterSpeed = 0f;

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

    [Header("World Gravity")]
    [SerializeField] private Transform worldCenter;
    [SerializeField] private float innerOrbit = 2500f;
    [SerializeField] private float outerOrbit = 4000f;
    [SerializeField] private float orbitSpeedInner = 500f;   // fast when near inner radius
    [SerializeField] private float orbitSpeedOuter = 300f;    // slow when near outer radius
    [SerializeField] private float radialOscPeriod = 12f;   // seconds for one in→out→in cycle
    [SerializeField] private float radialKp = 1.2f;         // radius spring (0.6–2)
    [SerializeField] private float radialKd = 4.0f;         // radial damping (2–8)
    [SerializeField] private float tangentAlignGain = 6f;   // 3–12
    [SerializeField] private float tangentAccel = 20f;      // 8–30
    [SerializeField] private float worldRecaptureRadius = 120f;

    private float worldOscStartTime;
    private float worldOrbitPhase; // per-seed phase so multiple seeds don't sync
    private bool committedToLight = false;
    private float commitRadius = 0f; // computed as lightSeekRadius * commitFrac

    void Start()
    {
        player = GameObject.FindWithTag("MainPlayer")?.transform;
        seedHolster = GameObject.FindWithTag("Player").transform.Find("SeedHolster");
        lightManager = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
        dovinaAudioManager = GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
        _hub = GameObject.FindWithTag("MainPlayer")?.GetComponent<MovementEventHub>();
        tutorialManager = GameObject.FindWithTag("MainPlayer")?.GetComponent<TutorialManager>();
        motion = GetComponent<SeedMotion>();
        worldCenter = GameObject.FindWithTag("Mountain")?.transform;
    }

    void Update()
    {
        if (externallyControlled) return;
        if (PauseState.IsPaused) return;
        if (player == null) return;
        if (currentState != State.AttachedToPlayer && player != null && Input.GetKey(KeyCode.C))
        {
            Debug.Log("Attempting to attract seed.");
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < attractionRadius)
            {
                currentState = State.AttractToPlayer;
            }
        }
        switch (currentState)
        {
            case State.Idle:
                CheckForPlayerProximity();
                break;

            case State.AttractToPlayer:
                float dt = Time.deltaTime;
                bool held = Input.GetKey(KeyCode.C);
                float distance = Vector3.Distance(transform.position, player.position);
                Debug.Log("Attracting seed...");
                if (!held || distance >= attractionRadius)
                {
                    currentState = State.Idle;
                    break;
                }

                if (held && motion != null)
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

                    // --- COMMIT when inside 1/5th of lightSeekRadius ---
                    if (!committedToLight && dist <= commitRadius)
                        committedToLight = true;

                    // --- outside-seek bailout only if NOT yet committed ---
                    if (!committedToLight)
                    {
                        if (dist > lightSeekRadius) outsideTimer += Time.deltaTime;
                        else outsideTimer = 0f;

                        if (outsideTimer > 20f)
                        {
                            currentLightTarget = null;
                            currentState = State.WorldGravity;   // or Idle if you prefer
                            outsideTimer = 0f;
                            return;
                        }
                    }

                    else
                    {
                        // once committed, never bail + clamp outward radial motion
                        outsideTimer = 0f;
                    }

                    if (dist > 30f)
                    {
                        // --- existing steer math, but scale with aggression ---
                        Vector3 rHat = toTarget / dist;
                        Vector3 v = motion.Velocity;
                        float speed = v.magnitude;
                        Vector3 vDir = (speed > 1e-6f) ? v / speed : rHat;

                        // 0 far → 1 near
                        float tNear = 1f - Mathf.InverseLerp(0f, lightSeekRadius, dist);

                        // aggression: weak when far, strong when near; hard-lock to 1 if committed
                        float aggr = committedToLight ? 1f : Mathf.Lerp(farAggression, nearAggression, tNear);

                        // winding/tangent
                        Vector3 upRef = Vector3.up;
                        float wind = Mathf.Sign(Vector3.Dot(Vector3.Cross(toTarget, v), upRef));
                        if (wind == 0f) wind = 1f;
                        Vector3 swirlDir = Vector3.Cross(rHat, upRef).normalized * wind;

                        // keep swirl when far, fade near; inward pull grows with proximity * aggression
                        float swirlAmt = (curveStrengthAt1 + curvePerMs * speed) * (1f - tNear);
                        float pullBase = 1.5f, pullPerMs = 0.06f;
                        float pullAmt = (pullBase + pullPerMs * speed) * tNear * aggr;

                        Vector3 desiredDir = (rHat * pullAmt + swirlDir * swirlAmt).normalized;

                        // inward bias scales with aggr; stronger if committed
                        float coneBias = Mathf.Lerp(0.05f, 0.7f, tNear) * (committedToLight ? 1.0f : aggr);
                        desiredDir = Vector3.Normalize(Vector3.Lerp(desiredDir, rHat, coneBias));

                        // turn rate scales with proximity and aggression
                        float turnDegSec = Mathf.Lerp(baseTurnDegPerSec, maxTurnDegPerSec, Mathf.Pow(tNear, 0.6f))
                                         * Mathf.Lerp(0.6f, 1f, aggr);
                        float maxRadStep = turnDegSec * Mathf.Deg2Rad * Time.deltaTime;
                        Vector3 newDir = Vector3.RotateTowards(vDir, desiredDir, maxRadStep, 0f);

                        // speed up as we approach; earlier ramp softened unless committed
                        float targetSpeed = Mathf.Lerp(homingMinCruise, homingMaxSpeed, tNear * (committedToLight ? 1f : aggr));
                        float newSpeed = Mathf.MoveTowards(speed, targetSpeed, (homingAccel * (committedToLight ? 1f : aggr)) * Time.deltaTime);

                        // build velocity, then enforce inward convergence
                        Vector3 newV = newDir * newSpeed;

                        // never allow outward radial when committed (guarantees eventual lighting)
                        float vRad = Vector3.Dot(newV, rHat); // >0 toward light
                        if (committedToLight && vRad < 0f) newV -= rHat * vRad;

                        // minimum inward component ramps up near the center (strong if committed)
                        const float minInwardSpeedNear = 8f;
                        float minInward = (committedToLight ? 1f : aggr) * Mathf.Lerp(0f, minInwardSpeedNear, tNear);
                        vRad = Vector3.Dot(newV, rHat);
                        if (vRad < minInward) newV += rHat * (minInward - vRad);

                        motion.Velocity = newV;
                    }
                    else
                    {
                        // ARRIVE & LIGHT (unchanged)
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

            case State.WorldGravity:
                {
                    if (!worldCenter || motion == null) break;

                    Vector3 toCenter = worldCenter.position - transform.position;
                    float dist = toCenter.magnitude;
                    if (dist < 0.001f) break;

                    // OUTWARD unit (easier PD signs)
                    Vector3 rOutHat = -toCenter / dist;
                    Vector3 v = motion.Velocity;

                    // Oscillating target radius: inner ↔ outer with phase
                    float omega = 2f * Mathf.PI / Mathf.Max(0.01f, radialOscPeriod);
                    float osc = 0.5f + 0.5f * Mathf.Sin(omega * (Time.time - worldOscStartTime) + worldOrbitPhase); // 0..1
                    float rTarget = Mathf.Lerp(innerOrbit, outerOrbit, osc);

                    // Decompose velocity
                    float vRadOut = Vector3.Dot(v, rOutHat);         // + outward, - inward
                    Vector3 vTan = v - vRadOut * rOutHat;

                    // Desired tangential direction (keep winding)
                    Vector3 upRef = Vector3.up; // replace with planet-up if you have it
                    Vector3 tDesired = Vector3.Cross(upRef, rOutHat).normalized;
                    if (Vector3.Dot(vTan, tDesired) < 0f) tDesired = -tDesired;

                    // --- Radial PD toward oscillating shell ---
                    float e = dist - rTarget;                 // >0 too far out, <0 too far in
                    float aOut = -radialKp * e - radialKd * vRadOut;
                    float vRadOutNext = vRadOut + aOut * Time.deltaTime;

                    // --- Tangential cruise speed: faster at inner, slower at outer ---
                    // Use current distance so speed follows actual radius (Kepler-ish feel)
                    float innerToOuter01 = Mathf.InverseLerp(innerOrbit, outerOrbit, dist); // 0 at inner, 1 at outer
                    float tanTargetSpeed = Mathf.Lerp(orbitSpeedInner, orbitSpeedOuter, innerToOuter01);

                    // Align tangent and approach target speed
                    Vector3 tHat = (vTan.sqrMagnitude > 1e-6f) ? vTan.normalized : tDesired;
                    Vector3 tNew = Vector3.Slerp(tHat, tDesired, tangentAlignGain * Time.deltaTime);
                    float tanNextSp = Mathf.MoveTowards(vTan.magnitude, tanTargetSpeed, tangentAccel * Time.deltaTime);

                    // Rebuild velocity
                    Vector3 vNext = tNew * tanNextSp + rOutHat * vRadOutNext;

                    // When very close to the desired shell, kill tiny radial creep
                    if (Mathf.Abs(e) < 0.5f)
                        vNext -= rOutHat * Vector3.Dot(vNext, rOutHat);

                    motion.Velocity = vNext;
                    
                    // Allow pickup
                    CheckForAttachment();
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

        commitRadius = lightSeekRadius * Mathf.Clamp01(commitFrac);
        committedToLight = false;

        if (motion && motion.Velocity.sqrMagnitude < 1e-6f)
        {
            Vector3 toTarget = currentLightTarget.position - transform.position;
            motion.Velocity = (toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : transform.forward) * homingMinKick;
        }
    }


    public void BeginWorldGravity(Transform centerOverride = null)
    {
        if (centerOverride) worldCenter = centerOverride;
        currentLightTarget = null;
        externallyControlled = false;
        currentState = State.WorldGravity;

        worldOscStartTime = Time.time;
        worldOrbitPhase = UnityEngine.Random.value * Mathf.PI * 2f; // de-sync seeds
    }


    public bool BeginSeekNearestUnlit(float searchRadius = 300f)
    {
        Transform nearest = FindNearestUnlitLight(searchRadius);
        if (nearest == null)
        {
            currentState = State.WorldGravity;
            return false;
        }
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
