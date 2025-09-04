// CineSpawnPrefabAtPoint.cs
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Cinematic/Spawn seed at point")]
public class CineSeedSpawner : CineAction
{
    [Header("Spawn")]
    public GameObject prefab;
    [Tooltip("scenePoints id from TutorialManager")]
    public string pointId;
    public Vector3 localOffset;
    public bool parentToPoint = false;

    [Header("Timing")]
    [Tooltip("Optional delay before spawning")]
    public float preDelay = 0f;

    public override IEnumerator Execute(CineContext ctx)
    {
        if (preDelay > 0f)
            yield return new WaitForSeconds(preDelay);

        var anchor = (ctx.ResolveTarget != null) ? ctx.ResolveTarget(pointId) : null;
        Debug.Log("Resolved anchor: " + anchor.transform.position);
        if (anchor == null)
        {
            Debug.LogWarning($"CineSpawnPrefabAtPoint: point '{pointId}' not found. Using playerRoot.");
            anchor = ctx.playerRoot != null ? ctx.playerRoot : ctx.playerHead;
            if (anchor == null)
            {
                Debug.LogError("CineSpawnPrefabAtPoint: No valid anchor to spawn at.");
                yield break;
            }
        }

        // Use same rotation as SeedSpawner
        Vector3 euler = anchor.rotation.eulerAngles;
        Quaternion spawnRotation = Quaternion.Euler(-90f, euler.y, euler.z);

        var pos = anchor.position + anchor.TransformVector(localOffset);
        var go = Instantiate(prefab, pos, spawnRotation);

        if (parentToPoint)
            go.transform.SetParent(anchor, worldPositionStays: true);

        yield break;
    }
}
