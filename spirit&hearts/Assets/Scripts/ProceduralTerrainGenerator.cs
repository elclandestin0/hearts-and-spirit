using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    public int width = 200;
    public int depth = 200;
    public float heightMultiplier = 100f;

    [Header("Noise Settings")]
    public float baseScale = 0.01f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float peakSharpness = 2f;
    public int seed = 42;

    [Header("Falloff")]
    [Range(0f, 1f)] public float borderFalloffPercent = 0.2f;

    [Header("Peak Roughness")]
    public float roughnessFrequency = 1.2f;
    public float roughnessStrength = 10f;

    [HideInInspector] public Vector2 offset;

    public MeshFilter meshFilter;
    private MeshCollider meshCollider;

    [Header("Curved Edge Settings")]
    public bool enableCurvedEdges = true;
    public float edgeCurvatureStrength = 20f;

    [Header("Border Ridge Settings")]
    public bool enableRidgeFusion = true;
    public float ridgeBoostStrength = 15f;
    public float ridgeWidthPercent = 0.15f;
    public float ridgeNoiseScale = 0.3f;

    public struct TerrainSpot
    {
        public Vector3 worldPos;
        public float heightNorm;
        public float slope;
    }

    public List<TerrainSpot> GetClassifiedSpots()
    {
        List<TerrainSpot> spots = new();
        if (meshFilter.sharedMesh == null) return spots;

        Vector3[] verts = meshFilter.sharedMesh.vertices;
        Vector3[] normals = meshFilter.sharedMesh.normals;

        float maxY = float.MinValue;
        float minY = float.MaxValue;

        foreach (var v in verts)
        {
            if (v.y > maxY) maxY = v.y;
            if (v.y < minY) minY = v.y;
        }

        for (int i = 0; i < verts.Length; i++)
        {
            float heightNorm = Mathf.InverseLerp(minY, maxY, verts[i].y);
            float slope = 1f - Mathf.Abs(Vector3.Dot(normals[i], Vector3.up));

            spots.Add(new TerrainSpot
            {
                worldPos = transform.TransformPoint(verts[i]),
                heightNorm = heightNorm,
                slope = slope
            });
        }

        return spots;
    }

    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        Vector3[] vertices = new Vector3[(width + 1) * (depth + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[width * depth * 6];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float xOff = prng.Next(-100000, 100000);
            float zOff = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(xOff, zOff);
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
                    float worldX = (x + offset.x) * baseScale * frequency + octaveOffsets[o].x;
                    float worldZ = (z + offset.y) * baseScale * frequency + octaveOffsets[o].y;
                    float perlin = Mathf.PerlinNoise(worldX, worldZ);

                    noiseHeight += perlin * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseHeight = Mathf.Pow(noiseHeight, peakSharpness);
                float height = noiseHeight * heightMultiplier * falloff;

                if (enableRidgeFusion)
                {
                    float edgeBand = width * ridgeWidthPercent;
                    float distToLeft = Mathf.Abs(x);
                    float distToRight = Mathf.Abs(width - x);
                    float distToBottom = Mathf.Abs(z);
                    float distToTop = Mathf.Abs(depth - z);
                    float edgeCenterBoost = 0f;

                    if (distToLeft < edgeBand || distToRight < edgeBand)
                    {
                        float verticalCenterDist = Mathf.Abs(z - (depth / 2f)) / (depth / 2f);
                        float vFalloff = Mathf.Clamp01(1f - verticalCenterDist);
                        float vNoise = Mathf.PerlinNoise((x + offset.x + 2000f) * ridgeNoiseScale, (z + offset.y + 2000f) * ridgeNoiseScale);
                        edgeCenterBoost += vFalloff * vNoise * ridgeBoostStrength * 0.5f;
                    }

                    if (distToBottom < edgeBand || distToTop < edgeBand)
                    {
                        float horizontalCenterDist = Mathf.Abs(x - (width / 2f)) / (width / 2f);
                        float hFalloff = Mathf.Clamp01(1f - horizontalCenterDist);
                        float hNoise = Mathf.PerlinNoise((x + offset.x + 3000f) * ridgeNoiseScale, (z + offset.y + 3000f) * ridgeNoiseScale);
                        edgeCenterBoost += hFalloff * hNoise * ridgeBoostStrength * 0.5f;
                    }

                    height += edgeCenterBoost;
                }

                if (noiseHeight > 0.7f)
                {
                    float rX = (x + offset.x) * roughnessFrequency;
                    float rZ = (z + offset.y) * roughnessFrequency;
                    float roughNoise = Mathf.PerlinNoise(rX, rZ) - 0.5f;
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
        mesh.name = "Procedural Sky Terrain";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public float GetMaxHeight()
    {
        if (meshFilter.sharedMesh == null)
            return 0f;

        float max = float.MinValue;
        Vector3[] verts = meshFilter.sharedMesh.vertices;

        for (int i = 0; i < verts.Length; i++)
        {
            if (verts[i].y > max)
                max = verts[i].y;
        }

        return max;
    }
}
