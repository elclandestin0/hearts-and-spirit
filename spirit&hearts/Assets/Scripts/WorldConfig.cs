using UnityEngine;
using System.Collections.Generic;
public static class WorldConfig
{
    public static readonly int minX = -3;
    public static readonly int maxX = 3;
    public static readonly int minZ = -3;
    public static readonly int maxZ = 3;
    public static int WrapCoord(int raw, int min, int max)
    {
        int rangeSize = max - min + 1;
        int wrapped = ((raw - min) % rangeSize + rangeSize) % rangeSize + min;
        return wrapped;
    }
}
