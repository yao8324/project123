using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MilanAcceptance
{
    [Serializable] public sealed class AcceptanceSuite { public RunConfig[] runs; }
    [InitializeOnLoad]
    public static class MilanAcceptanceLauncher
    {
        const string Pending = "MilanAcceptance.PendingConfig";
        const string SuiteKey = "MilanAcceptance.Suite";
        const string IndexKey = "MilanAcceptance.Index";
        const string CompleteKey = "MilanAcceptance.TestComplete";
        static MilanAcceptanceLauncher() { EditorApplication.playModeStateChanged += Changed; }
        public static void Run()
        {
            string configPath = Environment.GetEnvironmentVariable("MILAN_ACCEPTANCE_CONFIG");
            if (string.IsNullOrEmpty(configPath)) throw new ArgumentException("Set MILAN_ACCEPTANCE_CONFIG to a reviewed suite JSON.");
            string json = File.ReadAllText(configPath);
            var suite = JsonUtility.FromJson<AcceptanceSuite>(json);
            if (suite.runs == null || suite.runs.Length == 0) throw new ArgumentException("Empty suite.");
            foreach (var config in suite.runs)
            {
                if (Directory.Exists(config.outputDirectory)) throw new IOException("Refusing existing report directory: " + config.outputDirectory);
                if (string.IsNullOrEmpty(config.scenePath) || !File.Exists(config.scenePath)) throw new FileNotFoundException("Missing reviewed scene.");
            }
            SessionState.SetString(SuiteKey, json);
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetInt("MilanAcceptance.ExitCode", 0);
            EditorApplication.update += WaitForStartup;
        }
        static void WaitForStartup()
        {
            if (EditorApplication.timeSinceStartup < 20 || EditorApplication.isUpdating || EditorApplication.isCompiling) return;
            EditorApplication.update -= WaitForStartup;
            StartNext();
        }
        static void StartNext()
        {
            var suite = JsonUtility.FromJson<AcceptanceSuite>(SessionState.GetString(SuiteKey, ""));
            int index = SessionState.GetInt(IndexKey, 0);
            if (index >= suite.runs.Length)
            {
                int exit = SessionState.GetInt("MilanAcceptance.ExitCode", 0);
                SessionState.EraseString(SuiteKey);
                Debug.Log("MILAN_ACCEPTANCE_SUITE_FINISHED exit=" + exit);
                EditorApplication.Exit(exit);
                return;
            }
            var config = suite.runs[index];
            EditorSceneManager.OpenScene(config.scenePath, OpenSceneMode.Single);
            SessionState.SetString(Pending, JsonUtility.ToJson(config));
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(CompleteKey, false))
            {
                SessionState.SetBool(CompleteKey, false);
                SessionState.SetInt(IndexKey, SessionState.GetInt(IndexKey, 0) + 1);
                EditorApplication.delayCall += StartNext;
                return;
            }
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            string json = SessionState.GetString(Pending, "");
            if (string.IsNullOrEmpty(json)) return;
            SessionState.EraseString(Pending);
            MilanAcceptanceRunner.Completed = code => Complete(code);
            try { new GameObject("AcceptanceRecorder_TestOnly").AddComponent<MilanAcceptanceRunner>().Initialize(JsonUtility.FromJson<RunConfig>(json)); }
            catch (Exception ex) { Debug.LogException(ex); Complete(2); }
        }
        static void Complete(int code)
        {
            if (code != 0) SessionState.SetInt("MilanAcceptance.ExitCode", code);
            SessionState.SetBool(CompleteKey, true);
            EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
        }
    }
}
