using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArcaneArchive.Visuals.Editor;
namespace MilanAcceptance
{
    public static class MilanSceneSaveGuardTests
    {
        [Serializable] private sealed class Report
        {
            public bool dirtyTargetPreserved, additiveDirtyScenePreserved, singleCleanUntitledRebuildPassed;
            public string multiSceneOutcome, unityVersion;
        }
        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
        public static void Run()
        {
            const string scenePath = "Assets/Scenes/MilanLibrary_Magic_V6.unity";
            var report = new Report { unityVersion = Application.unityVersion };
            Scene target = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var marker = new GameObject("QA_UNSAVED_TARGET_SENTINEL");
            EditorSceneManager.MarkSceneDirty(target);
            bool blocked = false;
            try { MilanV6SceneBuilder.Build(); }
            catch (InvalidOperationException ex) { blocked = ex.Message.Contains("Save the existing V6 scene"); }
            report.dirtyTargetPreserved = blocked && target.IsValid() && target.isLoaded && target.isDirty && marker;
            Require(report.dirtyTargetPreserved, "Dirty target scene protection failed.");
            UnityEngine.Object.DestroyImmediate(marker);
            EditorSceneManager.CloseScene(target, true); // Only the test's deliberately unsaved edit is discarded.

            string folder = "Assets/Scripts/Visuals/AcceptanceTestOnly/Editor/Fixtures";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            Scene saved = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Require(EditorSceneManager.SaveScene(saved, folder + "/SaveGuardSource.unity"), "Could not save test fixture.");
            marker = new GameObject("QA_UNSAVED_ADDITIVE_SENTINEL");
            EditorSceneManager.MarkSceneDirty(saved);
            Scene blank = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(blank);
            Require(!blank.isDirty && string.IsNullOrEmpty(blank.path) && SceneManager.sceneCount == 2, "Invalid multi-scene fixture.");
            try { MilanV6SceneBuilder.Build(); report.multiSceneOutcome = "rebuilt_without_losing_dirty_scene"; }
            catch (InvalidOperationException ex) { report.multiSceneOutcome = "blocked_safely: " + ex.Message; }
            report.additiveDirtyScenePreserved = saved.IsValid() && saved.isLoaded && saved.isDirty && marker && marker.scene == saved;
            Require(report.additiveDirtyScenePreserved, "Unrelated dirty additive scene was lost.");
            UnityEngine.Object.DestroyImmediate(marker);
            EditorSceneManager.CloseScene(saved, true); // Discard only this owned test fixture's sentinel.

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MilanV6SceneBuilder.Build();
            report.singleCleanUntitledRebuildPassed = SceneManager.GetActiveScene().path == scenePath && !SceneManager.GetActiveScene().isDirty;
            Require(report.singleCleanUntitledRebuildPassed, "Clean single untitled rebuild failed.");
            MilanArchiveValidation.Run();
            File.WriteAllText(Environment.GetEnvironmentVariable("MILAN_SAVE_GUARD_REPORT"), JsonUtility.ToJson(report, true));
            Debug.Log("MILAN_SCENE_SAVE_GUARDS_PASSED");
        }
    }
}
