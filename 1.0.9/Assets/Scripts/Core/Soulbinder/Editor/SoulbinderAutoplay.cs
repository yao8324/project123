#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ArcaneArchive.Soulbinder.Editor {
 [InitializeOnLoad] public static class SoulbinderAutoplay {
  static SoulbinderAutoplay(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("SoulbinderAutoplay",false)){SessionState.SetBool("SoulbinderAutoplay",false);new GameObject("Actual shot playthrough").AddComponent<SoulbinderBot>();}};}
  public static void Run(){SessionState.SetBool("SoulbinderAutoplay",true);EditorSceneManager.OpenScene(SoulbinderSceneBuilder.ScenePath);EditorApplication.EnterPlaymode();}
 }
 public sealed class SoulbinderBot:MonoBehaviour {
  SoulbinderLevel level;string report="Docs/SoulbinderCombat20261108/Implementation";
  readonly Vector3[] route={new Vector3(7,0,-7),new Vector3(8,0,3),new Vector3(4,0,9.5f),new Vector3(-4,0,9.5f),new Vector3(-8,0,3),new Vector3(-7,0,-7),new Vector3(0,0,-10)};
  IEnumerator Start(){yield return null;level=FindFirstObjectByType<SoulbinderLevel>();level.Automated=true;level.SaveOverride=Path.GetFullPath(report+"/autoplay-checkpoint.json");Time.timeScale=3;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
   for(int run=0;run<2;run++){
    if(!level.Begin(true)){Fail("Begin failed");yield break;}level.player.enabled=false;var cc=level.player.GetComponent<CharacterController>();int goal=0,shots=0;float fireAt=0,t=0;var path=new NavMeshPath();
    while(t<180&&level.Model.Phase!=EncounterPhase.Won&&level.Model.Phase!=EncounterPhase.Lost){
     float dt=Time.deltaTime;t+=dt;var pos=level.player.transform.position;
     if(level.Model.Phase==EncounterPhase.Combat){
      Vector3 destination=route[goal];if(Vector3.Distance(new Vector3(pos.x,0,pos.z),destination)<1)goal=(goal+1)%route.Length;
      Vector3 next=destination;if(NavMesh.CalculatePath(pos,destination,NavMesh.AllAreas,path)&&path.corners.Length>1)next=path.corners[1];var move=next-pos;move.y=0;cc.Move(move.normalized*5.5f*dt+Vector3.down*3*dt);
      Transform target=level.boss;Vector3 aim=target.position+Vector3.up*1.6f;
      if(run==1){
       for(int i=0;i<3;i++)if(level.Model.Anchors[i]&&Visible(level.anchorLocks[i].position,10+i)){target=level.anchorLocks[i];aim=target.position;break;}
       bool allClosed=!level.Model.Anchors[0]&&!level.Model.Anchors[1]&&!level.Model.Anchors[2];
       if(allClosed)for(int i=0;i<5;i++)if(level.Actors[i]&&level.Model.Slots[i]==SoulSlot.Alive&&Visible(level.Actors[i].transform.position+Vector3.up*1.1f,i)){target=level.Actors[i].transform;aim=target.position+Vector3.up*1.1f;break;}
      }
      level.view.transform.LookAt(aim);Physics.SyncTransforms();if(t>=fireAt){level.Fire();fireAt=t+.32f;shots++;}
     }
     level.Step(dt,false);yield return null;
    }
    if(level.Model.Phase!=EncounterPhase.Won){Fail("Route "+run+" ended "+level.Model.Phase+" player="+level.Model.PlayerHealth+" boss="+level.Model.BossHealth+" time="+t);yield break;}
    if(run==1&&(level.Model.Anchors[0]||level.Model.Anchors[1]||level.Model.Anchors[2])){Fail("Anchor route won without closing all sources");yield break;}
    File.AppendAllText(report+"/autoplay-results.txt",(run==0?"Boss rush":"Close anchors + legion")+": WON; actual physics shots="+shots+"; game seconds="+t.ToString("F1")+"; HP="+level.Model.PlayerHealth+"; absorption=0; direct damage injection=0\n");
    level.OpenConsole();level.Choose(run==0?"Contract":"Contain");level.ExitDemo();if(!level.Completed){Fail("Exit blocked");yield break;}yield return null;
   }
   Time.timeScale=1;Debug.Log("SOULBINDER_AUTOPLAY_PASS two actual-shot routes");EditorApplication.Exit(0);
  }
  bool Visible(Vector3 point,int kind){var delta=point-level.view.transform.position;return Physics.Raycast(level.view.transform.position,delta.normalized,out var hit,delta.magnitude+.5f,~0,QueryTriggerInteraction.Collide)&&hit.collider.GetComponent<SoulbinderTarget>()?.kind==kind;}
  void Fail(string error){Time.timeScale=1;File.WriteAllText(report+"/autoplay-failure.txt",error);Debug.LogError(error);EditorApplication.Exit(1);}
 }
}
#endif
