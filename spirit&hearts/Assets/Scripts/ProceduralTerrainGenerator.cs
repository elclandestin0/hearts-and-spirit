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

    public struct TerrainSpot
    {
        public Vector3 worldPos;
        public float heightNorm; // 0–1 normalized
        public float slope; // estimated
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
            float slope = 1f - Mathf.Abs(Vector3.Dot(normals[i], Vector3.up)); // 0 = flat, 1 = vertical

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
        GenerateTerrain();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && meshFilter != null)
        {
            GenerateTerrain();
        }
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
                // Normalized distance to edge for falloff
                float edgeX = Mathf.Min(x, width - x);
                float edgeZ = Mathf.Min(z, depth - z);
                float edgeDist = Mathf.Min(edgeX, edgeZ);
                float falloff = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edgeDist / maxDistanceToEdge));

                // Multi-octave noise
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

                noiseHeight = Mathf.Pow(noiseHeight, peakSharpness); // spike the peaks
                float height = noiseHeight * heightMultiplier * falloff;

                // Add localized roughness for rocky peaks
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
