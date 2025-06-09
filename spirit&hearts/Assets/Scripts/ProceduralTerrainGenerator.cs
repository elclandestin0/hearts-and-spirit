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

        // Create vertices
        for (int z = 0, i = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                float worldX = (x + offset.x) * scale;
                float worldZ = (z + offset.y) * scale;
                float noise = Mathf.PerlinNoise(worldX, worldZ);
                float borderX = Mathf.Min(x, width - x);
                float borderZ = Mathf.Min(z, depth - z);
                float borderDistance = Mathf.Min(borderX, borderZ);
                float maxBlend = width * borderBlendPercent;
                float blendFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(borderDistance / maxBlend));
                float blendedHeight = heightCurve.Evaluate(noise) * heightMultiplier * blendFactor;
                
                vertices[i] = new Vector3(x, blendedHeight, z);
                uvs[i] = new Vector2((float)x / width, (float)z / depth);
            }
        }

        // Create triangles with holes
        int tris = 0;
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsHole(x, z) || IsHole(x + 1, z) || IsHole(x, z + 1) || IsHole(x + 1, z + 1))
                    continue;

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

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private bool IsHole(int x, int z)
    {
        float noise = Mathf.PerlinNoise(x * holeScale, z * holeScale);
        return noise > holeThreshold;
    }
}
