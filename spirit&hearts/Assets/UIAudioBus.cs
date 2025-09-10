using UnityEngine;

public interface IHoverState { bool IsHovered { get; } }

public class UIAudioBus : MonoBehaviour
{
    public static UIAudioBus Instance { get; private set; }

    [Header("Shared UI Audio Source (2D)")]
    public AudioSource audioSource;

    // click-lock
    private float lockUntil = -1f; // Time.unscaledTime when we may play hover again

    // pending hover request
    private AudioClip pendingHoverClip;
    private IHoverState pendingHoverState;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI
        }
    }

    void Update()
    {
        // when click-lock ends, if the requested button is still hovered, play hover
        if (pendingHoverClip && Time.unscaledTime >= lockUntil)
        {
            if (pendingHoverState != null && pendingHoverState.IsHovered)
                PlayNow(pendingHoverClip);

            pendingHoverClip = null;
            pendingHoverState = null;
        }
    }

    public void PlayClick(AudioClip clip)
    {
        if (!clip) return;
        PlayNow(clip);
        lockUntil = Time.unscaledTime + clip.length; // lock hovers until click finishes
        // clear any previous pending; a new hover may re-queue
        pendingHoverClip = null;
        pendingHoverState = null;
    }

    public void RequestHover(AudioClip clip, IHoverState state)
    {
        if (!clip || state == null) return;

        // If a click is still playing, remember the latest hover request
        if (Time.unscaledTime < lockUntil)
        {
            pendingHoverClip = clip;
            pendingHoverState = state;
            return;
        }

        // No lock: only play if still hovered at request time
        if (state.IsHovered)
            PlayNow(clip);
    }

    private void PlayNow(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
