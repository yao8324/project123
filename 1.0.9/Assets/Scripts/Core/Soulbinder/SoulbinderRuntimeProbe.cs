using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace ArcaneArchive.Soulbinder {
 public sealed class SoulbinderRuntimeProbe:MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--soulbinder-test")>=0)new GameObject("Runtime acceptance").AddComponent<SoulbinderRuntimeProbe>();}
  int assertions;SoulbinderLevel level;string report;bool failed;
  void Check(bool ok,string message){assertions++;if(!ok)throw new Exception("Runtime assertion: "+message);}
  void Advance(float sec){for(int i=0;i<Mathf.CeilToInt(sec/.02f);i++)level.Step(.02f,false);}
  void Damage(int target,float amount,bool powered=false){level.HitTarget(target,amount,powered,level.player.transform.position);level.Step(.001f,false);}
  void Capture(string name,Vector3 pos,Vector3 look){var oldPos=level.view.transform.position;var oldRot=level.view.transform.rotation;level.view.transform.position=pos;level.view.transform.LookAt(look);var rt=new RenderTexture(1440,900,24);level.view.targetTexture=rt;level.view.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(report,name+".png"),tex.EncodeToPNG());RenderTexture.active=prev;level.view.targetTexture=null;Destroy(rt);Destroy(tex);level.view.transform.position=oldPos;level.view.transform.rotation=oldRot;}
  IEnumerator Start(){
   report=Path.GetFullPath("Docs/SoulbinderCombat20261108/Implementation");Directory.CreateDirectory(report);Application.logMessageReceived+=Log;yield return null;
   level=FindFirstObjectByType<SoulbinderLevel>();
   try {
    Check(level,"Scene has encounter");level.Automated=true;level.SaveOverride=Path.Combine(report,"test-checkpoint.json");if(File.Exists(level.SaveOverride))File.Delete(level.SaveOverride);
    Check(level.Preflight(out string e),"Preflight "+e);Check(level.Begin(),"Begin works");
    var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.transform.position=SoulbinderLevel.SpawnPoints[0]+Vector3.up*1.5f;obstruction.transform.localScale=new Vector3(8,3,8);Physics.SyncTransforms();Advance(2.02f);Check(level.Model.Phase==EncounterPhase.Ready&&!level.Started&&level.Model.Occupied==0,"Blocked intro rolls back entire batch");DestroyImmediate(obstruction);Physics.SyncTransforms();Check(level.Begin(),"Begin after obstruction removed");level.player.enabled=false;
    Advance(2.02f);Check(level.Model.Occupied==5,"All five reserved atomically");for(int i=0;i<5;i++)Check(level.Actors[i]!=null&&level.Actors[i].agent.isOnNavMesh,"Actor on navmesh "+i);
    var wraith=level.Actors[4];var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=wraith.transform.position+Vector3.forward+Vector3.up;wall.transform.localScale=new Vector3(3,2,.2f);Physics.SyncTransforms();Check(!wraith.CanDashStep(Vector3.forward*2),"Wraith stops at actual unbaked collider");DestroyImmediate(wall);
    Check(level.boss.GetComponent<SoulbinderTarget>()?.kind==-1,"Boss target serialized");for(int a=0;a<3;a++)Check(level.anchorLocks[a].GetComponent<SoulbinderTarget>()?.kind==10+a,"Anchor target serialized");
    Advance(1.6f);Check(level.Model.Phase==EncounterPhase.Combat,"Combat starts");Physics.SyncTransforms();
    Capture("Game_Entry",new Vector3(0,1.8f,-11.5f),new Vector3(0,2,5));Capture("Game_Overview",new Vector3(-9,6,-13),new Vector3(0,1.7f,4));Capture("Game_Console",new Vector3(3,2.8f,9.5f),new Vector3(0,1.4f,12.5f));
    // Exercise actual physics ray, not only direct model damage.
    level.player.TryReturnToSpawn(new Vector3(0,.1f,1),Quaternion.identity);level.view.transform.LookAt(level.boss.position+Vector3.up*1.6f);Physics.SyncTransforms();float hp=level.Model.BossHealth;level.Fire();level.Step(.02f,false);Check(level.Model.BossHealth<hp,"Player shot hits boss through scene geometry");
    Damage(1,80,true);Check(level.Actors[1].Health<=20,"Empowered shield break ignores frontal reduction");
    Check(level.TryAbsorb(),"Absorb starts");level.ShootArrow(level.view.transform.position+level.view.transform.forward*1.5f,-level.view.transform.forward);level.Step(.03f,false);Check(level.Power==25,"Soul arrow absorbed for 25 energy");Check(!level.TryAbsorb(),"Absorb cooldown enforced");
    // The five actors must produce damage without player assistance.
    Advance(8);Check(level.Model.PlayerHealth<100,"Enemy AI deals damage");
    level.TogglePause();float before=level.Model.Time;Vector3 old=level.Actors[0].transform.position;Advance(5);Check(level.Model.Time==before&&level.Actors[0].agent.isStopped,"Pause freezes AI and clocks");level.TogglePause();level.player.enabled=false;
    // Retry reset, source closure, actor cleanup and victory.
    Check(level.Begin(true),"Retry");level.player.enabled=false;Advance(1.6f);
    for(int i=0;i<3;i++)Damage(10+i,240);
    Check(!level.Model.Anchors[0]&&!level.Model.Anchors[1]&&!level.Model.Anchors[2],"Three controls close");
    for(int i=0;i<5;i++)Damage(i,1000,true);Advance(.1f);Check(level.Model.Occupied==0,"Minion death releases slots");Advance(30);Check(level.Model.Occupied==0,"No refill from closed anchors");
    Damage(-1,1800);Check(level.Model.Phase==EncounterPhase.Won,"Victory reachable without absorption");Check(level.Record.decision=="Pending"&&File.Exists(level.SaveOverride),"Pending saved immediately");Check(!level.stairBarriers[0].activeSelf,"Gallery released after win");
    level.OpenConsole();level.Choose("Contract");Check(SoulbinderCheckpoint.Read(level.SaveOverride,out var record,out _)&&record.assistTokens==1,"Contract atomically saved");level.Choose("Contract");Check(level.Record.assistTokens==1,"Repeated choice cannot farm tokens");level.ReturnToHall();level.ContinueSaved();Check(level.Model.Phase==EncounterPhase.Won,"Saved reload cannot restart enemies");level.OpenConsole();level.Choose("Contain");Check(level.Record.assistTokens==0,"Release contract removes token");level.ExitDemo();Check(level.Completed,"Exit reaches completion screen");
    // Intentionally unwritable path (a file used as parent), not permissions-dependent.
    level.ContinueSaved();string blocked=Path.Combine(report,"save-blocker");File.WriteAllText(blocked,"block");level.SaveOverride=Path.Combine(blocked,"save.json");level.Choose("Contract");Check(level.SaveError!=null,"Save failure visible");level.OpenConsole();level.ExitDemo();Check(level.ConsoleOpen,"Cannot silently exit without failed-save choice");level.AllowUnsaved();level.ExitDemo();Check(level.Completed&&!level.ConsoleOpen,"Explicit unsaved exit works");
    for(int i=0;i<10;i++){Check(level.Begin(true),"Retry "+i);level.player.enabled=false;Advance(1.6f);Check(level.Model.Occupied==5&&level.Model.PlayerHealth==100,"Clean retry state "+i);level.Model.Damage(0,100);level.Step(.01f,false);Check(level.Model.Phase==EncounterPhase.Lost,"Loss "+i);}
    Check(level.Begin(true),"Magic test restart");level.player.enabled=false;Advance(1.6f);for(int i=0;i<3;i++)Damage(10+i,240);for(int i=0;i<5;i++)Damage(i,1000,true);Advance(.05f);
    level.player.TryReturnToSpawn(new Vector3(0,.1f,-5),Quaternion.identity);Physics.SyncTransforms();
    for(int i=0;i<600&&!level.MagicCharging;i++)level.Step(.02f,false);Check(level.MagicCharging&&level.MagicBoltsFired==0,"Boss warns before firing");
    level.TogglePause();Advance(3);Check(level.MagicCharging&&level.MagicBoltsFired==0,"Magic windup freezes during pause");level.TogglePause();level.player.enabled=false;
    Advance(1.3f);Check(level.MagicVolleyCount==1&&level.MagicBoltsFired==3,"One magic cast fires three same-frame bolts");Check(!level.Acquire(3,true),"Magic projectiles retain heavy-threat token");Advance(.25f);Capture("Game_BossMagic_V3",new Vector3(0,2,-7),level.boss.position+Vector3.up*1.8f);
    Advance(2);Check(level.Model.PlayerHealth<100,"Boss magic actually damages player");Advance(5);Check(level.ActiveMagicBolts==0,"Magic expires or hits geometry");
    level.view.transform.LookAt(level.boss.position+Vector3.up*2);Check(level.TryAbsorb(),"Boss bolt absorb starts");float power=level.Power;level.ShootArrow(level.view.transform.position+level.view.transform.forward*1.5f,-level.view.transform.forward,true);level.Step(.03f,false);Check(level.Power==power+25,"Boss magic can be absorbed");
    Check(level.Begin(true),"Retry after magic");Check(level.ActiveMagicBolts==0&&level.MagicVolleyCount==0,"Retry clears magic projectiles and counters");
    level.player.enabled=false;Advance(1.6f);for(int i=0;i<3;i++)Damage(10+i,240);for(int i=0;i<5;i++)Damage(i,1000,true);for(int i=0;i<600&&!level.MagicCharging;i++)level.Step(.02f,false);Check(level.MagicCharging,"Victory cancellation test reaches magic windup");Damage(-1,1800);Check(level.Model.Phase==EncounterPhase.Won&&!level.MagicCharging&&level.ActiveMagicBolts==0,"Victory cancels magic immediately");
    File.WriteAllText(Path.Combine(report,"runtime-tests.txt"),assertions+" runtime assertions passed\nPhysics ray, nav placement, AI damage, pause, ten retries, no-absorb win, save success/failure, pending reload, contract, exit.\n");Debug.Log("SOULBINDER_RUNTIME_PASS "+assertions);
   }catch(Exception ex){failed=true;Debug.LogException(ex);File.WriteAllText(Path.Combine(report,"runtime-failure.txt"),ex.ToString());}
   yield return null;
   Application.logMessageReceived-=Log;
#if UNITY_EDITOR
   UnityEditor.EditorApplication.Exit(failed?1:0);
#else
   Application.Quit(failed?1:0);
#endif
  }
  void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){failed=true;File.AppendAllText(Path.Combine(report,"runtime-errors.txt"),message+"\n"+stack+"\n");}}
 }
}
