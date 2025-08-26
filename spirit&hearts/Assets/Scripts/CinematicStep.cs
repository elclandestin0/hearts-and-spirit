using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(menuName="Tutorial/Cinematic Step")]
public class CinematicStep : ScriptableObject
{
    public string displayName = "Intro";
    public MovementAbility allowedAbilities = MovementAbility.Look; // lock movement
    public List<CineAction> actions = new(); // executed in order
}

// Base class for actions
public abstract class CineAction : ScriptableObject
{
    public abstract IEnumerator Execute(CineContext ctx);
}

// Context passed to actions
public class CineContext
{
    public Transform playerHead;
    public Transform playerRoot;
    public DoveCompanion dove;
    public DovinaAudioManager speaker;
}


[CreateAssetMenu(menuName = "Tutorial/Cinematic/Move Dove To")]
public class CineMoveDoveTo : CineAction
{
    public Transform target; // assign a scene ref via a runtime setter (see note below)
    public override IEnumerator Execute(CineContext ctx)
    {
        yield return ctx.dove.SmoothHoverApproach(target.position);
    }
}

[CreateAssetMenu(menuName="Tutorial/Cinematic/Face Player")]
public class CineFacePlayer : CineAction
{
    public float duration = 1.5f;
    public override IEnumerator Execute(CineContext ctx)
    {
        // yield return ctx.dove.Face(ctx.playerHead, duration);
        yield return null;
    }
}
