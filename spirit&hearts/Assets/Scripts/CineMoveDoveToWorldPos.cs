using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This cinematic step is so that the dove eventually moves towards the player
[CreateAssetMenu(menuName = "Tutorial/Cinematic/Move Dove To World")]
public class CineMoveDoveToWorldPos: CineAction
{
    public Vector3 targetPos;

    public override IEnumerator Execute(CineContext ctx)
    {
        yield return ctx.dove.SmoothHoverApproachWorld(targetPos);
    }
}