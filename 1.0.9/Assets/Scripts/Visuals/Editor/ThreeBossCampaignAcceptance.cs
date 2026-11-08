using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ArcaneArchive.Core;
using ArcaneArchive.Soulbinder;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
 [InitializeOnLoad] public static class ThreeBossCampaignAcceptance
 {
  const string Key="ThreeBossAcceptance";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static Keyboard keyboard;static IEnumerator run;static BossEncounter e;static BookCampaign campaign;static RecoveryMenu menu;static FirstPersonController player;static BossTestInputScope input;
  static readonly List<string> checks=new List<string>(),errors=new List<string>();static double deadline;
  static ThreeBossCampaignAcceptance(){EditorApplication.playModeStateChanged+=Changed;}
  public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(ThreeBossBuilder.Scene);EditorApplication.isPlaying=true;}
  static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false) || state!=PlayModeStateChange.EnteredPlayMode)return;checks.Clear();errors.Clear();deadline=EditorApplication.timeSinceStartup+300;run=Review();Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}
  static void Log(string msg,string stack,LogType type){if((type==LogType.Exception || type==LogType.Error || type==LogType.Assert)&&!stack.Contains("UnityEditor.Search"))errors.Add(msg+"\n"+stack);}
  static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Campaign acceptance timed out");EditorApplication.QueuePlayerLoopUpdate();if(!run.MoveNext())Finish(null);}catch(Exception ex){Finish(ex.GetBaseException().ToString());}}
  static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add("PASS "+text);}
  static object Call(string method,params object[] args)=>typeof(BossEncounter).GetMethod(method,Flags).Invoke(e,args);
  static void Set(string field,object value)=>typeof(BossEncounter).GetField(field,Flags).SetValue(e,value);
  static IEnumerator Frames(int count=2){int start=Time.frameCount;while(Time.frameCount<start+count)yield return null;}
  static Button Button(string text)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.gameObject.activeInHierarchy && b.GetComponentInChildren<TMP_Text>() && b.GetComponentInChildren<TMP_Text>().text==text);
  static IEnumerator Review()
  {
   for(int i=0;i<8;i++)yield return null;
   e=Object.FindFirstObjectByType<BossEncounter>();campaign=e.GetComponent<BookCampaign>();menu=e.GetComponent<RecoveryMenu>();player=e.GetComponent<FirstPersonController>();input=new BossTestInputScope();keyboard=InputSystem.AddDevice<Keyboard>("CampaignAcceptanceKeyboard");
   Check(campaign && campaign.definition.chapters.Length==3,"Exactly three data-driven book chapters");
   Check(menu.CurrentPage==RecoveryMenu.Page.DeepExplore && !e.IsActive,"Startup opens the campaign book without auto-launching knight");
   Check(Button("翻开故事 · 连续挑战三关"),"Book exposes continuous story start");
   foreach(var chapter in campaign.definition.chapters)Check(Button(chapter.title),"Book exposes single challenge: "+chapter.title);
   Check(!campaign.StartChapter(-1,true) && !campaign.StartChapter(3,true),"Invalid chapter indices rejected");
   CaptureUI("BookUI");Button("翻开故事 · 连续挑战三关").onClick.Invoke();CaptureUI("StoryUI");
   Check(campaign.ShowingStory && !e.IsActive,"Story is shown before first combat");menu.HandleEscape();Check(campaign.ShowingStory && menu.CurrentPage==RecoveryMenu.Page.CampaignStory,"Escape cannot bypass pending story");
   Check(!campaign.StartChapter(2,false),"Cannot replace a chapter while its introduction is open");
   for(int chapter=0;chapter<3;chapter++)
   {
    Check(campaign.ChapterIndex==chapter,"Sequential order chapter "+chapter);
    Check(campaign.ContinueStory(),"Introduction launches actual encounter "+chapter);
    if(chapter==0 && !string.IsNullOrEmpty(campaign.Chapter.externalScene)){var soul=ReviewSoulbinder();while(soul.MoveNext())yield return null;continue;}
    var frames=Frames();while(frames.MoveNext())yield return null;
    // Exercise real Update, controller input and enemy decisions before controlled combat assertions.
    e.enabled=true;player.enabled=true;Set("focusLost",false);menu.Resume();
    var released=Frames(6);while(released.MoveNext())yield return null;
    Vector3 walkStart=player.transform.position;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W));InputSystem.Update();
    double walkUntil=EditorApplication.timeSinceStartup+1.2;
    while(EditorApplication.timeSinceStartup<walkUntil){Set("focusLost",false);InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.W));yield return null;}
    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
    checks.Add("MOVE_DIAGNOSTIC pos="+player.transform.position+" start="+walkStart+" dt="+Time.deltaTime+" input="+((InputAction)typeof(FirstPersonController).GetField("moveAction",Flags).GetValue(player)).ReadValue<Vector2>()+" enabled="+player.GameplayInputEnabled+" locked="+player.CursorIsLocked+" paused="+player.SimulationPaused+" release="+typeof(FirstPersonController).GetField("waitForInputRelease",Flags).GetValue(player)+" page="+menu.CurrentPage);
    Check(Vector3.Distance(walkStart,player.transform.position)>.5f,"Real WASD moves character with collision "+chapter);
    double aiUntil=EditorApplication.timeSinceStartup+10;bool warned=false,attacked=false;float startHealth=e.Combat.Health;
    while(EditorApplication.timeSinceStartup<aiUntil && !e.IsFinished){Set("focusLost",false);warned|=e.CurrentState==BossEncounter.State.Telegraph;attacked|=e.CurrentState==BossEncounter.State.Attack;yield return null;}
    Check(warned && attacked,"Unmodified enemy AI telegraphs and attacks "+chapter);
    Check(e.BossDistanceTravelled>0 || chapter==2,"Enemy actively repositions "+chapter);
    checks.Add("LIVE AI chapter "+chapter+" player health "+startHealth+" -> "+e.Combat.Health);
    e.enabled=false;player.enabled=false;Set("focusLost",false);if(e.IsFinished)e.Retry();menu.Resume();
    Check(e.IsActive && !e.IsFinished,"Live encounter active "+chapter);
    Check(e.KnightMode==(chapter==2),"Correct combat type "+chapter);
    Check(e.Definition.displayName.Contains(chapter==0?"缚魂":chapter==1?"典狱":"骑士"),"Correct boss identity "+chapter);
    Check(Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None).Length==1,"Only one active arena "+chapter);
    Check(Object.FindObjectsByType<BossCombatHUD>(FindObjectsSortMode.None).Length==1,"Only one combat HUD "+chapter);
    Check(!campaign.AdvanceAfterVictory(),"Cannot advance while battle unfinished "+chapter);
    Check(Physics.Raycast(player.transform.position+Vector3.up,Vector3.down,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Player spawn has walkable support "+chapter);
    Call("SetState",BossEncounter.State.Observe);
    e.Combat.Tick(2);e.Combat.TakeDamage(100000);Check(e.CurrentState==BossEncounter.State.Lost,"Player damage causes loss "+chapter);
    Check(!campaign.AdvanceAfterVictory(),"Loss cannot advance chapter "+chapter);
    Check(!Button("进入下一关"),"Loss has no next-level button "+chapter);
    Button("重试本关").onClick.Invoke();
    Check(e.CurrentState==BossEncounter.State.Intro && e.Combat.Health==e.Definition.playerHealth && e.BossHealth==e.Definition.health,"Retry restores player and boss health "+chapter);
    Check(campaign.ChapterIndex==chapter && !campaign.ShowingStory,"Retry stays in current chapter without repeating story "+chapter);
    Call("SetState",BossEncounter.State.Observe);Set("focusLost",false);
    Capture("Chapter"+(chapter+1),false);Capture("Boss"+(chapter+1),true);
    var melee=e.GetComponent<PlayerMelee>();int hits=0;bool phase2=false;
    while(!e.IsFinished && hits<100)
    {
     Call("ClearAttacks");Call("SetState",BossEncounter.State.Observe);Set("focusLost",false);
     e.Arena.boss.localPosition=Vector3.zero;e.Arena.boss.localRotation=Quaternion.identity;
     player.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,-2));
     e.View.transform.rotation=Quaternion.LookRotation(e.Arena.boss.position+Vector3.up*1.3f-e.View.transform.position);Physics.SyncTransforms();
     melee.RestoreStamina(1000);float before=e.BossHealth;
     Check(melee.TryLightAttack(),"Player can begin melee "+chapter+"/"+hits);
     for(int frame=0;frame<90 && !e.IsFinished;frame++)e.Combat.Tick(1/30f);
     Check(e.BossHealth<before,"Actual melee collision damages boss "+chapter+"/"+hits);
     phase2|=e.Phase==2;hits++;
     if(hits%8==0){frames=Frames();while(frames.MoveNext())yield return null;}
    }
    Check(phase2,"Melee crosses phase threshold "+chapter);
    Check(e.CurrentState==BossEncounter.State.Won && e.BossHealth==0,"Real repeated melee reaches victory "+chapter);
    Check(menu.CurrentPage==RecoveryMenu.Page.BattleResult && !player.GameplayInputEnabled,"Victory freezes input and opens result "+chapter);
    CaptureUI("Victory"+chapter);var next=Button(chapter==2?"完成故事":"进入下一关");Check(next,"Victory exposes progression button "+chapter);next.onClick.Invoke();
    Check(campaign.ShowingStory && !e.IsActive && e.ActiveAttackCount==0,"Victory progression cleans combat and opens aftermath "+chapter+" story="+campaign.ShowingStory+" active="+e.IsActive+" attacks="+e.ActiveAttackCount+" message="+e.Message+" position="+e.transform.position);
    Check(!campaign.AdvanceAfterVictory(),"Repeated progression cannot double-advance "+chapter);
    frames=Frames();while(frames.MoveNext())yield return null;
    Check(Object.FindObjectsByType<BossArena>(FindObjectsSortMode.None).Length==0 && Object.FindObjectsByType<BossCombatHUD>(FindObjectsSortMode.None).Length==0,"Old arena and HUD destroyed "+chapter);
    Check(campaign.ContinueStory(),"Aftermath advances story "+chapter);
    frames=Frames();while(frames.MoveNext())yield return null;
   }
   Check(campaign.Complete && campaign.Victories==3 && campaign.ShowingStory,"Three actual victories reach final story");
   Check(campaign.StoryText.Contains("三章通关"),"Ending contains completion and evacuation resolution");
   CaptureUI("EndingUI");campaign.ContinueStory();Check(menu.CurrentPage==RecoveryMenu.Page.DeepExplore && !e.IsActive,"Ending returns to replayable book");
   Check(campaign.StartChapter(1,false),"Single-chapter selection works after full completion");
   var wait=Frames();while(wait.MoveNext())yield return null;Check(campaign.ContinueStory(),"Selected Warden chapter starts");
   wait=Frames();while(wait.MoveNext())yield return null;e.enabled=false;Set("focusLost",false);Call("SetState",BossEncounter.State.Observe);e.DamageBoss(100000,false);
   Check(campaign.AdvanceAfterVictory(),"Single chapter has aftermath");wait=Frames();while(wait.MoveNext())yield return null;campaign.ContinueStory();
   Check(!campaign.Complete && menu.CurrentPage==RecoveryMenu.Page.DeepExplore,"Single victory does not claim full-game completion");
   Check(campaign.StartChapter(0,true) && campaign.Victories==0,"New campaign resets progress");campaign.ReturnToBook();
   Check(errors.Count==0,"No runtime error / exception during the full campaign");
  }
  static IEnumerator ReviewSoulbinder(){
   SoulbinderLevel level=null;
   while(!level||!level.Started||SoulbinderCampaignBridge.Transitioning){level=Object.FindFirstObjectByType<SoulbinderLevel>();yield return null;}
   level.Automated=true;level.SaveOverride=Path.GetFullPath("Docs/Release109/test-soulbinder-save.json");
   Check(!player.gameObject.activeInHierarchy,"Original player suspended during additive first chapter");
   Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1,"One active audio listener in new hall");
   Check(!SoulbinderCampaignBridge.Return(true),"Unfinished first chapter cannot advance");
   for(int i=0;i<200&&level.Model.Phase==EncounterPhase.Intro;i++)level.Step(.02f,false);
   Check(level.Actors.All(x=>x),"All five minion types spawn in integrated chapter");
   level.Model.Damage(0,1000);level.Step(.02f,false);
   Check(level.Model.Phase==EncounterPhase.Lost&&!SoulbinderCampaignBridge.Return(true),"First chapter loss cannot advance");
   Check(level.Begin(true),"First chapter retry succeeds");for(int i=0;i<100;i++)level.Step(.02f,false);
   Check(level.Actors.All(x=>x)&&level.Model.PlayerHealth==100,"Retry restores legion and player");
   for(int i=0;i<3;i++)level.HitTarget(10+i,1000,true,level.player.transform.position);
   for(int i=0;i<5;i++)level.HitTarget(i,1000,true,level.player.transform.position);
   level.Step(.02f,false);level.player.enabled=false;
   for(int shot=0;shot<120&&level.Model.Phase==EncounterPhase.Combat;shot++){
    level.player.TryReturnToSpawn(level.boss.position+new Vector3(0,.2f,-2.5f),Quaternion.identity);
    level.view.transform.rotation=Quaternion.LookRotation(level.boss.position+Vector3.up*1.8f-level.view.transform.position);Physics.SyncTransforms();
    level.Fire();for(int f=0;f<17;f++)level.Step(.02f,false);
    if(shot%8==0)yield return null;
   }
   Check(level.Model.Phase==EncounterPhase.Won,"Actual projectile ray hits defeat integrated first boss");
   level.OpenConsole();level.Choose("Contain");level.ExitDemo();
   while(SoulbinderCampaignBridge.Active)yield return null;
   Check(player.gameObject.activeInHierarchy&&campaign.Victories==1&&campaign.ShowingStory,"Console decision restores original player and first aftermath");
   Check(!Object.FindFirstObjectByType<SoulbinderLevel>(),"New arena unloaded after victory");
   var frames=Frames();while(frames.MoveNext())yield return null;
   Check(campaign.ContinueStory(),"First aftermath advances to second chapter");frames=Frames();while(frames.MoveNext())yield return null;
  }
  static void CaptureUI(string name)
  {
   var go=new GameObject("UI acceptance camera");var c=go.AddComponent<Camera>();c.CopyFrom(e.View);c.transform.SetPositionAndRotation(e.View.transform.position,e.View.transform.rotation);
   var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x=>x.isRootCanvas && x.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=c;canvas.planeDistance=1;}
   Canvas.ForceUpdateCanvases();var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
   var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Docs/ThreeBoss/"+name+".png",tex.EncodeToPNG());
   foreach(var canvas in canvases){canvas.worldCamera=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;}
   c.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);
  }
  static void Capture(string name,bool close)
  {
   var go=new GameObject("Acceptance camera");var c=go.AddComponent<Camera>();c.CopyFrom(e.View);
   c.transform.position=close?e.Arena.boss.position+e.Arena.boss.forward*5.8f+e.Arena.boss.right*3+Vector3.up*2.6f:e.Arena.spawn.position+new Vector3(0,2.8f,1);
   c.transform.LookAt(e.Arena.boss.position+Vector3.up*1.5f);c.fieldOfView=close?42:72;
   var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
   var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Docs/ThreeBoss/"+name+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);
  }
  static void Finish(string failure)
  {
   if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}input?.Dispose();Application.logMessageReceived-=Log;EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
   if(errors.Count>0 && failure==null)failure="Runtime error log is nonempty";
   File.WriteAllText("Docs/Release109/Acceptance.txt","Play Mode: real WASD input and autonomous AI smoke checks, followed by controlled repeated-melee and campaign flow checks. Not a first-time human playthrough or Windows native runtime check.\n"+string.Join("\n",checks)+"\nERRORS\n"+string.Join("\n",errors)+"\nFailure: "+(failure??"none"));
   Debug.Log("THREE_BOSS_ACCEPTANCE "+(failure??"PASS"));EditorApplication.Exit(failure==null?0:1);
  }
 }
}
