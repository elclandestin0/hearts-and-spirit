using UnityEngine;

public class SeedBehavior : MonoBehaviour
{
    public enum State { Idle, AttractToPlayer, AttachedToPlayer, MoveToLight }
    public State currentState = State.Idle;

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
    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        seedHolster = player.transform.Find("SeedHolster");
        lightManager = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
        dovinaAudioManager = GameObject.Find("Dove")?.GetComponent<DovinaAudioManager>();
        _hub = GameObject.FindWithTag("MainPlayer")?.GetComponent<MovementEventHub>();
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case State.Idle:
                CheckForPlayerProximity();
                break;

            case State.AttractToPlayer:
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
                        // Reached light source
                        LightController light = currentLightTarget.GetComponent<LightController>()
                                                ?? currentLightTarget.GetComponentInParent<LightController>()
                                                ?? currentLightTarget.GetComponentInChildren<LightController>();

                        if (light != null && !light.isLit)
                        {
                            light.isLit = true;
                            player.gameObject.GetComponent<ItemManager>().RemoveSeed();
                            player.gameObject.GetComponent<ItemManager>().PlayLightSound();
                            // dovinaAudioManager.PlayPriority("gp_changes/light", 2, 1, 12);
                            // dovinaAudioManager.PlayPriority("parables", 2, 0, 999);
                            lightManager.UpdateAmbientLight();
                            _hub.RaiseLightLit();
                            Destroy(this.gameObject);
                        }
                    }
                }
                break;
        }
    }

    private void CheckForAttachment()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attachDistance)
        {
            currentState = State.AttachedToPlayer;
            transform.SetParent(seedHolster);
            transform.localPosition = Vector3.zero;

            var items = player.gameObject.GetComponent<ItemManager>();
            items.AddSeed();
            items.PlayPickUpSound();
            dovinaAudioManager.PlayPriority("gp_changes/seed", 2, 2, 14);

            _hub.RaiseSeedPicked();
        }
    }

    private void CheckForPlayerProximity()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attractionRadius)
        {
            currentState = State.AttractToPlayer;
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
