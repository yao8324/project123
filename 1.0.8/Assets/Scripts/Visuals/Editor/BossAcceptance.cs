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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace ArcaneArchive.Visuals.Editor
{
    // Editor-only acceptance driver. Runs the real player input map and physical projectiles.
    // Never changes health, damage, timing, saved key bindings or prefab assets.
    public static class BossAcceptance
    {
        static BossEncounter e; static RecoveryMenu menu; static FirstPersonController player;
        static int step,frame;static float since,pauseClock,pauseHealth;static Vector3 pausePosition,origin;
        static bool sawDamage,sawPhase2,sawJump,sawAbsorb; static float maxY;
        static readonly HashSet<string> seen=new HashSet<string>();
        static readonly List<string> checks=new List<string>();
        static double started;static string failure;
        static bool running,wasBackground;
        static BossTestInputScope inputScope;
        static string reportPath="PlayModeAcceptance.json";
        static float defenseHealth;static uint jumpStart;static int cycles;
        public static string Status => JsonUtility.ToJson(new Report { step=step,state=e?e.CurrentState.ToString():"none",health=e&&e.Combat?e.Combat.Health:0,bossHealth=e?e.BossHealth:0,battleTime=e?e.BattleTime:0,elapsed=(float)(EditorApplication.timeSinceStartup-started),checks=checks.ToArray(),observed=seen.ToArray(),failure=failure });
        [Serializable] class Report {public int step;public string state,failure;public float health,bossHealth,battleTime,elapsed;public string[] checks,observed;}
        public static string Start()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            Stop();wasBackground=Application.runInBackground;Application.runInBackground=true;running=true;
            inputScope=new BossTestInputScope();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            e=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();menu=e.GetComponent<RecoveryMenu>();player=e.GetComponent<FirstPersonController>();
            if(e.IsActive)e.ReturnToLibrary();
            checks.Clear();seen.Clear();failure=null;sawDamage=sawPhase2=sawJump=sawAbsorb=false;maxY=0;
            reportPath="PlayModeAcceptance.json";
            origin=player.transform.position;started=EditorApplication.timeSinceStartup;frame=-1;
            menu.ShowBook(UnityEngine.Object.FindObjectsByType<ArcaneArchive.Interaction.DemoBookTarget>(FindObjectsSortMode.None).First(t=>t.Record&&t.Record.opensTrial).Record);Click("DeepExploreButton");
            Check(e.IsActive && !e.libraryEnvironment.activeSelf,"Button enters arena and hides library");
            Step(0);EditorApplication.update+=Tick;return Status;
        }
        public static void Stop(){EditorApplication.update-=Tick;Release();inputScope?.Dispose();inputScope=null;if(running)Application.runInBackground=wasBackground;running=false;}
        public static string StartDefenses()
        {
            Start();reportPath="DefenseAcceptance.json";Step(20);return Status;
        }
        static void Step(int value){step=value;since=Time.time;Release();}
        static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add("PASS: "+label);}
        static void Click(string name)
        {
            var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.name==name);
            ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        static void ClickText(string text)
        {
            var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(x=>x.GetComponentInChildren<TMPro.TMP_Text>().text==text);
            ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        static void Keys(params Key[] keys){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
        static void Mouse(bool left,bool right){if(UnityEngine.InputSystem.Mouse.current!=null)InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));}
        static void Release(){Keys();Mouse(false,false);}
        static bool IncomingPage()
        {
            var shots=(System.Collections.IList)typeof(BossEncounter).GetField("shots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
            foreach(var shot in shots)
            {
                var type=shot.GetType();if(!(bool)type.GetField("enemy").GetValue(shot))continue;
                var go=(GameObject)type.GetField("go").GetValue(shot);if(!go)continue;
                var delta=go.transform.position-e.View.transform.position;
                if(delta.sqrMagnitude<4.2f*4.2f && Vector3.Dot(e.View.transform.forward,delta.normalized)>.75f)return true;
            }
            return false;
        }
        static void Aim()
        {
            var delta=e.Arena.boss.TransformPoint(new Vector3(0,2.55f,.88f))-e.View.transform.position;
            var look=Quaternion.LookRotation(delta);player.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y,0);
            float pitch=Mathf.DeltaAngle(0,look.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,pitch);
            var pivot=(Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
            pivot.localRotation=Quaternion.Euler(pitch,0,0);
            player.SetCursorLocked(true);
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop();return;}
            if(Time.frameCount==frame)return;frame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup-started>240)throw new TimeoutException("Acceptance exceeded 240 seconds");
                float t=Time.time-since;
                if(e.IsActive){seen.Add(e.CurrentState.ToString());if(e.Message!="")seen.Add(e.Message);if(e.Combat.Health<100)sawDamage=true;if(e.Phase==2)sawPhase2=true;maxY=Mathf.Max(maxY,player.transform.position.y);if(e.Combat.Power>0)sawAbsorb=true;}
                switch(step)
                {
                    case 0:
                        if(t>.5f){Check(player.IsGrounded,"Spawn rests on arena mesh collider");Step(1);}break;
                    case 1:
                        if(t<.8f)Keys(Key.W,Key.LeftShift);else if(t<1.5f)Keys(Key.Space);else Keys();
                        if(t>2.3f){Check(Vector3.Distance(player.transform.position,e.Arena.spawn.position)>2,"Real W + Shift moves player");Check(maxY>.7f,"Real Space key jumps on arena floor");sawJump=true;Keys(Key.Escape);StepWithoutRelease(2);}break;
                    case 2:
                        if(t>.15f)Keys();
                        if(t>.5f){Check(menu.IsOpen && e.IsPaused,"Real Esc opens combat pause");pauseClock=e.BattleTime;pauseHealth=e.Combat.Health;pausePosition=player.transform.position;Step(3);}break;
                    case 3:
                        Keys(Key.W);Mouse(true,true);
                        if(t>1.1f){Check(Mathf.Abs(e.BattleTime-pauseClock)<.001f && e.Combat.Health==pauseHealth,"Pause freezes encounter clock and health");Check(Vector3.Distance(player.transform.position,pausePosition)<.02f,"Pause disables movement");Release();Keys(Key.Escape);StepWithoutRelease(4);}break;
                    case 4:
                        if(t>.15f)Keys();
                        if(t>.6f){Check(!menu.IsOpen && !e.IsPaused,"Esc resumes battle");Check(e.BossHealth==e.Definition.health,"UI click does not fire a combat shot");player.TryReturnToSpawn(e.Arena.spawn.position,e.Arena.spawn.rotation);Step(5);}break;
                    case 5:
                        Aim();
                        // Stand still to verify damage and natural death from the attack cycle.
                        if(e.CurrentState==BossEncounter.State.Lost){Check(sawDamage,"Natural enemy attack cycle inflicts damage and defeats player");Check(menu.CurrentPage==RecoveryMenu.Page.BattleResult,"Loss opens result panel");SaveCapture("defeat");Step(6);}break;
                    case 6:
                        if(t>.4f){ClickText("再次挑战");Check(e.CurrentState==BossEncounter.State.Intro && e.Combat.Health==100 && e.BossHealth==1000 && e.Phase==1 && e.ActiveAttackCount==0,"Retry resets health, phase and attacks");Step(7);}break;
                    case 7:
                        Aim();
                        if(e.CanFight)
                        {
                            Mouse(true,e.Combat.AbsorbCooldown<=0 && IncomingPage());
                            if(e.Message.StartsWith("裂地禁咒") || (e.CurrentState==BossEncounter.State.Attack && e.BattleTime>18))Keys(Key.D);else Keys();
                        }
                        if(e.CurrentState==BossEncounter.State.Won){Check(sawPhase2,"Actual player bolts trigger phase two");Check(sawAbsorb,"Right mouse absorption consumes an actual projectile and gains power");Check(menu.CurrentPage==RecoveryMenu.Page.BattleResult,"Actual player bolts produce victory and result panel");SaveCapture("victory");Step(8);}
                        else if(e.CurrentState==BossEncounter.State.Lost)throw new Exception("Win input scenario lost unexpectedly");break;
                    case 8:
                        if(t>.4f){ClickText("返回图书馆");Step(9);}break;
                    case 9:
                        if(t>.4f){Check(!e.IsActive && e.libraryEnvironment.activeSelf && Vector3.Distance(origin,player.transform.position)<.2f,"Return restores library and prior player position");Check(UnityEngine.Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None).Length==0,"Return removes runtime arena");Check(UnityEngine.Object.FindObjectsByType<BossCombatHUD>(FindObjectsSortMode.None).Length==0,"Return removes combat HUD");Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Single EventSystem retained");Write();Stop();step=100;}break;
                    case 20:
                        Aim();
                        // The new second attack is a laser: dodge its locked direction before testing the wave.
                        if(e.CurrentAttack==3 && (e.CurrentState==BossEncounter.State.Attack ||
                            e.CurrentState==BossEncounter.State.Telegraph && e.AttackElapsed>=e.Definition.laserChargeTime-e.Definition.laserLockTime))Keys(Key.D,Key.LeftShift);
                        else Keys();
                        if(e.Message.StartsWith("缄默环波")){defenseHealth=e.Combat.Health;jumpStart=player.JumpSequence;Step(21);}
                        break;
                    case 21:
                        Aim();
                        var waves=(System.Collections.IList)typeof(BossEncounter).GetField("waves",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
                        if(waves.Count>0)
                        {
                            var w=waves[0];float radius=(float)w.GetType().GetField("radius").GetValue(w);
                            var center=(Vector3)w.GetType().GetField("origin").GetValue(w);center.y=player.transform.position.y;
                            float distance=Vector3.Distance(player.transform.position,center);
                            if(distance-radius<1.65f && player.JumpSequence==jumpStart)Keys(Key.Space);else Keys();
                            if(radius>distance+1){Check(player.JumpSequence>jumpStart && e.Combat.Health==defenseHealth,"Timed Space jump avoids actual expanding wave damage");SaveCapture("wave_jump");Step(22);}
                        }
                        break;
                    case 22:
                        Aim();
                        if(e.Message.StartsWith("封印审判"))
                        {
                            float warningTime=(float)typeof(BossEncounter).GetField("stateTime",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
                            if(warningTime>=1.0f){defenseHealth=e.Combat.Health;SaveCapture("seal_warning");Keys(Key.S,Key.LeftShift);StepWithoutRelease(23);}
                        }
                        break;
                    case 23:
                        if(t<.6f)Keys(Key.S,Key.LeftShift);else Keys();
                        if(t>1.4f){Check(e.Combat.Health==defenseHealth,"Sprint out after target lock avoids seal impact damage");menu.GetComponent<BossEncounter>().SendMessage("OnApplicationFocus",false);pauseClock=e.BattleTime;Step(24);}
                        break;
                    case 24:
                        if(t>.7f){Check(menu.IsOpen && e.BattleTime==pauseClock,"Focus-loss handler opens pause and freezes battle");e.SendMessage("OnApplicationFocus",true);menu.Resume();Check(e.ReturnToLibrary(),"Mid-battle abandon returns safely");cycles=0;Step(25);}break;
                    case 25:
                        if(t>.15f){Check(e.Begin(),"Repeat entry "+(cycles+1));Check(e.ReturnToLibrary(),"Repeat return "+(cycles+1));cycles++;Step(cycles>=5?26:25);}break;
                    case 26:
                        if(t>.3f){Check(UnityEngine.Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None).Length==0 && UnityEngine.Object.FindObjectsByType<BossCombatHUD>(FindObjectsSortMode.None).Length==0,"Five sessions leave no arena or HUD");Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Five sessions keep one EventSystem");Write();Stop();step=100;}break;
                }
            }
            catch(Exception ex){failure=ex.ToString();Write();Stop();step=-1;Debug.LogError("[Boss acceptance] "+ex.Message);}
        }
        static void StepWithoutRelease(int value){step=value;since=Time.time;}
        static void SaveCapture(string name){Directory.CreateDirectory("Docs/BossDesign/Captures");ScreenCapture.CaptureScreenshot("Docs/BossDesign/Captures/"+name+".png");}
        static void Write(){Directory.CreateDirectory("Docs/BossDesign");File.WriteAllText("Docs/BossDesign/"+reportPath,Status);Debug.Log("[Boss acceptance] "+Status);}
    }
}
