using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class HoverTextLightTweenerSafe : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Find target automatically if left empty")]
    public TextLightController target;

    [Header("Hover (unlit) overrides")]
    public Color hoverUnlitColor = new Color(1f, 0.9f, 0.6f, 1f);
    public float hoverUnlitIntensity = 1.2f;

    // backup
    private Color _baseUnlitColor;
    private float _baseUnlitIntensity;
    private bool _cached;

    private RectTransform _rt;
    private bool _isHovered;          // our own truth
    private bool _appliedHover;       // did we apply hover style?

    [Header("XR UI Rays (Left/Right)")]
    // Keep your original field type, but we’ll also auto-discover if empty.
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] uiRays;

    [Header("Debug")]
    [SerializeField] private bool enableXRDebug = true;
    [SerializeField] private bool logOnChangeOnly = true;
    [SerializeField] private KeyCode holdToLog = KeyCode.F9;

    private bool[] _lastUIHitState;   // per-ray last UI hit state

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        CacheTarget();
        CacheBase();
    }

    void OnEnable()
    {
        CacheTarget();
        CacheBase();
        ApplyHover(_isHovered);

        // Ensure rays list
        EnsureXRUIWiring();          // warn if EventSystem/Canvas not XR-ready
        AutoFindXRRaysIfEmpty();     // try to populate rays if you forgot to assign
        SizeDebugArrays();
        LogFoundRaysOnce();
    }

    void OnDisable()
    {
        if (_appliedHover) ApplyHover(false, force: true);
        _isHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        ApplyHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ApplyHover(false);
    }

    void Update()
    {
        // Watchdog: if we think we are hovered but pointer no longer over us, synthesize Exit.
        if (_isHovered && !PointerCurrentlyOverThis())
        {
            _isHovered = false;
            ApplyHover(false, force: true);
        }
    }

    private void CacheTarget()
    {
        if (target != null) return;
        var tmp = GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (tmp) target = tmp.GetComponent<TextLightController>();
        if (!target) target = GetComponentInChildren<TextLightController>(includeInactive: true);
    }

    private void CacheBase()
    {
        if (target == null || _cached) return;
        _baseUnlitColor = target.unlitColor;
        _baseUnlitIntensity = target.unlitIntensity;
        _cached = true;
    }

    private void ApplyHover(bool on, bool force = false)
    {
        if (target == null) return;

        if (on)
        {
            if (_appliedHover && !force) return;
            target.unlitColor = hoverUnlitColor;
            target.unlitIntensity = hoverUnlitIntensity;
            target.StartTransition(target.isLit);
            _appliedHover = true;
        }
        else
        {
            if (!_appliedHover && !force) return;
            target.unlitColor = _baseUnlitColor;
            target.unlitIntensity = _baseUnlitIntensity;
            target.StartTransition(target.isLit);
            _appliedHover = false;
        }
    }

    private bool IsSelfOrChild(GameObject go)
        => go == gameObject || (go != null && go.transform.IsChildOf(transform));

    // Raycast check against current pointer position; handles mouse + UI system.
    public bool PointerCurrentlyOverThis()
    {
        if (EventSystem.current == null) return false;

        // 1) Mouse path
        if (Input.mousePresent)
        {
            var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = ListPool<RaycastResult>.Get();
            EventSystem.current.RaycastAll(data, results);

            var over = false;
            for (int i = 0; i < results.Count; i++)
            {
                if (IsSelfOrChild(results[i].gameObject)) { over = true; break; }
            }

            results.Clear();
            ListPool<RaycastResult>.Release(results);
            if (over) return true;
        }

        // 2) XR rays
        bool uiHitAny = false;
        if (uiRays != null && uiRays.Length > 0)
        {
            for (int i = 0; i < uiRays.Length; i++)
            {
                var ray = uiRays[i];
                if (ray == null || !ray.isActiveAndEnabled)
                {
                    XRLog(i, ray, uiHit:false, uiObj:null, has3D:false, default, reasonHint:"Ray is null or inactive (not enabled / wrong object).");
                    continue;
                }

                GameObject uiObj = null;
                bool uiHit = false;

                if (ray.TryGetCurrentUIRaycastResult(out var uiHitRes) && uiHitRes.isValid && uiHitRes.gameObject != null)
                {
                    uiObj = uiHitRes.gameObject;
                    if (IsSelfOrChild(uiObj))
                    {
                        uiHit = true;
                        uiHitAny = true;
                    }
                }

                bool has3D = ray.TryGetCurrent3DRaycastHit(out var hit3D);
                XRLog(i, ray, uiHit, uiObj, has3D, hit3D,
                      reasonHint: uiHit ? "" :
                      "No UI hit – verify Canvas has TrackedDeviceGraphicRaycaster, EventSystem uses InputSystemUIInputModule (XR enabled), ray masks include UI, and no 3D blockers.");
                if (uiHit) return true;
            }
        }
        else
        {
            Debug.LogWarning("[XRUI] No XR rays assigned or auto-found. Assign Left/Right XRRayInteractors in the inspector.");
        }

        // 3) Fallback to flag
        return _isHovered;
    }

    // ---------- XR wiring sanity checks & helpers ----------

    private void EnsureXRUIWiring()
    {
        // EventSystem + UI module checks
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[XRUI] No EventSystem in scene.");
            return;
        }

        var module = EventSystem.current.currentInputModule;
        if (module == null)
        {
            Debug.LogWarning("[XRUI] EventSystem has no active InputModule.");
        }
        else
        {
            var type = module.GetType().FullName;
            bool isInputSystemUI = type.Contains("InputSystemUIInputModule");
            bool isXrUiModule    = type.Contains("XRUIInputModule");
            if (!isInputSystemUI && !isXrUiModule)
            {
                Debug.LogWarning($"[XRUI] InputModule is {type}. For XR UI, use InputSystemUIInputModule (Enable XR Support) or XRUIInputModule.");
            }
        }

        // Canvas checks (closest canvas up the hierarchy + all found canvases)
        var myCanvas = GetComponentInParent<Canvas>();
        if (myCanvas != null)
        {
            bool hasTracked = myCanvas.GetComponent("UnityEngine.InputSystem.UI.TrackedDeviceGraphicRaycaster") != null
                           || myCanvas.GetComponent("TrackedDeviceGraphicRaycaster") != null;
            if (!hasTracked)
            {
                Debug.LogWarning($"[XRUI] Canvas \"{myCanvas.name}\" lacks TrackedDeviceGraphicRaycaster. Add it (keep GraphicRaycaster for mouse).");
            }
        }
        else
        {
            Debug.LogWarning("[XRUI] No parent Canvas found. Make sure your UI element lives under a Canvas.");
        }
    }

    private void AutoFindXRRaysIfEmpty()
    {
        if (uiRays != null && uiRays.Length > 0) return;

        // Try to find rays by type name to support multiple XRI namespaces.
        var found = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();

        // 1) Direct find with your declared type (will work if project uses that namespace)
        var typed = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true);
        if (typed != null && typed.Length > 0) found.AddRange(typed);

        // 2) Fallback: try the classic namespace via reflection (UnityEngine.XR.Interaction.Toolkit.XRRayInteractor)
        var classicType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRRayInteractor, Unity.XR.Interaction.Toolkit");
        if (classicType != null)
        {
            var objs = FindObjectsOfType(classicType, true);
            foreach (var o in objs)
            {
                // best-effort cast; if not compatible, skip
                if (o is UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rCast)
                    found.Add(rCast);
            }
        }

        if (found.Count > 0)
        {
            uiRays = found.ToArray();
        }
    }

    private void SizeDebugArrays()
    {
        if (uiRays == null) { _lastUIHitState = null; return; }
        _lastUIHitState = new bool[uiRays.Length];
    }

    private void LogFoundRaysOnce()
    {
        if (uiRays == null || uiRays.Length == 0)
        {
            Debug.LogWarning("[XRUI] No XR rays detected in scene. Assign them in the inspector or ensure XR Interaction Toolkit rays are present.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[XRUI] Found/Assigned {uiRays.Length} XRRayInteractor(s):");
        for (int i = 0; i < uiRays.Length; i++)
        {
            var r = uiRays[i];
            sb.AppendLine($"  [{i}] {(r ? r.name : "null")}");
        }
        Debug.Log(sb.ToString());
    }

    // ---------- Debug helpers ----------
    private void XRLog(int index,
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor ray,
        bool uiHit, GameObject uiObj,
        bool has3D, RaycastHit hit3D,
        string reasonHint)
    {
        if (!enableXRDebug) return;

        bool wantSpam = Application.isEditor && Input.GetKey(holdToLog);
        bool changed = (_lastUIHitState != null && index < _lastUIHitState.Length && _lastUIHitState[index] != uiHit);
        if (!wantSpam && logOnChangeOnly && !changed) return;

        var sb = new StringBuilder();
        string rayName = ray != null ? ray.name : "null";

        sb.AppendLine($"[XRUI] Ray[{index}] \"{rayName}\" active={(ray != null && ray.isActiveAndEnabled)}");

        if (ray != null)
        {
            // interaction layers + raycast mask
            sb.AppendLine($"  InteractionLayerMask: {ray.interactionLayers.value}");

            // Some XRI versions expose raycastMask/maxRaycastDistance as fields/properties; use reflection safely.
            int mask = GetIntFieldOrProperty(ray, "raycastMask", -1);
            if (mask != -1) sb.AppendLine($"  RaycastMask: {LayerMaskToString((LayerMask)mask)}");

            float maxDist = GetFloatFieldOrProperty(ray, "maxRaycastDistance", -1f);
            if (maxDist > 0f) sb.AppendLine($"  MaxRaycastDistance: {maxDist:F2}");

            // UI hit
            if (uiHit && uiObj != null)
                sb.AppendLine($"  UI Hit -> {uiObj.name} (layer {uiObj.layer})");
            else
                sb.AppendLine("  UI Hit: none");

            // 3D hit
            if (has3D && hit3D.collider != null)
                sb.AppendLine($"  3D Hit -> {hit3D.collider.name} (layer {hit3D.collider.gameObject.layer}) @ {hit3D.distance:F2}m");
            else
                sb.AppendLine("  3D Hit: none");
        }

        if (!string.IsNullOrEmpty(reasonHint))
            sb.AppendLine($"  HINT: {reasonHint}");

        Debug.Log(sb.ToString());

        if (_lastUIHitState != null && index < _lastUIHitState.Length)
            _lastUIHitState[index] = uiHit;
    }

    private static float GetFloatFieldOrProperty(object obj, string name, float fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();
        var pi = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        if (pi != null && pi.PropertyType == typeof(float))
        {
            try { return (float)pi.GetValue(obj); } catch { }
        }
        var fi = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        if (fi != null && fi.FieldType == typeof(float))
        {
            try { return (float)fi.GetValue(obj); } catch { }
        }
        return fallback;
    }

    private static int GetIntFieldOrProperty(object obj, string name, int fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();
        var pi = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        if (pi != null && pi.PropertyType == typeof(int))
        {
            try { return (int)pi.GetValue(obj); } catch { }
        }
        var fi = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        if (fi != null && fi.FieldType == typeof(int))
        {
            try { return (int)fi.GetValue(obj); } catch { }
        }
        return fallback;
    }

    private string LayerMaskToString(LayerMask mask)
    {
        if (mask.value == ~0) return "Everything";
        var sb = new StringBuilder();
        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask.value & (1 << layer)) != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                var name = LayerMask.LayerToName(layer);
                sb.Append(string.IsNullOrEmpty(name) ? layer.ToString() : name);
            }
        }
        return sb.Length > 0 ? sb.ToString() : mask.value.ToString();
    }
}
