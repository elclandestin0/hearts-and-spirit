using UnityEngine;
using System.Collections.Generic;

public static class WorldConfig
{
    public static readonly int minX = -8;
    public static readonly int maxX = 8;
    public static readonly int minZ = -8;
    public static readonly int maxZ = 8;

    // Fixed landmark placements
    public static readonly Dictionary<Vector2Int, string> Landmarks = new()
    {
        { new Vector2Int(2, 3), "Level 1" },
        { new Vector2Int(-4, -4), "Level 2" },
        { new Vector2Int(-8, -6), "Level 3" },
    };

    public static bool IsLandmark(Vector2Int coord)
    {
        return Landmarks.ContainsKey(coord);
    }

    public static string GetLandmarkName(Vector2Int coord)
    {
        return Landmarks.TryGetValue(coord, out var name) ? name : null;
    }
}