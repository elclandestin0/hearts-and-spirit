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
    // v1 stored state in an `object` field, which JsonUtility silently writes as "{}".
    // v2 serializes the concrete state type by name so the payload survives the round trip.
    private const int CurrentVersion = 2;

    [Serializable]
    private class SaveRecord
    {
        public string id;          // entity id
        public string type;        // component type
        public string stateType;   // concrete type of the state object
        public string json;        // json of component state
    }

    [Serializable]
    private class SaveEnvelope
    {
        public int version = CurrentVersion;
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
            version = CurrentVersion,
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            timestamp = DateTime.UtcNow.ToString("o"),
            records = new List<SaveRecord>()
        };

        var seenIds = new HashSet<string>();

        foreach (var e in entities)
        {
            if (e == null) continue;

            if (!seenIds.Add(e.Id))
                Debug.LogWarning($"[SaveManager] Duplicate entity id '{e.Id}' on '{e.name}'. " +
                                 "Only one of these will be restored.");

            foreach (var (type, state) in e.CaptureAll())
            {
                if (state == null) continue;

                env.records.Add(new SaveRecord
                {
                    id = e.Id,
                    type = type,
                    stateType = state.GetType().AssemblyQualifiedName,
                    json = JsonUtility.ToJson(state)
                });
            }
        }

        var json = JsonUtility.ToJson(env, true);
        File.WriteAllText(GetSavePath(slot), json);
        GameLog.Log($"[SaveManager] Saved {env.records.Count} records to: {GetSavePath(slot)}");
    }

    public static bool Load(string slot)
    {
        var path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            GameLog.Log($"[SaveManager] No save file found at {path}");
            return false;
        }

        var json = File.ReadAllText(path);
        var env = JsonUtility.FromJson<SaveEnvelope>(json);
        if (env == null) return false;

        if (env.version < CurrentVersion)
        {
            Debug.LogWarning($"[SaveManager] Save at {path} is version {env.version} " +
                             $"(current {CurrentVersion}) and carries no usable state. Ignoring it.");
            return false;
        }

        // group by entity id then feed back to components
        var byEntity = env.records.GroupBy(r => r.id);
        int restored = 0;
        foreach (var group in byEntity)
        {
            var entity = entities.FirstOrDefault(e => e != null && e.Id == group.Key);
            if (entity == null) continue;

            var list = new List<(string type, object state)>();
            foreach (var rec in group)
            {
                var state = Deserialize(rec);
                if (state == null) continue;
                list.Add((rec.type, state));
                restored++;
            }
            entity.RestoreAll(list);
        }

        GameLog.Log($"[SaveManager] Loaded {restored} component states from: {path}");
        return true;
    }

    private static object Deserialize(SaveRecord rec)
    {
        if (rec == null || string.IsNullOrEmpty(rec.stateType) || string.IsNullOrEmpty(rec.json))
            return null;

        var t = Type.GetType(rec.stateType);
        if (t == null)
        {
            Debug.LogWarning($"[SaveManager] Unknown state type '{rec.stateType}'; skipping record.");
            return null;
        }

        try
        {
            return JsonUtility.FromJson(rec.json, t);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Could not read state for '{rec.type}': {ex.Message}");
            return null;
        }
    }
}
