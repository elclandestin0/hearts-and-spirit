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

    private Dictionary<Vector2Int, GameObject> allTiles = new();

    void Start()
    {
        // Pre-generate and cache all terrain tiles
        for (int z = WorldConfig.minZ; z <= WorldConfig.maxZ; z++)
        {
            for (int x = WorldConfig.minX; x <= WorldConfig.maxX; x++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 pos = new Vector3(x * blockSize, 0f, z * blockSize);

                GameObject tile = Instantiate(terrainPrefab, pos, Quaternion.identity);
                tile.name = $"Mountain_{x}_{z}";

                var gen = tile.GetComponent<ProceduralTerrainGenerator>();
                gen.offset = new Vector2(x * blockSize, z * blockSize);
                gen.GenerateTerrain();

                var assetGen = tile.GetComponent<TileAssetGenerator>();
                if (assetGen != null)
                {
                    assetGen.rawCoord = new Vector2Int(x, z);
                    assetGen.SetTerrainReference(gen);

                    float maxHeight = gen.GetMaxHeight();
                    assetGen.heightRange.x = Mathf.Max(assetGen.heightRange.x, maxHeight + 10f);
                    assetGen.heightRange.y = Mathf.Max(assetGen.heightRange.y, assetGen.heightRange.x + 50f);

                    assetGen.GenerateIslands();
                }

                allTiles[coord] = tile;
            }
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // No longer moving sky elements dynamically; static at (0,0)
    }

    void MoveToCenter(GameObject obj, Vector2Int centerCoord)
    {
        if (obj == null || !allTiles.ContainsKey(centerCoord))
            return;

        GameObject centerTile = allTiles[centerCoord];
        Vector3 centerPos = centerTile.transform.position;
        centerPos.y = obj.transform.position.y;
        centerPos.x += blockSize / 2f;
        centerPos.z += blockSize / 2f;

        obj.transform.position = centerPos;
    }
}