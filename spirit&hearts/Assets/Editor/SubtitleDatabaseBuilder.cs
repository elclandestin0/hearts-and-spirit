// Assets/Editor/SubtitleDatabaseBuilder.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Globalization;

public static class SubtitleDatabaseBuilder
{
    private const string kScanRoot = "Assets/Resources/Dovina/Audio";
    private const string kDefaultDBPath = "Assets/Resources/Dovina/SubtitleDatabase.asset";

    [MenuItem("Tools/Dovina/Build/Refresh Subtitle Database")]
    public static void BuildOrRefresh()
    {
        // Find or create DB
        var db = AssetDatabase.LoadAssetAtPath<SubtitleDatabase>(kDefaultDBPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<SubtitleDatabase>();
            EnsureFolder("Assets/Resources/Dovina/Audio");
            AssetDatabase.CreateAsset(db, kDefaultDBPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SubtitleDB] Created new database at {kDefaultDBPath}");
        }

        Undo.RecordObject(db, "Update Subtitle Database");

        // Scan all AudioClips in the folder
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { kScanRoot });
        int added = 0, total = guids.Length;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (!clip) continue;

                if (!db.ContainsClip(clip))
                {
                    db.entries.Add(new SubtitleDatabase.Entry
                    {
                        clip = clip,
                        text = Humanize(clip.name)
                    });
                    added++;
                }

                if (i % 25 == 0)
                    EditorUtility.DisplayProgressBar("Building Subtitle Database", clip ? clip.name : "Scanning…", (float)i / total);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        db.RebuildIndex();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SubtitleDB] Scanned {total} clips. Added {added} new entries. DB now has {db.entries.Count} items.\nPath: {kDefaultDBPath}");
        Selection.activeObject = db;
    }

    private static string Humanize(string raw)
    {
        // Turn "gp_changes-movement_toGliding_03" into "Gp Changes Movement To Gliding 03"
        string s = raw.Replace('_', ' ').Replace('-', ' ');
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
    }

    private static void EnsureFolder(string folderPath)
    {
        var parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
