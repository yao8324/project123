using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneArchive.Visuals.Editor
{
    // Runs the existing production-parameter checks against the integrated arena.
    [InitializeOnLoad]
    public static class MainV1AcceptanceBatch
    {
        const string Key = "IMMonster.MainV1.Acceptance";
        const string Output = "Docs/MainV1";
        const string History = "Docs/BossDesign/ArcaneEffects20261027";
        static MainV1AcceptanceBatch() { EditorApplication.update += Tick; }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch entry only");
            Directory.CreateDirectory(Output);
            foreach (string name in new[] { "RuntimeAcceptance.json", "MotionFlowAcceptance.json", "FinalEditorValidation.json", "AcceptanceFailure.json" })
                if (File.Exists(Output + "/" + name)) File.Delete(Output + "/" + name);
            if (Directory.Exists(Output + "/Captures")) Directory.Delete(Output + "/Captures", true);
            string backup = "Temp/MainV1EvidenceBackup-" + Guid.NewGuid().ToString("N");
            CopyTree(History, backup);
            SessionState.SetString(Key + ".backup", backup);
            SessionState.SetString(Key + ".started", DateTime.UtcNow.ToString("o"));
            EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 720);
            SessionState.SetInt(Key, 1);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            int stage = SessionState.GetInt(Key, 0);
            if (stage == 0 || !Application.isBatchMode) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0))
                    throw new TimeoutException("Main v1 acceptance exceeded 720 seconds");
                if (stage == 1 && EditorApplication.isPlaying && !EditorApplication.isCompiling && Time.frameCount > 5)
                { WardenRitualAcceptance.Start(); SessionState.SetInt(Key, 2); }
                else if (stage == 2)
                {
                    var report = JObject.Parse(WardenRitualAcceptance.Status);
                    if (!string.IsNullOrEmpty((string)report["failure"])) throw new Exception((string)report["failure"]);
                    if ((bool?)report["complete"] != true) return;
                    if (((JArray)report["checks"]).Count != 36) throw new Exception("Expected 36 ritual checks");
                    File.WriteAllText(Output + "/RuntimeAcceptance.json", report.ToString());
                    WardenFlowAcceptance.Start(); SessionState.SetInt(Key, 3);
                }
                else if (stage == 3)
                {
                    var report = JObject.Parse(WardenFlowAcceptance.Status);
                    if (!string.IsNullOrEmpty((string)report["failure"])) throw new Exception((string)report["failure"]);
                    if ((int?)report["step"] != 100) return;
                    if (((JArray)report["checks"]).Count != 25) throw new Exception("Expected 25 motion checks");
                    File.WriteAllText(Output + "/MotionFlowAcceptance.json", report.ToString());
                    SessionState.SetInt(Key, 4); EditorApplication.isPlaying = false;
                }
                else if (stage == 4 && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
                {
                    CopyTree(History + "/Captures", Output + "/Captures/Skills");
                    CopyTree(History + "/FlowRegression/Captures", Output + "/Captures/Motion");
                    RestoreHistory();
                    File.WriteAllText(Output + "/FinalEditorValidation.json", new JObject {
                        ["unity"] = Application.unityVersion, ["ritualChecks"] = 36, ["motionChecks"] = 25,
                        ["runtimePassed"] = true, ["compiling"] = EditorApplication.isCompiling,
                        ["playing"] = EditorApplication.isPlaying, ["sceneDirty"] = EditorSceneManager.GetActiveScene().isDirty,
                        ["historicalEvidenceRestored"] = true, ["startedAtUtc"] = SessionState.GetString(Key + ".started", ""),
                        ["completedAtUtc"] = DateTime.UtcNow.ToString("o")
                    }.ToString());
                    SessionState.SetInt(Key, 0); Debug.Log("[MainV1Acceptance] 36 ritual + 25 motion checks passed"); EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                WardenRitualAcceptance.Stop(); WardenFlowAcceptance.Stop();
                string cleanupError = "";
                try { RestoreHistory(); } catch (Exception cleanup) { cleanupError = cleanup.ToString(); }
                Directory.CreateDirectory(Output);
                File.WriteAllText(Output + "/AcceptanceFailure.json", new JObject { ["stage"] = stage, ["error"] = ex.ToString(), ["cleanupError"] = cleanupError }.ToString());
                SessionState.SetInt(Key, 0); Debug.LogException(ex); EditorApplication.Exit(1);
            }
        }

        static void CopyTree(string source, string destination)
        {
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(file, target, true);
            }
        }

        static void RestoreHistory()
        {
            string backup = SessionState.GetString(Key + ".backup", "");
            if (string.IsNullOrEmpty(backup)) return;
            if (!Directory.Exists(backup)) throw new DirectoryNotFoundException("Historical evidence backup missing: " + backup);
            foreach (string file in Directory.GetFiles(History, "*", SearchOption.AllDirectories))
                if (!File.Exists(Path.Combine(backup, Path.GetRelativePath(History, file)))) File.Delete(file);
            CopyTree(backup, History); Directory.Delete(backup, true);
            SessionState.SetString(Key + ".backup", "");
        }
    }
}
