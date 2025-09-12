using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.EventSystems;
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
        // When re-enabled, make sure visual state matches flag
        ApplyHover(_isHovered);
    }

    void OnDisable()
    {
        // If disabled while hovered, force a clean exit style
        if (_appliedHover) ApplyHover(false, force:true);
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
            ApplyHover(false, force:true);
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

    private void ApplyHover(bool on, bool force=false)
    {
        if (target == null) return;

        if (on)
        {
            if (_appliedHover && !force) return; // already applied
            target.unlitColor = hoverUnlitColor;
            target.unlitIntensity = hoverUnlitIntensity;
            target.StartTransition(target.isLit);
            _appliedHover = true;
        }
        else
        {
            if (!_appliedHover && !force) return; // already off
            target.unlitColor = _baseUnlitColor;
            target.unlitIntensity = _baseUnlitIntensity;
            target.StartTransition(target.isLit);
            _appliedHover = false;
        }
    }

    // Raycast check against current pointer position; handles mouse + UI system.
    private bool PointerCurrentlyOverThis()
    {
        if (EventSystem.current == null) return false;

        // If no mouse present (e.g., gamepad), you can skip this check.
        if (Input.mousePresent)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            var results = ListPool<RaycastResult>.Get(); // any list pool; or new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            bool over = false;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject == gameObject || results[i].gameObject.transform.IsChildOf(transform))
                {
                    over = true; break;
                }
            }
            results.Clear();
            ListPool<RaycastResult>.Release(results);
            return over;
        }

        // Fallback: if no mouse, trust our flag (Enter/Exit from XR/UI should keep it consistent)
        return _isHovered;
    }
}
