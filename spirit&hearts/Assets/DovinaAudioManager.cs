using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DovinaAudioManager : MonoBehaviour
{
    public AudioSource audioSource;

    private Dictionary<string, AudioClip[]> audioCategories = new();
    private bool isOnCooldown = false;
    private Coroutine cooldownCoroutine;

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
        if (isOnCooldown || audioSource.isPlaying) return;

        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return;

        AudioClip chosen = clips[Random.Range(0, clips.Length)];
        audioSource.clip = chosen;
        audioSource.Play();
        Debug.Log($"[DovinaAudioManager] Playing: {category}");

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownCoroutine());
    }

    public void PlaySpecific(string category, int index)
    {
        if (isOnCooldown || audioSource.isPlaying) return;

        if (!audioCategories.TryGetValue(category, out var clips) || index < 0 || index >= clips.Length) return;

        audioSource.clip = clips[index];
        audioSource.Play();
        Debug.Log($"[DovinaAudioManager] Playing specific: {category} #{index}");

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownCoroutine());
    }

    public void PlayPriority(string category, int index)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || index < 0 || index >= clips.Length) return;

        if (audioSource.isPlaying)
            audioSource.Stop(); // stop anything currently playing

        audioSource.clip = clips[index];
        audioSource.Play();
        Debug.Log($"[DovinaAudioManager] Playing PRIORITY: {category} #{index}");

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownCoroutineAfterClip(clips[index].length));
    }

    private IEnumerator CooldownCoroutine()
    {
        isOnCooldown = true;
        float waitTime = Random.Range(45f, 90f);
        yield return new WaitForSeconds(waitTime);
        isOnCooldown = false;
    }

    private IEnumerator CooldownCoroutineAfterClip(float clipLength)
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(clipLength); // wait for clip to finish
        float waitTime = Random.Range(45f, 90f);
        yield return new WaitForSeconds(waitTime);
        isOnCooldown = false;
    }

    public bool IsPlaying => audioSource.isPlaying;
    public bool IsOnCooldown => isOnCooldown;
}
