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

    private float t;
    private Action onBegin, onDestroyed;

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
        var sb = GetComponent<SeedBehavior>();
        if (sb) sb.externallyControlled = true;
    }

    void Update()
    {
        var sb = GetComponent<SeedBehavior>();

        if (t < duration)
        {
            transform.position += forwardDir * speed * Time.deltaTime;

            Transform instant = FindUnlitLightWithin(arriveDistance);
            if (instant != null)
            {
                TryLight(instant);
                onDestroyed?.Invoke();
                Destroy(gameObject);
                return;
            }

            if (sb != null)
            {
                sb.externallyControlled = false;
                bool started = sb.BeginSeekNearestUnlit();
                if (started)
                {
                    Destroy(this);
                    return;
                }
                sb.externallyControlled = true;
            }

            t += Time.deltaTime;
            return;
        }

        if (sb != null)
        {
            sb.externallyControlled = false;
            bool started = sb.BeginSeekNearestUnlit();
            if (!started)
            {
                sb.currentState = SeedBehavior.State.Idle;
            }
        }

        Destroy(this);
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
