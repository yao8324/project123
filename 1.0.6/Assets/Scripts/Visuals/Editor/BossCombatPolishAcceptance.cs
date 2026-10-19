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
    // Real input and collision tests. Only the next attack index is seeded; gameplay values stay unchanged.
    public static class BossCombatPolishAcceptance
    {
        static BossEncounter e;static PlayerCombat combat;static FirstPersonController player;static RecoveryMenu menu;
        static BossTestInputScope scope;static bool running,background;static int step,frame=-1;static double started;
        static float since,hp,guard,clock,enemyTurn,playerTurn;static string failure;static GameObject wall;
        static Vector3 position,direction;static Quaternion rotation,arm;static Transform slab;static Vector3 slabPosition;
        static readonly List<string> checks=new List<string>();
        [Serializable] class Report {public int step;public string failure;public string[] checks;public float health,guard,enemyTurn,playerTurn;}
        public static string Status=>JsonUtility.ToJson(new Report{step=step,failure=failure,checks=checks.ToArray(),health=combat?combat.Health:0,guard=combat?combat.Guard:0,enemyTurn=enemyTurn,playerTurn=playerTurn},true);
        public static string Start()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
            BossAcceptance.Stop();BossLaserGuardAcceptance.Stop();Stop();background=Application.runInBackground;Application.runInBackground=true;running=true;
            scope=new BossTestInputScope();EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            e=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();combat=e.Combat;player=e.GetComponent<FirstPersonController>();menu=e.GetComponent<RecoveryMenu>();
            checks.Clear();failure=null;enemyTurn=playerTurn=0;started=EditorApplication.timeSinceStartup;frame=-1;NewSession(0);Step(0);EditorApplication.update+=Tick;return Status;
        }
        public static void Stop(){EditorApplication.update-=Tick;Release();scope?.Dispose();scope=null;if(wall)UnityEngine.Object.Destroy(wall);wall=null;if(running)Application.runInBackground=background;running=false;}
        static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS: "+text);}
        static void Step(int next){step=next;since=Time.time;Release();}
        static void Keys(params Key[] keys){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
        static void Mouse(bool left=false,bool right=false){if(UnityEngine.InputSystem.Mouse.current!=null)InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
        static void Release(){Keys();Mouse();}
        static void NewSession(int pattern)
        {
            Release();if(e.IsActive)Check(e.ReturnToLibrary(),"Return between scenarios");Check(e.Begin(),"Enter fresh encounter");
            typeof(BossEncounter).GetField("pattern",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(e,pattern);
        }
        static void Aim(float yaw=0)
        {
            var target=e.Arena.boss.TransformPoint(new Vector3(0,2.55f,.88f));var look=Quaternion.LookRotation(target-e.View.transform.position);
            player.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y+yaw,0);float pitch=Mathf.DeltaAngle(0,look.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)).localRotation=Quaternion.Euler(pitch,0,0);player.SetCursorLocked(true);
        }
        static void Capture(string name){Directory.CreateDirectory("Docs/BossDesign/Captures");ScreenCapture.CaptureScreenshot("Docs/BossDesign/Captures/polish_"+name+".png");}
        static bool Warning(int attack,float elapsed){return e.CurrentAttack==attack && e.CurrentState==BossEncounter.State.Telegraph && e.AttackElapsed>=elapsed;}
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop();return;}if(frame==Time.frameCount)return;frame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup-started>210)throw new TimeoutException("Combat polish tests timed out at "+step);
                if(e.IsFinished)throw new InvalidOperationException("Unexpected defeat at "+step);
                // MCP calls can move editor focus. Explicit pause coverage is step 12.
                if(e.IsPaused && step!=12){e.SendMessage("OnApplicationFocus",true);menu.Resume();}
                float t=Time.time-since;if(e.IsActive && !e.IsPaused)Aim(step==71?5:0);
                switch(step)
                {
                    case 0:
                        if(e.CurrentState==BossEncounter.State.Observe){guard=combat.Guard;Step(1);Mouse(false,true);}break;
                    case 1:
                        Mouse(false,true);
                        if(t>=1){float spent=guard-combat.Guard;Check(spent>=29 && spent<=36,"Holding guard for one second spends about 32 including activation");Step(2);}break;
                    case 2:
                        if(Warning(0,.9f)){arm=e.Arena.model.rightArm.localRotation;Capture("volley_windup");Step(3);}break;
                    case 3:
                        if(e.CurrentState==BossEncounter.State.Attack && e.AttackElapsed>.35f)
                        {Check(Quaternion.Angle(arm,e.Arena.model.rightArm.localRotation)>20,"Volley releases with a visible arm sweep");Capture("volley_sweep");Step(4);}break;
                    case 4:
                        if(t<.65f)Keys(Key.D,Key.LeftShift);else Keys();
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {enemyTurn=e.MaximumEnemyHomingTurn;Check(enemyTurn>.2f && enemyTurn<=e.Definition.enemyHomingBudget+.05f,"Enemy missiles curve but stay within total turning budget");rotation=e.Arena.boss.rotation;position=player.transform.position;Step(5);}break;
                    case 5:
                        Keys(Key.A,Key.LeftShift);
                        if(t>.65f)
                        {Check(Vector3.Distance(position,player.transform.position)>1,"Player can flank during recovery");Check(Quaternion.Angle(rotation,e.Arena.boss.rotation)<.05f,"Boss does not auto-track player during recovery");NewSession(2);Step(10);}break;
                    case 10:
                        if(Warning(2,1.53f))
                        {
                            hp=combat.Health;Check(e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>().Length==3,"Three irregular fracture patches replace pillars");
                            Check(Vector3.Dot(e.Arena.model.leftArm.localRotation*Vector3.down,Vector3.up)>.1f && Vector3.Dot(e.Arena.model.rightArm.localRotation*Vector3.down,Vector3.up)>.1f,"Fracture charge lifts both arms");
                            Capture("fracture_warning");Step(11);
                        }break;
                    case 11:
                        if(t<.64f)Keys(Key.S,Key.LeftShift);else Keys();
                        if(e.CurrentState==BossEncounter.State.Attack && e.AttackElapsed>.55f)
                        {
                            var f=e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>()[0];slab=f.slabs[0];
                            Check(f.slabs.Length>=12 && f.slabs.Any(s=>s.localPosition.y>.06f),"Irregular slabs lift during eruption");
                            Check(combat.Health==hp,"Leaving locked fracture avoids ground damage");Capture("fracture_eruption");
                            Step(12);menu.Open();clock=e.BattleTime;slabPosition=slab.localPosition;
                        }break;
                    case 12:
                        if(t>.55f){Check(e.BattleTime==clock && Vector3.Distance(slab.localPosition,slabPosition)<.0001f,"Pause freezes fracture animation and combat clock");menu.Resume();Step(13);}break;
                    case 13:
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(combat.Health==hp && player.IsGrounded,"Fracture dodge retains health and intact floor collision");NewSession(2);Step(20);}break;
                    case 20:
                        if(Warning(2,1.5f)){hp=combat.Health;Step(21);}break;
                    case 21:
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(Mathf.Abs(hp-combat.Health-e.Definition.fractureDamage)<.01f,"Standing in fracture takes one real ground hit");NewSession(3);Step(30);}break;
                    case 30:
                        if(Warning(4,1.12f)){direction=e.RushDirection;hp=combat.Health;Capture("rush_windup");Step(31);}break;
                    case 31:
                        if(t<.7f)Keys(Key.A,Key.LeftShift);else Keys();
                        CheckDirection();
                        if(e.CurrentState==BossEncounter.State.Attack && e.AttackElapsed>.2f)
                        {Check(Mathf.DeltaAngle(0,e.Arena.model.floatingBody.localEulerAngles.x)>15,"Rush leans body into forward motion");Capture("rush_attack");Step(32);}break;
                    case 32:
                        CheckDirection();
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(e.RushTravel>5 && e.RushHitCount==0 && combat.Health==hp,"Side dodge avoids committed rush with substantial travel");NewSession(3);Step(40);}break;
                    case 40:
                        if(Warning(4,1.12f)){hp=combat.Health;Step(41);}break;
                    case 41:
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(e.RushHitCount==1 && Mathf.Abs(hp-combat.Health-e.Definition.rushDamage)<.01f,"Rush hits player exactly once for configured damage");Check(Vector3.Distance(e.Arena.boss.position,player.transform.position)>.7f,"Boss stops before passing through player");NewSession(3);Step(50);}break;
                    case 50:
                        if(Warning(4,1.12f))
                        {
                            hp=combat.Health;wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA_RushWall";
                            wall.transform.SetPositionAndRotation(e.Arena.boss.position+e.RushDirection*3+Vector3.up*2,Quaternion.LookRotation(e.RushDirection));wall.transform.localScale=new Vector3(5,4,.4f);Physics.SyncTransforms();Step(51);
                        }break;
                    case 51:
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {Check(e.RushTravel<3 && e.RushHitCount==0 && combat.Health==hp,"Solid obstacle stops rush and protects player");UnityEngine.Object.Destroy(wall);wall=null;NewSession(1);Step(60);}break;
                    case 60:
                        if(Warning(3,1.85f))
                        {Check(Vector3.Dot(e.Arena.model.leftArm.localRotation*Vector3.down,Vector3.up)>.25f && Vector3.Dot(e.Arena.model.rightArm.localRotation*Vector3.down,Vector3.up)>.25f,"Laser charge raises both arms above shoulder line");Capture("laser_raised_arms");Step(61);Keys(Key.D,Key.LeftShift);}break;
                    case 61:
                        if(t<.85f)Keys(Key.D,Key.LeftShift);else Keys();
                        if(e.CurrentState==BossEncounter.State.Recovery){NewSession(0);Step(70);}break;
                    case 70:
                        if(e.CurrentState==BossEncounter.State.Observe){Step(71);}break;
                    case 71:
                        Mouse(true,false);
                        if(t>.85f){playerTurn=e.MaximumPlayerHomingTurn;Check(playerTurn>.2f && playerTurn<=e.Definition.playerHomingBudget+.05f,"Player bolts gently correct small aim error with bounded turning");Step(72);}break;
                    case 72:
                        Aim(45);Mouse(true,false);
                        if(t>.45f)
                        {
                            var shots=(System.Collections.IList)typeof(BossEncounter).GetField("shots",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(e);
                            bool saw=false;foreach(var s in shots){var ty=s.GetType();if(!(bool)ty.GetField("enemy").GetValue(s) && (float)ty.GetField("age").GetValue(s)<.35f){saw=true;Check(!(bool)ty.GetField("homing").GetValue(s),"Wide player miss does not acquire a target");}}
                            Check(saw,"Off-axis shot is present for acquisition check");Check(e.ReturnToLibrary(),"Return after all new attacks");Step(80);
                        }break;
                    case 80:
                        if(t>.35f)
                        {
                            Check(UnityEngine.Object.FindObjectsByType<GroundFractureVisuals>(FindObjectsSortMode.None).Length==0 && UnityEngine.Object.FindObjectsByType<RushTelegraphVisuals>(FindObjectsSortMode.None).Length==0,"Return cleans fracture and rush visuals");
                            Check(UnityEngine.Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None).Length==0,"Return removes arena after repeated sessions");step=100;Write();Stop();
                        }break;
                }
            }
            catch(Exception ex){failure=ex.ToString();step=-1;Write();Stop();menu.Open();Debug.LogError("[Combat polish acceptance] "+ex.Message);}
        }
        static void CheckDirection(){if(Vector3.Angle(direction,e.RushDirection)>.05f)throw new InvalidOperationException("Locked rush bends toward player");}
        static void Write(){Directory.CreateDirectory("Docs/BossDesign");File.WriteAllText("Docs/BossDesign/CombatPolishAcceptance.json",Status);Debug.Log("[Combat polish acceptance] "+Status);}
    }
}
