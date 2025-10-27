using UnityEngine;
using System;

public class SeedCourier : MonoBehaviour
{
    // config
    private Vector3 forwardDir;
    private float duration;
    private float speed;
    private float arriveDistance;
    private string lightTag = "Light";

    // refs for lighting
    private DovinaAudioManager dove;
    private AmbientLightManager ambient;
    private MovementEventHub hub;
    private TutorialManager tutorial;
    private Transform playerRoot;
    private SeedMotion motion;
    private float t;
    private Action onBegin, onDestroyed;
    [SerializeField] private float launchDecel = 2f;     // m/s² — speed bleed after throw
    [SerializeField] private float minLaunchSpeed = 0.5f; // when below this, we consider the throw “over”
    [SerializeField] private float worldSeekDelay = 5f; // if no light within this time → world gravity
    private float lifeTimer = 0f;
    [SerializeField] private Transform worldCenterOverride; // optional: assign your planet center GO

    public void InitForward(
        Vector3 dir, float duration, float speed, float arriveDistance,
        DovinaAudioManager dove, AmbientLightManager ambient, MovementEventHub hub,
        TutorialManager tutorial, Transform playerRoot,
        Action onBegin = null, Action onDestroyed = null)
    {
        this.forwardDir = dir.normalized;
        this.duration = Mathf.Max(0.01f, duration);
        this.speed = speed;
        this.arriveDistance = arriveDistance;

        this.dove = dove;
        this.ambient = ambient;
        this.hub = hub;
        this.tutorial = tutorial;
        this.playerRoot = playerRoot;

        this.onBegin = onBegin;
        this.onDestroyed = onDestroyed;

        onBegin?.Invoke();

        motion = GetComponent<SeedMotion>();
        var sb = GetComponent<SeedBehavior>();

        Vector3 v0 = dir.normalized * speed;
        if (motion) motion.SetVelocity(v0);
        if (sb) sb.externallyControlled = true;
    }

    void Update()
    {
        var sb = GetComponent<SeedBehavior>();
        motion ??= GetComponent<SeedMotion>();

        // if homing already started, courier is done
        if (sb != null && sb.IsHomingToLight)
        {
            lifeTimer = 0f; // safety
            Destroy(this);
            return;
        }

        // 1) instant brush lighting during the throw
        Transform instant = FindUnlitLightWithin(arriveDistance);
        if (instant != null)
        {
            TryLight(instant);
            onDestroyed?.Invoke();
            lifeTimer = 0f;          // reset timer on success
            Destroy(this);
            return;
        }

        // 2) let SeedBehavior try to acquire a light every frame
        if (sb != null)
        {
            sb.externallyControlled = false;          // allow FSM
            if (sb.BeginSeekNearestUnlit())           // will set MoveToLight on success
            {
                lifeTimer = 0f;                       // reset timer on success
                Destroy(this);                        // courier hands off
                return;
            }
            sb.externallyControlled = true;           // keep courier in charge for now
        }

        // 3) bleed launch energy while searching
        // if (motion != null)
        // {
        //     float s = motion.Velocity.magnitude;
        //     s = Mathf.Max(0f, s - launchDecel * Time.deltaTime);
        //     motion.SetSpeed(s);
        // }

        // 4) fallback timer → WorldGravity
        lifeTimer += Time.deltaTime;
        bool throwExpired = motion != null && motion.Velocity.magnitude <= minLaunchSpeed;

        if ((lifeTimer >= worldSeekDelay || throwExpired) && sb != null)
        {
            // final handoff to world gravity
            sb.externallyControlled = false;
            sb.BeginWorldGravity(worldCenterOverride);     // <- your planet center GO (optional override)
            onDestroyed?.Invoke();
            // do NOT damp velocity; let WorldGravity capture from current v
            Destroy(this);
            return;
        }
    }
    private Transform FindUnlitLightWithin(float radius)
    {
        GameObject[] lights = GameObject.FindGameObjectsWithTag(lightTag);
        foreach (var go in lights)
        {
            var lc = go.GetComponent<LightController>() ?? go.GetComponentInParent<LightController>() ?? go.GetComponentInChildren<LightController>();
            if (lc == null || lc.isLit) continue;

            if (Vector3.Distance(transform.position, go.transform.position) <= radius)
                return go.transform;
        }
        return null;
    }

    private void TryLight(Transform targetLight)
    {
        var lc = targetLight.GetComponent<LightController>() ?? targetLight.GetComponentInParent<LightController>() ?? targetLight.GetComponentInChildren<LightController>();
        if (lc == null || lc.isLit) return;

        lc.isLit = true;

        if (tutorial == null || !tutorial.IsRunning)
        {
            dove?.PlayPriority("gp_changes/light", 2, 1, 12);
            dove?.PlayPriority("parables", 4, 0, 999);
            ambient?.UpdateAmbientLight();
        }

        hub?.RaiseLightLit();

        var pid = lc.GetComponent<PersistentId>();
        ProgressManager.Instance?.MarkLastLit(pid, playerRoot);
        SaveManager.Save("slot1");
    }
}
