using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int width = 200;
    public int depth = 200;
    public float scale = 0.01f;
    public float heightMultiplier = 100f;
    public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(0f, 1f)] public float borderBlendPercent = 0.1f;


    [Header("Cave Settings")]
    public float holeScale = 0.01f;
    public float holeThreshold = 0.7f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    public Vector2 offset = Vector2.zero;


    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        GenerateTerrain();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
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
        float[,] heights = new float[width + 1, depth + 1];

        // STEP 1: Compute raw heights with noise, peak profile, and blending
        for (int z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float worldX = (x + offset.x) * scale;
                float worldZ = (z + offset.y) * scale;

                float noise = Mathf.PerlinNoise(worldX, worldZ);
                // float profileNoise = Mathf.PerlinNoise(worldX * 0.5f, worldZ * 0.5f);
                // float peakPower = profileNoise < 0.33f ? 0.5f : (profileNoise < 0.66f ? 1.0f : 2.0f);
                // float shapedNoise = Mathf.Pow(noise, peakPower);

                float borderX = Mathf.Min(x, width - x);
                float borderZ = Mathf.Min(z, depth - z);
                float borderDistance = Mathf.Min(borderX, borderZ);
                float maxBlend = width * borderBlendPercent;
                float blendFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(borderDistance / maxBlend));

                float baseHeight = 0.25f * heightMultiplier;
                float blendedHeight = Mathf.Lerp(baseHeight, noise * heightMultiplier, blendFactor);

                heights[x, z] = blendedHeight;
            }
        }

        // STEP 2: Smooth height deltas between neighboring vertices
        float maxDelta = 20f; // max allowed vertical difference
        for (int z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float current = heights[x, z];

                if (x > 0)
                    current = Mathf.Clamp(current, heights[x - 1, z] - maxDelta, heights[x - 1, z] + maxDelta);

                if (z > 0)
                    current = Mathf.Clamp(current, heights[x, z - 1] - maxDelta, heights[x, z - 1] + maxDelta);

                heights[x, z] = current;
            }
        }

        // STEP 3: Generate vertices and UVs from final heightmap
        for (int z = 0, i = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                float y = heights[x, z];
                vertices[i] = new Vector3(x, y, z);
                uvs[i] = new Vector2((float)x / width, (float)z / depth);
            }
        }

        // STEP 4: Generate triangles
        int tris = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = z * (width + 1) + x;

                triangles[tris + 0] = i;
                triangles[tris + 1] = i + width + 1;
                triangles[tris + 2] = i + 1;

                triangles[tris + 3] = i + 1;
                triangles[tris + 4] = i + width + 1;
                triangles[tris + 5] = i + width + 2;

                tris += 6;
            }
        }

        // STEP 5: Apply to mesh
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }


    private bool IsHole(int x, int z)
    {
        float noise = Mathf.PerlinNoise(x * holeScale, z * holeScale);
        return noise > holeThreshold;
    }
}
