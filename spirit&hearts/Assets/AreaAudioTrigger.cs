using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaAudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public string audioCategory = "gp_changes/locations";
    public string locationType;
    public float cooldown = 1f;

    private float lastPlayTime;
    [SerializeField] private DovinaAudioManager dovinaAudioManager;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // if (Time.time - lastPlayTime < cooldown) return;
        // lastPlayTime = Time.time;

        var clips = dovinaAudioManager.GetClips(audioCategory);
        if (clips == null || clips.Length == 0) return;

        LightController light = GetComponentInChildren<LightController>();
        string litSuffix = light != null && light.isLit ? "_lit" : "_unlit";

        // Find matching clips like "lighthouse_lit", "altar_unlit", etc.
        var matchingClips = clips
            .Where(clip =>
                clip != null &&
                clip.name.ToLower().Contains(locationType.ToLower()) &&
                clip.name.ToLower().EndsWith(litSuffix))
            .ToArray();

        if (matchingClips.Length == 0)
        {
            Debug.LogWarning($"No matching clips found for {locationType}{litSuffix}");
            return;
        }

        var selectedClip = matchingClips[Random.Range(0, matchingClips.Length)];
        dovinaAudioManager.PlayPriorityClip(selectedClip);
    }
}
