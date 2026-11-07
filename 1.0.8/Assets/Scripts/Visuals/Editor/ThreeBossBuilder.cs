using System;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using ArcaneArchive.UI;
using ArcaneArchive.Knowledge;
using ArcaneArchive.Interaction;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using TMPro;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
 public static class ThreeBossBuilder
 {
  public const string Root="Assets/Art/ThreeBoss";
  public const string Scene="Assets/Scenes/IMMonster_ThreeBoss_Book.unity";
  [Serializable] class MatSpec { public string name; public float[] color,emission;public float metallic,roughness; }
  [Serializable] class MatSpecs {public MatSpec[] materials;}
  static T Save<T>(T obj,string path) where T:Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(obj,old);Object.DestroyImmediate(obj);return old;}AssetDatabase.CreateAsset(obj,path);return obj;}
  static void Import(string name)
  {
   string path=Root+"/Imported/"+name+".fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);
   imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationType=name.StartsWith("Soulbinder") && name!="SoulbinderHall"?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;imp.importAnimation=name!="SoulbinderHall";imp.isReadable=false;imp.SaveAndReimport();
  }
  static GameObject Model(string name,Transform parent)
  {
   var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Imported/"+name+".fbx"),parent);go.name=name;
   var specs=JsonUtility.FromJson<MatSpecs>(File.ReadAllText(Root+"/Imported/"+name+"_materials.json"));
   // FBX material slot order follows the export manifest.
   var materials=specs.materials.Select((s,i)=>{
    var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name+"_"+s.name};
    m.SetColor("_BaseColor",new Color(s.color[0],s.color[1],s.color[2],s.color[3]));m.SetFloat("_Metallic",s.metallic);m.SetFloat("_Smoothness",1-s.roughness);
    var em=new Color(s.emission[0],s.emission[1],s.emission[2]);if(em.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",em);}
    return Save(m,Root+"/Materials/"+name+"_"+i+".mat");}).ToArray();
   foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterials=materials;if(r is SkinnedMeshRenderer sk)sk.updateWhenOffscreen=true;}
   return go;
  }
  static void Box(Transform parent,string name,Vector3 pos,Vector3 size)
  {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.AddComponent<BoxCollider>().size=size;}
  static void ClearEnvironment(BossArena a)
  {
   foreach(var r in a.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(a.boss))r.enabled=false;
   foreach(var c in a.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(a.boss))Object.DestroyImmediate(c);
   foreach(var l in a.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(l);
  }
  static void Light(Transform parent,Vector3 pos,Color color,float power,float range)
  {var g=new GameObject("Chapter fill light");g.transform.SetParent(parent,false);g.transform.localPosition=pos;var l=g.AddComponent<Light>();l.color=color;l.intensity=power;l.range=range;l.type=LightType.Point;l.shadows=LightShadows.None;}
  static void AddChapterLighting(BossArena arena)
  {
   var owner=arena.gameObject.AddComponent<KnightSanctuaryLighting>();
   owner.pipeline=AssetDatabase.LoadAssetAtPath<KnightDefinition>(SanctuaryR3CombatBuilder.Config).arena.GetComponent<KnightSanctuaryLighting>().pipeline;
   var go=new GameObject("Chapter soft overhead");go.transform.SetParent(arena.transform,false);go.transform.localRotation=Quaternion.Euler(55,-25,0);var light=go.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(.86f,.83f,1);light.intensity=.85f;light.shadows=LightShadows.Soft;
  }
  public static void Prepare()
  {
   Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory("Docs/ThreeBoss");AssetDatabase.Refresh();Import("Soulbinder");Import("SoulbinderHall");Import("Soulbinder_Idle");Import("Soulbinder_Invocation");
   var scene=EditorSceneManager.OpenScene(SanctuaryR3CombatBuilder.Scene);
   var e=Object.FindFirstObjectByType<BossEncounter>();var menu=e.GetComponent<RecoveryMenu>();var trial=e.GetComponent<KnightTrialLauncher>();if(trial)Object.DestroyImmediate(trial);
   e.transform.position=new Vector3(-8,.08f,-.8f);e.transform.rotation=Quaternion.Euler(0,90,0);
   foreach(var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(c=>c && c.GetType().Namespace!=null && c.GetType().Namespace.Contains("Acceptance")).ToArray())Object.DestroyImmediate(c);
   var baseArena=e.arenaPrefab;
   var soul=Object.Instantiate(baseArena.gameObject);soul.name="SoulbinderHall_Campaign";var a=soul.GetComponent<BossArena>();ClearEnvironment(a);
   foreach(var r in a.boss.GetComponentsInChildren<Renderer>(true))r.enabled=false;
   Model("SoulbinderHall",soul.transform);
   var model=Model("Soulbinder",a.boss);model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one*.82f;
   var visuals=a.boss.gameObject.AddComponent<SoulbinderCampaignVisuals>();visuals.actor=model.GetComponent<Animator>();if(!visuals.actor)visuals.actor=model.AddComponent<Animator>();
   var clips=AssetDatabase.LoadAllAssetsAtPath(Root+"/Imported/Soulbinder.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
   visuals.idle=AssetDatabase.LoadAllAssetsAtPath(Root+"/Imported/Soulbinder_Idle.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));visuals.invocation=AssetDatabase.LoadAllAssetsAtPath(Root+"/Imported/Soulbinder_Invocation.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));
   if(!visuals.idle || !visuals.invocation)throw new Exception("Soulbinder exported actions missing: "+string.Join(",",clips.Select(c=>c.name)));
   a.radius=8.5f;a.spawn.localPosition=new Vector3(0,.1f,-7);a.spawn.localRotation=Quaternion.identity;
   var marker=new GameObject("SoulbinderSpawn").transform;marker.SetParent(a.transform,false);marker.localPosition=new Vector3(0,0,4);a.bossSpawn=marker;
   Box(a.transform,"Walkable Floor",new Vector3(0,-.15f,0),new Vector3(19,.3f,25));
   Box(a.transform,"West Limit",new Vector3(-9,3,0),new Vector3(.4f,6,26));Box(a.transform,"East Limit",new Vector3(9,3,0),new Vector3(.4f,6,26));
   Box(a.transform,"North Limit",new Vector3(0,3,11),new Vector3(18,6,.4f));Box(a.transform,"South Limit",new Vector3(0,3,-11),new Vector3(18,6,.4f));
   Light(a.transform,new Vector3(0,7,-3),new Color(.72f,.65f,1),26,25);Light(a.transform,new Vector3(0,5,8),new Color(.45f,.25f,1),18,19);
   for(int i=-1;i<=1;i+=2)Light(a.transform,new Vector3(i*7,4,0),new Color(1,.62f,.25f),12,13);
   AddChapterLighting(a);
   var soulArena=PrefabUtility.SaveAsPrefabAsset(soul,Root+"/SoulbinderArena.prefab").GetComponent<BossArena>();Object.DestroyImmediate(soul);
   var soulCombat=Object.Instantiate(e.definition);soulCombat.displayName="缚魂者";soulCombat.health=700;soulCombat.bossScale=1;soulCombat.attackSequence=new[]{0,2,1,0,2};soulCombat.meleeDamage=16;soulCombat.volleyDamage=7;soulCombat.fractureDamage=12;soulCombat.waveDamage=14;soulCombat.moveSpeed=1.8f;soulCombat.preferredMinDistance=4;soulCombat.preferredMaxDistance=6;soulCombat.rushDistance=7;soulCombat.volleyRetreatDistance=1.5f;soulCombat=soulCombat?Save(soulCombat,Root+"/SoulbinderCombat.asset"):null;
   // The local latest Warden environment is imported as scenery; existing combat rig and effects remain authoritative.
   var ward=Object.Instantiate(baseArena.gameObject);ward.name="Warden_Campaign";var wa=ward.GetComponent<BossArena>();ClearEnvironment(wa);
   var scenery=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/SealedDuelArena20261104/Prefabs/SealedDuelArena.prefab"),ward.transform);scenery.name="Local_SealedDuelArena";
   var sa=scenery.GetComponent<BossArena>();if(sa){if(sa.boss)Object.DestroyImmediate(sa.boss.gameObject);if(sa.effectRoot)Object.DestroyImmediate(sa.effectRoot.gameObject);}
   foreach(var t in scenery.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
   foreach(var c in scenery.GetComponentsInChildren<MonoBehaviour>(true))if(c)Object.DestroyImmediate(c);
   foreach(var l in scenery.GetComponentsInChildren<LineRenderer>(true))l.enabled=false;
   wa.radius=14;wa.spawn.localPosition=new Vector3(0,.08f,-10);wa.spawn.localRotation=Quaternion.identity;
   Light(wa.transform,new Vector3(0,9,-3),new Color(.76f,.69f,1),22,35);
   AddChapterLighting(wa);
   var wardArena=PrefabUtility.SaveAsPrefabAsset(ward,Root+"/WardenArena.prefab").GetComponent<BossArena>();Object.DestroyImmediate(ward);
   var wardCombat=Object.Instantiate(e.definition);wardCombat.displayName="缄默典狱长";wardCombat=Save(wardCombat,Root+"/WardenCombat.asset");
   var book=ScriptableObject.CreateInstance<DemoBookRecord>();book.id="im-monster-three-chapters";book.title="我是怪物：遗赠";book.opensTrial=true;book.category="封印档案";book.summary="翻开书，完成三场战斗，把尚未离开的人与遗赠送出去。";book=Save(book,Root+"/CampaignBook.asset");
   var def=ScriptableObject.CreateInstance<CampaignDefinition>();def.book=book;def.chapters=new[]{
    new CampaignDefinition.Chapter{title="第一章 · 缚魂者",combat=soulCombat,arena=soulArena,
     introduction="灾变后，你在守藏院醒来。蓝绳、封章、最后一批遗赠——你记得自己还没有完成工作。\n\n收容大厅里，缚魂者正在驱使亡灵。\n「能吞纳魔能？你正适合替我维持这道束缚。」\n\n先突破他的术式，打开撤离通路。",
     aftermath="束缚散去，亡灵退回收容锚点。\n\n你在档案台后找到遇难者。她怀中的蓝绳、受伤的衣料、写着「伊莱恩」的工作牌，都与你的记忆吻合。\n\n「那些记忆属于她。我不是伊莱恩。」\n封馆警报响起。典狱长正在检查你的掌纹。"},
    new CampaignDefinition.Chapter{title="第二章 · 典狱长",combat=wardCombat,arena=wardArena,
     introduction="撤离名单上，有些名字已被标记为「已送出」。典狱长守着剩下的通路。\n\n「先接受收容。我会派人处理剩下的档案。」\n你仍记得那份未完成的交接，不愿在此停下。\n\n封馆术式启动。打断收容，继续前往核心。",
     aftermath="收容术式被打断。事故抄件写着「地下封印空置」，你的掌部三缺环纹与封印共鸣。\n「封印里的东西，是我。」\n\n你稳住受损装置，救出典狱长，归还封馆印。\n他接通撤离回路：「先把他们送出去。之后，你跟我走。」"},
    new CampaignDefinition.Chapter{title="第三章 · 圣骑士",knight=AssetDatabase.LoadAssetAtPath<KnightDefinition>(SanctuaryR3CombatBuilder.Config),
     introduction="核心圣堂里，黄金羽翼收拢在厚重封印门前。\n圣骑士准备切断失控的外层供能。\n「继续连接，危险就会蔓延。」\n\n通讯里传来请求：撤离尚未结束。\n你必须争取修复时间，让圣骑士暂停紧急隔离。",
     aftermath="黄金羽翼缓缓收拢。圣骑士仍活着，紧急隔离暂停。\n\n典狱长恢复设施权限，你将外溢能量压回回路。\n圣骑士看见稳定的读数，放下剑，守住通路。\n最后一批蓝绳转运箱开始离馆。"}};
   def.endingTitle="终章 · 完成她未完成的工作";def.ending="幸存者与遗赠完成撤离，亡灵被安全转移。原石仍留在核心，供能恢复。\n\n你不是伊莱恩，也无法抹去自己的过去。\n但这一次，你选择完成她交给你的工作。\n\n那封信终于送出：她可以来读，也可以选择不读。\n\n三章通关 · 感谢游玩";def=Save(def,Root+"/Campaign.asset");
   var campaign=e.GetComponent<BookCampaign>();if(!campaign)campaign=e.gameObject.AddComponent<BookCampaign>();campaign.definition=def;
   foreach(var target in Object.FindObjectsByType<DemoBookTarget>(FindObjectsSortMode.None))if(target.Record && target.Record.opensTrial){var so=new SerializedObject(target);so.FindProperty("record").objectReferenceValue=book;so.ApplyModifiedPropertiesWithoutUndo();}
   var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKsc-Regular.otf");var font=TMP_FontAsset.CreateFontAsset(source,48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);font.name="ThreeBossChinese";
   string fontPath=Root+"/ThreeBossChinese.asset";if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath))AssetDatabase.DeleteAsset(fontPath);AssetDatabase.CreateAsset(font,fontPath);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
   string chars=string.Concat(Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));chars=new string(chars.Where(c=>c>=32).Distinct().ToArray());font.TryAddCharacters(chars,out var missing);font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
   var ui=new SerializedObject(menu);ui.FindProperty("uiFont").objectReferenceValue=font;ui.ApplyModifiedPropertiesWithoutUndo();e.combatFont=font;menu.bookKnightFont=font;
   EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();Debug.Log("THREE_BOSS_PREPARED");
  }
  public static void Preview(){EditorSceneManager.OpenScene(Scene);EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;}
  public static void BuildWindows()
  {
   if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new Exception("Windows Mono module is unavailable");
   string output=Path.GetFullPath("Builds/IMMonster_ThreeBoss_Windows/IMMonster.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
   PlayerSettings.productName="IM MONSTER - Legacy";PlayerSettings.bundleVersion="1.0.8-threeboss";PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
   File.WriteAllText("Docs/ThreeBoss/WindowsBuild.txt",$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nSeconds: {report.summary.totalTime.TotalSeconds}\nOutput: {output}\n");
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");
  }
 }
}
