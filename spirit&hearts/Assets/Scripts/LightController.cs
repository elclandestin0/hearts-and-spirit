using UnityEngine;
using System;
using System.Collections;

public class LightController : MonoBehaviour, ISaveable
{
    [Header("Target Material (auto-found if left empty)")]
    [Tooltip("If left empty, the script finds the first material that has _BaseColor and _EmissionColor.")]
    public Material lightMaterial; // optional reference; we still write via MPB

    [Header("Unlit → Lit Settings")]
    public Color unlitColor = Color.black;
    [Min(0)] public float unlitIntensity = 0.0f;

    public Color litColor = Color.yellow;
    [Min(0)] public float litIntensity = 0.5f;

    [Tooltip("Extra multiplier only on the emission output to really pop bloom.")]
    [Min(0)] public float bloomBoost = 1.2f;

    [Tooltip("Seconds for the base transition.")]
    [Min(0.01f)] public float transitionDuration = 1.5f;

    [Tooltip("Easing across transition (0..1 → 0..1). Default EaseInOut.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional end-pulse")]
    [Tooltip("Adds a short  ‘whoompf’ pulse when finishing lighting.")]
    public bool pulseAtEnd = true;
    [Min(0)] public float pulseDuration = 0.25f;
    [Min(1)] public float pulseMultiplier = 1.75f;

    [Header("State")]
    public bool isLit = false;

    // EVENTS
    public event Action onLit;
    public event Action onUnlit;

    // --- private/cached ---
    private bool _prevLit;
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private int _matIndex = -1;
    private Coroutine _activeTween;

    // shader property IDs
    private static readonly int _BaseColorID     = Shader.PropertyToID("_BaseColor");
    private static readonly int _EmissionColorID = Shader.PropertyToID("_EmissionColor");

    [Serializable] private struct LightState { public bool lit; }
    public object CaptureState() => new LightState { lit = isLit };

    public void RestoreState(object sObj)
    {
        var s = (LightState)sObj;
        isLit = s.lit;
        // apply instantly without tween
        ApplyInstant(isLit);
    }

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        ResolveTargetMaterialIndex();
    }

    private void Start()
    {
        _prevLit = isLit;
        ApplyInstant(isLit);
    }

    private void Update()
    {
        if (isLit != _prevLit)
        {
            BeginTransition(isLit);
            _prevLit = isLit;
        }
    }

    // --- public control API ---
    public void SetLit(bool value, bool instant = false)
    {
        isLit = value;
        if (instant) ApplyInstant(isLit);
        else BeginTransition(isLit);
    }

    // --- core writing helpers ---
    private void ApplyInstant(bool toLit)
    {
        if (!Validate()) return;

        var baseCol = toLit ? litColor : unlitColor;
        var baseInt = toLit ? litIntensity : unlitIntensity;

        var baseOut = baseCol * baseInt;
        var emiOut  = baseCol * baseInt * bloomBoost; // give emission extra juice

        _renderer.GetPropertyBlock(_mpb, _matIndex);
        _mpb.SetColor(_BaseColorID,     baseOut);
        _mpb.SetColor(_EmissionColorID, emiOut);
        _renderer.SetPropertyBlock(_mpb, _matIndex);

        // fire events
        if (toLit) onLit?.Invoke(); else onUnlit?.Invoke();
    }

    private void BeginTransition(bool toLit)
    {
        if (!Validate()) return;
        if (_activeTween != null) StopCoroutine(_activeTween);
        _activeTween = StartCoroutine(Tween(toLit));
    }

    private IEnumerator Tween(bool toLit)
    {
        // read current for smooth blend from whatever is on GPU
        _renderer.GetPropertyBlock(_mpb, _matIndex);
        Color startBase = _mpb.GetVector(_BaseColorID);
        Color startEmis = _mpb.GetVector(_EmissionColorID);

        // targets
        var targetBase = (toLit ? litColor : unlitColor) * (toLit ? litIntensity : unlitIntensity);
        var targetEmis = (toLit ? litColor : unlitColor) * (toLit ? litIntensity : unlitIntensity) * bloomBoost;

        float t = 0f;
        float dur = Mathf.Max(0.01f, transitionDuration);

        while (t < dur)
        {
            float a = ease.Evaluate(t / dur);
            Color baseOut = Color.LerpUnclamped(startBase, targetBase, a);
            Color emiOut  = Color.LerpUnclamped(startEmis, targetEmis, a);

            _mpb.SetColor(_BaseColorID,     baseOut);
            _mpb.SetColor(_EmissionColorID, emiOut);
            _renderer.SetPropertyBlock(_mpb, _matIndex);

            t += Time.deltaTime;
            yield return null;
        }

        // final snap to exact
        _mpb.SetColor(_BaseColorID,     targetBase);
        _mpb.SetColor(_EmissionColorID, targetEmis);
        _renderer.SetPropertyBlock(_mpb, _matIndex);

        if (toLit && pulseAtEnd && pulseDuration > 0f && pulseMultiplier > 1f)
            yield return PulseEmission(targetEmis, pulseDuration, pulseMultiplier);

        _activeTween = null;

        if (toLit) onLit?.Invoke(); else onUnlit?.Invoke();
    }

    private IEnumerator PulseEmission(Color baseEmis, float dur, float mul)
    {
        float half = dur * 0.5f;
        float t = 0f;

        // up
        while (t < half)
        {
            float a = t / half; // 0..1
            float m = Mathf.Lerp(1f, mul, a);
            _mpb.SetColor(_EmissionColorID, baseEmis * m);
            _renderer.SetPropertyBlock(_mpb, _matIndex);
            t += Time.deltaTime;
            yield return null;
        }

        // down
        t = 0f;
        while (t < half)
        {
            float a = t / half;
            float m = Mathf.Lerp(mul, 1f, a);
            _mpb.SetColor(_EmissionColorID, baseEmis * m);
            _renderer.SetPropertyBlock(_mpb, _matIndex);
            t += Time.deltaTime;
            yield return null;
        }
    }

    // --- utils ---
    private bool Validate()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null) return false;
        }
        if (_matIndex < 0) ResolveTargetMaterialIndex();
        return _matIndex >= 0;
    }

    private void ResolveTargetMaterialIndex()
    {
        _matIndex = -1;

        var mats = _renderer != null ? _renderer.sharedMaterials : null;
        if (mats == null || mats.Length == 0) return;

        // 1) if explicit material reference matches one in the renderer, use its index
        if (lightMaterial != null)
        {
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == lightMaterial)
                {
                    _matIndex = i;
                    return;
                }
            }
        }

        // 2) best effort: find first mat that has our properties
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (m && (m.HasProperty(_BaseColorID) || m.shader.name.Contains("Shader Graph", StringComparison.OrdinalIgnoreCase)))
            {
                // most Shader Graph Lit templates expose _BaseColor/_EmissionColor
                if (m.HasProperty(_EmissionColorID))
                {
                    _matIndex = i;
                    return;
                }
            }
        }

        // 3) last resort: name hint (your original WindowEmi)
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] && mats[i].name.Contains("WindowEmi", StringComparison.OrdinalIgnoreCase))
            {
                _matIndex = i;
                return;
            }
        }
    }
}
