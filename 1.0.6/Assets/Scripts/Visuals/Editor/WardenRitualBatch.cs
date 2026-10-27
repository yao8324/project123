using System;
using System.IO;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Runs the same Play Mode acceptance without capturing the desktop cursor.</summary>
    [InitializeOnLoad]
    public static class WardenRitualBatch
    {
        const string Key = "ArcaneArchive.RitualBatch";
        const string Output = "Docs/BossDesign/ArcaneEffects20261027";
        [Serializable] class Result { public bool complete; public string failure; }
        static WardenRitualBatch() { EditorApplication.update += Tick; }

        // Unity -batchmode -projectPath ... -executeMethod ArcaneArchive.Visuals.Editor.WardenRitualBatch.Run
        // Omit -quit: the runner exits only after Play Mode checks and cleanup have completed.
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch entry point only");
            EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 360);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            int phase = SessionState.GetInt(Key, 0);
            if (phase == 0 || !Application.isBatchMode) return;
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0))
            { SessionState.SetInt(Key, 0); Debug.LogError("Ritual batch exceeded 360 seconds"); EditorApplication.Exit(2); return; }
            if (phase == 1 && EditorApplication.isPlaying && !EditorApplication.isCompiling &&
                Time.frameCount > 3 && UnityEngine.Object.FindFirstObjectByType<BossEncounter>())
            { WardenRitualAcceptance.Start(); SessionState.SetInt(Key, 2); }
            else if (phase == 2)
            {
                var result = JsonUtility.FromJson<Result>(WardenRitualAcceptance.Status);
                if (!result.complete && string.IsNullOrEmpty(result.failure)) return;
                SessionState.SetBool(Key + ".passed", result.complete);
                SessionState.SetInt(Key, 3);
                EditorApplication.isPlaying = false;
            }
            else if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            {
                bool passed = SessionState.GetBool(Key + ".passed", false);
                Directory.CreateDirectory(Output);
                File.WriteAllText(Output + "/FinalEditorValidation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    unity = Application.unityVersion, batchMode = true, runtimePassed = passed,
                    compiling = EditorApplication.isCompiling, playing = EditorApplication.isPlaying,
                    sceneDirty = EditorSceneManager.GetActiveScene().isDirty
                }, Newtonsoft.Json.Formatting.Indented));
                SessionState.SetInt(Key, 0); EditorApplication.Exit(passed ? 0 : 1);
            }
        }
    }
}
