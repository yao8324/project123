using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneArchive.Contest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Renders actual game/Timeline frames at fixed times; not a performance recording.</summary>
    [InitializeOnLoad]
    public static class ContestEndingVideoExporter
    {
        const string Flag="IMMonster.Ending.VideoExport";
        const int Fps=30, Count=26*Fps;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static bool running;
        static string directory;
        static ContestStoryGame game;
        static string failure;
        [Serializable] sealed class Report {public bool complete;public string failure,scope;public int frames,fps,width,height;public float seconds;}
        static ContestEndingVideoExporter(){EditorApplication.update+=Tick;}

        [MenuItem("IM Monster/Ending/Export video frames (no save)")]
        public static void Export()
        {
            directory=Environment.GetEnvironmentVariable("IM_ENDING_VIDEO_OUTPUT");
            if(string.IsNullOrWhiteSpace(directory))throw new InvalidOperationException("Set IM_ENDING_VIDEO_OUTPUT to an output folder on a data drive.");
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(Path.Combine(directory,"Frames"));
            SessionState.SetString(Flag+".directory",directory);SessionState.SetBool(Flag,true);
            SessionState.SetFloat(Flag+".deadline",(float)EditorApplication.timeSinceStartup+600);
            EditorSceneManager.OpenScene(ContestSceneBuilder.ScenePath);
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            view.maximized=false;view.position=new Rect(30,35,1310,810);view.Focus();
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Flag,false))return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Flag+".deadline",0)){Finish(false,"Frame export timeout",0,0,0);return;}
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(running)return;
            game=Object.FindFirstObjectByType<ContestStoryGame>();
            if(!game || !game.Hud || game.player.Bindings==null)return;
            directory=SessionState.GetString(Flag+".directory","");running=true;
            Application.logMessageReceived+=Log;
            var state=new ContestStoryState{stage=7,verifiedMask=7,sealedBatch=true,nodes=3,clues=7,pulses=3,incidentRead=true,identityKnown=true,rescued=true,tutorialDone=true,transferred=true,batchLocation=3};
            typeof(ContestStoryGame).GetMethod("Restore",Private).Invoke(game,new object[]{state});
            game.ReplayEnding();
            var director=game.EndingCutscene.GetComponent<PlayableDirector>();
            director.Pause();director.timeUpdateMode=DirectorUpdateMode.Manual;
            game.StartCoroutine(Capture(director));
        }
        static void Log(string message,string trace,LogType type)
        {if((type==LogType.Error||type==LogType.Exception)&&!trace.Contains("UnityEditor.Search.SearchDatabase"))failure=message;}
        static IEnumerator Capture(PlayableDirector director)
        {
            int width=0,height=0;
            // Let the production scene finish its first-frame quality/layout setup.
            yield return null;yield return null;
            for(int i=0;i<Count;i++)
            {
                if(failure!=null){Finish(false,failure,i,width,height);yield break;}
                director.time=(double)i/Fps;director.Evaluate();
                typeof(ContestEndingCutscene).GetMethod("Render",Private).Invoke(game.EndingCutscene,null);
                Canvas.ForceUpdateCanvases();
                yield return new WaitForEndOfFrame();
                var frame=ScreenCapture.CaptureScreenshotAsTexture();width=frame.width;height=frame.height;
                File.WriteAllBytes(Path.Combine(directory,"Frames","frame-"+i.ToString("D5")+".tga"),frame.EncodeToTGA());
                Object.Destroy(frame);
                if(i%120==0)Debug.Log("[EndingVideo] Captured "+i+" / "+Count);
            }
            File.Copy(Path.Combine(Application.dataPath,"Contest20261107/Resources/Ending20261104/EndingSoundtrack.wav"),Path.Combine(directory,"EndingSoundtrack.wav"),true);
            Finish(true,null,Count,width,height);
        }
        static void Finish(bool complete,string error,int frames,int width,int height)
        {
            SessionState.SetBool(Flag,false);Application.logMessageReceived-=Log;
            directory=directory??SessionState.GetString(Flag+".directory","");
            File.WriteAllText(Path.Combine(directory,"render-report.json"),JsonUtility.ToJson(new Report{complete=complete,failure=error,frames=frames,fps=Fps,width=width,height=height,seconds=26,scope="Fixed-time rendering of the actual Unity ending scene, camera, animation, subtitles and fade at 30 fps. Original Timeline WAV supplied for muxing. No player checkpoint is written; this is not an FPS/performance capture."},true));
            Debug.Log("[EndingVideo] "+(complete?"PASS "+frames:"FAIL "+error));
            EditorApplication.delayCall+=()=>EditorApplication.Exit(complete?0:1);
            EditorApplication.isPlaying=false;
        }
    }
}
