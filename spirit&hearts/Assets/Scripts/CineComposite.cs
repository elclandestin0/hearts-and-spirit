using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Tutorial/Cinematic/Composite")]
public class CineComposite : CineAction
{
    public List<CineAction> actions = new List<CineAction>();
    [Tooltip("If false, fire-and-forget (composite completes immediately).")]
    public bool waitForAll = true;

    public override IEnumerator Execute(CineContext ctx)
    {
        if (actions == null || actions.Count == 0) yield break;

        var runner = ctx.runner != null ? ctx.runner
                   : (MonoBehaviour)ctx.dove; // fallback if you prefer
        if (runner == null)
        {
            Debug.LogWarning("[CineComposite] No runner to start coroutines.");
            yield break;
        }

        int remaining = 0;

        // start all children in parallel
        foreach (var act in actions)
        {
            if (act == null) continue;
            remaining++;

            // clone so we never mutate assets
            var child = ScriptableObject.Instantiate(act);
            runner.StartCoroutine(RunChild(child, ctx, () => remaining--));
        }

        if (waitForAll)
            yield return new WaitUntil(() => remaining == 0);
        else
            yield return null; // don't wait—advance immediately
    }

    private IEnumerator RunChild(CineAction action, CineContext ctx, System.Action onDone)
    {
        yield return action.Execute(ctx);
        onDone?.Invoke();
    }
}
