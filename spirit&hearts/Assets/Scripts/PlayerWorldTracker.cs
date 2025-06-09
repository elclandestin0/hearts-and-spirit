using UnityEngine;
using System;

public static class PlayerWorldTracker
{
    public static Vector2Int CurrentCoord { get; private set; }
    public static event Action<Vector2Int> OnZoneChanged;

    private static Vector2Int lastCoord = new Vector2Int(int.MinValue, int.MinValue);

    public static void UpdateCoord(Vector3 worldPosition, int blockSize)
    {
        int rawX = Mathf.FloorToInt(worldPosition.x / blockSize);
        int rawZ = Mathf.FloorToInt(worldPosition.z / blockSize);

        int wrappedX = WorldConfig.WrapCoord(rawX, WorldConfig.minX, WorldConfig.maxX);
        int wrappedZ = WorldConfig.WrapCoord(rawZ, WorldConfig.minZ, WorldConfig.maxZ);

        Vector2Int newCoord = new Vector2Int(wrappedX, wrappedZ);

        if (newCoord != lastCoord)
        {
            lastCoord = newCoord;
            CurrentCoord = newCoord;
            OnZoneChanged?.Invoke(newCoord);
        }
    }
}
