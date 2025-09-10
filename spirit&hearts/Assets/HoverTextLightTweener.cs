using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class HoverTextLightTweener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Find target automatically if left empty")]
    public TextLightController target;

    [Header("Hover (unlit) overrides")]
    public Color hoverUnlitColor = new Color(1f, 0.9f, 0.6f, 1f); // warm gold
    public float hoverUnlitIntensity = 1.2f;

    // backup
    private Color _baseUnlitColor;
    private float _baseUnlitIntensity;
    private bool _cached;

    void Awake()
    {
        CacheTarget();
        CacheBase();
    }

    void OnEnable()
    {
        CacheTarget();
        CacheBase();
    }

    private void CacheTarget()
    {
        if (target != null) return;

        // Try to find a TMP child first, then its TextLightController
        var tmp = GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (tmp != null)
            target = tmp.GetComponent<TextLightController>();

        // Fallback: find any TextLightController in children
        if (target == null)
            target = GetComponentInChildren<TextLightController>(includeInactive: true);
    }

    private void CacheBase()
    {
        if (target == null || _cached) return;
        _baseUnlitColor = target.unlitColor;
        _baseUnlitIntensity = target.unlitIntensity;
        _cached = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target == null) return;

        // apply hover values
        target.unlitColor = hoverUnlitColor;
        target.unlitIntensity = hoverUnlitIntensity;

        // force re-eval so you see it even if isLit is already false
        target.StartTransition(target.isLit);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target == null) return;

        // restore base values
        target.unlitColor = _baseUnlitColor;
        target.unlitIntensity = _baseUnlitIntensity;

        // refresh again
        target.StartTransition(target.isLit);
    }
}
