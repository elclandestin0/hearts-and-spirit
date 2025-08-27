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

    public override IEnumerator Execute(CineContext ctx)
    {
        Transform t = null;
        if (!string.IsNullOrEmpty(targetId) && ctx.ResolveTarget != null)
            t = ctx.ResolveTarget(targetId);

        Vector3 randomDir = Random.onUnitSphere;
        randomDir.y = Mathf.Clamp(randomDir.y, -0.1f, 0.5f);
        Vector3 offset = randomDir.normalized * 200f;
        Vector3 testOffset = new Vector3(0, 200.0f, -200.0f);

        yield return ctx.dove.SmoothHoverApproach(testOffset);
    }
}