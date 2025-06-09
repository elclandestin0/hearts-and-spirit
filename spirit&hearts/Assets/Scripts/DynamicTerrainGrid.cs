using UnityEngine;
using System.Collections.Generic;

public class DynamicTerrainGrid : MonoBehaviour
{
    [Header("Setup")]
    public GameObject terrainBlockPrefab;
    public Transform player;
    public int blockSize = 200;

    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Vector2Int currentReflectedCenter;

    void Start()
    {
        Vector2Int rawCoord = new Vector2Int(
            Mathf.FloorToInt(player.position.x / blockSize),
            Mathf.FloorToInt(player.position.z / blockSize)
        );
        UpdateGridAroundPlayer(rawCoord);
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        Vector2Int rawCoord = new Vector2Int(
            Mathf.FloorToInt(player.position.x / blockSize),
            Mathf.FloorToInt(player.position.z / blockSize)
        );

        Vector2Int reflectedCoord = new Vector2Int(
            WorldConfig.ReflectCoord(rawCoord.x, WorldConfig.minX, WorldConfig.maxX),
            WorldConfig.ReflectCoord(rawCoord.y, WorldConfig.minZ, WorldConfig.maxZ)
        );

        PlayerWorldTracker.UpdateCoord(player.position, blockSize);

        if (reflectedCoord != currentReflectedCenter)
        {
            currentReflectedCenter = reflectedCoord;
            UpdateGridAroundPlayer(rawCoord); // ← pass raw!
        }
    }

    void UpdateGridAroundPlayer(Vector2Int rawCenterCoord)
    {
        HashSet<Vector2Int> newTileKeys = new();

        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                Vector2Int rawCoord = new Vector2Int(rawCenterCoord.x + dx, rawCenterCoord.y + dz);
                Vector2Int reflectedCoord = new Vector2Int(
                    WorldConfig.ReflectCoord(rawCoord.x, WorldConfig.minX, WorldConfig.maxX),
                    WorldConfig.ReflectCoord(rawCoord.y, WorldConfig.minZ, WorldConfig.maxZ)
                );

                newTileKeys.Add(rawCoord); // ✅ Track by RAW COORD

                if (!activeTiles.ContainsKey(rawCoord))
                {
                    Vector3 position = new Vector3(rawCoord.x * blockSize, 0, rawCoord.y * blockSize);
                    GameObject tile = Instantiate(terrainBlockPrefab, position, Quaternion.identity, transform);
                    tile.name = $"Tile_{reflectedCoord.x}_{reflectedCoord.y}_at_{rawCoord.x}_{rawCoord.y}";

                    var gen = tile.GetComponent<ProceduralTerrainGenerator>();
                    if (gen != null)
                    {
                        gen.offset = new Vector2(reflectedCoord.x * blockSize, reflectedCoord.y * blockSize);
                        gen.GenerateTerrain();
                    }

                    activeTiles.Add(rawCoord, tile);
                }
            }
        }

        // Clean up tiles no longer needed
        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeTiles)
        {
            if (!newTileKeys.Contains(kvp.Key))
            {
                DestroyImmediate(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            activeTiles.Remove(key);
        }
    }
}
