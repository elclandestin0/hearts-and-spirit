using UnityEngine;

public class AmbientLightManager : MonoBehaviour
{
    [Header("Ambient Light Settings")]
    public Color darkColor = Color.black;
    public Color fullLitColor = Color.white;
    public int lit = 0;
    public int total;
    GameObject[] allLights;
    void Start() 
    {
        GameObject[] allLights = GameObject.FindGameObjectsWithTag("Light");
        total = allLights.Length;
    }
    public void UpdateAmbientLight()
    {
        foreach (GameObject obj in allLights)
        {
            LightController controller = obj.GetComponent<LightController>()
                                    ?? obj.GetComponentInParent<LightController>()
                                    ?? obj.GetComponentInChildren<LightController>();

            if (controller != null && controller.isLit)
                lit++;
        }

        float litPercent = total > 0 ? (float)lit / total : 0f;

        // Lerp from darkColor to fullLitColor based on percentage of lights lit
        RenderSettings.ambientLight = Color.Lerp(darkColor, fullLitColor, litPercent);
    }
}
