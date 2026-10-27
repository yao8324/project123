using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ArcaneArchive.Visuals.Editor
{
    public static class TrackingRiftAcceptance
    {
        const string Output="Docs/BossDesign/CombatFlow20261026";
        static BossEncounter e;static FirstPersonController p;static RecoveryMenu menu;static BossTestInputScope scope;
        static UnityEngine.InputSystem.Mouse testMouse;
        static bool running,background,captured,commandedRight;static int step,frame,peak,observed;
        static float since,clock,guard,health,pausedAge,releaseUntil;static double start;
        static Vector3 lastEnd,lastDirection,pausedHand;static bool hasEnd;static GameObject wall;static GroundFractureVisuals paused;
        static readonly HashSet<int> seen=new HashSet<int>();static readonly Dictionary<int,Vector3> origins=new Dictionary<int,Vector3>();
        static readonly List<string> checks=new List<string>();static string failure;
        [Serializable] class Report {public int step,peakSegments;public string failure;public string[] checks;public float guardRecoveryPerSecond,guardRecoveryDelay;}
        public static string Status=>JsonUtility.ToJson(new Report{step=step,peakSegments=peak,failure=failure,checks=checks.ToArray(),guardRecoveryPerSecond=e?e.Definition.guardRegen:0,guardRecoveryDelay=e?e.Definition.guardRegenDelay:0},true);
        public static string Start()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
            WardenFlowAcceptance.Stop();WardenCombatV2Acceptance.Stop();BossCombatPolishAcceptance.Stop();BossLaserGuardAcceptance.Stop();BossAcceptance.Stop();Stop();
            e=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();p=e.GetComponent<FirstPersonController>();menu=e.GetComponent<RecoveryMenu>();
            background=Application.runInBackground;Application.runInBackground=true;scope=new BossTestInputScope();
            testMouse=InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("TrackingRiftTestMouse");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            running=true;start=EditorApplication.timeSinceStartup;frame=-1;peak=0;failure=null;checks.Clear();Session();Step(0);InputSystem.onBeforeUpdate+=InjectMouse;EditorApplication.update+=Tick;return Status;
        }
        public static void Stop(){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=InjectMouse;Keys();Mouse();if(testMouse!=null && testMouse.added)InputSystem.RemoveDevice(testMouse);testMouse=null;scope?.Dispose();scope=null;if(wall)UnityEngine.Object.Destroy(wall);wall=null;if(running)Application.runInBackground=background;running=false;}
        static void Keys(params Key[] keys){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
        static void Mouse(bool right=false){commandedRight=right;}
        static void InjectMouse()
        {
            if(!running || InputState.currentUpdateType!=InputUpdateType.Dynamic || testMouse==null)return;
            InputSystem.QueueStateEvent(testMouse,new MouseState{buttons=(ushort)(commandedRight?2:0)});
        }
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS: "+message);}
        static void Step(int n){step=n;since=Time.time;Keys();Mouse();}
        static void Session()
        {
            Keys();Mouse();if(e.IsActive)Check(e.ReturnToLibrary(),"Return between line scenarios");Check(e.Begin(),"Enter production encounter");
            typeof(BossEncounter).GetField("pattern",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,2);
            seen.Clear();origins.Clear();hasEnd=false;captured=false;observed=0;
        }
        static void Aim(bool orbit=false)
        {
            var target=orbit?e.Arena.transform.position+Vector3.up*1.1f:e.Arena.boss.position+Vector3.up*2.5f;
            var q=Quaternion.LookRotation(target-e.View.transform.position);p.transform.rotation=Quaternion.Euler(0,q.eulerAngles.y,0);
            float pitch=Mathf.DeltaAngle(0,q.eulerAngles.x);typeof(FirstPersonController).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p)).localRotation=Quaternion.Euler(pitch,0,0);p.SetCursorLocked(true);
        }
        static void Orbit(){Aim(true);if(Time.time<releaseUntil)Keys();else Keys(Key.D);}
        static void Capture(string name,Vector3 target)
        {
            Directory.CreateDirectory(Output+"/Captures");var cam=e.View;var old=cam.targetTexture;var active=RenderTexture.active;var rotation=cam.transform.rotation;
            var rt=RenderTexture.GetTemporary(1440,810,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var tex=new Texture2D(1440,810,TextureFormat.RGB24,false);
            try{cam.transform.LookAt(target);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,810),0,0);tex.Apply();File.WriteAllBytes(Output+"/Captures/"+name+".png",tex.EncodeToPNG());}
            finally{cam.transform.rotation=rotation;cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
        static void Inspect()
        {
            var parts=e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>();peak=Mathf.Max(peak,parts.Length);
            foreach(var r in parts.OrderByDescending(r=>r.EruptionAge))
            {
                int id=r.GetInstanceID();
                if(seen.Add(id))
                {
                    if(hasEnd && Vector3.Distance(lastEnd,r.StartPoint)>.025f)throw new InvalidOperationException("Fracture line has a disconnected segment");
                    var dir=(r.EndPoint-r.StartPoint).normalized;
                    if(hasEnd && Vector3.Angle(lastDirection,dir)>e.Definition.fractureTurnSpeed*e.Definition.fractureInterval+.15f)throw new InvalidOperationException("Fracture snapped its steering angle");
                    if(r.boundary.Length!=4)throw new InvalidOperationException("Circular hazard boundary remains");
                    lastEnd=r.EndPoint;lastDirection=dir;hasEnd=true;observed++;origins[id]=r.StartPoint;
                }
                else if(Vector3.Distance(origins[id],r.StartPoint)>.001f)throw new InvalidOperationException("Committed fracture segment moved after warning");
            }
            if(!captured && e.FracturePathLength>7 && parts.Any(r=>r.EruptionAge>.05f))
            {Capture(step>=30?"linear_rift_phase2":"linear_rift_tracking",(parts[0].StartPoint+lastEnd)*.5f+Vector3.up*.2f);captured=true;}
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop();return;}if(frame==Time.frameCount)return;frame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup-start>160)throw new TimeoutException("Line/guard test timed out at "+step);
                if(e.IsPaused && step!=21 && !e.IsFinished){e.SendMessage("OnApplicationFocus",true);menu.Resume();releaseUntil=Time.time+.1f;}
                if(e.IsActive && !e.IsPaused)Aim();float t=Time.time-since;
                switch(step)
                {
                    case 0:
                        if(e.CurrentState==BossEncounter.State.Observe){guard=e.Combat.Guard;clock=e.BattleTime;Step(1);Mouse(true);}break;
                    case 1:
                        Mouse(true);
                        if(e.BattleTime-clock>=1)
                        {Check(guard-e.Combat.Guard>28,"Guard still consumes resources while held (spent="+(guard-e.Combat.Guard)+")");guard=e.Combat.Guard;clock=e.BattleTime;Step(2);}break;
                    case 2:
                        if(e.BattleTime-clock>=2.2f)
                        {Check(Mathf.Abs(e.Combat.Guard-guard)<.2f,"Guard does not regenerate during the first 2.2 seconds after release");Step(3);}break;
                    case 3:
                        if(e.BattleTime-clock>=3.5f)
                        {
                            float expected=(e.BattleTime-clock-2.5f)*12;
                            Check(Mathf.Abs((e.Combat.Guard-guard)-expected)<1.6f,"Measured guard regeneration is 12 per second after the 2.5-second delay");
                            Check(e.Definition.guardRegen==12 && e.Definition.guardRegenDelay==2.5f,"Saved production guard parameters are active");Session();Step(10);
                        }break;
                    case 10:
                        if(e.FractureSpawnCount>0)
                        {
                            var f=e.Arena.effectRoot.GetComponentInChildren<GroundFractureVisuals>();
                            Check(Vector3.Distance(f.StartPoint,e.Arena.boss.position)<1.3f,"Fracture starts at the Boss and advances across the floor");
                            Check(f.boundary.Length==4 && f.warningRim.GetComponent<MeshFilter>().sharedMesh.bounds.size.x>=.95f,"Warning includes both edges of the widened damage strip");
                            Check(f.GetComponentsInChildren<Collider>().Length==0,"Visual segments do not replace floor collisions");
                            Check(f.EruptionAge<-.5f && e.Combat.Health==e.Definition.playerHealth,"Each leading segment starts with a full warning before damage");
                            health=e.Combat.Health;Step(11);Inspect();Orbit();
                        }break;
                    case 11:
                        Orbit();Inspect();
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {
                            Check(observed>=8 && e.FracturePathLength>8,"A continuous multi-segment line advances for a sustained attack");
                            Check(e.FractureTotalTurn>5,"The leading line visibly turns toward a moving target");
                            Check(e.Combat.Health==health,"Lateral movement can evade the tracking line");
                            Check(peak<=10,"Active segments stay within a bounded lifetime budget");
                            Check(e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>().Length==0,"Line and afterglow expire before recovery");Session();Step(20);
                        }break;
                    case 20:
                        if(e.FractureHitCount>=1)
                        {
                            Check(Mathf.Abs(e.Combat.Health-(e.Definition.playerHealth-e.Definition.fractureDamage))<.1f,"Standing in the advancing line takes one configured impact, not per-frame damage");
                            paused=e.Arena.effectRoot.GetComponentInChildren<GroundFractureVisuals>();pausedAge=paused.EruptionAge;pausedHand=e.Arena.model.animatedRig.leftHand.position;
                            clock=e.BattleTime;observed=e.FractureSpawnCount;Capture("linear_rift_close",e.transform.position+e.transform.forward*2);menu.Open();Step(21);
                        }break;
                    case 21:
                        if(t>.45f)
                        {
                            Check(clock==e.BattleTime && observed==e.FractureSpawnCount && pausedAge==paused.EruptionAge,"Pause freezes pursuit, warning and afterglow");
                            Check(Vector3.Distance(pausedHand,e.Arena.model.animatedRig.leftHand.position)<.0001f,"Pause also freezes the sustained casting pose");
                            menu.Resume();releaseUntil=Time.time+.1f;e.DamageBoss(e.Definition.health*.55f,false);Step(22);
                        }break;
                    case 22:
                        if(t>.15f)
                        {
                            Check(e.CurrentState==BossEncounter.State.PhaseBreak && e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>().Length==0,"Phase change cancels the entire pending line");
                            typeof(BossEncounter).GetField("pattern",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(e,2);seen.Clear();origins.Clear();hasEnd=false;captured=false;observed=0;health=e.Combat.Health;Step(30);
                        }break;
                    case 30:
                        if(e.CurrentState==BossEncounter.State.Telegraph && e.CurrentAttack==2){Step(31);Orbit();}break;
                    case 31:
                        Orbit();Inspect();
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(e.Phase==2 && observed>=8 && e.FractureTotalTurn>5,"Phase-two sustained line retains bounded tracking");Check(e.Combat.Health==health,"Phase-two line remains avoidable by movement");Session();Step(40);}break;
                    case 40:
                        if(e.FractureSpawnCount==1)
                        {
                            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA_TrackingRiftWall";
                            wall.transform.SetPositionAndRotation(e.RiftHead+e.RiftDirection*2+Vector3.up*.6f,Quaternion.LookRotation(e.RiftDirection));wall.transform.localScale=new Vector3(4,1.2f,.2f);Physics.SyncTransforms();health=e.Combat.Health;Step(41);
                        }break;
                    case 41:
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {
                            Check(e.FractureBlocked && e.FracturePathLength<4,"Solid obstacle stops the advancing line");
                            Check(e.Combat.Health==health,"Player behind the obstacle is protected");UnityEngine.Object.Destroy(wall);wall=null;Check(e.ReturnToLibrary(),"Return after all line and guard scenarios");Step(50);
                        }break;
                    case 50:
                        if(t>.3f)
                        {Check(UnityEngine.Object.FindObjectsByType<GroundFractureVisuals>(FindObjectsSortMode.None).Length==0 && UnityEngine.Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None).Length==0,"No line, arena or test obstacle survives return");step=100;Write();Stop();}break;
                }
                if(e.CurrentState==BossEncounter.State.Lost)throw new InvalidOperationException("Unexpected defeat in line test "+step);
            }
            catch(Exception ex){failure=ex.ToString();step=-1;Write();Stop();if(menu)menu.Open();Debug.LogError("[Tracking rift acceptance] "+ex.Message);}
        }
        static void Write(){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/LineAndGuardAcceptance.json",Status);Debug.Log("[Tracking rift acceptance] "+Status);}
    }
}
