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
    public GameObject forestPrefab;
    public GameObject altarPrefab;
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

        var spots = terrainGen.GetClassifiedSpots();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Island_") || child.name.StartsWith("FunnelStep") || child.name.StartsWith("LightSource"))
                DestroyImmediate(child.gameObject);
        }

        var role = WorldMap.GetRole(rawCoord);

        switch (role)
        {
            case WorldTileRole.Landmark:
                GenerateLandmarkTile();
                break;
            case WorldTileRole.Funnel:
                GenerateLightFunnel(rawCoord, spots);
                break;
            case WorldTileRole.Bridge:
                GenerateBridgeTile(spots);
                break;
            case WorldTileRole.Wander:
            default:
                GenerateWanderTile();
                break;
        }
    }

    private void GenerateLandmarkTile()
    {
        GameObject prefabToUse = null;

        if (rawCoord == new Vector2Int(-3, 3))
            prefabToUse = landmarkIslandPrefab;
        else if (rawCoord == new Vector2Int(3, -3))
            prefabToUse = altarPrefab;
        else if (rawCoord == new Vector2Int(0, -3))
            prefabToUse = forestPrefab;
        else
            prefabToUse = landmarkIslandPrefab; // fallback (optional)

        if (prefabToUse == null)
        {
            Debug.LogWarning($"No landmark prefab assigned for coord {rawCoord}");
            return;
        }

        GameObject island = Instantiate(prefabToUse, transform);
        island.name = $"Island_Landmark_{rawCoord.x}_{rawCoord.y}";
        island.transform.position = new Vector3(
            (rawCoord.x + 0.5f) * tileSize,
            tileSize,
            (rawCoord.y + 0.5f) * tileSize
        );
    }


    private void GenerateWanderTile()
    {
        // Empty for now
    }

    private void GenerateBridgeTile(List<ProceduralTerrainGenerator.TerrainSpot> spots)
    {
        System.Random rand = new System.Random(rawCoord.x * 73856093 ^ rawCoord.y * 19349663 ^ worldSeed);

        Vector3 start = GetRandomPoint(spots, rand, 750f);
        GameObject startObj = Instantiate(smallFlatPrefab, start, Quaternion.identity, transform);
        startObj.name = "Bridge_Start";

        Vector3 mid = start + new Vector3(rand.NextFloat(100f, 200f), rand.NextFloat(20f, 40f), rand.NextFloat(100f, 200f));
        GameObject midObj = Instantiate(smallDoublePrefab, mid, Quaternion.identity, transform);
        midObj.name = "Bridge_Mid";

        Vector3 archPos = Vector3.Lerp(start, mid, 0.5f) + Vector3.up * 10f;
        GameObject archPrefab = rand.NextDouble() > 0.5 ? smallArchPrefab : smallDonutPrefab;
        GameObject arch = Instantiate(archPrefab, archPos, Quaternion.identity, transform);
        arch.name = "Bridge_Connector";
    }

    private Vector3 GetRandomPoint(List<ProceduralTerrainGenerator.TerrainSpot> spots, System.Random rand, float height)
    {
        float tileStartX = rawCoord.x * tileSize + padding;
        float tileEndX = (rawCoord.x + 1) * tileSize - padding;
        float tileStartZ = rawCoord.y * tileSize + padding;
        float tileEndZ = (rawCoord.y + 1) * tileSize - padding;

        float px = rand.NextFloat(tileStartX, tileEndX);
        float pz = rand.NextFloat(tileStartZ, tileEndZ);
        return new Vector3(px, height, pz);
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

            float px = Mathf.Lerp(tileStartX, tileEndX, Mathf.PerlinNoise(t * 2f, tileCoord.y + 123));
            float pz = Mathf.Lerp(tileStartZ, tileEndZ, Mathf.PerlinNoise(tileCoord.x + 321, t * 2f));

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
