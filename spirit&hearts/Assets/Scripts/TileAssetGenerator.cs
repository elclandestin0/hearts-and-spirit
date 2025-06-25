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
    public float clusterChance = 0.6f; // % chance a tile will contain a cluster

    [HideInInspector] public Vector2Int rawCoord;

    // Set Terrain Reference
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

        var spots = terrainGen.GetClassifiedSpots();

        // Clear old
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Island_") || child.name.StartsWith("FunnelStep") || child.name.StartsWith("LightSource"))
                DestroyImmediate(child.gameObject);
        }

        bool isLandmark = WorldLandmarks.IsLandmarkTile(rawCoord);
        bool isFunnel = WorldLandmarks.IsFunnelTile(rawCoord);

        if (isLandmark)
        {
            GameObject island = Instantiate(landmarkIslandPrefab, transform);
            island.name = $"Island_Landmark";
            island.transform.position = new Vector3(
                (rawCoord.x + 0.5f) * tileSize,
                1000f,
                (rawCoord.y + 0.5f) * tileSize
            );
            return;
        }

        if (isFunnel)
        {
            GenerateLightFunnel(rawCoord, spots);
            return;
        }

        // Fallback: regular tile
        List<Vector3> rampSpots = new();
        List<Vector3> ringSpots = new();
        List<Vector3> obstacleSpots = new();
        List<Vector3> chillSpots = new();

        foreach (var s in spots)
        {
            if (s.heightNorm < 0.4f && s.slope > 0.2f) rampSpots.Add(s.worldPos);
            else if (s.heightNorm > 0.4f && s.heightNorm < 0.7f && s.slope < 0.2f) ringSpots.Add(s.worldPos);
            else if (s.heightNorm > 0.7f && s.heightNorm < 0.95f && s.slope > 0.3f) obstacleSpots.Add(s.worldPos);
            else if (s.heightNorm >= 0.95f && s.slope < 0.15f) chillSpots.Add(s.worldPos);
        }

        int hash = rawCoord.x * 73856093 ^ rawCoord.y * 19349663 ^ worldSeed;
        System.Random rand = new System.Random(hash);
        int clusterCount = rand.Next(1, maxIslands + 1);

        for (int c = 0; c < clusterCount; c++)
        {
            if (rand.NextDouble() > clusterChance)
                continue; // Skip cluster for intentional gap

            int islandsInCluster = rand.Next(minIslandsPerCluster, maxIslandsPerCluster + 1);

            // Pick a base position for the cluster (center point)
            float minX = rawCoord.x * tileSize + padding;
            float maxX = (rawCoord.x + 1) * tileSize - padding;
            float minZ = rawCoord.y * tileSize + padding;
            float maxZ = (rawCoord.y + 1) * tileSize - padding;
            float baseX = rand.NextFloat(minX, maxX);
            float baseZ = rand.NextFloat(minZ, maxZ);
            float baseY = rand.NextFloat(750f, 850f); // mid-high base

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

    private void GenerateLightFunnel(Vector2Int tileCoord, List<ProceduralTerrainGenerator.TerrainSpot> spots)
    {
        float minY = 700f, maxY = 950f;
        int steps = 6;
        float stepY = (maxY - minY) / steps;

        float tileStartX = tileCoord.x * tileSize + padding;
        float tileEndX = (tileCoord.x + 1) * tileSize - padding;
        float tileStartZ = tileCoord.y * tileSize + padding;
        float tileEndZ = (tileCoord.y + 1) * tileSize - padding;

        List<Vector3> funnelPoints = new();
        System.Random rand = new System.Random(tileCoord.x * 7919 ^ tileCoord.y * 1973 ^ worldSeed);

        for (int i = 0; i < steps; i++)
        {
            float y = minY + i * stepY;

            float t = i / (float)steps;

            // Smooth curve through the tile (slight spiral offset)
            float px = Mathf.Lerp(tileStartX, tileEndX, Mathf.PerlinNoise(t * 2f, tileCoord.y + 123));
            float pz = Mathf.Lerp(tileStartZ, tileEndZ, Mathf.PerlinNoise(tileCoord.x + 321, t * 2f));

            Vector3 p = new Vector3(px, y, pz);
            funnelPoints.Add(p);
        }

        // Instantiate the steps
        for (int i = 0; i < funnelPoints.Count; i++)
        {
            var prefab = funnelStepsPrefabs[rand.Next(funnelStepsPrefabs.Length)];
            var obj = Instantiate(prefab, funnelPoints[i], Quaternion.identity, this.transform);
            obj.name = $"FunnelStep_{i}";
            obj.transform.localScale = Vector3.one * 4f;
        }

        // Place the light source at the top
        var top = funnelPoints[^1];
        var lightPrefab = lightPrefabs[rand.Next(lightPrefabs.Length)];
        var lightObj = Instantiate(lightPrefab, top + Vector3.up * 5f, Quaternion.identity, this.transform);
        lightObj.name = $"LightSource";
    }


    private GameObject ChooseIslandType(System.Random rand, Vector2Int coord, int index)
    {
        if (index == 0 && WorldLandmarks.IsLandmarkTile(coord))
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
