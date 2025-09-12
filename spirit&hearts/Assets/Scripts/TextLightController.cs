using UnityEngine;
using System.Collections;
using TMPro;

[ExecuteAlways]
public class TextLightController : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Color unlitColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public float unlitIntensity = 0.5f;

    public Color litColor = Color.white;
    public float litIntensity = 2f;

    [Tooltip("Extra HDR boost (acts like emission). Set Bloom in your pipeline.")]
    public float bloomBoost = 8f;

    public float transitionDuration = 0.25f;

    [Header("Outline / Glow (TMP only)")]
    public float outlineWidth = 0.12f;
    public Color outlineColor = new Color(0.5f, 0.35f, 0.08f, 1f); // darker gold/brown
    public Color glowColor = new Color(1f, 0.92f, 0.65f, 1f);
    [Range(0f, 1f)] public float glowOuter = 0.6f;
    [Range(0f, 1f)] public float glowPower = 0.6f; // lower = wider halo

    [Header("State")]
    public bool isLit = false;

    // internals
    private bool _prevLit;
    private Renderer _renderer;
    private TMP_Text _tmp;
    private Material _matInstance;

    void Awake() { CacheTargets(); }
    void OnEnable() { CacheTargets(); ApplyImmediate(isLit); }
    void Start() { _prevLit = isLit; ApplyImmediate(isLit); }

    void OnValidate()
    {
        CacheTargets();
        if (!Application.isPlaying) ApplyImmediate(isLit);
    }

    void Update()
    {
        if (Application.isPlaying && isLit != _prevLit)
        {
            StartTransition(isLit);
            _prevLit = isLit;
        }
    }

    void CacheTargets()
    {
        if (_tmp == null) TryGetComponent(out _tmp);
        if (_renderer == null) TryGetComponent(out _renderer);

        if (_tmp != null)
        {
            // Make a unique material instance for this text so edits don’t affect others.
            var current = _tmp.fontSharedMaterial;
            if (_matInstance == null || _matInstance == current)
            {
                _matInstance = new Material(current);
                _matInstance.name = current.name + " (Instance)";
                _tmp.fontSharedMaterial = _matInstance;
            }
        }
        else if (_renderer != null)
        {
            // For MeshRenderer text or meshes: .material gives an instance.
            _matInstance = _renderer.material;
        }
    }

    void ApplyImmediate(bool toLit)
    {
        if (_matInstance == null) return;

        var baseCol = toLit ? litColor : unlitColor;
        var baseInt = toLit ? litIntensity : unlitIntensity;

        if (_tmp != null)
        {
            // TextMeshPro material properties
            // Face (use HDR multiplier so Bloom catches it)
            _matInstance.SetColor("_FaceColor", baseCol * (baseInt * Mathf.Max(1f, bloomBoost)));

            // Outline / Glow styling
            _matInstance.SetFloat("_OutlineWidth", outlineWidth);
            _matInstance.SetColor("_OutlineColor", outlineColor);

            _matInstance.SetColor("_GlowColor", glowColor * Mathf.Max(1f, bloomBoost * 0.5f));
            _matInstance.SetFloat("_GlowOuter", glowOuter);
            _matInstance.SetFloat("_GlowPower", Mathf.Max(0.001f, glowPower));
        }
        else
        {
            // Generic lit shader path (URP/HDRP/Standard) – try common props
            if (_matInstance.HasProperty("_BaseColor"))
                _matInstance.SetColor("_BaseColor", baseCol * baseInt);

            if (_matInstance.HasProperty("_Color"))
                _matInstance.SetColor("_Color", baseCol * baseInt);

            if (_matInstance.HasProperty("_EmissionColor"))
            {
                _matInstance.EnableKeyword("_EMISSION");
                _matInstance.SetColor("_EmissionColor", baseCol * (baseInt * Mathf.Max(1f, bloomBoost)));
            }
        }
    }
    public void StartTransition(bool toLit)
    {
        // If we can't run coroutines (disabled/not playing), just snap
        if (!Application.isPlaying || !isActiveAndEnabled || _matInstance == null)
        {
            ApplyImmediate(toLit);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(DoTransition(toLit));
    }


    IEnumerator DoTransition(bool toLit)
    {
        if (_matInstance == null) yield break;

        var baseCol = toLit ? litColor : unlitColor;
        var baseInt = toLit ? litIntensity : unlitIntensity;

        float t = 0f;

        // capture starts
        Color startFace = _matInstance.HasProperty("_FaceColor") ? _matInstance.GetColor("_FaceColor") : Color.black;
        Color startBase = _matInstance.HasProperty("_BaseColor") ? _matInstance.GetColor("_BaseColor") : startFace;
        Color startColor = _matInstance.HasProperty("_Color") ? _matInstance.GetColor("_Color") : startBase;
        Color startEmi  = _matInstance.HasProperty("_EmissionColor") ? _matInstance.GetColor("_EmissionColor") : startFace;

        // targets
        Color targetHDR = baseCol * (baseInt * Mathf.Max(1f, bloomBoost));
        Color targetBase = baseCol * baseInt;

        while (t < transitionDuration)
        {
            float k = transitionDuration <= 0f ? 1f : t / transitionDuration;

            if (_tmp != null)
            {
                _matInstance.SetColor("_FaceColor", Color.Lerp(startFace, targetHDR, k));
                // keep outline/glow constant; or lerp if you want:
                _matInstance.SetFloat("_OutlineWidth", outlineWidth);
                _matInstance.SetColor("_OutlineColor", outlineColor);
                _matInstance.SetColor("_GlowColor", glowColor * Mathf.Max(1f, bloomBoost * 0.5f));
                _matInstance.SetFloat("_GlowOuter", glowOuter);
                _matInstance.SetFloat("_GlowPower", Mathf.Max(0.001f, glowPower));
            }
            else
            {
                if (_matInstance.HasProperty("_BaseColor"))
                    _matInstance.SetColor("_BaseColor", Color.Lerp(startBase, targetBase, k));

                if (_matInstance.HasProperty("_Color"))
                    _matInstance.SetColor("_Color", Color.Lerp(startColor, targetBase, k));

                if (_matInstance.HasProperty("_EmissionColor"))
                {
                    _matInstance.EnableKeyword("_EMISSION");
                    _matInstance.SetColor("_EmissionColor", Color.Lerp(startEmi, targetHDR, k));
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        // final snap
        ApplyImmediate(toLit);
    }
}
