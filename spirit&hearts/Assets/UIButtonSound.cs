using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IHoverState
{
    [Header("Clips")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

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
        if (_isHovered) return;
        _isHovered = true;
        UIAudioBus.Instance.RequestHover(hoverClip, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        // no sound on exit by design
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIAudioBus.Instance.PlayClick(clickClip);
    }
}
