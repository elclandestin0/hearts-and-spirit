using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Tutorial/Cinematic/Face Player")]
public class CineFacePlayer : CineAction
{
    public float duration = 1.5f;
    [Tooltip("Local offset in player space. (0,0,-2) looks a bit 'ahead' of the player.")]
    public Vector3 localLookAtOffset = new Vector3(0f, 0f, -2f);
    public float turnSpeed;
    public override IEnumerator Execute(CineContext ctx)
    {
        if (ctx.dove != null && ctx.playerHead != null)
            yield return ctx.dove.Face(ctx.playerHead, duration, localLookAtOffset, turnSpeed);
        else
            yield return null;
    }
}
