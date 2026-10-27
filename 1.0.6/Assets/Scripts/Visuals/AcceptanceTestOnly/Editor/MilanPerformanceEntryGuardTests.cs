using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArcaneArchive.Visuals.Editor;
namespace MilanAcceptance
{
    public static class MilanPerformanceEntryGuardTests
    {
        [Serializable] private sealed class Report
        {
            public string unityVersion;
            public bool dirtyTargetBlocked, dirtyTargetRetained, unrelatedDirtyBlocked,
                unrelatedDirtyRetained, activeSceneUnchanged, loadedScenesUnchanged, sourceAssetsUnchanged;
        }
        static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
        static string Hash(string path)
        { using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
        static bool Blocked()
        {
            try { MilanPerformanceSetup.ApplyToExistingScene(); return false; }
            catch(InvalidOperationException ex)
            { return ex.Message == "Save unsaved scenes before applying the performance profile."; }
        }
        public static void Run()
        {
            const string targetPath="Assets/Scenes/MilanLibrary_Magic_V6.unity";
            string[] paths={targetPath,"ProjectSettings/QualitySettings.asset","ProjectSettings/GraphicsSettings.asset",
                "ProjectSettings/ProjectSettings.asset","Assets/Art/MilanLibraryV6/Settings/MilanV6_Balanced.asset",
                "Assets/Art/MilanLibraryV6/Settings/MilanV6_URP.asset"};
            string[] before=paths.Select(Hash).ToArray();
            var report=new Report {unityVersion=Application.unityVersion};
            Scene target=EditorSceneManager.OpenScene(targetPath,OpenSceneMode.Single);
            var marker=new GameObject("QA_PERFORMANCE_UNSAVED_TARGET");
            EditorSceneManager.MarkSceneDirty(target);
            int firstCount=SceneManager.sceneCount, firstActive=SceneManager.GetActiveScene().handle;
            report.dirtyTargetBlocked=Blocked();
            report.dirtyTargetRetained=target.IsValid() && target.isLoaded && target.isDirty && marker && marker.scene==target;
            Require(report.dirtyTargetBlocked && report.dirtyTargetRetained,"Dirty target protection failed.");
            Require(SceneManager.sceneCount==firstCount && SceneManager.GetActiveScene().handle==firstActive,"Target scene arrangement changed.");
            // Discard only this batch test's deliberately unsaved sentinel.
            EditorSceneManager.CloseScene(target,true);
            target=EditorSceneManager.OpenScene(targetPath,OpenSceneMode.Single);
            Require(!target.isDirty,"Target scene must be clean for unrelated-scene test.");
            Scene unrelated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            marker=new GameObject("QA_PERFORMANCE_UNSAVED_UNRELATED");
            SceneManager.MoveGameObjectToScene(marker,unrelated);
            EditorSceneManager.MarkSceneDirty(unrelated);
            SceneManager.SetActiveScene(target);
            var handles=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).handle).ToArray();
            report.unrelatedDirtyBlocked=Blocked();
            report.unrelatedDirtyRetained=unrelated.IsValid() && unrelated.isLoaded && unrelated.isDirty && marker && marker.scene==unrelated;
            report.activeSceneUnchanged=SceneManager.GetActiveScene().handle==target.handle && !target.isDirty;
            report.loadedScenesUnchanged=handles.SequenceEqual(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).handle));
            report.sourceAssetsUnchanged=before.SequenceEqual(paths.Select(Hash));
            Require(report.unrelatedDirtyBlocked && report.unrelatedDirtyRetained && report.activeSceneUnchanged &&
                report.loadedScenesUnchanged && report.sourceAssetsUnchanged,"Unrelated dirty scene or disk asset preservation failed.");
            File.WriteAllText(Environment.GetEnvironmentVariable("MILAN_PERFORMANCE_GUARD_REPORT"),JsonUtility.ToJson(report,true));
            Debug.Log("MILAN_PERFORMANCE_ENTRY_GUARDS_PASSED");
        }
    }
}
