using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>Editor-only input-driven tests; no test code ships in the player.</summary>
    public static class BossLaserGuardAcceptance
    {
        static BossEncounter e;static PlayerCombat combat;static FirstPersonController player;static RecoveryMenu menu;
        static bool running,wasBackground,commandedRight;static int step,frame=-1;static float since,hp,guard,clock;static double started;
        static Vector3 lockedDirection,originalPosition;static GameObject wall;static string failure;
        static Vector3 previousDirection;static float previousClock,maxTrackingAngle,maxTurnSpeed;
        static readonly List<string> checks=new List<string>();
        static BossTestInputScope inputScope;
        [Serializable] class Report{public int step;public string failure;public string[] checks;public float playerHealth,guard,bossTravel,battleTime,maxTrackingAngle,maxTurnSpeed;}
        public static string Status=>JsonUtility.ToJson(new Report{step=step,failure=failure,checks=checks.ToArray(),playerHealth=combat?combat.Health:0,guard=combat?combat.Guard:0,bossTravel=e?e.BossDistanceTravelled:0,battleTime=e?e.BattleTime:0,maxTrackingAngle=maxTrackingAngle,maxTurnSpeed=maxTurnSpeed},true);
        public static string Start()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            BossAcceptance.Stop();Stop();wasBackground=Application.runInBackground;Application.runInBackground=true;running=true;
            inputScope=new BossTestInputScope();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            e=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();player=e.GetComponent<FirstPersonController>();menu=e.GetComponent<RecoveryMenu>();combat=e.Combat;
            if(e.IsActive)e.ReturnToLibrary();originalPosition=e.transform.position;
            checks.Clear();failure=null;maxTrackingAngle=maxTurnSpeed=0;started=EditorApplication.timeSinceStartup;frame=-1;
            menu.ShowPage(RecoveryMenu.Page.DeepExplore);menu.StartEncounter();Check(e.IsActive,"Existing menu enters upgraded encounter");
            Step(0);InputSystem.onBeforeUpdate+=InjectMouse;EditorApplication.update+=Tick;return Status;
        }
        public static void Stop(){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=InjectMouse;Release();if(UnityEngine.InputSystem.Mouse.current!=null)InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new MouseState());inputScope?.Dispose();inputScope=null;if(wall)UnityEngine.Object.Destroy(wall);if(running)Application.runInBackground=wasBackground;running=false;}
        static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add("PASS: "+label);}
        static void Step(int value){step=value;since=Time.time;Release();}
        static void Keys(params Key[] keys){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
        static void Mouse(bool right){commandedRight=right;}
        static void InjectMouse()
        {
            if(!running || InputState.currentUpdateType!=InputUpdateType.Dynamic || UnityEngine.InputSystem.Mouse.current==null)return;
            InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new MouseState{buttons=(ushort)(commandedRight?2:0)});
        }
        static void Release(){Keys();Mouse(false);}
        static void Aim()
        {
            var target=e.Arena.boss.TransformPoint(new Vector3(0,2.6f,.92f));var look=Quaternion.LookRotation(target-e.View.transform.position);
            player.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y,0);float pitch=Mathf.DeltaAngle(0,look.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player)).localRotation=Quaternion.Euler(pitch,0,0);
            player.SetCursorLocked(true);
        }
        static void NewLaserSession()
        {
            Release();Check(e.ReturnToLibrary(),"Return between laser scenarios");Check(e.Begin(),"Fresh laser scenario resets guard");
            // Seed only the next attack choice, keeping all real timings, damage and input logic.
            typeof(BossEncounter).GetField("pattern",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,1);
            Check(combat.Guard==e.Definition.guardCapacity && !combat.GuardBroken,"Fresh session has full guard and no break state");
        }
        static void Capture(string name){Directory.CreateDirectory("Docs/BossDesign/Captures");ScreenCapture.CaptureScreenshot("Docs/BossDesign/Captures/"+name+".png");}
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop();return;}if(frame==Time.frameCount)return;frame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup-started>180)throw new TimeoutException("Laser/guard verification timed out");
                if(e.IsFinished)throw new InvalidOperationException("Unexpected defeat during laser test");
                float t=Time.time-since;
                if(e.IsActive)
                {
                    var local=e.Arena.transform.InverseTransformPoint(e.Arena.boss.position);local.y=0;
                    if(local.magnitude>e.Arena.radius-2.29f)throw new InvalidOperationException("Boss crossed movement boundary");
                    if(!menu.IsOpen)Aim();
                }
                switch(step)
                {
                    case 0:
                        if(e.CurrentAttack==3 && e.CurrentState==BossEncounter.State.Telegraph)
                        {Check(e.BossDistanceTravelled>5,"Boss travels through pursuit and orbit before second attack");hp=combat.Health;guard=combat.Guard;Step(1);}break;
                    case 1:
                        Mouse(true);
                        if(e.LaserIsFiring && e.AttackElapsed>.09f)
                        {Check(combat.Health==hp && combat.Guard<guard-25,"Held right mouse blocks real laser damage and spends guard");Capture("laser_block");Step(2);Mouse(true);}break;
                    case 2:
                        Mouse(true);
                        if(combat.GuardBroken)
                        {Check(combat.Guard==0 && !combat.Guarding && combat.Health<hp,"Sustained laser exhausts guard, breaks shield and damages player");Capture("guard_break");menu.Open();clock=e.BattleTime;hp=combat.Health;guard=combat.Guard;Step(3);}break;
                    case 3:
                        if(t>.7f){Check(e.IsPaused && e.BattleTime==clock && combat.Health==hp && combat.Guard==guard,"Pause freezes active laser, guard and health");menu.Resume();Step(4);}break;
                    case 4:
                        if(t<.6f)Keys(Key.A,Key.LeftShift);else Keys();
                        if(t>3.3f){Check(!combat.GuardBroken && combat.Guard>20,"Release exits broken state and regenerates guard");NewLaserSession();Step(5);}break;
                    case 5:
                        if(e.CurrentAttack==3 && e.CurrentState==BossEncounter.State.Telegraph && e.AttackElapsed>e.Definition.laserChargeTime-e.Definition.laserLockTime+.08f)
                        {hp=combat.Health;lockedDirection=previousDirection=e.LaserDirection;previousClock=e.BattleTime;Step(6);Keys(Key.A,Key.LeftShift);Capture("laser_charge");}break;
                    case 6:
                        if(t<.8f)Keys(Key.A,Key.LeftShift);else Keys();
                        float angle=Vector3.Angle(lockedDirection,e.LaserDirection);
                        if(e.CurrentState==BossEncounter.State.Telegraph && angle>.1f)throw new InvalidOperationException("Telegraph lock was lost");
                        float elapsed=e.BattleTime-previousClock;
                        if(elapsed>.0001f)
                        {
                            // atan2 is stable for tiny frame-to-frame angles; acos is not.
                            float turn=Mathf.Atan2(Vector3.Cross(previousDirection,e.LaserDirection).magnitude,Vector3.Dot(previousDirection,e.LaserDirection))*Mathf.Rad2Deg;
                            maxTurnSpeed=Mathf.Max(maxTurnSpeed,turn/elapsed);
                            if(turn>e.Definition.laserTrackingSpeed*elapsed+.025f)throw new InvalidOperationException("Laser exceeds configured tracking speed");
                        }
                        maxTrackingAngle=Mathf.Max(maxTrackingAngle,angle);previousClock=e.BattleTime;previousDirection=e.LaserDirection;
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(combat.Health==hp,"Sprint sideways outruns tracking laser without damage");Check(maxTrackingAngle>2,"Firing beam visibly tracks a moving player");Check(maxTurnSpeed<=e.Definition.laserTrackingSpeed+.5f,"Tracking obeys angular speed limit without snapping");NewLaserSession();Step(7);}break;
                    case 7:
                        if(e.CurrentAttack==3 && e.CurrentState==BossEncounter.State.Telegraph && e.AttackElapsed>e.Definition.laserChargeTime-e.Definition.laserLockTime+.08f)
                        {
                            hp=combat.Health;wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA_TemporaryBeamOccluder";
                            wall.transform.SetPositionAndRotation(e.LaserOrigin+e.LaserDirection*4,Quaternion.LookRotation(e.LaserDirection));wall.transform.localScale=new Vector3(4,4,.4f);Physics.SyncTransforms();Step(8);
                        }break;
                    case 8:
                        if(e.LaserIsFiring && e.LaserLength>4)throw new InvalidOperationException("Beam did not clip at solid obstacle");
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(combat.Health==hp && e.LaserLength<4,"World collider stops beam rendering and damage");UnityEngine.Object.Destroy(wall);wall=null;Check(e.ReturnToLibrary(),"Return works after laser encounter");Step(9);}break;
                    case 9:
                        if(t>.3f)
                        {
                            Check(UnityEngine.Object.FindObjectsByType<LaserBeamVisuals>(FindObjectsSortMode.None).Length==0,"No laser objects remain after return");
                            Check(Vector3.Distance(originalPosition,e.transform.position)<.2f,"Original library position restored");
                            step=100;Write();Stop();
                        }break;
                }
            }
            catch(Exception ex){failure=ex.ToString();step=-1;Write();Stop();menu.Open();Debug.LogError("[Laser guard acceptance] "+ex.Message);}
        }
        static void Write(){Directory.CreateDirectory("Docs/BossDesign");File.WriteAllText("Docs/BossDesign/LaserGuardAcceptance.json",Status);Debug.Log("[Laser guard acceptance] "+Status);}
    }
}
