// PauseState.cs
using System;
using UnityEngine;

public static class PauseState
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> OnPauseChanged;

    public static void SetPaused(bool paused)
    {
        Debug.Log("Set paused. " + paused);
        if (IsPaused == paused) return;
        IsPaused = paused;
        OnPauseChanged?.Invoke(IsPaused);
    }
}
