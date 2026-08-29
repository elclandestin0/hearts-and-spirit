using UnityEngine;

/// <summary>
/// Keyboard shortcuts for flatscreen play: flap, glide, hover, attract a
/// seed, skip the tutorial. Useful in the editor, and they ARE the shipped
/// control scheme on WebGL, where there is no headset. They stay out of the
/// installable VR players.
///
/// Enabled is a compile-time constant, so in a VR release player every call
/// short-circuits before it reaches UnityEngine.Input.
/// </summary>
public static class DebugKeys
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
    public const bool Enabled = true;
#else
    public const bool Enabled = false;
#endif

    /// <summary>Set while a UI text field (e.g. the join-code box) has focus, so
    /// typing a code doesn't flap the bird.</summary>
    public static bool SuppressForTyping = false;

    private static bool Active => Enabled && !SuppressForTyping;

    public static bool Held(KeyCode key) => Active && Input.GetKey(key);
    public static bool Pressed(KeyCode key) => Active && Input.GetKeyDown(key);
    public static bool Released(KeyCode key) => Active && Input.GetKeyUp(key);
}
