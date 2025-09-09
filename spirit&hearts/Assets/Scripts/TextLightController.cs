using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class TextLightController : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Color unlitColor = Color.gray;
    public float unlitIntensity = 0.5f;

    public Color litColor = Color.white;
    public float litIntensity = 2f;

    [Tooltip("Extra HDR boost to trigger Bloom (>1).")]
    public float bloomBoost = 12f;

    public float transitionDuration = 1.5f;

    [Header("State")]
    public bool isLit = false;

    private bool _previousLitState;
    private Material _mat;       // cached instance
    private Renderer _renderer;

    void Awake()
    {
        CacheMaterial();
    }

    void OnEnable()
    {
        // In Editor (not playing), push state immediately
        ApplyImmediate(isLit);
    }

    void Start()
    {
        _previousLitState = isLit;
        ApplyImmediate(isLit);
    }

    void Update()
    {
        // Only run transition watcher during Play
        if (Application.isPlaying && isLit != _previousLitState)
        {
            StartTransition(isLit);
            _previousLitState = isLit;
        }
    }

    void OnValidate()
    {
        // Update live while editing inspector
        if (!Application.isPlaying)
        {
            CacheMaterial();
            ApplyImmediate(isLit);
        }
    }

    void CacheMaterial()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_renderer == null) return;

        // Create instances so we don't edit shared materials in the project
        var mats = _renderer.materials;
        _mat = null;
        foreach (var m in mats)
        {
            if (m != null && m.name.Contains("WindowEmi"))
            {
                _mat = m;
                break;
            }
        }
        if (_mat == null && mats.Length > 0)
            _mat = mats[0];
    }

    // --- Apply instantly (used in Editor & at start) ---
    void ApplyImmediate(bool toLit)
    {
        if (_mat == null) return;

        Color baseCol = toLit ? litColor : unlitColor;
        float baseInt = toLit ? litIntensity : unlitIntensity;

        _mat.EnableKeyword("_EMISSION");

        // Base (albedo)
        if (_mat.HasProperty("_BaseColor"))
            _mat.SetColor("_BaseColor", baseCol * baseInt);

        // Emission (HDR so Bloom can see it)
        if (_mat.HasProperty("_EmissionColor"))
            _mat.SetColor("_EmissionColor", baseCol * Mathf.Max(1f, baseInt) * Mathf.Max(1f, bloomBoost));
    }

    // --- Smooth transition (Play mode) ---
    public void StartTransition(bool toLit)
    {
        if (!Application.isPlaying || _mat == null) { ApplyImmediate(toLit); return; }
        StopAllCoroutines();
        StartCoroutine(DoTransition(toLit));
    }

    private IEnumerator DoTransition(bool toLit)
    {
        if (_mat == null) yield break;

        Color startBase = _mat.HasProperty("_BaseColor") ? _mat.GetColor("_BaseColor") : Color.black;
        Color startEmi  = _mat.HasProperty("_EmissionColor") ? _mat.GetColor("_EmissionColor") : Color.black;

        Color targetBaseCol = toLit ? litColor : unlitColor;
        float targetInt     = toLit ? litIntensity : unlitIntensity;

        Color targetBase = targetBaseCol * targetInt;
        Color targetEmi  = targetBaseCol * Mathf.Max(1f, targetInt) * Mathf.Max(1f, bloomBoost);

        float t = 0f;
        _mat.EnableKeyword("_EMISSION");

        while (t < transitionDuration)
        {
            float k = transitionDuration <= 0f ? 1f : t / transitionDuration;

            if (_mat.HasProperty("_BaseColor"))
                _mat.SetColor("_BaseColor", Color.Lerp(startBase, targetBase, k));

            if (_mat.HasProperty("_EmissionColor"))
                _mat.SetColor("_EmissionColor", Color.Lerp(startEmi, targetEmi, k));

            t += Time.deltaTime;
            yield return null;
        }

        if (_mat.HasProperty("_BaseColor"))
            _mat.SetColor("_BaseColor", targetBase);
        if (_mat.HasProperty("_EmissionColor"))
            _mat.SetColor("_EmissionColor", targetEmi);
    }

    private float GetIntensity(Color c) => Mathf.Max(c.r, c.g, c.b);
}
