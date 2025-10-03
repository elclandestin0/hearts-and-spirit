using System.Collections;
using UnityEngine;

public static class PauseYields
{
    public static IEnumerator Seconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (PauseState.IsPaused)
            {
                yield return null;
                continue;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
    public static IEnumerator UntilUnpaused()
    {
        while (PauseState.IsPaused) yield return null;
    }
}
