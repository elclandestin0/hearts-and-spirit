using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static string path => Application.persistentDataPath + "/save.json";

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static SaveData LoadLatestSave()
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return null;
    }

    public static void DeleteSave()
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
