using UnityEngine;
using System;
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
    private readonly Queue<ClipRequest> clipQueue = new();                           // queued (clip, priority, context)

    // -------------------- COOLDOWNS --------------------
    [Header("Cooldown Durations (seconds)")]
    [Tooltip("Intro has NO cooldown (ignored).")]
    public float cd_speed_slow = 60f;
    public float cd_speed_fast = 60f;
    public float cd_wind = 60f;
    public float cd_world = 60f;
    [Tooltip("Per-PLACE cooldown inside locations (forest_, altar_, lighthouse_, etc).")]
    public float cd_locations_place = 60f;
    public float cd_locations = 30f;
    public float cd_move_hovering = 15f;
    public float cd_move_toGliding = 15f;
    public float cd_move_gliding = 60f;
    public float cd_move_toHovering = 15f;
    public float cd_bouncing = 15f;
    [Tooltip("Parables has NO cooldown (ignored).")]
    public float cd_parables_ignored = 0f;

    // Category → cooldown-until timestamp (applies to entire category)
    private readonly Dictionary<string, float> categoryCooldownUntil = new();
    // Locations per-place → cooldown-until
    private readonly Dictionary<string, float> placeCooldownUntil = new();

    // Priority / state
    private bool isPriorityPlaying = false;
    private int currentPriority = 0;

    // Global “chatter” cooldown (only for priority 0), unchanged
    private bool isOnCooldown = false;
    private float cooldownRemaining = 0f;
    private Coroutine cooldownCoroutine;

    // Do-not-queue categories while busy
    private readonly HashSet<string> doNotQueueWhenBusy = new()
    {
        "gp_changes/bouncing"
    };

    // Events for subtitles
    public event Action<AudioClip,int> OnClipStarted;
    public event Action<int> OnClipEnded;
    public event Action<int> OnClipInterrupted;
    int _playToken = 0;

    private void Awake() => LoadAllClips();

    // -------------------- LOAD --------------------
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

    // -------------------- Category helpers --------------------
    public AudioClip[] GetClips(string category) =>
        audioCategories.TryGetValue(category, out var clips) ? clips : null;

    public AudioClip GetClip(string category)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return null;
        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    private static bool IsIntro(string category) =>
        !string.IsNullOrEmpty(category) && category.Equals("intro", StringComparison.OrdinalIgnoreCase);

    private static bool IsParables(string category) =>
        !string.IsNullOrEmpty(category) && category.Equals("parables", StringComparison.OrdinalIgnoreCase);

    private static bool IsLocationsCategory(string category) =>
        !string.IsNullOrEmpty(category) &&
        category.StartsWith("gp_changes/locations", StringComparison.OrdinalIgnoreCase);

    // -------------------- Place helpers (locations) --------------------
    private static string GetPlaceKey(AudioClip clip)
    {
        if (clip == null) return null;
        var name = clip.name.Trim().ToLowerInvariant();
        int us = name.IndexOf('_');
        if (us <= 0) return null;
        return name.Substring(0, us); // "forest" from "forest_whatever"
    }

    private bool IsPlaceOnCooldown(string placeKey)
    {
        if (string.IsNullOrEmpty(placeKey)) return false;
        if (!placeCooldownUntil.TryGetValue(placeKey, out var until)) return false;
        return Time.time < until;
    }

    private void StartPlaceCooldown(string placeKey)
    {
        if (string.IsNullOrEmpty(placeKey)) return;
        placeCooldownUntil[placeKey] = Time.time + Mathf.Max(0.01f, cd_locations_place);
    }

    // Try to find a different clip in same category that isn't cooling down
    private AudioClip FindAlternateClipNotOnPlaceCooldown(string category)
    {
        if (string.IsNullOrEmpty(category)) return null;
        if (!audioCategories.TryGetValue(category, out var clips) || clips == null || clips.Length == 0)
            return null;

        int n = clips.Length;
        int start = UnityEngine.Random.Range(0, n);
        for (int i = 0; i < n; i++)
        {
            var c = clips[(start + i) % n];
            if (c == null) continue;
            var place = GetPlaceKey(c);
            if (!IsPlaceOnCooldown(place)) return c;
        }
        return null;
    }

    // -------------------- Category cooldown helpers --------------------
    private bool CategoryHasCooldown(string category)
    {
        if (string.IsNullOrEmpty(category)) return false;
        if (IsIntro(category) || IsParables(category)) return false; // no cooldowns
        return true;
    }

    private float GetCategoryCooldownDuration(string category)
    {
        if (string.IsNullOrEmpty(category)) return 0f;
        if (IsIntro(category) || IsParables(category)) return 0f;
        if (IsLocationsCategory(category)) return 0f; // handled per-place

        // Map folder names to inspector fields
        // (keep simple string checks; fast & readable)
        if (category.Equals("gp_changes/speed/slow", StringComparison.OrdinalIgnoreCase)) return cd_speed_slow;
        if (category.Equals("gp_changes/speed/fast", StringComparison.OrdinalIgnoreCase)) return cd_speed_fast;
        if (category.Equals("gp_changes/wind", StringComparison.OrdinalIgnoreCase)) return cd_wind;
        if (category.Equals("gp_changes/world", StringComparison.OrdinalIgnoreCase)) return cd_world;
        if (category.Equals("gp_changes/locations/chill_suggestions", StringComparison.OrdinalIgnoreCase)) return cd_locations;
        if (category.Equals("gp_changes/movement/hovering", StringComparison.OrdinalIgnoreCase)) return cd_move_hovering;
        if (category.Equals("gp_changes/movement/toGliding", StringComparison.OrdinalIgnoreCase)) return cd_move_toGliding;
        if (category.Equals("gp_changes/movement/gliding", StringComparison.OrdinalIgnoreCase)) return cd_move_gliding;
        if (category.Equals("gp_changes/movement/toHovering", StringComparison.OrdinalIgnoreCase)) return cd_move_toHovering;
        if (category.Equals("gp_changes/bouncing", StringComparison.OrdinalIgnoreCase)) return cd_bouncing;

        // default safety
        return 60f;
    }

    private bool IsCategoryOnCooldown(string category)
    {
        if (!CategoryHasCooldown(category)) return false;
        if (!categoryCooldownUntil.TryGetValue(category, out var until)) return false;
        Debug.Log("Category " + category + " on cooldown. Until: " + until);
        return Time.time < until;
    }

    private void StartCategoryCooldown(string category)
    {
        if (!CategoryHasCooldown(category)) return;
        float dur = Mathf.Max(0.01f, GetCategoryCooldownDuration(category));
        categoryCooldownUntil[category] = Time.time + dur;
    }

    // -------------------- Non-repeating bag --------------------
    private void RefillBag(string category)
    {
        if (!audioCategories.TryGetValue(category, out var src) || src.Length == 0) return;
        var list = new List<AudioClip>(src);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
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

    // -------------------- High-level selection (respects cooldowns) --------------------
    public void PlayNonRepeating(string category, int priority)
    {
        var clip = GetNonRepeatingClip(category);

        if (IsLocationsCategory(category))
        {
            var place = GetPlaceKey(clip);
            if (IsPlaceOnCooldown(place))
                clip = FindAlternateClipNotOnPlaceCooldown(category);
        }
        else
        {
            if (IsCategoryOnCooldown(category))
                return; // whole category cooling; skip
        }

        if (clip != null) PlayClip(clip, priority, category);
    }

    public void PlayFromCategory(string category, int priority)
    {
        if (IsLocationsCategory(category))
        {
            if (IsCategoryOnCooldown(category))
            {
                // Not expected for locations (we only cool per-place), but keep guard
            }

            var clip = GetClip(category);
            var place = GetPlaceKey(clip);
            if (IsPlaceOnCooldown(place))
                clip = FindAlternateClipNotOnPlaceCooldown(category);

            if (clip != null) PlayClip(clip, priority, category);
            return;
        }

        if (IsCategoryOnCooldown(category)) return;
        var clip2 = GetClip(category);
        if (clip2 != null) PlayClip(clip2, priority, category);
    }

    public void PlayPriority(string category, int priority, int startIndex = 0, int endIndexInclusive = -1)
    {
        if (!audioCategories.TryGetValue(category, out var clips) || clips.Length == 0) return;

        int idx = (endIndexInclusive == -1)
            ? Mathf.Clamp(startIndex, 0, clips.Length - 1)
            : UnityEngine.Random.Range(Mathf.Clamp(startIndex, 0, clips.Length - 1), Mathf.Clamp(endIndexInclusive + 1, 0, clips.Length));

        AudioClip clip = (idx >= 0 && idx < clips.Length) ? clips[idx] : null;

        if (IsLocationsCategory(category))
        {
            var place = GetPlaceKey(clip);
            if (IsPlaceOnCooldown(place))
                clip = FindAlternateClipNotOnPlaceCooldown(category);

            if (clip != null) PlayClip(clip, priority, category);
            return;
        }

        if (IsCategoryOnCooldown(category)) return;
        if (clip != null) PlayClip(clip, priority, category);
    }

    public void PlayBackToBack(params (string category, int start, int endInclusive, int priority)[] items)
    {
        foreach (var it in items)
        {
            if (!audioCategories.TryGetValue(it.category, out var clips) || clips.Length == 0) continue;
            int idx = (it.endInclusive == -1)
                ? Mathf.Clamp(it.start, 0, clips.Length - 1)
                : UnityEngine.Random.Range(Mathf.Clamp(it.start, 0, clips.Length - 1), Mathf.Clamp(it.endInclusive + 1, 0, clips.Length));

            AudioClip choice = (idx >= 0 && idx < clips.Length) ? clips[idx] : null;
            if (choice == null) continue;

            if (IsLocationsCategory(it.category))
            {
                var place = GetPlaceKey(choice);
                if (IsPlaceOnCooldown(place))
                    choice = FindAlternateClipNotOnPlaceCooldown(it.category);
                if (choice == null) continue;
            }
            else
            {
                if (IsCategoryOnCooldown(it.category)) continue;
            }

            clipQueue.Enqueue(new ClipRequest(choice, it.priority, it.category));
        }
        if (!isPriorityPlaying) TryPlayNextQueued();
    }

    public void PlayClip(AudioClip clip, int priority, string context = null)
    {
        if (PauseState.IsPaused) return;
        if (clip == null) return;

        // Respect per-category/place cooldowns before playing
        if (IsLocationsCategory(context))
        {
            var place = GetPlaceKey(clip);
            if (IsPlaceOnCooldown(place))
            {
                var alt = FindAlternateClipNotOnPlaceCooldown(context);
                if (alt == null) return;
                clip = alt;
            }
        }

        else if (IsCategoryOnCooldown(context))
        {
            return;
        }

        // chatter (prio 0) respects chatter cooldown/now-playing → queue
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
                OnClipInterrupted?.Invoke(_playToken);
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

        PlayNow(clip, priority, context);
    }

    private void PlayNow(AudioClip clip, int priority, string context)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        Debug.Log("Playing clip " + clip.name);

        isPriorityPlaying = true;
        currentPriority = priority;
        _playToken++;
        OnClipStarted?.Invoke(clip, _playToken);

        // Start cooldowns right when we play
        if (IsLocationsCategory(context))
        {
            var place = GetPlaceKey(clip);
            StartPlaceCooldown(place);
        }
        else
        {
            StartCategoryCooldown(context);
        }

        if (priority == 0)
        {
            isOnCooldown = true;
            cooldownRemaining = UnityEngine.Random.Range(20f, 30f);
            StartCoroutine(ResumeCooldownAfterClip(clip.length, true, _playToken));
        }
        else
        {
            StartCoroutine(ResumeCooldownAfterClip(clip.length, false, _playToken));
        }
    }

    private IEnumerator ResumeCooldownAfterClip(float clipLength, bool isChatter, int token)
    {
        yield return new WaitForSeconds(clipLength);
        if (token != _playToken) yield break;
        isPriorityPlaying = false;
        OnClipEnded?.Invoke(token);

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

            // Respect cooldowns at dequeue time, too
            if (IsLocationsCategory(req.context))
            {
                var place = GetPlaceKey(req.clip);
                if (IsPlaceOnCooldown(place))
                {
                    var alt = FindAlternateClipNotOnPlaceCooldown(req.context);
                    if (alt == null) continue; // skip this request
                    req = new ClipRequest(alt, req.priority, req.context);
                }
            }
            else
            {
                if (IsCategoryOnCooldown(req.context)) continue;
            }

            // skip chatter if chatter-cooldown still active
            if (req.priority == 0 && isOnCooldown) continue;

            PlayNow(req.clip, req.priority, req.context);
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
    public bool IsOnCooldown => isOnCooldown;                 // chatter cooldown (prio 0)
    public bool IsPriorityPlaying => isPriorityPlaying;
    public int CurrentPriority => currentPriority;
}
