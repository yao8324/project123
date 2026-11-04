using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ArcaneArchive.Core;
namespace ArcaneArchive.Visuals.Editor {
 public static class SealedDuelArenaAcceptance {
  public const string ArenaPath="Assets/Art/SealedDuelArena20261104/Prefabs/SealedDuelArena.prefab";
  public const string ScenePath="Assets/Scenes/SealedDuelArena_Showcase.unity";
  public const string Output="Docs/SealedDuelArena";
  [Serializable] class Report { public bool complete;public string failure,unity;public string[] checks;public int floorSamples,clearSamples,boundarySamples; }
  static List<string> checks=new List<string>();
  static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks.Add(message);}
  static string VisibleMaterial(GameObject root,Ray ray){
   float nearest=float.PositiveInfinity;string result="none";
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){
    var renderer=mf.GetComponent<Renderer>();if(!renderer || !renderer.bounds.IntersectRay(ray))continue;
    var mesh=mf.sharedMesh;var verts=mesh.vertices;var indices=mesh.triangles;var matrix=mf.transform.localToWorldMatrix;
    for(int i=0;i<indices.Length;i+=3){
     Vector3 a=matrix.MultiplyPoint3x4(verts[indices[i]]),b=matrix.MultiplyPoint3x4(verts[indices[i+1]]),c=matrix.MultiplyPoint3x4(verts[indices[i+2]]);
     var e1=b-a;var e2=c-a;var h=Vector3.Cross(ray.direction,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<.0000001f)continue;
     float inv=1/det;var delta=ray.origin-a;float u=Vector3.Dot(delta,h)*inv;if(u<0||u>1)continue;var q=Vector3.Cross(delta,e1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0||u+v>1)continue;
     float t=Vector3.Dot(e2,q)*inv;if(t>0&&t<nearest){nearest=t;result=renderer.sharedMaterial.name;}
    }
   }return result;
  }
  static void Outward(Mesh mesh,string label){
   var v=mesh.vertices;var t=mesh.triangles;int bad=0;
   for(int i=0;i<t.Length;i+=3){var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];var normal=Vector3.Cross(b-a,c-a);if(normal.sqrMagnitude>1e-12f && Vector3.Dot(normal,(a+b+c)/3)<-1e-7f)bad++;}
   Require(bad==0,label+" has outward-facing surfaces (inward triangles="+bad+")");
  }
  public static void RunGeometry(){
   checks.Clear();var errors=new List<string>();
   var box=SealedDuelArenaGeometry.Box(new Vector3(2,4,1),.04f);
   var stroke=SealedDuelArenaGeometry.Stroke(new[]{Vector3.down*2,Vector3.up*2},.3f,.4f);
   var tube=SealedDuelArenaGeometry.Tube(new[]{Vector3.down*2,Vector3.up*2},.2f,12);
   foreach(var item in new[]{box,stroke,tube})try{Outward(item,item.name);}catch(Exception e){errors.Add(e.Message);}
   UnityEngine.Object.DestroyImmediate(stroke);UnityEngine.Object.DestroyImmediate(tube);SealedDuelArenaGeometry.ClearCache();
   if(errors.Count>0)throw new Exception(string.Join("; ",errors));Debug.Log("SEALED_GEOMETRY_NORMALS_PASS");
  }
  public static void TestDirtySceneProtection(){
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var sentinel=new GameObject("Unsaved arena authoring sentinel");EditorSceneManager.MarkSceneDirty(scene);
   bool rejected=false;try{RunStatic();}catch(InvalidOperationException e){rejected=e.Message.Contains("unsaved");}
   bool intact=sentinel && scene.IsValid() && scene.isDirty;
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   if(!rejected || !intact)throw new Exception("Dirty scene protection failed: validation must reject before discarding unsaved work");
   Debug.Log("DIRTY_SCENE_PROTECTION_PASS");
  }
  public static void GuardSceneWork(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before changing review scenes");
   for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save unsaved scenes before arena validation or review");
  }
  public static void RunStatic(){
   GuardSceneWork();var priorSetup=EditorSceneManager.GetSceneManagerSetup();
   checks.Clear();var report=new Report{unity=Application.unityVersion};GameObject instance=null;
   try {
    RunGeometry();
    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPath);Require(asset,"Sealed boss arena Prefab exists");
    var a=asset.GetComponent<BossArena>();Require(a && a.spawn && a.boss && a.model && a.effectRoot,"Original Boss rig, spawn and effects root preserved");
    Require(a.pageProjectile && a.ringEffect && a.boltEffect && a.impactEffect && a.laserEffect && a.fractureEffect && a.rushEffect && a.volleyEffect && a.rushWakeEffect && a.pageImpactEffect,"All ten combat effect references preserved");
    Require(Mathf.Abs(a.radius-14)<.001f,"Original combat radius remains 14 metres");
    Require(a.model.animatedRig && a.model.animatedRig.clips.Length==17,"Animated Boss retains its seventeen clips");
    EditorSceneManager.OpenScene(ScenePath);
    var controller=UnityEngine.Object.FindFirstObjectByType<ArcaneArchive.Player.FirstPersonController>().GetComponent<CharacterController>();
    float capsuleRadius=controller.radius,halfSegment=controller.height*.5f-capsuleRadius;
    Vector3 lower=controller.center-Vector3.up*halfSegment+Vector3.up*.02f,upper=controller.center+Vector3.up*halfSegment+Vector3.up*.02f;
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    instance=UnityEngine.Object.Instantiate(asset);var arena=instance.GetComponent<BossArena>();Physics.SyncTransforms();
    Require(VisibleMaterial(instance,new Ray(new Vector3(.8f,2.4f,0),Vector3.back)).Contains("Recessed_DarkStone"),"Recessed arch faces remain visible in front of the backing masonry");
    for(float x=-14;x<=14;x+=.5f)for(float z=-14;z<=14;z+=.5f){
     if(x*x+z*z>196)continue;var p=new Vector3(x,0,z);
     var overlaps=Physics.OverlapCapsule(p+lower,p+upper,capsuleRadius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(arena.boss)).ToArray();
     if(overlaps.Length>0)throw new Exception("Central fighting capsule blocked at "+p+": "+string.Join(",",overlaps.Select(c=>c.name)));report.clearSamples++;
     var floor=Physics.RaycastAll(p+Vector3.up*.5f,Vector3.down,1,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(arena.boss)).OrderBy(h=>h.distance).FirstOrDefault();
     if(!floor.collider || floor.normal.y<.99f || Mathf.Abs(floor.point.y)>.025f)throw new Exception("Floor is not flat and supported at "+p);report.floorSamples++;
    }
    checks.Add("Full combat radius 14m is flat, supported and clear for the player capsule ("+report.clearSamples+" samples)");
    for(int i=0;i<72;i++){
     float angle=i*5*Mathf.Deg2Rad;Vector3 dir=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
     var hits=Physics.RaycastAll(Vector3.up*.9f,dir,18,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(arena.boss)).OrderBy(h=>h.distance).ToArray();
     if(hits.Length==0 || hits[0].distance<14.1f)throw new Exception("Boundary missing or blocks combat radius at degrees "+i*5);report.boundarySamples++;
    }
    checks.Add("Closed perimeter without holes or obstructions inside combat radius (72 directions)");
    for(int i=0;i<72;i++){
     float angle=i*5*Mathf.Deg2Rad;var direction=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));var start=-direction*14;
     var blocked=Physics.CapsuleCastAll(start+lower,start+upper,capsuleRadius,direction,28,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(arena.boss)).ToArray();
     Require(blocked.Length==0,"Full-diameter player capsule sweep clear at "+(i*5)+" degrees");
    }

    var spawn=arena.spawn.position;Require(Physics.Raycast(spawn+Vector3.up,Vector3.down,2),"Player spawn is supported by the arena floor");
    Require(!Physics.OverlapCapsule(spawn+Vector3.up*.39f,spawn+Vector3.up*1.46f,.30f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(arena.boss)),"Player spawn capsule is clear");
    Require(instance.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.max.y>=12),"Full-height vault geometry exists above the arena");
    UnityEngine.Object.DestroyImmediate(instance);instance=null;
    Require(File.Exists(ScenePath),"Direct combat showcase scene exists");
    foreach(var path in new[]{"Assets/Scenes/MilanLibrary_Magic_V6.unity",ScenePath}){
     EditorSceneManager.OpenScene(path);var encounter=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();
     Require(encounter && AssetDatabase.GetAssetPath(encounter.arenaPrefab)==ArenaPath,"Playable scene connects to the new battle arena: "+path);
    }
    report.complete=true;Debug.Log("SEALED_ARENA_STATIC_PASS "+checks.Count);
   }catch(Exception ex){report.failure=ex.ToString();throw;}
   finally {if(instance)UnityEngine.Object.DestroyImmediate(instance);Directory.CreateDirectory(Output);report.checks=checks.ToArray();File.WriteAllText(Output+"/StaticChecks.json",JsonUtility.ToJson(report,true));if(priorSetup.Length>0 && priorSetup.All(x=>!string.IsNullOrEmpty(x.path)))EditorSceneManager.RestoreSceneManagerSetup(priorSetup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
