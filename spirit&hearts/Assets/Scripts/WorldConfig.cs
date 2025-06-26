using UnityEngine;
using System.Collections.Generic;
public static class WorldConfig
{
    public static readonly int minX = -3;
    public static readonly int maxX = 3;
    public static readonly int minZ = -3;
    public static readonly int maxZ = 3;

    // Landmarks...
    public static readonly Dictionary<Vector2Int, string> Landmarks = new()
    {
        { new Vector2Int(2, 3), "Level 1" },
        { new Vector2Int(-4, -4), "Level 2" },
        { new Vector2Int(-8, -6), "Level 3" }
    };
    public static int WrapCoord(int raw, int min, int max)
    {
        int rangeSize = max - min + 1;
        int wrapped = ((raw - min) % rangeSize + rangeSize) % rangeSize + min;
        return wrapped;
    }

}
