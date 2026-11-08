#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ArcaneArchive.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Soulbinder.Editor {
 public static class SoulbinderSceneBuilder {
  public const string Root="Assets/Art/SoulbinderRuntime", ScenePath="Assets/Scenes/SoulbinderComplete.unity";
  const string Report="Docs/SoulbinderCombat20261108/Implementation";
  [Serializable]class MaterialList {public MaterialInfo[] materials;}
  [Serializable]class MaterialInfo {public string name;public float[] color;public float metallic,roughness,emission;}
  static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
  static Material Make(string name,Color color,float metallic=0,float smooth=.3f,float emission=0){
   string path=Root+"/Materials/"+name.Replace('/','_')+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",Mathf.Min(.65f,metallic));mat.SetFloat("_Smoothness",smooth);
   if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}else {mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}EditorUtility.SetDirty(mat);return mat;
  }
  static void PrepareMaterials(string model){
   var json=JsonUtility.FromJson<MaterialList>(File.ReadAllText(Root+"/"+model+".json"));
   foreach(var m in json.materials){string key=model+"_"+m.name;var c=new Color(m.color[0],m.color[1],m.color[2],1);string lower=m.name.ToLowerInvariant();bool glow=lower.Contains("soul")||lower.Contains("rune")||lower.Contains("eye")||lower.Contains("lamp")||lower.Contains("glow");
    var mat=Make(key,c,m.metallic,1-m.roughness,glow?Mathf.Max(.8f,m.emission):0);
    if(model=="Hall"){
     string texture=lower.Contains("slate")?"slab_tiles":lower.Contains("limestone")?"stone_wall":lower.Contains("stone")?"rock_01":null;
     if(texture!=null){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+"_diff_2k.jpg"));mat.SetColor("_BaseColor",Color.white*.8f);}
    }materials[Key(key)]=mat;
   }
  }
  static string Key(string s)=>System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(),"[^a-z0-9]", "");
  static GameObject LoadModel(string name){var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+name+".fbx");if(!source)throw new Exception("Missing model "+name);var go=(GameObject)PrefabUtility.InstantiatePrefab(source);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   foreach(var r in go.GetComponentsInChildren<Renderer>()){var list=r.sharedMaterials;for(int i=0;i<list.Length;i++)if(list[i]&&materials.TryGetValue(Key(name+"_"+list[i].name),out var m))list[i]=m;r.sharedMaterials=list;r.shadowCastingMode=ShadowCastingMode.On;}
   return go;
  }
  static GameObject ActorPrefab(string name,float height){
   var root=new GameObject(name+"_Runtime");var pivot=new GameObject("BlenderAxisConversion").transform;pivot.SetParent(root.transform,false);pivot.localScale=new Vector3(1,1,-1);pivot.localRotation=Quaternion.Euler(0,180,0);
   var model=LoadModel(name);model.transform.SetParent(pivot,false);
   var renderers=model.GetComponentsInChildren<Renderer>();Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   float scale=height/b.size.y;pivot.localScale*=scale;pivot.localPosition=new Vector3(-b.center.x*scale,-b.min.y*scale,-b.center.z*scale);
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+"_Runtime.prefab");Object.DestroyImmediate(root);return prefab;
  }
  static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,bool visible=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().enabled=visible;return g;}
  static void LightAt(string name,Vector3 position,Color color,float intensity,float range){var l=new GameObject(name).AddComponent<Light>();l.type=LightType.Point;l.transform.position=position;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
  [MenuItem("IM Monster/Soulbinder/Build complete encounter")]
  public static void Build(){
   Directory.CreateDirectory(Report);Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Textures");
   foreach(var n in new[]{"slab_tiles","stone_wall","rock_01"})File.Copy("Blender/SceneBuild20261107/SoulbinderHall/Textures/"+n+"_diff_2k.jpg",Root+"/Textures/"+n+"_diff_2k.jpg",true);
   File.Copy("Blender/SceneBuild20261107/SoulbinderHall/Textures/sources.json",Root+"/Textures/sources.json",true);
   File.Copy("Blender/SceneBuild20261107/SoulbinderHall/Textures/sources_rock.json",Root+"/Textures/sources_rock.json",true);
   AssetDatabase.Refresh();materials.Clear();
   foreach(var name in new[]{"Hall","Soulbinder","Cleaver","SwordShield","Archer","Hammer","Wraith"}){
    var importer=AssetImporter.GetAtPath(Root+"/"+name+".fbx") as ModelImporter;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();PrepareMaterials(name);
   }
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var level=new GameObject("Soulbinder_V2_Encounter").AddComponent<SoulbinderLevel>();
   level.violet=Make("FX_Violet",new Color(.55f,.12f,1),0,.5f,2);level.gold=Make("FX_Gold",new Color(1,.56f,.12f),.4f,.4f,2);level.cyan=Make("FX_Cyan",new Color(.1f,.8f,1),0,.4f,2);level.red=Make("FX_Danger",new Color(1,.15f,.12f),0,.3f,2);
   var hall=LoadModel("Hall");hall.name="Soulbinder_Hall_R10";var hallAxes=new GameObject("Hall_BlenderXYZ_to_UnityXZY").transform;hall.transform.SetParent(hallAxes,false);hallAxes.localScale=new Vector3(1,1,-1);
   int tris=0;Bounds hallBounds=new Bounds();bool first=true;
   foreach(var mf in hall.GetComponentsInChildren<MeshFilter>()){var mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;tris+=mf.sharedMesh.triangles.Length/3;var rb=mf.GetComponent<Renderer>().bounds;if(first){hallBounds=rb;first=false;}else hallBounds.Encapsulate(rb);}
   // Ground slab also removes seams in detailed imported floor tiles.
   Box("Ground_collision",new Vector3(0,-.12f,0),new Vector3(22,.2f,30),level.violet,false);
   var player=new GameObject("Player");player.transform.position=new Vector3(0,.2f,-11.5f);var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.32f;cc.center=Vector3.up*.9f;cc.stepOffset=.25f;
   var cam=new GameObject("PlayerCamera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.SetParent(player.transform,false);cam.transform.localPosition=Vector3.up*1.65f;cam.nearClipPlane=.07f;cam.farClipPlane=100;cam.fieldOfView=75;cam.gameObject.AddComponent<AudioListener>();cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
   var pc=player.AddComponent<FirstPersonController>();var so=new SerializedObject(pc);so.FindProperty("inputActions").objectReferenceValue=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");so.FindProperty("cameraPivot").objectReferenceValue=cam.transform;so.FindProperty("jumpSpeed").floatValue=4;so.ApplyModifiedPropertiesWithoutUndo();level.player=pc;level.view=cam;
   string[] names={"Cleaver","SwordShield","Archer","Hammer","Wraith"};float[] heights={2.2f,2.3f,2.2f,2.7f,2.5f};level.minionModels=new GameObject[5];for(int i=0;i<5;i++)level.minionModels[i]=ActorPrefab(names[i],heights[i]);
   var bossPrefab=ActorPrefab("Soulbinder",3.5f);var boss=(GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);boss.name="缚魂者";boss.transform.position=new Vector3(0,0,5.7f);boss.transform.rotation=Quaternion.Euler(0,180,0);var bc=boss.AddComponent<CapsuleCollider>();bc.center=Vector3.up*1.6f;bc.height=3.2f;bc.radius=.8f;bc.isTrigger=true;boss.AddComponent<SoulbinderTarget>().kind=-1;level.boss=boss.transform;
   level.anchorLocks=new Transform[3];Vector3[] anchors={new Vector3(-6.5f,1.6f,-2.5f),new Vector3(6.5f,1.6f,-2.5f),new Vector3(0,1.6f,8.2f)};
   for(int i=0;i<3;i++){
    var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Anchor_Control_Lock_"+i;g.transform.position=anchors[i]+Vector3.back*.85f;g.transform.localScale=new Vector3(.65f,.85f,.5f);g.GetComponent<Renderer>().sharedMaterial=level.violet;g.GetComponent<Collider>().isTrigger=true;g.AddComponent<SoulbinderTarget>().kind=10+i;level.anchorLocks[i]=g.transform;LightAt("Anchor violet "+i,anchors[i]+Vector3.up, new Color(.55f,.2f,1),8,7);
   }
   level.console=new GameObject("Console_Interaction_Point").transform;level.console.position=new Vector3(0,.6f,12.5f);
   level.stairBarriers=new GameObject[2];for(int i=0;i<2;i++){float x=i==0?-8.1f:8.1f;var g=Box("Stair_Rune_Barrier_"+i,new Vector3(x,1.5f,-9.8f),new Vector3(2.7f,3,2.7f),level.violet,false);var ring=new GameObject("Barrier runes").AddComponent<LineRenderer>();ring.transform.SetParent(g.transform);ring.sharedMaterial=level.violet;ring.positionCount=65;ring.startWidth=ring.endWidth=.07f;for(int n=0;n<=64;n++)ring.SetPosition(n,new Vector3(x+Mathf.Cos(n*Mathf.PI/32)*1.45f,.3f,-9.8f+Mathf.Sin(n*Mathf.PI/32)*1.45f));level.stairBarriers[i]=g;}
   // Bake a ground-level navigation surface; gallery remains post-victory exploration.
   var sources=new List<NavMeshBuildSource>{new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(0,-.12f,0),Quaternion.identity,Vector3.one),size=new Vector3(21.5f,.2f,29.5f),area=0}};
   foreach(var mf in hall.GetComponentsInChildren<MeshFilter>())sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mf.sharedMesh,transform=mf.transform.localToWorldMatrix,area=0});
   foreach(var a in anchors)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(a.x,1,a.z),Quaternion.identity,Vector3.one),size=new Vector3(2.1f,2,2.1f),area=1});
   foreach(float x in new[]{-8.1f,8.1f})sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(x,1.5f,-9.8f),Quaternion.identity,Vector3.one),size=new Vector3(3,3,3),area=1});
   sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(0,.3f,12.7f),Quaternion.identity,Vector3.one),size=new Vector3(10,.6f,3.4f),area=1});
   var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.55f;settings.agentHeight=2.2f;settings.agentClimb=.25f;settings.overrideVoxelSize=true;settings.voxelSize=.12f;
   var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(24,6,32)),Vector3.zero,Quaternion.identity);if(!data)throw new Exception("Navigation build failed");
   string navPath=Root+"/SoulbinderNavigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);if(previous){EditorUtility.CopySerialized(data,previous);Object.DestroyImmediate(data);data=previous;}else AssetDatabase.CreateAsset(data,navPath);
   var nav=new GameObject("Navigation").AddComponent<SoulbinderNavigation>();nav.data=data;
   var sun=new GameObject("Moonlight").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(52,-25,0);sun.color=new Color(.62f,.71f,1);sun.intensity=1.1f;sun.shadows=LightShadows.Soft;
   for(int side=-1;side<=1;side+=2)for(int z=-10;z<=10;z+=5)LightAt("Warm bay lamp",new Vector3(side*9,3,z),new Color(1,.57f,.24f),4,7);
   LightAt("Central soft fill",new Vector3(0,6,0),new Color(.7f,.75f,1),16,25);LightAt("Console warm fill",new Vector3(0,4,11),new Color(1,.7f,.4f),8,9);
   var fill=new GameObject("Soft frontal character light").AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(30,180,0);fill.color=new Color(.88f,.82f,.72f);fill.intensity=.8f;fill.shadows=LightShadows.None;
   RenderSettings.ambientLight=new Color(.4f,.38f,.48f);RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientSkyColor=new Color(.5f,.52f,.65f);RenderSettings.ambientEquatorColor=new Color(.3f,.28f,.37f);RenderSettings.ambientGroundColor=new Color(.18f,.16f,.2f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.013f;RenderSettings.fogColor=new Color(.055f,.045f,.09f);
   var volume=new GameObject("Hall color and glow").AddComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=profile.Add<Bloom>();bloom.intensity.Override(.35f);bloom.threshold.Override(1);var tonemap=profile.Add<Tonemapping>();tonemap.mode.Override(TonemappingMode.ACES);var vignette=profile.Add<Vignette>();vignette.intensity.Override(.16f);string profilePath=Root+"/HallVolume.asset";var oldProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);if(oldProfile){volume.sharedProfile=oldProfile;Object.DestroyImmediate(profile);}else {AssetDatabase.CreateAsset(profile,profilePath);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);volume.sharedProfile=profile;}
   level.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Contest20261107/Fonts/NotoSansCJKsc-Regular.otf");
   level.castSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Selected20261104/VolleyCharge.wav");level.hitSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Selected20261104/ArmorHit.wav");level.breakSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/Selected20261104/CrystalBreak.wav");
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
   File.WriteAllText(Report+"/scene-build.txt","Scene: "+ScenePath+"\nHall imported bounds: "+hallBounds+"\nHall triangles: "+tris+"\nFive minion prefabs + boss imported. Navigation stored independently.\n");Debug.Log("SOULBINDER_SCENE_BUILT "+hallBounds+" triangles="+tris);
  }
  public static void BuildAndTest(){try{SoulbinderAcceptance.Tests();Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  public static void BuildWindows(){try{string path="Builds/SoulbinderComplete_Windows/Soulbinder.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});File.WriteAllText(Report+"/build-windows.txt",result.summary.result+" bytes="+result.summary.totalSize+" errors="+result.summary.totalErrors);EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  public static void BuildMac(){try{string path="Builds/SoulbinderComplete/Soulbinder.app";Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=BuildTarget.StandaloneOSX,options=BuildOptions.CompressWithLz4HC});File.WriteAllText(Report+"/build-result.txt",result.summary.result+" bytes="+result.summary.totalSize+" errors="+result.summary.totalErrors);EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
#endif
