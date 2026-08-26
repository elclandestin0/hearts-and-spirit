using System.Diagnostics;

/// <summary>
/// Logging that costs nothing in a release player.
/// Calls to these methods are removed by the compiler outside the editor and
/// development builds, so the arguments are never even evaluated - no string
/// concatenation, no boxing, no logcat round trip on device.
///
/// Use Debug.LogWarning / Debug.LogError directly for anything a player build
/// should still report.
/// </summary>
public static class GameLog
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message) => UnityEngine.Debug.Log(message);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context) =>
        UnityEngine.Debug.Log(message, context);
}
