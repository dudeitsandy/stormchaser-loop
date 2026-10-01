using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless builds. Output goes to &lt;repo&gt;/builds/ (gitignored).
/// Unity -batchmode -quit -projectPath ... -executeMethod BuildScript.BuildWindows
/// Unity -batchmode -quit -projectPath ... -executeMethod BuildScript.BuildWebGL
/// </summary>
public static class BuildScript
{
    private const string ProductName = "Doomsday";

    private static string OutputRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "builds"));

    [MenuItem("StormChaser/Build/Windows")]
    public static void BuildWindows() =>
        Build(BuildTarget.StandaloneWindows64, Path.Combine(OutputRoot, "windows", ProductName + ".exe"));

    [MenuItem("StormChaser/Build/WebGL")]
    public static void BuildWebGL() =>
        Build(BuildTarget.WebGL, Path.Combine(OutputRoot, "webgl"));

    private static void Build(BuildTarget target, string locationPath)
    {
        // Art/dev test scenes stay in Build Settings for editor reloads but never ship.
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !s.path.EndsWith("ArtTest.unity"))
            .Select(s => s.path).ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes in Build Settings.");

        PlayerSettings.productName = ProductName;
        PlayerSettings.companyName = "Ghostweave Games";
        if (target == BuildTarget.WebGL)
            // itch.io serves without Content-Encoding headers; gzip/brotli builds fail to load there.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPath,
            target = target,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[BuildScript] {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, " +
                  $"{summary.totalErrors} errors, {summary.totalTime.TotalSeconds:F0}s → {locationPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw new BuildFailedException($"{target} build failed: {summary.result}");
        }
    }
}
