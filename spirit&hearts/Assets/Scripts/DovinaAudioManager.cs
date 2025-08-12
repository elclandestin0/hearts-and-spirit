using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DovinaAudioManager : MonoBehaviour
{
    [System.Serializable]
    private struct ClipRequest
    {
        public AudioClip clip;
        public int priority;
        public string context;
        public ClipRequest(AudioClip c, int p, string ctx = null) { clip = c; priority = p; context = ctx; }
    }

    public AudioSource audioSource;

    private readonly Dictionary<string, AudioClip[]> audioCategories = new();
    private readonly Dictionary<string, Queue<AudioClip>> nonRepeatingBags = new(); // shuffle-bag per category
    private readonly Queue<ClipRequest> clipQueue = new();                        // queued (clip, priority)

    // Priority / state
    private bool isPriorityPlaying = false;
    private int currentPriority = 0;

    // Cooldown (only for priority 0 chatter)
    private bool isOnCooldown = false;
    private float cooldownRemaining = 0f;
    private Coroutine cooldownCoroutine;
    // categories that should NOT be queued if something is already playing
    private readonly HashSet<string> doNotQueueWhenBusy = new()
    {
        "gp_changes/bouncing"
    };


    private void Awake() => LoadAllClips();

    private void LoadAllClips()
    {
        // Register the categories you actually ship
        string[] categories = {
            "intro",
            "gp_changes/seed",
            "gp_changes/speed/slow",
            "gp_changes/speed/fast",
            "gp_changes/wind",
            "gp_changes/world",
            "gp_changes/locations",
            "gp_changes/movement/hovering",
            "gp_changes/movement/toGliding",
            "gp_changes/movement/gliding",
            "gp_changes/movement/toHovering",
            "gp_changes/bouncing",
            "gp_changes/light",
            "parables"
        };

        foreach (var path in categories)
        {
            var clips = Resources.LoadAll<AudioClip>($"Dovina/Audio/{path}");
            var valid = new List<AudioClip>(clips.Length);

#if UNITY_EDITOR
            foreach (var clip in clips)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(clip);
                if (assetPath.Replace("\\", "/").Contains($"/{path}/"))
                    valid.Add(clip);
            }
#else
            valid.AddRange(clips);
#endif
            if (valid.Count > 0) audioCategories[path] = valid.ToArray();
        }
    }

    // -------- Category access --------
    public AudioClip[] GetClips(string category) =>
        audioCategories.TryGetValue(category, out var clips) ? clips : null;

    public AudioClip GetClip(string category)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    // -------- Non-repeating (shuffle bag) --------
    private void RefillBag(string category)
    {
        if (!audioCategories.TryGetValue(category, out var src) || src.Length == 0) return;
        var list = new List<AudioClip>(src);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        nonRepeatingBags[category] = new Queue<AudioClip>(list);
    }

    public AudioClip GetNonRepeatingClip(string category)
    {
        if (!nonRepeatingBags.TryGetValue(category, out var bag) || bag == null || bag.Count == 0)
        {
            RefillBag(category);
            nonRepeatingBags.TryGetValue(category, out bag);
        }
        if (bag == null || bag.Count == 0) return null;
        return bag.Dequeue();
    }

    public void PlayNonRepeating(string category, int priority)
    {
        var clip = GetNonRepeatingClip(category);
        if (clip != null) PlayClip(clip, priority, category);
    }

    // -------- High-level helpers --------
    // Random in category at given priority (uses queue rules)
    public void PlayFromCategory(string category, int priority)
    {
        var clip = GetClip(category);
        if (clip != null) PlayClip(clip, priority, category);
    }

    // Index/range in category at given priority (uses queue rules)
    public void PlayPriority(string category, int priority, int startIndex = 0, int endIndexInclusive = -1)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return;

        int idx = (endIndexInclusive == -1)
            ? Mathf.Clamp(startIndex, 0, clips.Length - 1)
            : Random.Range(Mathf.Clamp(startIndex, 0, clips.Length - 1), Mathf.Clamp(endIndexInclusive + 1, 0, clips.Length));

        if (idx < 0 || idx >= clips.Length) return;
        PlayClip(clips[idx], priority, category);
    }

    // Back-to-back ordered enqueue helper
    public void PlayBackToBack(params (string category, int start, int endInclusive, int priority)[] items)
    {
        foreach (var it in items)
        {
            if (!audioCategories.TryGetValue(it.category, out var clips) || clips.Length == 0) continue;
            int idx = (it.endInclusive == -1)
                ? Mathf.Clamp(it.start, 0, clips.Length - 1)
                : Random.Range(Mathf.Clamp(it.start, 0, clips.Length - 1), Mathf.Clamp(it.endInclusive + 1, 0, clips.Length));
            if (idx < 0 || idx >= clips.Length) continue;
            clipQueue.Enqueue(new ClipRequest(clips[idx], it.priority));
        }
        if (!isPriorityPlaying) TryPlayNextQueued();
    }

    // -------- Core play w/ priority & queue rules --------
    public void PlayClip(AudioClip clip, int priority, string context = null)
    {
        if (clip == null) return;

        // chatter (prio 0) respects cooldown/now-playing → queue
        if (priority == 0 && (isOnCooldown || isPriorityPlaying))
        {
            clipQueue.Enqueue(new ClipRequest(clip, priority, context));
            return;
        }

        if (isPriorityPlaying)
        {
            if (priority > currentPriority) 
            { 
                Debug.Log("stopping audio"); 
                audioSource.Stop();
            }
            else
            {
                if (doNotQueueWhenBusy.Contains(context))
                {
                    Debug.Log("Context is in do-not-queue list, skipping queue.");
                    return;
                }
                clipQueue.Enqueue(new ClipRequest(clip, priority, context));
                return;
            }
        }

        PlayNow(clip, priority);
    }


    private void PlayNow(AudioClip clip, int priority)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        isPriorityPlaying = true;
        currentPriority = priority;

        if (priority == 0)
        {
            isOnCooldown = true;
            cooldownRemaining = Random.Range(20f, 30f);
            StartCoroutine(ResumeCooldownAfterClip(clip.length, /*isChatter*/ true));
        }
        else
        {
            StartCoroutine(ResumeCooldownAfterClip(clip.length, /*isChatter*/ false));
        }
    }


    private IEnumerator ResumeCooldownAfterClip(float clipLength, bool isChatter)
    {
        yield return new WaitForSeconds(clipLength);
        isPriorityPlaying = false;

        // After a clip ends, always try queue first
        if (!TryPlayNextQueued())
        {
            // If no queued item and we had chatter cooldown to run, tick it down
            if (isChatter && cooldownRemaining > 0f && cooldownCoroutine == null)
                cooldownCoroutine = StartCoroutine(CooldownTimer());
        }
    }

    private bool TryPlayNextQueued()
    {
        while (clipQueue.Count > 0)
        {
            var req = clipQueue.Dequeue();
            // skip chatter if cooldown still active
            if (req.priority == 0 && isOnCooldown) continue;

            PlayNow(req.clip, req.priority);
            return true;
        }
        return false;
    }
    private IEnumerator CooldownTimer()
    {
        while (cooldownRemaining > 0f)
        {
            if (isPriorityPlaying) { cooldownCoroutine = null; yield break; } // pause while something plays
            cooldownRemaining -= Time.deltaTime;
            yield return null;
        }
        isOnCooldown = false;
        cooldownCoroutine = null;
    }


    // -------- Status --------
    public bool IsPlaying => audioSource.isPlaying;
    public bool IsOnCooldown => isOnCooldown;
    public bool IsPriorityPlaying => isPriorityPlaying;
    public int CurrentPriority => currentPriority;
}
