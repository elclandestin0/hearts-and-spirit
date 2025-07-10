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

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        GenerateMainTerrainMesh();
        SpawnCoordinateTiles();
    }

    void GenerateMainTerrainMesh()
    {
        int width = totalSize;
        int depth = totalSize;
        Vector3[] vertices = new Vector3[(width + 1) * (depth + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[width * depth * 6];

        System.Random prng = new(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxDistanceToEdge = Mathf.Min(width, depth) * borderFalloffPercent;

        for (int z = 0, i = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                float edgeX = Mathf.Min(x, width - x);
                float edgeZ = Mathf.Min(z, depth - z);
                float edgeDist = Mathf.Min(edgeX, edgeZ);
                float falloff = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edgeDist / maxDistanceToEdge));

                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = x * baseScale * frequency + octaveOffsets[o].x;
                    float sampleZ = z * baseScale * frequency + octaveOffsets[o].y;
                    float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
                    noiseHeight += perlin * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseHeight = Mathf.Pow(noiseHeight, peakSharpness);
                float height = noiseHeight * heightMultiplier * falloff;

                if (noiseHeight > 0.7f)
                {
                    float roughX = x * roughnessFrequency;
                    float roughZ = z * roughnessFrequency;
                    float roughNoise = Mathf.PerlinNoise(roughX, roughZ) - 0.5f;
                    height += roughNoise * roughnessStrength;
                }

                vertices[i] = new Vector3(x, height, z);
                uvs[i] = new Vector2((float)x / width, (float)z / depth);
            }
        }

        int tris = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = z * (width + 1) + x;

                triangles[tris++] = i;
                triangles[tris++] = i + width + 1;
                triangles[tris++] = i + 1;

                triangles[tris++] = i + 1;
                triangles[tris++] = i + width + 1;
                triangles[tris++] = i + width + 2;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "WorldTerrain";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
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
