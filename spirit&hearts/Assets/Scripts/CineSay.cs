using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// This cinematic step is made so that the dove speaks to the player
[CreateAssetMenu(menuName = "Tutorial/Cinematic/Speak")]
public class CineSay : CineAction
{
    [TextArea] public string subtitle;
    public AudioClip voice;
    public float minHold = 0.5f;
    public override IEnumerator Execute(CineContext ctx)
    {
        if (ctx.subtitleUI) ctx.subtitleUI.text = subtitle;
        // in some bootstrap, once:
        PauseState.OnPauseChanged += paused =>
        {
            var s = ctx.speaker?.audioSource; // or find all DovinaAudioManager instances and pause them
            if (s == null) return;
            if (paused) s.Pause();
            else s.UnPause();
        };

        // If we start while paused, stall until unpaused
        yield return PauseYields.UntilUnpaused();

        if (voice)
        {
            ctx.speaker?.PlayClip(voice, 2);
            float hold = Mathf.Max(minHold, voice.length);

            yield return PauseYields.Seconds(hold);
            yield return PauseYields.Seconds(0.5f);
        }
        else
        {
            yield return PauseYields.Seconds(minHold + 0.5f);
        }
    }

}
