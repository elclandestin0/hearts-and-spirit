#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class WorldPreGenerator : MonoBehaviour
{
    public GameObject terrainPrefab;
    public int minX = -8, maxX = 8;
    public int minZ = -8, maxZ = 8;
    public int blockSize = 200;

    [ContextMenu("Pre-Generate World")]
    public void PreGenerateWorld()
    {
        Transform parent = new GameObject("AllTerrainTiles").transform;
        parent.SetParent(transform);

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector3 pos = new Vector3(x * blockSize, 0f, z * blockSize);
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(terrainPrefab);
                tile.transform.position = pos;
                tile.transform.SetParent(parent);
                tile.name = $"Tile_{x}_{z}";

                var gen = tile.GetComponent<ProceduralTerrainGenerator>();
                gen.offset = new Vector2(x * blockSize, z * blockSize);
                gen.GenerateTerrain();

                EditorUtility.SetDirty(tile);
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Terrain world pre-generated.");
    }
}
#endif
