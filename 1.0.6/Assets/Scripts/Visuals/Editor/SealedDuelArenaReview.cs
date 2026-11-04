using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class SealedDuelArenaReview
    {
        const string Out="Docs/SealedDuelArena";
        static double due;static int lastFrame;
        [Serializable] class Request{public string action,name;public float[] feet,look;public bool hideBoss;}
        static SealedDuelArenaReview(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode)Application.runInBackground=true;};}
        public static void Launch(){SealedDuelArenaAcceptance.GuardSceneWork();EditorSceneManager.OpenScene(SealedDuelArenaAcceptance.ScenePath);EditorApplication.EnterPlaymode();}
        static void Aim(BossEncounter e,Vector3 point)
        {
            var p=e.GetComponent<FirstPersonController>();var rotation=Quaternion.LookRotation(point-e.View.transform.position);
            p.transform.rotation=Quaternion.Euler(0,rotation.eulerAngles.y,0);float pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p)).localRotation=Quaternion.Euler(pitch,0,0);
        }
        static void Capture(BossEncounter e,string name,bool hideBoss)
        {
            Directory.CreateDirectory(Out+"/Evidence");var cam=e.View;var old=cam.targetTexture;var active=RenderTexture.active;bool boss=e.Arena.boss.gameObject.activeSelf;
            var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{if(hideBoss)e.Arena.boss.gameObject.SetActive(false);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Out+"/Evidence/"+name+".png",tex.EncodeToPNG());}
            finally{e.Arena.boss.gameObject.SetActive(boss);cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
        static void Tick()
        {
            if(EditorApplication.isPlaying)Application.runInBackground=true;
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<due || Time.frameCount==lastFrame)return;
            lastFrame=Time.frameCount;var e=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();if(!e)return;
            string request=Out+"/request.json";if(!File.Exists(request))return;
            try{
                var r=JsonUtility.FromJson<Request>(File.ReadAllText(request));File.Delete(request);Application.runInBackground=true;
                if(r.action=="quit-editor"){EditorApplication.Exit(0);return;}
                if(r.action=="ritual"){WardenRitualAcceptance.Start();}
                else if(r.action=="flow"){WardenFlowAcceptance.Start();}
                else if(r.action=="stop-tests"){WardenRitualAcceptance.Stop();WardenFlowAcceptance.Stop();}
                else if(r.action=="return"){if(e.IsActive)e.ReturnToLibrary();}
                else if(r.action=="begin"){if(e.IsActive)e.ReturnToLibrary();if(!e.Begin())throw new Exception("Begin failed");}
                else if(r.action=="build-mac"){EditorApplication.isPlaying=false;EditorApplication.delayCall+=SealedDuelArenaBuilder.BuildMac;}
                else if(e.IsActive){
                    var p=e.GetComponent<FirstPersonController>();var menu=e.GetComponent<RecoveryMenu>();e.SendMessage("OnApplicationFocus",true);if(menu.IsOpen)menu.Resume();
                    if(r.feet!=null && r.feet.Length==3 && !p.TryReturnToSpawn(e.Arena.transform.position+new Vector3(r.feet[0],r.feet[1],r.feet[2]),Quaternion.identity))throw new Exception("Unsafe capture spawn");
                    if(r.look!=null && r.look.Length==3)Aim(e,e.Arena.transform.position+new Vector3(r.look[0],r.look[1],r.look[2]));
                    if(r.action=="shot")Capture(e,r.name,r.hideBoss);
                    if(r.action=="pause")menu.Open();
                }
                File.WriteAllText(Out+"/ReviewStatus.txt",DateTime.Now.ToString("O")+" action="+r.action+" active="+e.IsActive+" state="+e.CurrentState+" position="+e.transform.position+" frame="+Time.frameCount+" fps="+(1/Mathf.Max(.001f,Time.unscaledDeltaTime)));
            }catch(Exception ex){File.WriteAllText(Out+"/ReviewStatus.txt",ex.ToString());Debug.LogException(ex);}
            due=EditorApplication.timeSinceStartup+.25;
        }
    }
}
