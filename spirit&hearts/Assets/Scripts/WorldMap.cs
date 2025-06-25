using UnityEngine;
using System.Collections.Generic;

public enum WorldTileRole
{
    Landmark,
    Funnel,
    Bridge,
    Wander
}

public static class WorldMap
{
    public static readonly Dictionary<Vector2Int, string> Landmarks = new()
    {
        { new Vector2Int(0, 0), "Heart Lighthouse" },
        { new Vector2Int(3, 3), "Cliff Tower" },
        { new Vector2Int(-2, 4), "Ancient Bell" }
    };

    public static WorldTileRole GetRole(Vector2Int coord)
    {
        if (Landmarks.ContainsKey(coord))
            return WorldTileRole.Landmark;

        foreach (var landmark in Landmarks.Keys)
        {
            if (IsBridgeTile(landmark, coord))
                return WorldTileRole.Bridge;

            if (IsFunnelTile(landmark, coord))
                return WorldTileRole.Funnel;
        }

        return WorldTileRole.Wander;
    }

    private static bool IsBridgeTile(Vector2Int landmark, Vector2Int coord)
    {
        Vector2Int delta = coord - landmark;
        return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;
    }

    private static bool IsFunnelTile(Vector2Int landmark, Vector2Int coord)
    {
        Vector2Int delta = coord - landmark;
        return Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1;
    }
}
