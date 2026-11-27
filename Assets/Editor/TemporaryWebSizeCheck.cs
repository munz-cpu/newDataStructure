using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
static class TemporaryWebSizeCheck
{
    static TemporaryWebSizeCheck() => EditorApplication.delayCall += Build;

    static void Build()
    {
        const string marker = "Temp/web-size-check.txt";
        if (File.Exists(marker)) return;
        try
        {
            var clips = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/DivineWar/Sounds/bgm/") && (path.EndsWith(".ogg") || path.EndsWith(".mp3")))
                .Select(path => (AudioImporter)AssetImporter.GetAtPath(path))
                .ToArray();
            foreach (var clip in clips)
            {
                var settings = clip.GetOverrideSampleSettings(BuildTargetGroup.WebGL);
                if (Math.Abs(settings.quality - 0.35f) > 0.001f)
                    throw new InvalidOperationException("Web audio override not imported: " + clip.assetPath);
            }
            File.WriteAllText(marker, $"Building with {clips.Length} audio overrides.");
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(
                "Assets/Settings/Build Profiles/Web - Desktop - Release.asset");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = "Builds/WebGL",
                options = BuildOptions.DetailedBuildReport
            });
            File.AppendAllText(marker, $"\n{report.summary.result}: {report.summary.totalSize} bytes, {report.summary.totalErrors} errors.");
        }
        catch (Exception error)
        {
            File.AppendAllText(marker, error.ToString());
        }
    }
}
