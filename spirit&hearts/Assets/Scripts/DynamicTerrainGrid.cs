using UnityEngine;
using System.Collections.Generic;

public class DynamicTerrainGrid : MonoBehaviour
{
    [Header("Setup")]
    public GameObject terrainBlockPrefab;
    public Transform player;
    public int blockSize = 200;
    [Header("Optional")]
    public GameObject cloudPlane, skyDome, cloudsParticle;
    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Vector2Int currentReflectedCenter;
    private Vector2Int currentRawCenter;


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
            WorldConfig.WrapCoord(rawCoord.x, WorldConfig.minX, WorldConfig.maxX),
            WorldConfig.WrapCoord(rawCoord.y, WorldConfig.minZ, WorldConfig.maxZ)
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
        currentRawCenter = rawCenterCoord;
        HashSet<Vector2Int> newTileKeys = new();

        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                Vector2Int rawCoord = new(rawCenterCoord.x + dx, rawCenterCoord.y + dz);
                Vector2Int reflectedCoord = new(
                    WorldConfig.WrapCoord(rawCoord.x, WorldConfig.minX, WorldConfig.maxX),
                    WorldConfig.WrapCoord(rawCoord.y, WorldConfig.minZ, WorldConfig.maxZ)
                );

                newTileKeys.Add(rawCoord);

                if (!activeTiles.ContainsKey(rawCoord))
                {
                    GameObject tile = InstantiateTile(rawCoord, reflectedCoord);
                    activeTiles.Add(rawCoord, tile);
                }
            }
        }

        CleanupOldTiles(newTileKeys);
        MoveToCenter(cloudPlane, rawCenterCoord);
        MoveToCenter(skyDome, rawCenterCoord);
        MoveToCenter(cloudsParticle, rawCenterCoord);
    }

    GameObject InstantiateTile(Vector2Int rawCoord, Vector2Int reflectedCoord)
    {
        Vector3 position = new Vector3(rawCoord.x * blockSize, transform.position.y, rawCoord.y * blockSize);
        GameObject tile = Instantiate(terrainBlockPrefab, position, Quaternion.identity, transform);
        tile.name = $"Tile_{reflectedCoord.x}_{reflectedCoord.y}_at_{rawCoord.x}_{rawCoord.y}";

        float maxHeight = 0f;

        var terrainGen = tile.GetComponent<ProceduralTerrainGenerator>();
        if (terrainGen != null)
        {
            terrainGen.offset = new Vector2(reflectedCoord.x * blockSize, reflectedCoord.y * blockSize);
            terrainGen.GenerateTerrain();
            maxHeight = terrainGen.GetMaxHeight();
        }

        var assetGen = tile.GetComponent<TileAssetGenerator>();
        if (assetGen != null)
        {
            assetGen.rawCoord = rawCoord;
            assetGen.SetTerrainReference(terrainGen);

            assetGen.heightRange.x = Mathf.Max(assetGen.heightRange.x, maxHeight + 10f);
            assetGen.heightRange.y = Mathf.Max(assetGen.heightRange.y, assetGen.heightRange.x + 50f);

            assetGen.GenerateIslands();
        }

        return tile;
    }

    void CleanupOldTiles(HashSet<Vector2Int> newTileKeys)
    {
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
