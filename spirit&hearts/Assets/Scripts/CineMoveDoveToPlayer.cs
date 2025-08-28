using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This cinematic step is so that the dove eventually moves towards the player
[CreateAssetMenu(menuName = "Tutorial/Cinematic/Move Dove To Player")]
public class CineMoveDoveToPlayer : CineAction
{
    public string targetId;
    public bool useArrivalPointIfMissing = true;
    public bool usePlayerHeadIfMissing = true;
    public Vector3 offset;

    public override IEnumerator Execute(CineContext ctx)
    {
        Transform t = null;
        if (!string.IsNullOrEmpty(targetId) && ctx.ResolveTarget != null)
            t = ctx.ResolveTarget(targetId);

        yield return ctx.dove.SmoothHoverApproachToPlayer(offset);
    }
}