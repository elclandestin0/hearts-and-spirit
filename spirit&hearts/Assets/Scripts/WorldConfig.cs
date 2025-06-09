using UnityEngine;
using System.Collections.Generic;
public static class WorldConfig
{
    public static readonly int minX = -8;
    public static readonly int maxX = 8;
    public static readonly int minZ = -8;
    public static readonly int maxZ = 8;

    // Landmarks...
    public static readonly Dictionary<Vector2Int, string> Landmarks = new()
    {
        { new Vector2Int(2, 3), "Level 1" },
        { new Vector2Int(-4, -4), "Level 2" },
        { new Vector2Int(-8, -6), "Level 3" }
    };

    public static bool IsLandmark(Vector2Int coord) =>
        Landmarks.ContainsKey(coord);

    public static string GetLandmarkName(Vector2Int coord) =>
        Landmarks.TryGetValue(coord, out var name) ? name : null;

    public static int ReflectCoord(int raw, int min, int max)
    {
        int range = max - min;

        if (raw <= max && raw >= min)
            return raw;

        int over = Mathf.Abs(raw - max);
        int under = Mathf.Abs(min - raw);

        if (raw > max)
            return max - (over % (range * 2 + 2));
        if (raw < min)
            return min + (under % (range * 2 + 2));

        return raw;
    }
}
