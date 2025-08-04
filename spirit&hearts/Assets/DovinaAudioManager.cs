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
            "gp_changes/movement/hovering/_gliding",
            "gp_changes/movement/gliding",
            "gp_changes/movement/gliding/_hovering",
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

    public void PlayPriority(string category, int index = 0, int endIndex = -1)
    {
        Debug.Log($"[DovinaAudioManager] Attempting to return clips.");
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0)
            return;
        
        Debug.Log($"[DovinaAudioManager] Returned clips from category " + category + ".");
        // If endIndex is -1 (default), just play the specific index
        if (endIndex == -1)
        {
            Debug.Log($"[DovinaAudioManager] Trying to play track.");
            if (index < 0 || index >= clips.Length) return;

            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.clip = clips[index];
            audioSource.Play();
            Debug.Log($"[DovinaAudioManager] Playing PRIORITY: {category} #{index}.");

            if (cooldownCoroutine != null)
                StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownCoroutineAfterClip(clips[index].length));
            return;
        }

        // Otherwise, play a random clip between index and endIndex
        index = Mathf.Clamp(index, 0, clips.Length - 1);
        endIndex = Mathf.Clamp(endIndex, index, clips.Length - 1); // ensure endIndex >= index

        int randomIndex = Random.Range(index, endIndex + 1);

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = clips[randomIndex];
        audioSource.Play();
        Debug.Log($"[DovinaAudioManager] Playing PRIORITY (RANGE): {category} #{randomIndex}");

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownCoroutineAfterClip(clips[randomIndex].length));
    }


    private IEnumerator CooldownCoroutine()
    {
        isOnCooldown = true;
        float waitTime = Random.Range(45f, 60f);
        yield return new WaitForSeconds(waitTime);
        isOnCooldown = false;
    }

    private IEnumerator CooldownCoroutineAfterClip(float clipLength)
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(clipLength);
        float waitTime = Random.Range(45f, 60f);
        yield return new WaitForSeconds(waitTime);
        isOnCooldown = false;
    }

    public bool IsPlaying => audioSource.isPlaying;
    public bool IsOnCooldown => isOnCooldown;
}
