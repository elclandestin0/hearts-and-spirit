using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batchmode WebGL build entry point.
/// Run with:
///   Unity -batchmode -quit -projectPath <path> -buildTarget WebGL \
///         -executeMethod WebGLBuilder.Build -logFile build.log \
///         [-outputPath <dir>]
/// </summary>
public static class WebGLBuilder
{
    public static void Build()
    {
        string output = GetArg("-outputPath") ?? "Builds/WebGL";

        // The networked avatar prefab must exist before the player build, and the
        // audio overrides no-op here if the prepare pass already applied them.
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Net/NetAvatar.prefab") == null)
            NetAvatarPrefabBuilder.Build();
        PrepareAudio();

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        // Netlify serves the .br files with Content-Encoding headers (netlify.toml),
        // so the browser decompresses natively — no JS fallback needed.
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.initialMemorySize = 128;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetIl2CppCodeGeneration(
            UnityEditor.Build.NamedBuildTarget.WebGL,
            UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"WebGL build failed: {report.summary.result}, errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
        }

        Debug.Log($"WebGL build succeeded → {output} ({report.summary.totalSize / (1024 * 1024)} MB)");
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// Pass 1, run on the current build target: compiles everything (surfacing C#
    /// errors early), builds the net avatar prefab, and applies the WebGL audio
    /// overrides so the later target switch imports audio once, correctly.
    /// </summary>
    public static void PrepareAll()
    {
        NetAvatarPrefabBuilder.Build();
        PrepareAudio();
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// WebGL platform overrides for every AudioClip: Vorbis at reduced quality, and
    /// big clips (music, voice lines) kept compressed in memory instead of being
    /// inflated to PCM in the wasm heap. Without this the 600+ MB of source audio
    /// makes the web build undownloadable. Runs standalone via the menu item so the
    /// reimport can happen on the current target before the WebGL target switch.
    /// </summary>
    [MenuItem("Seeyuf/Prepare Audio For WebGL")]
    public static void PrepareAudio()
    {
        const float quality = 0.35f;
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        int changed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                bool big = new System.IO.FileInfo(path).Length > 500 * 1024;
                var wanted = importer.defaultSampleSettings;
                wanted.compressionFormat = AudioCompressionFormat.Vorbis;
                wanted.quality = quality;
                wanted.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                if (big) wanted.loadType = AudioClipLoadType.CompressedInMemory;

                if (importer.ContainsSampleSettingsOverride("WebGL"))
                {
                    var current = importer.GetOverrideSampleSettings("WebGL");
                    if (current.compressionFormat == wanted.compressionFormat &&
                        Mathf.Approximately(current.quality, wanted.quality) &&
                        current.loadType == wanted.loadType)
                        continue;
                }

                importer.SetOverrideSampleSettings("WebGL", wanted);
                importer.SaveAndReimport();
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[WebGLBuilder] Audio prepared for WebGL: {changed} of {guids.Length} clips updated.");
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }
}
