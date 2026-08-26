using UnityEngine;

/// <summary>
/// Keyboard shortcuts for flatscreen testing: flap, glide, hover, attract a
/// seed, skip the tutorial. Useful in the editor, but they should not be live
/// in something a player installs.
///
/// Enabled is a compile-time constant, so in a release player every call
/// short-circuits before it reaches UnityEngine.Input.
/// </summary>
public static class DebugKeys
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public const bool Enabled = true;
#else
    public const bool Enabled = false;
#endif

    public static bool Held(KeyCode key) => Enabled && Input.GetKey(key);
    public static bool Pressed(KeyCode key) => Enabled && Input.GetKeyDown(key);
    public static bool Released(KeyCode key) => Enabled && Input.GetKeyUp(key);
}
