using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This cinematic step is made so that the dove eventually faces the player
[CreateAssetMenu(menuName = "Tutorial/Cinematic/Face Player")]
public class CineFacePlayer : CineAction
{
    public float duration = 1.5f;
    public override IEnumerator Execute(CineContext ctx)
    {
        // yield return ctx.dove.Face(ctx.playerHead, duration);
        yield return null;
    }
}