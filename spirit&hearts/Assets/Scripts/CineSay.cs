using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// This cinematic step is made so that the dove speaks to the player
[CreateAssetMenu(menuName="Tutorial/Cinematic/Speak")]
public class CineSay : CineAction
{
    [TextArea] public string subtitle;
    public AudioClip voice;
    public float minHold = 0.5f;

    public override IEnumerator Execute(CineContext ctx)
    {
        // TODO: hook your actual subtitle/audio here (e.g., ctx.speaker.PlayPriority(...))
        Debug.Log($"[CineSay] {subtitle}");
        Debug.Log("Playing clip " + voice.name);
        ctx.speaker?.PlayClip(voice, 2);
        yield return null;
        // Minimal, compile-safe wait (prefer voice.length if you can access it)
        // float hold = Mathf.Max(minHold, voice ? voice.length : 0f);
        // if (hold > 0f) yield return new WaitForSeconds(hold);
    }
}
