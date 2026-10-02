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
    /// <summary>Shipped version; also shown on the title screen via Application.version.</summary>
    public const string Version = "0.6.1";
    /// <summary>The only scene that ships. 0.5+: the ADR-0003 restyled scene.</summary>
    private const string ShippingScene = "Assets/Scenes/ArtTest.unity";
    /// <summary>S7-01 G1 gate scene; dev-only, own output folder.</summary>
    private const string SpikeScene = "Assets/Scenes/StreamingSpike.unity";

    private static string OutputRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "builds"));

    [MenuItem("StormChaser/Build/Windows")]
    public static void BuildWindows() =>
        Build(BuildTarget.StandaloneWindows64, Path.Combine(OutputRoot, "windows", ProductName + ".exe"));

    [MenuItem("StormChaser/Build/WebGL")]
    public static void BuildWebGL() =>
        Build(BuildTarget.WebGL, Path.Combine(OutputRoot, "webgl"));

    [MenuItem("StormChaser/Spike/Build Spike WebGL")]
    public static void BuildSpikeWebGL() =>
        Build(BuildTarget.WebGL, Path.Combine(OutputRoot, "spike-webgl"), SpikeScene);

    private static void Build(BuildTarget target, string locationPath, string scene = ShippingScene)
    {
        // Ship exactly one scene. VerificationScene (the 0.4 look) stays in Build Settings for editor A/B only.
        if (!EditorBuildSettings.scenes.Any(s => s.path == scene))
            throw new BuildFailedException($"{scene} is not in Build Settings. Run its builder (StormChaser menu) first.");
        string[] scenes = { scene };

        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = Version;
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
