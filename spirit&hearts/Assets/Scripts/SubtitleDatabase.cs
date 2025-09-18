using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "UI/Subtitle Database")]
public class SubtitleDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public AudioClip clip;          // exact VO file
        [TextArea] public string text;  // subtitle to show
    }

    [Header("Per-clip subtitles (preferred)")]
    public List<Entry> entries = new();

    [Header("Fallback by category (optional: matches Dovina categories like gp_changes/... )")]
    public List<CategoryEntry> categoryEntries = new();

    [System.Serializable]
    public class CategoryEntry
    {
        public string categoryPath;     // e.g. "gp_changes/movement/toGliding"
        [TextArea] public string text;  // generic line if you don't want per-clip
    }

    Dictionary<AudioClip, string> _byClip;
    Dictionary<string, string> _byCat;
    public bool ContainsClip(AudioClip clip) => entries.Exists(e => e != null && e.clip == clip);

    void OnEnable()
    {
        _byClip = new Dictionary<AudioClip, string>();
        foreach (var e in entries)
            if (e.clip && !string.IsNullOrEmpty(e.text)) _byClip[e.clip] = e.text;

        _byCat = new Dictionary<string, string>();
        foreach (var c in categoryEntries)
            if (!string.IsNullOrEmpty(c.categoryPath) && !string.IsNullOrEmpty(c.text))
                _byCat[c.categoryPath] = c.text;
    }

    public bool TryGet(AudioClip clip, out string text)
    {
        if (clip && _byClip != null && _byClip.TryGetValue(clip, out text)) return true;
        text = null; return false;
    }

    public void RebuildIndex()
    {
        _byClip = new Dictionary<AudioClip, string>();
        foreach (var e in entries)
        {
            if (e != null && e.clip && !string.IsNullOrEmpty(e.text))
                _byClip[e.clip] = e.text;
        }
    }
}
