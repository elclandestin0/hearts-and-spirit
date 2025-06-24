using UnityEngine;
using System.Collections.Generic;

public static class WorldLandmarks
{
    public static readonly HashSet<Vector2Int> Landmarks = new()
    {
        new Vector2Int(0, 0),
        new Vector2Int(3, 3),
        new Vector2Int(-2, 4)
    };

    public static readonly HashSet<Vector2Int> Funnels = new()
    {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1)
    };

    public static bool IsLandmarkTile(Vector2Int coord)
    {
        return Landmarks.Contains(coord);
    }

    public static bool IsFunnelTile(Vector2Int coord)
    {
        return Funnels.Contains(coord);
    }
}
