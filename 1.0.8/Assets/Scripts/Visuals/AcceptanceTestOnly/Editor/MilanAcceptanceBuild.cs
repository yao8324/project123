using System;
using System.IO;
using ArcaneArchive.Visuals.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace MilanAcceptance
{
    public static class MilanAcceptanceBuild
    {
        public static void Run()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            MilanV6SceneBuilder.Build();
            MilanArchiveValidation.Run();
        }
        [Serializable] private sealed class PlayerBuildSummary
        {
            public string result, outputPath, unityVersion;
            public ulong totalBytes;
            public int errors, warnings;
            public double seconds;
            public bool diagnosticHooksEnabled;
        }
        public static void BuildProfiledMacPlayer()
        {
            bool original = PlayerSettings.enableFrameTimingStats;
            try { PlayerSettings.enableFrameTimingStats = true; BuildMacPlayer(); }
            finally { PlayerSettings.enableFrameTimingStats = original; AssetDatabase.SaveAssets(); }
        }
        public static void BuildOptimizedPair()
        {
            MilanPerformanceSetup.ApplyToExistingScene();
            Environment.SetEnvironmentVariable("MILAN_DIAGNOSTIC_PLAYER", "0");
            BuildMacPlayer();
            Environment.SetEnvironmentVariable("MILAN_PLAYER_OUTPUT", Environment.GetEnvironmentVariable("MILAN_DIAGNOSTIC_OUTPUT"));
            Environment.SetEnvironmentVariable("MILAN_BUILD_REPORT", Environment.GetEnvironmentVariable("MILAN_DIAGNOSTIC_REPORT"));
            Environment.SetEnvironmentVariable("MILAN_DIAGNOSTIC_PLAYER", "1");
            BuildMacPlayer();
        }
        public static void BuildMacPlayer()
        {
            string output = Environment.GetEnvironmentVariable("MILAN_PLAYER_OUTPUT");
            string reportPath = Environment.GetEnvironmentVariable("MILAN_BUILD_REPORT");
            if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(reportPath)) throw new ArgumentException("Set reviewed output and report paths.");
            if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Refusing to overwrite an existing player build.");
            MilanArchiveValidation.Run();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool diagnostic = Environment.GetEnvironmentVariable("MILAN_DIAGNOSTIC_PLAYER") == "1";
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/MilanLibrary_Magic_V6.unity" },
                locationPathName = output, target = BuildTarget.StandaloneOSX, options = BuildOptions.None,
                extraScriptingDefines = diagnostic ? new[] { "MILAN_ACCEPTANCE_DIAGNOSTICS" } : Array.Empty<string>()
            });
            var value = new PlayerBuildSummary { result = report.summary.result.ToString(), outputPath = report.summary.outputPath,
                unityVersion = Application.unityVersion, totalBytes = report.summary.totalSize,
                errors = (int)report.summary.totalErrors, warnings = (int)report.summary.totalWarnings, seconds = report.summary.totalTime.TotalSeconds,
                diagnosticHooksEnabled = diagnostic };
            File.WriteAllText(reportPath, JsonUtility.ToJson(value, true));
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Mac player build failed; see build report.");
            Debug.Log("MILAN_MAC_PLAYER_BUILD_SUCCEEDED " + output);
        }
    }
}
