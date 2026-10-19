using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MilanAcceptance
{
    [InitializeOnLoad]
    public static class ReviewPlayLauncher
    {
        const string Key="MilanReviewPlay.Pending";
        static ReviewPlayLauncher(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("This acceptance entry is batch-only and never opens a GameView.");
            EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {SessionState.SetBool(Key,false);new GameObject("ReviewPlayTests_EditorOnly").AddComponent<ReviewPlayTests>();}
        }
    }
}
