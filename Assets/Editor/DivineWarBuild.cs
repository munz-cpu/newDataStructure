using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

public static class DivineWarBuild
{
    public static void Validate()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        const string results = "Temp/TrainingWork/results.txt";
        if (File.Exists(results)) File.Delete(results);
        ClassCombatChecks.EquippedSkills();
        TrainingQueueChecks.Run();
        if (File.Exists(results))
        {
            Directory.CreateDirectory("Builds/validation");
            File.Copy(results, "Builds/validation/battle.txt", true);
        }
        if (!File.Exists(results) || File.ReadLines(results).LastOrDefault() != "ALL PASSED")
            throw new BuildFailedException("Main scene training/replication validation failed. See the Editor log.");
    }

    public static void ValidateAndBuildDevelopment()
    {
        Validate();
        Development();
    }

    public static void Development() => Build(BuildTarget.StandaloneLinux64, "development");
    public static void WindowsDevelopmentForCi()
    {
        WindowsDevelopment();
        Build(BuildTarget.StandaloneWindows64, "development", true);
    }

    public static void Production() => Build(BuildTarget.StandaloneLinux64, "production");
    public static void WindowsDevelopment() => Build(BuildTarget.StandaloneWindows64, "development");
    public static void WindowsProduction() => Build(BuildTarget.StandaloneWindows64, "production");

    public static void ServerDevelopment() => Build(BuildTarget.StandaloneLinux64, "development", true);
    public static void ServerProduction() => Build(BuildTarget.StandaloneLinux64, "production", true);

    public static void LocalValidationLinux()
    {
        Build(BuildTarget.StandaloneLinux64, "development", localTest: true);
        Build(BuildTarget.StandaloneLinux64, "development", server: true, localTest: true);
    }

    public static void LocalValidationWindows() => Build(BuildTarget.StandaloneWindows64, "development", localTest: true);

    static void Build(BuildTarget target, string environment, bool server = false, bool localTest = false)
    {
        bool windows = target == BuildTarget.StandaloneWindows64;
        string output = windows ? $"Builds/windows/{environment}/DivineWar.exe" : $"Builds/{environment}/DivineWar.x86_64";
        if (server) output = windows
            ? $"Builds/windows-server/{environment}/DivineWarServer.exe"
            : $"Builds/server/{environment}/DivineWarServer.x86_64";
        var symbols = new System.Collections.Generic.List<string>();
        if (localTest)
        {
            output = "Builds/qa/" + output.Substring("Builds/".Length);
            symbols.Add("DIVINEWAR_LOCAL_QA");
        }
        if (server) symbols.Add("DIVINEWAR_DEDICATED");
        if (environment == "production") symbols.Add("DIVINEWAR_PRODUCTION");
        else if (!server) symbols.Add("DIVINEWAR_RELAY_SMOKE");
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = target,
            locationPathName = output,
            extraScriptingDefines = symbols.ToArray()
        };
        if (options.scenes.FirstOrDefault() != "Assets/Scenes/Title.unity")
            throw new BuildFailedException("Title must be the first enabled scene.");
        var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Split(';');
        if (Array.Exists(defines, d => d == "DIVINEWAR_PRODUCTION" || d == "DIVINEWAR_RELAY_SMOKE" || d == "DIVINEWAR_DEDICATED" || d == "DIVINEWAR_LOCAL_QA"))
            throw new BuildFailedException("Environment symbols belong to the build invocation.");
        var result = BuildPipeline.BuildPlayer(options).summary;
        if (result.result != BuildResult.Succeeded)
            throw new BuildFailedException($"{target} {environment} build {result.result} ({result.totalErrors} errors).");
    }
}
