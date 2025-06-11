using UnityEngine;
using System;
using System.Collections.Generic;

public class TileAssetGenerator : MonoBehaviour
{
    [Header("Config")]
    public int worldSeed = 42;
    public int maxIslands = 4;
    public Vector2 scaleRange = new Vector2(0.8f, 1.5f);
    public Vector2 heightRange;
    public float tileSize = 1000f;
    public float padding = 300f;

    [Header("Prefabs")]
    public GameObject navigationIslandPrefab;
    public GameObject ringIslandPrefab;
    public GameObject obstacleIslandPrefab;
    public GameObject landmarkIslandPrefab;

    [HideInInspector] public Vector2Int rawCoord;

    private void Start()
    {
        GenerateIslands();
    }

    [ContextMenu("Generate Islands")]
    public void GenerateIslands()
    {
        // Clear old
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Island_"))
                DestroyImmediate(child.gameObject);
        }

        // Deterministic seed
        int hash = rawCoord.x * 73856093 ^ rawCoord.y * 19349663 ^ worldSeed;
        System.Random rand = new System.Random(hash);
        int islandCount = rand.Next(1, maxIslands + 1);

        for (int i = 0; i < islandCount; i++)
        {
            GameObject prefab = ChooseIslandType(rand, rawCoord, i);
            Vector3 position;

            if (i == 0 && WorldLandmarks.IsLandmarkTile(rawCoord))
            {
                position = new Vector3(
                    (rawCoord.x + 0.5f) * tileSize,
                    500f,
                    (rawCoord.y + 0.5f) * tileSize
                );
            }
            else
            {
                float minX = rawCoord.x * tileSize + padding;
                float maxX = (rawCoord.x + 1) * tileSize - padding;
                float minZ = rawCoord.y * tileSize + padding;
                float maxZ = (rawCoord.y + 1) * tileSize - padding;

                position = new Vector3(
                    rand.NextFloat(minX, maxX),
                    rand.NextFloat(heightRange.x, heightRange.y),
                    rand.NextFloat(minZ, maxZ)
                );
            }

            float scale = rand.NextFloat(scaleRange.x, scaleRange.y);
            GameObject island = Instantiate(prefab, transform);
            island.name = $"Island_{i}";
            island.transform.position = position;
            island.transform.localScale = WorldLandmarks.IsLandmarkTile(rawCoord) ? island.transform.localScale : Vector3.one * scale;
        }
    }

    private GameObject ChooseIslandType(System.Random rand, Vector2Int coord, int index)
    {
        if (index == 0 && WorldLandmarks.IsLandmarkTile(coord))
            return landmarkIslandPrefab;

        int roll = rand.Next(0, 100);
        if (roll < 40) return navigationIslandPrefab;
        if (roll < 70) return ringIslandPrefab;
        return obstacleIslandPrefab;
    }
}
