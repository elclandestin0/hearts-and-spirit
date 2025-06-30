using UnityEngine;
using System.Collections;

public class LightController : MonoBehaviour
{
    public Material lightMaterial;

    [Header("Lighting Settings")]
    public Color unlitColor;
    public float unlitIntensity;

    public Color litColor;
    public float litIntensity;

    public float transitionDuration = 1.5f;

    [Header("State")]
    public bool isLit = false;
    private bool _previousLitState;
    private Material _mat
    {
        get
        {
            var mats = GetComponent<Renderer>().materials;
            foreach (var m in mats)
            {
                if (m.name.Contains("WindowEmi"))
                    return m;
            }
            return null;
        }
    }


    private void Start()
    {
        _previousLitState = isLit;
        ApplyInitialState();
    }


    private void Update()
    {
        if (isLit != _previousLitState)
        {
            StartTransition(isLit);
            _previousLitState = isLit;
        }
    }

    private void ApplyInitialState()
    {
        Color baseColor = isLit ? litColor : unlitColor;
        float intensity = isLit ? litIntensity : unlitIntensity;
        _mat.SetColor("_BaseColor", baseColor * intensity);
        _mat.EnableKeyword("_EMISSION");
        _mat.SetColor("_EmissionColor", Color.yellow * 5f);
    }

    private void StartTransition(bool toLit)
    {
        StopAllCoroutines();
        StartCoroutine(DoTransition(toLit));
    }

    private IEnumerator DoTransition(bool toLit)
    {
        Color startColor = _mat.GetColor("_BaseColor");
        float startIntensity = GetIntensity(startColor);

        Color targetColor = toLit ? litColor : unlitColor;
        float targetIntensity = toLit ? litIntensity : unlitIntensity;

        float time = 0f;
        _mat.EnableKeyword("_EMISSION");

        while (time < transitionDuration)
        {
            float t = time / transitionDuration;
            Color baseBlended = Color.Lerp(startColor, targetColor * targetIntensity, t);
            Color emissionBlended = Color.Lerp(unlitColor, litColor, t) * Mathf.Lerp(unlitIntensity, litIntensity, t);

            _mat.SetColor("_BaseColor", baseBlended);
            _mat.SetColor("_EmissionColor", emissionBlended);

            time += Time.deltaTime;
            yield return null;
        }

        _mat.SetColor("_BaseColor", targetColor * targetIntensity);
        _mat.SetColor("_EmissionColor", targetColor * targetIntensity);

    }

    private float GetIntensity(Color c) => Mathf.Max(c.r, c.g, c.b);
}
