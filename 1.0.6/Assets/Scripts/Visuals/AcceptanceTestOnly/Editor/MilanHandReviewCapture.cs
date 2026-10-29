using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ArcaneArchive.Player;
using ArcaneArchive.Visuals.Editor;
namespace MilanAcceptance
{
    // Offscreen Play-mode capture. Never opens/focuses GameView or reads/sends OS input.
    [InitializeOnLoad]
    public static class MilanHandReviewCapture
    {
        const string Key="MilanHandReview.Pending";
        static MilanHandReviewCapture(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scenes before hand review.");
            EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            Camera eye=Camera.main;
            // Only this review's owned scene is changed, and it is not saved by this method.
            MilanFirstPersonSetup.Attach(player.gameObject,eye);
            player.enabled=false; // Prevent Start/Update from locking the user's OS cursor.
            foreach(var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))listener.enabled=false;
            SessionState.SetBool(Key,true);
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {
                SessionState.SetBool(Key,false);
                new GameObject("HandReviewCapture_EditorOnly").AddComponent<HandReviewFrames>();
            }
        }
    }
}
