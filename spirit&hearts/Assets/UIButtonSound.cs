using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IHoverState
{
    [Header("Clips")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Header("Hover Cooldown")]
    public float hoverCooldown = 0.25f;   // seconds between hover plays
    private float _nextHoverAt = -1f;

    private bool _isHovered;
    public bool IsHovered => _isHovered;

    void EnsureBus()
    {
        if (UIAudioBus.Instance == null)
        {
            var go = new GameObject("UIAudioBus");
            go.hideFlags = HideFlags.DontSave;
            go.AddComponent<UIAudioBus>();
        }
    }

    void Awake() => EnsureBus();

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;

        if (Time.unscaledTime < _nextHoverAt) return;

        if (hoverClip != null)
        {
            UIAudioBus.Instance.RequestHover(hoverClip, this);
            _nextHoverAt = Time.unscaledTime + hoverCooldown; // arm cooldown
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        // no sound on exit by design
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickClip != null)
        {
            UIAudioBus.Instance.PlayClick(clickClip);
            _nextHoverAt = Mathf.Max(_nextHoverAt, Time.unscaledTime + 0.1f);
        }
    }
}
