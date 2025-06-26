using UnityEngine;
using System.Collections;

public class LanternController : MonoBehaviour
{
    public Renderer lanternRenderer;

    [Header("Lighting Settings")]
    public Color unlitColor;
    public float unlitIntensity;

    public Color litColor;
    public float litIntensity;

    public float transitionDuration = 1.5f;

    [Header("State")]
    public bool isLit = false;

    private Material _mat => lanternRenderer.material;
    private bool _previousLitState;

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
        while (time < transitionDuration)
        {
            float t = time / transitionDuration;
            Color blended = Color.Lerp(startColor, targetColor * targetIntensity, t);
            _mat.SetColor("_BaseColor", blended);
            time += Time.deltaTime;
            yield return null;
        }

        _mat.SetColor("_BaseColor", targetColor * targetIntensity);
    }

    private float GetIntensity(Color c) => Mathf.Max(c.r, c.g, c.b);
}
