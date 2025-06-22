using UnityEngine;
using System.Collections.Generic;

public class DynamicTerrainGrid : MonoBehaviour
{
    [Header("Setup")]
    public GameObject terrainPrefab;
    public Transform player;
    public int blockSize = 200;

    [Header("Optional")]
    public GameObject cloudPlane, skyDome, cloudsParticle;

    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Dictionary<Vector2Int, GameObject> preGeneratedTiles = new();
    private Vector2Int currentReflectedCenter;
    private Vector2Int currentRawCenter;

    void Start()
    {
        // Step 1: Pre-generate and cache all terrain tiles
        for (int z = WorldConfig.minZ; z <= WorldConfig.maxZ; z++)
        {
            for (int x = WorldConfig.minX; x <= WorldConfig.maxX; x++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 pos = new Vector3(x * blockSize, 0f, z * blockSize);

                GameObject tile = Instantiate(terrainPrefab, pos, Quaternion.identity);
                tile.name = $"Mountain_{x}_{z}";
                tile.SetActive(false);

                var gen = tile.GetComponent<ProceduralTerrainGenerator>();
                gen.offset = new Vector2(x * blockSize, z * blockSize);
                gen.GenerateTerrain();

                  var assetGen = tile.GetComponent<TileAssetGenerator>();
                    if (assetGen != null)
                    {
                        assetGen.rawCoord = new Vector2Int(x, z);
                        assetGen.SetTerrainReference(gen);

                        // Optionally adjust height range if your mountains vary
                        float maxHeight = gen.GetMaxHeight(); // if you have this method
                        assetGen.heightRange.x = Mathf.Max(assetGen.heightRange.x, maxHeight + 10f);
                        assetGen.heightRange.y = Mathf.Max(assetGen.heightRange.y, assetGen.heightRange.x + 50f);

                        assetGen.GenerateIslands();
                    }

                preGeneratedTiles[coord] = tile;
            }
        }

        // Step 2: Find player starting position and activate the 5x5 grid
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
            WorldConfig.WrapCoord(rawCoord.x, WorldConfig.minX, WorldConfig.maxX),
            WorldConfig.WrapCoord(rawCoord.y, WorldConfig.minZ, WorldConfig.maxZ)
        );

        PlayerWorldTracker.UpdateCoord(player.position, blockSize);

        if (reflectedCoord != currentReflectedCenter)
        {
            currentReflectedCenter = reflectedCoord;
            UpdateGridAroundPlayer(rawCoord);
        }
    }

    void PreGenerateTiles()
    {
        for (int z = WorldConfig.minZ; z <= WorldConfig.maxZ; z++)
        {
            for (int x = WorldConfig.minX; x <= WorldConfig.maxX; x++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 pos = new Vector3(x * blockSize, 0f, z * blockSize);

                GameObject tile = Instantiate(terrainPrefab, pos, Quaternion.identity);
                tile.name = $"Mountain_{x}_{z}";
                tile.SetActive(false);

                var gen = tile.GetComponent<ProceduralTerrainGenerator>();
                gen.offset = new Vector2(x * blockSize, z * blockSize);
                gen.GenerateTerrain();

                preGeneratedTiles[coord] = tile;
            }
        }
    }

    void UpdateGridAroundPlayer(Vector2Int rawCenterCoord)
    {
        currentRawCenter = rawCenterCoord;
        HashSet<Vector2Int> newTileKeys = new();

        for (int dz = -2; dz <= 2; dz++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                Vector2Int rawCoord = new(rawCenterCoord.x + dx, rawCenterCoord.y + dz);
                Vector2Int reflectedCoord = new(
                    WorldConfig.WrapCoord(rawCoord.x, WorldConfig.minX, WorldConfig.maxX),
                    WorldConfig.WrapCoord(rawCoord.y, WorldConfig.minZ, WorldConfig.maxZ)
                );

                newTileKeys.Add(rawCoord);

                if (!activeTiles.ContainsKey(rawCoord))
                {
                    if (preGeneratedTiles.TryGetValue(reflectedCoord, out GameObject tile))
                    {
                        tile.SetActive(true);
                        activeTiles[rawCoord] = tile;
                    }
                }
            }
        }

        CleanupOldTiles(newTileKeys);
        MoveToCenter(cloudPlane, rawCenterCoord);
        MoveToCenter(skyDome, rawCenterCoord);
        MoveToCenter(cloudsParticle, rawCenterCoord);
    }

    void CleanupOldTiles(HashSet<Vector2Int> newTileKeys)
    {
        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeTiles)
        {
            if (!newTileKeys.Contains(kvp.Key))
            {
                kvp.Value.SetActive(false);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            activeTiles.Remove(key);
        }
    }

    void MoveToCenter(GameObject obj, Vector2Int centerCoord)
    {
        if (obj == null || !activeTiles.ContainsKey(centerCoord))
            return;

        GameObject centerTile = activeTiles[centerCoord];
        Vector3 centerPos = centerTile.transform.position;
        centerPos.y = obj.transform.position.y;
        centerPos.x += blockSize / 2f;
        centerPos.z += blockSize / 2f;

        obj.transform.position = centerPos;
    }
}
