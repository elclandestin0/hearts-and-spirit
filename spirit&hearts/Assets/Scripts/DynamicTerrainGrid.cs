using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class DynamicTerrainGrid : MonoBehaviour
{
    [Header("World Setup")]
    public int totalSize = 7000;
    public int divisions = 7;
    public float heightMultiplier = 100f;

    [Header("Noise Settings")]
    public float baseScale = 0.01f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float peakSharpness = 2f;
    public float roughnessFrequency = 0.8f;
    public float roughnessStrength = 5f;
    public float borderFalloffPercent = 0.2f;
    public int seed = 42;

    [Header("Tile Prefab Settings")]
    public GameObject tilePrefab;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private Dictionary<Vector2Int, GameObject> tileLookup = new();
    [SerializeField] private Material defaultTerrainMaterial;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // GenerateMainTerrainMesh();
        SpawnCoordinateTiles();
    }
    void SpawnCoordinateTiles()
    {
        int tileSize = totalSize / divisions;
        float maxY = GetMaxHeight();
        for (int z = 0; z < divisions; z++)
        {
            for (int x = 0; x < divisions; x++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                Vector3 tilePos = new Vector3(x * tileSize, 0f, z * tileSize);

                GameObject tile = Instantiate(tilePrefab, tilePos, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{z}";

                if (tile.TryGetComponent(out TileAssetGenerator assetGen))
                {
                    assetGen.rawCoord = coord;
                    assetGen.heightRange = new Vector2(maxY + 10f, maxY + 60f); // arbitrary buffer for now
                    assetGen.GenerateIslands();
                }

                tileLookup[coord] = tile;
            }
        }
    }

    float GetMaxHeight()
    {
        if (meshFilter.sharedMesh == null) return 0f;
        float max = float.MinValue;
        foreach (Vector3 v in meshFilter.sharedMesh.vertices)
        {
            if (v.y > max) max = v.y;
        }
        return max;
    }
}
