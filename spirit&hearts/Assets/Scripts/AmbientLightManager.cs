using UnityEngine;

public class AmbientLightManager : MonoBehaviour
{
    [Header("Ambient Light Settings")]
    public Color darkColor = Color.black;
    public Color fullLitColor = Color.white;
    [SerializeField] private int total;
    [SerializeField] private int lit;

    // Transition duration between currentStartColor and currentTargetColor
    public float transitionDuration = 1.5f;
    private Color currentTargetColor;
    private Color currentStartColor;
    private float transitionTimer = 0f;
    private bool isTransitioning = false;
    public float litPercent = 0f;
    private GameObject[] allLights;

    private void Start()
    {
        allLights = GameObject.FindGameObjectsWithTag("Light");
        total = allLights.Length;
    }

    private void Update()
    {
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime; 
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            RenderSettings.ambientLight = Color.Lerp(currentStartColor, currentTargetColor, t);

            if (t >= 1f)
                isTransitioning = false;
        }
    }

    public void UpdateAmbientLight()
    {
        lit = 0;

        foreach (GameObject obj in allLights)
        {
            LightController controller = obj.GetComponent<LightController>()
                                    ?? obj.GetComponentInParent<LightController>()
                                    ?? obj.GetComponentInChildren<LightController>();

            if (controller != null && controller.isLit)
                lit++;
        }

        litPercent = total > 0 ? (float)lit / total : 0f;
        currentStartColor = RenderSettings.ambientLight;
        currentTargetColor = Color.Lerp(darkColor, fullLitColor, litPercent);
        transitionTimer = 0f;
        isTransitioning = true;
    }
}
