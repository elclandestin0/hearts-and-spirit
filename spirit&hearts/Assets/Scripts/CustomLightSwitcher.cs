using UnityEngine;

public class LightingZone : MonoBehaviour
{
    public float maxDistance = 5f;

    private MaterialPropertyBlock mpb;
    private Renderer rend;

    private Transform customLight;
    private bool inLightZone = false;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!inLightZone || customLight == null || rend == null) return;

        Vector3 lightDir = -customLight.forward.normalized;
        float distance = Vector3.Distance(customLight.position, transform.position);
        float attenuation = Mathf.Clamp01(distance / maxDistance); // invertido = 0 cerca, 1 lejos

        mpb.SetVector("_CustomLightDirection", lightDir);
        mpb.SetFloat("_CustomLightIntensity", attenuation);
        mpb.SetFloat("_UseCustomLight", 1);
        rend.SetPropertyBlock(mpb);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Light"))
        {
            customLight = other.transform;
            inLightZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Light") && other.transform == customLight)
        {
            inLightZone = false;
            customLight = null;

            mpb.SetFloat("_UseCustomLight", 0);
            mpb.SetFloat("_CustomLightIntensity", 1); // intensidad apagada
            rend.SetPropertyBlock(mpb);
        }
    }
}
