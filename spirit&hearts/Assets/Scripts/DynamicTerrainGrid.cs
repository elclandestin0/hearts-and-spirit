using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class DynamicTerrainGrid : MonoBehaviour
{
    [Header("Setup")]
    public GameObject terrainBlockPrefab;
    public Transform player;
    public int blockSize = 200;

    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Vector2Int currentCenter;

    void Start()
    {
        UpdateGrid(force: true);
    }

    void Update()
    {
        Vector2Int playerCoord = GetPlayerCoord();
        PlayerWorldTracker.UpdateCoord(player.position, blockSize); // ← ADD THIS

        if (playerCoord != currentCenter)
        {
            currentCenter = playerCoord;
            UpdateGrid();
        }
    }


    Vector2Int GetPlayerCoord()
    {
        int x = Mathf.FloorToInt(player.position.x / blockSize);
        int z = Mathf.FloorToInt(player.position.z / blockSize);
        return new Vector2Int(x, z);
    }

    void UpdateGrid(bool force = false)
    {
        HashSet<Vector2Int> newTiles = new();

        // Only operate within world bounds
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                Vector2Int coord = new Vector2Int(currentCenter.x + dx, currentCenter.y + dz);

                if (coord.x < WorldConfig.minX || coord.x > WorldConfig.maxX ||
                    coord.y < WorldConfig.minZ || coord.y > WorldConfig.maxZ)
                {
                    continue; // Out of bounds
                }

                newTiles.Add(coord);

                if (!activeTiles.ContainsKey(coord))
                {
                    Vector3 position = new Vector3(coord.x * blockSize, 0, coord.y * blockSize);
                    GameObject prefabToUse = terrainBlockPrefab;

                    if (WorldConfig.IsLandmark(coord))
                    {
                        Debug.Log($"Spawning {WorldConfig.GetLandmarkName(coord)} at {coord}");
                        // Swap prefab here if needed based on name
                    }

                    GameObject tile = Instantiate(prefabToUse, position, Quaternion.identity, transform);
                    tile.name = $"Tile_{coord.x}_{coord.y}";

                    var gen = tile.GetComponent<ProceduralTerrainGenerator>();
                    if (gen != null)
                    {
                        gen.offset = new Vector2(coord.x * blockSize, coord.y * blockSize);
                        gen.GenerateTerrain(); // Explicitly generate
                    }

                    activeTiles.Add(coord, tile);
                }
            }
        }

        // Remove tiles no longer needed
        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeTiles)
        {
            if (!newTiles.Contains(kvp.Key))
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
