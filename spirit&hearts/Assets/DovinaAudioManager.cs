using UnityEngine;
using System.Collections.Generic;

public class DovinaAudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    private Dictionary<string, AudioClip[]> audioCategories = new();

    private void Awake()
    {
        LoadAllClips();
    }

    private void LoadAllClips()
    {
        string[] categories = {
            "intro",
            "gp_changes/seed",
            "gp_changes/speed",
            "gp_changes/wind",
            "gp_changes/world",
            "gp_changes/movement/hovering",
            "gp_changes/movement/gliding",
            "gp_changes/movement/bouncing",
            "gp_changes/light",
            "parables"
        };

        foreach (var path in categories)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>($"Dovina/Audio/{path}");
            if (clips.Length > 0)
                audioCategories[path] = clips;
        }
    }

    public void PlayRandom(string category)
    {
        if (!audioCategories.ContainsKey(category))
        {
            Debug.LogWarning($"No audio clips found for category: {category}");
            return;
        }

        // If something is already playing, skip
        if (audioSource.isPlaying) return;
        AudioClip[] clips = audioCategories[category];
        if (clips.Length == 0) return;

        AudioClip chosen = clips[Random.Range(0, clips.Length)];
        audioSource.clip = chosen;
        audioSource.Play();
        Debug.Log("Playing " + category);
    }


    public void PlaySpecific(string category, int index)
    {
        if (!audioCategories.ContainsKey(category)) return;

        var clips = audioCategories[category];
        if (index < 0 || index >= clips.Length) return;

        audioSource.PlayOneShot(clips[index]);
    }

    public bool IsPlaying => audioSource.isPlaying;
}
