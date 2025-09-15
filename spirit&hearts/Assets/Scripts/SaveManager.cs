using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Centralized JSON save/load manager.
/// Uses Unity's JsonUtility to serialize per-component states and a small envelope.
/// </summary>
public static class SaveManager
{
    [Serializable]
    private class SaveRecord
    {
        public string id;          // entity id
        public string type;        // component type
        public string json;        // json of component state
    }

    [Serializable]
    private class SaveEnvelope
    {
        public int version = 1;
        public string scene;
        public string timestamp;
        public List<SaveRecord> records = new List<SaveRecord>();
    }

    private static readonly HashSet<SaveableEntity> entities = new HashSet<SaveableEntity>();

    public static void Register(SaveableEntity e) => entities.Add(e);
    public static void Unregister(SaveableEntity e) => entities.Remove(e);

    public static string GetSavePath(string slot)
    {
        var fname = $"save_{slot}.json";
        return Path.Combine(Application.persistentDataPath, fname);
    }

    public static void Save(string slot)
    {
        var env = new SaveEnvelope
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            timestamp = DateTime.UtcNow.ToString("o"),
            records = new List<SaveRecord>()
        };

        foreach (var e in entities)
        {
            foreach (var (type, state) in e.CaptureAll())
            {
                var record = new SaveRecord
                {
                    id = e.Id,
                    type = type,
                    json = JsonUtility.ToJson(new Box() { payload = state })
                };
                env.records.Add(record);
            }
        }

        var json = JsonUtility.ToJson(env, true);
        File.WriteAllText(GetSavePath(slot), json);

#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Saved {env.records.Count} records to: {GetSavePath(slot)}");
#endif
    }

    public static bool Load(string slot)
    {
        var path = GetSavePath(slot);
        if (!File.Exists(path))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[SaveManager] No save file found at {path}");
#endif
            return false;
        }

        var json = File.ReadAllText(path);
        var env = JsonUtility.FromJson<SaveEnvelope>(json);

        // group by entity id then feed back to components
        var byEntity = env.records.GroupBy(r => r.id);
        int restored = 0;
        foreach (var group in byEntity)
        {
            var entity = entities.FirstOrDefault(e => e.Id == group.Key);
            if (entity == null) continue;

            var list = new List<(string type, object state)>();
            foreach (var rec in group)
            {
                var boxed = JsonUtility.FromJson<Box>(rec.json);
                list.Add((rec.type, boxed.payload));
                restored++;
            }
            entity.RestoreAll(list);
        }

#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Loaded {restored} component states from: {path}");
#endif
        return true;
    }

    // Small helper since JsonUtility can't (de)serialize object directly.
    [Serializable]
    private class Box
    {
        public object payload;
    }
}
