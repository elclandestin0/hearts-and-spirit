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
    public Transform arrivalPoint;
    public DoveCompanion dove;
    public DovinaAudioManager speaker;
    public System.Func<string, Transform> ResolveTarget; // optional ID → Transform
}


[CreateAssetMenu(menuName="Tutorial/Cinematic/Move Dove To")]
public class CineMoveDoveTo : CineAction
{
    public string targetId;                       // optional: "Arrival"
    public bool useArrivalPointIfMissing = true;  // uses ctx.arrivalPoint
    public bool usePlayerHeadIfMissing  = true;
    public Vector3 headOffset = new Vector3(0, -0.1f, 0.6f);

    public override IEnumerator Execute(CineContext ctx)
    {
        Transform t = null;

        if (!string.IsNullOrEmpty(targetId) && ctx.ResolveTarget != null)
            t = ctx.ResolveTarget(targetId);

        Vector3 dest;
        if (t != null)                           dest = t.position;
        else if (useArrivalPointIfMissing && ctx.arrivalPoint)
                                                 dest = ctx.arrivalPoint.position;
        else if (usePlayerHeadIfMissing && ctx.playerHead)
                                                 dest = ctx.playerHead.TransformPoint(headOffset);
        else                                      dest = ctx.playerRoot.position;

        yield return ctx.dove.SmoothHoverApproach(dest);
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


    [CreateAssetMenu(menuName="Tutorial/Cinematic/Speak")]
    public class CineSay : CineAction
    {
        [TextArea] public string subtitle;
        public AudioClip voice;
        public float minHold = 0.5f;

        public override IEnumerator Execute(CineContext ctx)
        {
            float t = 0f;
            Debug.Log("Hello...?");
            ctx.speaker?.PlayClip(voice, 2);
            yield return null;
        }
    }
