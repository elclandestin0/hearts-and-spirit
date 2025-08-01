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
    private LightController light;
    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        seedHolster = player.transform.Find("SeedHolster");
        lightManager = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
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
                Debug.Log("Moving to player");
                MoveToward(seedHolster);
                CheckForAttachment();
                break;

            case State.AttachedToPlayer:
                Debug.Log("Attached to player");
                SearchForNearbyLight();
                break;

            case State.MoveToLight:
                if (currentLightTarget != null)
                {
                    MoveToward(currentLightTarget);

                    float d = Vector3.Distance(transform.position, currentLightTarget.position);
                    if (d < 10f)
                    {
                        // Reached light source
                        Debug.Log("Seed arrived at light");

                        if (light != null && !light.isLit)
                        {
                            light.isLit = true;
                            player.gameObject.GetComponent<ItemManager>().RemoveSeed();
                            player.gameObject.GetComponent<ItemManager>().PlayLightSound();
                            lightManager?.UpdateAmbientLight();
                            Debug.Log("Seed activated the light source.");
                            Destroy(this.gameObject);
                        }

                        else
                        {
                            Debug.Log(light == null ? "Light null" : "Light not null");
                            Debug.Log(!light?.isLit ?? false ? "Not lit" : "It's lit");
                        }
                    }
                }
                break;
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

    private void CheckForAttachment()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attachDistance)
        {
            currentState = State.AttachedToPlayer;
            transform.SetParent(seedHolster);
            transform.localPosition = new Vector3(0f, 0f, 0f);
            player.gameObject.GetComponent<ItemManager>().AddSeed();
            player.gameObject.GetComponent<ItemManager>().PlayPickUpSound();
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
            Debug.Log("light source: " + light.name + " distance: " + d);
            if (d < lightSeekRadius)
            {
                closest = light.transform;
            }
        }

        if (closest != null)
        {
            currentLightTarget = closest;
            LightController light = currentLightTarget.GetComponent<LightController>()
                                                ?? currentLightTarget.GetComponentInParent<LightController>()
                                                ?? currentLightTarget.GetComponentInChildren<LightController>();

            transform.SetParent(null);
            if (light.isLit) return;
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
