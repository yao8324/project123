using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArcaneArchive.Player;
namespace MilanAcceptance
{
    [InitializeOnLoad] public static class GlassReviewLauncher
    {
        const string Key="MilanGlassReview.Pending";
        static GlassReviewLauncher(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new System.InvalidOperationException("Batch only, no UI focus.");
            EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            Object.FindFirstObjectByType<FirstPersonController>().enabled=false;
            foreach(var a in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))a.enabled=false;
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("GlassReview_EditorOnly").AddComponent<GlassMaterialReview>();}}
    }
}
