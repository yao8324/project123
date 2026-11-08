using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArcaneArchive.Visuals;
namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class SanctuaryR3PlayCheck
    {
        const string Key="R3_PlayCheck";
        static double start;
        static int stage;
        static string report;
        static SanctuaryR3Preview preview;
        static Vector3 initial;
        static Transform motionRoot;
        static Vector3 initialBone;
        static SanctuaryR3PlayCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){SanctuaryR3Import.Build();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){stage=-1;start=EditorApplication.timeSinceStartup;report="Actual Play Mode, three sequential clips. Not combat acceptance.\n";EditorApplication.update+=Tick;}
        }
        static void Tick()
        {
            try{
                if(EditorApplication.timeSinceStartup-start>120)throw new Exception("R3 runtime preview timed out");
                if(!preview)preview=UnityEngine.Object.FindFirstObjectByType<SanctuaryR3Preview>();
                if(!preview)return;
                if(stage<0){stage=0;initial=preview.actor.transform.position;preview.Play(stage);motionRoot=preview.actor.GetComponentsInChildren<Transform>().First(t=>t.name=="Root");initialBone=motionRoot.position;return;}
                if(preview.Elapsed<preview.clips[stage].length-.001)return;
                var delta=preview.actor.transform.position-initial;
                var boneDelta=motionRoot.position-initialBone;
                report+=$"{preview.clips[stage].name}: elapsed={preview.Elapsed:F3}, actor displacement={delta}, bone displacement={boneDelta}\n";
                float expected=stage==0?2.2f:stage==1?8f:10f;
                if(Mathf.Abs(new Vector2(boneDelta.x,boneDelta.z).magnitude-expected)>.12f)throw new Exception("Unexpected animated Root displacement: "+boneDelta+" expected "+expected);
                stage++;
                if(stage<3){preview.Play(stage);return;}
                File.WriteAllText("Docs/SanctuaryR3Build/UnityPlayCheck.txt",report);Debug.Log("R3_PLAY_CHECK_COMPLETE\n"+report);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);
            }catch(Exception ex){Debug.LogException(ex);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
    }
}
