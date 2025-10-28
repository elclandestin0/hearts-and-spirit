using UnityEngine;
using System;

[DisallowMultipleComponent]
public class LightController : MonoBehaviour, ISaveable
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;      // assign explicitly
    [SerializeField] private int targetMaterialIndex = 0;  // assign explicitly

    [Header("Write Channels")]
    [SerializeField] private bool writeBaseColor = false;  // default OFF
    [SerializeField] private bool writeEmission = true;

    [Header("Lighting Settings")]
    public Color unlitColor = Color.black;
    public float unlitIntensity = 0f;
    public Color litColor = Color.yellow;
    public float litIntensity = 6f;
    public float transitionDuration = 1.5f;
    [Min(0)] public float emissionBoost = 1.25f;
    [Header("State")]
    public bool isLit = false;
    bool prev;
    MaterialPropertyBlock mpb;
    static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int _EmissionID = Shader.PropertyToID("_EmissionColor");
    Coroutine tween;

    [Serializable] struct S { public bool lit; }
    public object CaptureState() => new S { lit = isLit };
    public void RestoreState(object o) { isLit = ((S)o).lit; ApplyInstant(); }

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }
    private void Start()
    {
        isLit = false;
        prev = false;
    }

    void Update()
    {
        if (isLit != prev) { Begin(isLit); prev = isLit; }
    }

    public void Ignite() { isLit = true; }   // call this when seed lights it
    public void Extinguish() { isLit = false; }
    void ApplyInstant()
    {
        if (!targetRenderer) return;
        targetRenderer.GetPropertyBlock(mpb, targetMaterialIndex);

        Color targetBase = (isLit ? litColor : unlitColor) * (isLit ? litIntensity : unlitIntensity);
        Color targetEmi = targetBase * emissionBoost;

        if (writeBaseColor) mpb.SetColor(_BaseColorID, targetBase);
        if (writeEmission) mpb.SetColor(_EmissionID, targetEmi);

        targetRenderer.SetPropertyBlock(mpb, targetMaterialIndex);
    }

    void Begin(bool toLit)
    {
        if (tween != null) StopCoroutine(tween);
        tween = StartCoroutine(Tween(toLit));
    }

    System.Collections.IEnumerator Tween(bool toLit)
    {
        if (!targetRenderer) yield break;

        targetRenderer.GetPropertyBlock(mpb, targetMaterialIndex);
        // read current (preserve existing HDR)
        Color startBase = mpb.GetColor(_BaseColorID);
        Color startEmi = mpb.GetColor(_EmissionID);

        Color endBase = (toLit ? litColor : unlitColor) * (toLit ? litIntensity : unlitIntensity);
        Color endEmi = endBase * emissionBoost;

        float t = 0, dur = Mathf.Max(0.01f, transitionDuration);
        while (t < dur)
        {
            float a = t / dur;
            if (writeBaseColor) mpb.SetColor(_BaseColorID, Color.LerpUnclamped(startBase, endBase, a));
            if (writeEmission) mpb.SetColor(_EmissionID, Color.LerpUnclamped(startEmi, endEmi, a));
            targetRenderer.SetPropertyBlock(mpb, targetMaterialIndex);
            t += Time.deltaTime;
            yield return null;
        }
        if (writeBaseColor) mpb.SetColor(_BaseColorID, endBase);
        if (writeEmission) mpb.SetColor(_EmissionID, endEmi);
        targetRenderer.SetPropertyBlock(mpb, targetMaterialIndex);
        tween = null;
    }
}
