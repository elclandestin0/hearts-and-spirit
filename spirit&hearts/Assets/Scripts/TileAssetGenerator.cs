using UnityEngine;
using System;
using System.Collections.Generic;

public class TileAssetGenerator : MonoBehaviour
{
    [Header("Config")]
    public int worldSeed = 42;
    public int maxIslands = 4;
    public Vector2 scaleRange = new Vector2(0.8f, 1.5f);
    public float tileSize = 1000f;
    public float padding = 300f;
    public Vector2 heightRange;
    public ProceduralTerrainGenerator terrainGen;

    [Header("Prefabs")]
    public GameObject navigationIslandPrefab;
    public GameObject ringIslandPrefab;
    public GameObject obstacleIslandPrefab;
    public GameObject landmarkIslandPrefab;
    public GameObject smallArchPrefab;
    public GameObject smallDiagonalPrefab;
    public GameObject smallDonutPrefab;
    public GameObject smallDoublePrefab;
    public GameObject smallFlatPrefab;
    public GameObject smallPikesPrefab;

    [Header("Light Funnel Assets")]
    public GameObject[] funnelStepsPrefabs;
    public GameObject[] lightPrefabs;

    [Header("Cluster Config")]
    public int minIslandsPerCluster = 2;
    public int maxIslandsPerCluster = 4;
    public float clusterRadius = 200f;
    public float clusterVerticalVariation = 50f;
    public float clusterChance = 0.6f;

    [HideInInspector] public Vector2Int rawCoord;

    public void SetTerrainReference(ProceduralTerrainGenerator gen)
    {
        terrainGen = gen;
    }

    private void Start()
    {
        GenerateIslands();
    }

    [ContextMenu("Generate Islands")]
    public void GenerateIslands()
    {
        if (terrainGen == null || terrainGen.meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("Missing terrain generator or mesh.");
            return;
        }

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Island_") || child.name.StartsWith("FunnelStep") || child.name.StartsWith("LightSource"))
                DestroyImmediate(child.gameObject);
        }

        var role = WorldMap.GetRole(rawCoord);
        var spots = terrainGen.GetClassifiedSpots();

        switch (role)
        {
            case WorldTileRole.Landmark:
                GenerateLandmarkTile();
                break;

            case WorldTileRole.Funnel:
                GenerateFunnelTile(spots);
                break;

            case WorldTileRole.Bridge:
                GenerateBridgeTile(spots);
                break;

            case WorldTileRole.Wander:
                GenerateWanderTile();
                break;
        }
    }

    private void GenerateLandmarkTile()
    {
        GameObject island = Instantiate(landmarkIslandPrefab, transform);
        island.name = $"Island_Landmark";
        island.transform.position = new Vector3(
            (rawCoord.x + 0.5f) * tileSize,
            1000f,
            (rawCoord.y + 0.5f) * tileSize
        );
    }

    private void GenerateFunnelTile(List<ProceduralTerrainGenerator.TerrainSpot> spots)
    {
        float minY = 700f, maxY = 950f;
        int steps = 6;
        float stepY = (maxY - minY) / steps;

        float tileStartX = rawCoord.x * tileSize + padding;
        float tileEndX = (rawCoord.x + 1) * tileSize - padding;
        float tileStartZ = rawCoord.y * tileSize + padding;
        float tileEndZ = (rawCoord.y + 1) * tileSize - padding;

        List<Vector3> funnelPoints = new();
        System.Random rand = new System.Random(rawCoord.x * 7919 ^ rawCoord.y * 1973 ^ worldSeed);

        for (int i = 0; i < steps; i++)
        {
            float y = minY + i * stepY;
            float t = i / (float)steps;

            float px = Mathf.Lerp(tileStartX, tileEndX, Mathf.PerlinNoise(t * 2f, rawCoord.y + 123));
            float pz = Mathf.Lerp(tileStartZ, tileEndZ, Mathf.PerlinNoise(rawCoord.x + 321, t * 2f));

            Vector3 p = new Vector3(px, y, pz);
            funnelPoints.Add(p);
        }

        for (int i = 0; i < funnelPoints.Count; i++)
        {
            var prefab = funnelStepsPrefabs[rand.Next(funnelStepsPrefabs.Length)];
            var obj = Instantiate(prefab, funnelPoints[i], Quaternion.identity, this.transform);
            obj.name = $"FunnelStep_{i}";
            obj.transform.localScale = Vector3.one * 4f;
        }

        var top = funnelPoints[^1];
        var lightPrefab = lightPrefabs[rand.Next(lightPrefabs.Length)];
        var lightObj = Instantiate(lightPrefab, top + Vector3.up * 5f, Quaternion.identity, this.transform);
        lightObj.name = $"LightSource";
    }

    private void GenerateBridgeTile(List<ProceduralTerrainGenerator.TerrainSpot> spots)
    {
        // Start similar to fallback terrain but simpler or themed
        // For now, we'll reuse the cluster generator from fallback
        GenerateClusteredIslands(spots, bridge: true);
    }

    private void GenerateWanderTile()
    {
        // Currently empty. Could be populated with ambient life later.
    }

    private void GenerateClusteredIslands(List<ProceduralTerrainGenerator.TerrainSpot> spots, bool bridge = false)
    {
        int hash = rawCoord.x * 73856093 ^ rawCoord.y * 19349663 ^ worldSeed;
        System.Random rand = new System.Random(hash);
        int clusterCount = rand.Next(1, maxIslands + 1);

        for (int c = 0; c < clusterCount; c++)
        {
            if (!bridge && rand.NextDouble() > clusterChance)
                continue;

            int islandsInCluster = rand.Next(minIslandsPerCluster, maxIslandsPerCluster + 1);

            float minX = rawCoord.x * tileSize + padding;
            float maxX = (rawCoord.x + 1) * tileSize - padding;
            float minZ = rawCoord.y * tileSize + padding;
            float maxZ = (rawCoord.y + 1) * tileSize - padding;
            float baseX = rand.NextFloat(minX, maxX);
            float baseZ = rand.NextFloat(minZ, maxZ);
            float baseY = rand.NextFloat(750f, 850f);

            for (int i = 0; i < islandsInCluster; i++)
            {
                GameObject prefab = ChooseIslandType(rand, rawCoord, c * 10 + i);

                Vector3 offset = new Vector3(
                    rand.NextFloat(-clusterRadius, clusterRadius),
                    rand.NextFloat(-clusterVerticalVariation, clusterVerticalVariation),
                    rand.NextFloat(-clusterRadius, clusterRadius)
                );

                Vector3 position = new Vector3(baseX, baseY, baseZ) + offset;

                GameObject island = Instantiate(prefab, transform);
                island.name = $"Cluster_{c}_Island_{i}";
                island.transform.position = position;

                float scale = rand.NextFloat(scaleRange.x, scaleRange.y);
                island.transform.localScale = Vector3.one * scale * 4f;
            }
        }
    }

    private GameObject ChooseIslandType(System.Random rand, Vector2Int coord, int index)
    {
        if (index == 0 && WorldMap.GetRole(coord) == WorldTileRole.Landmark)
            return landmarkIslandPrefab;

        GameObject[] allPrefabs = new GameObject[]
        {
            navigationIslandPrefab,
            ringIslandPrefab,
            obstacleIslandPrefab,
            smallArchPrefab,
            smallDiagonalPrefab,
            smallDonutPrefab,
            smallDoublePrefab,
            smallFlatPrefab,
            smallPikesPrefab
        };

        return allPrefabs[rand.Next(0, allPrefabs.Length)];
    }
}
