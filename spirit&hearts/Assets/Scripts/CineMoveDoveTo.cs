using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This cinematic step is so that the dove eventually moves towards the player
[CreateAssetMenu(menuName = "Tutorial/Cinematic/Move Dove To")]
public class CineMoveDoveTo : CineAction
{
    public string targetId;
    public bool useArrivalPointIfMissing = true;
    public bool usePlayerHeadIfMissing = true;
    public Vector3 headOffset = new Vector3(0, -0.1f, 0.6f);

    public override IEnumerator Execute(CineContext ctx)
    {
        Transform t = null;
        if (!string.IsNullOrEmpty(targetId) && ctx.ResolveTarget != null)
            t = ctx.ResolveTarget(targetId);

        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.5f);
        Vector3 offset = randomDir.normalized * 200f;

        yield return ctx.dove.SmoothHoverApproach(offset);
    }
}