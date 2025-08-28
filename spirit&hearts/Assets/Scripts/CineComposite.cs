using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Tutorial/Cinematic/Composite")]
public class CineComposite : CineAction
{
    public List<CineAction> actions = new();

    public override IEnumerator Execute(CineContext ctx)
    {
        List<Coroutine> running = new();
        MonoBehaviour runner = ctx.dove; // something with StartCoroutine

        foreach (var act in actions)
        {
            if (act != null)
                running.Add(runner.StartCoroutine(act.Execute(ctx)));
        }

        // Wait until all sub-actions finish
        foreach (var c in running)
            yield return c;
    }
}
