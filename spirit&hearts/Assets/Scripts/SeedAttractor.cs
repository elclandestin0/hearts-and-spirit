using UnityEngine;

public class SeedBehavior : MonoBehaviour
{
    public enum State { Idle, AttractToPlayer, AttachedToPlayer, MoveToLight }
    public State currentState = State.Idle;

    private Transform player;
    private Transform currentLightTarget;

    [Header("Attraction Settings")]
    [SerializeField] private float attractionRadius = 150f;
    [SerializeField] private float attachDistance = 25f;
    [SerializeField] private float minSpeed = 120f;
    [SerializeField] private float maxSpeed = 200f;

    [Header("Light Seeking Settings")]
    [SerializeField] private float lightSeekRadius = 50f;
    [SerializeField] private string lightTag = "Light";
    [SerializeField] private AmbientLightManager lightManager;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        lightManager = GameObject.Find("AmbientLightManager")?.GetComponent<AmbientLightManager>();
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case State.Idle:
                Debug.Log("Idle");
                CheckForPlayerProximity();
                break;

            case State.AttractToPlayer:
                Debug.Log("Moving to player");
                MoveToward(player);
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
                    if (d < 0.5f)
                    {
                        // Reached light source
                        Debug.Log("Seed arrived at light");

                        LightController light = currentLightTarget.GetComponent<LightController>()
                                                ?? currentLightTarget.GetComponentInParent<LightController>()
                                                ?? currentLightTarget.GetComponentInChildren<LightController>();

                        if (light != null && !light.isLit)
                        {
                            light.isLit = true;
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

            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Rotate>().enabled = false;
            currentState = State.AttachedToPlayer;
            transform.SetParent(player, false);
            transform.localPosition = new Vector3(0f, 0f, 0f);
            player.gameObject.GetComponent<ItemManager>().PlayPickUpSound();
        }

    }

    private void SearchForNearbyLight()
    {
        GameObject[] lightSources = GameObject.FindGameObjectsWithTag(lightTag);
        Transform closest = null;
        foreach (GameObject light in lightSources)
        {
            float d = Vector3.Distance(transform.position, light.transform.position);
            Debug.Log("light: " + light.name + " andistanced distance: " + d);
            if (d < lightSeekRadius)
            {
                Debug.Log("found light source. distance " + d);
                Debug.Log(light.name);
                closest = light.transform;
            }
        }

        if (closest != null)
        {
            currentLightTarget = closest;
            transform.SetParent(null); // Detach from player
            currentState = State.MoveToLight;
            GetComponent<MeshRenderer>().enabled = true;
            GetComponent<Rotate>().enabled = true;
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
