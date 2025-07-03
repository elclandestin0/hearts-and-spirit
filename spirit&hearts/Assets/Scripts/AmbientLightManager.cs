using UnityEngine;

public class AmbientLightManager : MonoBehaviour
{
    [Header("Ambient Light Settings")]
    public Color darkColor = Color.black;
    public Color fullLitColor = Color.white;

    [SerializeField] private int total;
    [SerializeField] private int lit;

    public void UpdateAmbientLight()
    {
        GameObject[] allLights = GameObject.FindGameObjectsWithTag("Light");
        total = allLights.Length;
        lit = 0;

        foreach (GameObject obj in allLights)
        {
            LightController controller = obj.GetComponent<LightController>()
                                    ?? obj.GetComponentInParent<LightController>()
                                    ?? obj.GetComponentInChildren<LightController>();

            if (controller != null && controller.isLit)
                lit++;
        }

        float litPercent = total > 0 ? (float)lit / total : 0f;
        RenderSettings.ambientLight = Color.Lerp(darkColor, fullLitColor, litPercent);
    }
}
