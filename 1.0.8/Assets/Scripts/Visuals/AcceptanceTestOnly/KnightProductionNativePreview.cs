#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>Opt-in native startup/render sampling only. Not a combat or human-input acceptance test.</summary>
    public sealed class KnightProductionNativePreview : MonoBehaviour
    {
        string output;
        float began, readyAt = -1;
        int rendered;
        readonly List<float> samples = new List<float>();
        readonly List<string> errors = new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-knight-v5-preview")<0)return;
            int index=Array.IndexOf(args,"-knight-v5-output");
            if(index<0 || index+1>=args.Length){Debug.LogError("Native preview requires an explicit output directory.");Application.Quit(1);return;}
            var instance=new GameObject("Opt-in Knight V5 native preview").AddComponent<KnightProductionNativePreview>();
            instance.output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(instance.output);
            instance.began=Time.realtimeSinceStartup;Application.runInBackground=true;Application.targetFrameRate=60;
            Application.logMessageReceived+=instance.Log;
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(text+"\n"+stack);}
        void Update()
        {
            var encounter=FindFirstObjectByType<BossEncounter>();
            if(Time.realtimeSinceStartup-began>90){errors.Add("Native scene startup/render timeout");Finish(encounter);return;}
            if(!encounter || !encounter.KnightMode || !encounter.IsActive)return;
            if(readyAt<0){readyAt=Time.realtimeSinceStartup;return;}
            if(Time.realtimeSinceStartup-readyAt<3)return;
            samples.Add(Time.unscaledDeltaTime*1000);rendered++;
            if(rendered<120)return;
            try
            {
                var cam=encounter.View;var original=cam.targetTexture;var active=RenderTexture.active;
                var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"native-preview.png"),image.EncodeToPNG());}
                finally{cam.targetTexture=original;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Destroy(image);}
            }
            catch(Exception exception){errors.Add(exception.ToString());}
            Finish(encounter);
        }
        void Finish(BossEncounter encounter)
        {
            enabled=false;Application.logMessageReceived-=Log;
            var ordered=samples.OrderBy(v=>v).ToArray();
            var report=new Report{unity=Application.unityVersion,platform=Application.platform.ToString(),gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,complete=errors.Count==0 && rendered>=120,frames=rendered,meanMs=samples.Count>0?samples.Average():0,p95Ms=ordered.Length>0?ordered[Mathf.Min(ordered.Length-1,(int)(ordered.Length*.95f))]:0,errors=errors.ToArray(),knight=encounter && encounter.KnightMode,paused=encounter && encounter.IsPaused,scope="Native startup and 120-frame render sample after three-second warmup, 1280x720, capped at 60. Focus/pause state recorded. Not full combat, human input, or target-device performance acceptance."};
            File.WriteAllText(Path.Combine(output,"NativePreview.json"),JsonUtility.ToJson(report,true));Application.Quit(report.complete?0:1);
        }
        [Serializable] class Report{public string unity,platform,gpu,cpu,scope;public bool complete,knight,paused;public int frames;public float meanMs,p95Ms;public string[] errors;}
    }
}
#endif
