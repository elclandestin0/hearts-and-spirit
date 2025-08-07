using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaAudioTrigger : MonoBehaviour
{
    public enum AreaAudioType { Landmark, LightPuzzle, Chill }

    [Header("Audio Settings")]
    public AreaAudioType audioType;
    public string audioCategory = "gp_changes/locations";
    public string locationType;
    public float cooldown = 1f;

    private float lastPlayTime;
    [SerializeField] private DovinaAudioManager dovinaAudioManager;

    private void OnTriggerEnter(Collider other)
    {
        // if (!other.CompareTag("Player") || Time.time - lastPlayTime < cooldown) return;
        // lastPlayTime = Time.time;
        Debug.Log("Attempting to play area trigger for " + locationType + " with object name: " + other.gameObject.name);
        if (!other.CompareTag("Player")) return;

        Debug.Log("Attempting to play area trigger for " + locationType);
        var clips = dovinaAudioManager.GetClips(audioCategory);
        if (clips == null || clips.Length == 0) return;

        if (TryGetMatchingClip(clips, out var selectedClip))
        {
            Debug.Log("Playing area trigger for " + locationType);
            dovinaAudioManager.PlayClip(selectedClip, 1);
        }
    }

    private bool TryGetMatchingClip(AudioClip[] clips, out AudioClip selectedClip)
    {
        selectedClip = null;
        var locationTypeLower = locationType.ToLower();
        var clip = clips.Where(c => c != null);

        LightController light = GetComponentInChildren<LightController>();

        switch (audioType)
        {
            case AreaAudioType.Landmark:
                string suffix = (light != null && light.isLit) ? "_lit" : "_unlit";
                selectedClip = clip
                    .FirstOrDefault(c => c.name.ToLower().Contains(locationTypeLower) && c.name.ToLower().EndsWith(suffix));
                if (!selectedClip) Debug.LogWarning($"No matching Landmark clip: {locationType}{suffix}");
                break;

            case AreaAudioType.LightPuzzle:
                if (light == null || light.isLit) return false;
                var lightClips = clip.Where(c => c.name.ToLower().Contains("lantern")).ToArray();
                if (lightClips.Length == 0) return false;
                selectedClip = lightClips[Random.Range(0, lightClips.Length)];
                break;

            case AreaAudioType.Chill:
                var chillClips = clip.Where(c => c.name.ToLower().Contains("chill")).ToArray();
                if (chillClips.Length == 0) return false;
                selectedClip = chillClips[Random.Range(0, chillClips.Length)];
                break;
        }

        return selectedClip != null;
    }
}
