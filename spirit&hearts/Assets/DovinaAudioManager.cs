using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DovinaAudioManager : MonoBehaviour
{
    public AudioSource audioSource;

    private Dictionary<string, AudioClip[]> audioCategories = new();
    private Queue<AudioClip> clipQueue = new();
    private bool isOnCooldown = false;
    private bool isPriorityPlaying = false;

    private Coroutine cooldownCoroutine;
    private float cooldownRemaining = 0f;
    private float cooldownTotal = 0f;

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
            "gp_changes/locations",
            "gp_changes/movement/hovering",
            "gp_changes/movement/toGliding",
            "gp_changes/movement/gliding",
            "gp_changes/movement/toHovering",
            "gp_changes/movement/bouncing",
            "gp_changes/light",
            "parables"
        };

        foreach (var path in categories)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>($"Dovina/Audio/{path}");
            List<AudioClip> validClips = new();

#if UNITY_EDITOR
            foreach (var clip in clips)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(clip);
                if (assetPath.Replace("\\", "/").Contains($"/{path}/"))
                    validClips.Add(clip);
            }
#else
            validClips.AddRange(clips);
#endif

            if (validClips.Count > 0)
                audioCategories[path] = validClips.ToArray();
        }
    }

    public AudioClip[] GetClips(string category)
    {
        if (audioCategories.TryGetValue(category, out var clips))
            return clips;
        return null;
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        if (isPriorityPlaying)
        {
            clipQueue.Enqueue(clip);
            return;
        }

        if (isOnCooldown || audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.Play();

        StartNewCooldown();
    }

    public void PlayRandom(string category)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return;

        AudioClip chosen = clips[Random.Range(0, clips.Length)];

        PlayClip(chosen);
    }

    public void PlaySpecific(string category, int index)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || index < 0 || index >= clips.Length) return;

        AudioClip chosen = clips[index];

        PlayClip(chosen);
    }

    public void PlayPriority(string category, int index = 0, int endIndex = -1)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return;

        int selectedIndex = endIndex == -1
            ? index
            : Random.Range(Mathf.Clamp(index, 0, clips.Length - 1), Mathf.Clamp(endIndex + 1, 0, clips.Length));

        if (selectedIndex < 0 || selectedIndex >= clips.Length) return;

        AudioClip clip = clips[selectedIndex];
        PlayPriorityClip(clip);
    }

    public void PlayPriorityClip(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource.isPlaying)
            audioSource.Stop();

        isPriorityPlaying = true;
        audioSource.clip = clip;
        audioSource.Play();

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownRemaining = Mathf.Max(cooldownRemaining, 0f);
        }

        StartCoroutine(ResumeCooldownAfterPriority(clip.length));
    }

    private IEnumerator ResumeCooldownAfterPriority(float clipLength)
    {
        float timer = 0f;
        while (timer < clipLength)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isPriorityPlaying = false;

        if (clipQueue.Count > 0)
        {
            PlayNextQueuedClip();
        }
        else if (cooldownRemaining > 0f)
        {
            cooldownCoroutine = StartCoroutine(CooldownTimer());
        }
    }

    private void PlayNextQueuedClip()
    {
        if (clipQueue.Count == 0) return;

        var nextClip = clipQueue.Dequeue();
        if (nextClip == null) return;

        audioSource.clip = nextClip;
        audioSource.Play();

        StartNewCooldown();
    }

    private void StartNewCooldown()
    {
        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);

        cooldownTotal = Random.Range(20f, 30f);
        cooldownRemaining = cooldownTotal;
        cooldownCoroutine = StartCoroutine(CooldownTimer());
    }

    private IEnumerator CooldownTimer()
    {
        isOnCooldown = true;

        while (cooldownRemaining > 0f)
        {
            if (isPriorityPlaying)
                yield break;

            cooldownRemaining -= Time.deltaTime;
            yield return null;
        }

        isOnCooldown = false;
    }

    public bool IsPlaying => audioSource.isPlaying;
    public bool IsOnCooldown => isOnCooldown;
}
