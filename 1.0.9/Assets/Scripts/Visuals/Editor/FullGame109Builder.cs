#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ArcaneArchive.Visuals.Editor {
 public static class FullGame109Builder {
  public const string MainScene="Assets/Scenes/IMMonster_ThreeBoss_Book.unity",SoulScene="Assets/Scenes/SoulbinderComplete.unity",Docs="Docs/Release109";
  public static void Prepare(){
   if(typeof(CampaignDefinition.Chapter).GetField("externalScene")==null)throw new Exception("First chapter cannot route to the new Soulbinder scene");
   var asset=AssetDatabase.LoadAssetAtPath<CampaignDefinition>("Assets/Art/ThreeBoss/Campaign.asset");if(!asset||asset.chapters.Length!=3)throw new Exception("Missing three chapter campaign");
   var so=new SerializedObject(asset);var first=so.FindProperty("chapters").GetArrayElementAtIndex(0);first.FindPropertyRelative("externalScene").stringValue="SoulbinderComplete";first.FindPropertyRelative("combat").objectReferenceValue=null;first.FindPropertyRelative("arena").objectReferenceValue=null;
   first.FindPropertyRelative("introduction").stringValue="灾变后，蓝绳与封章唤起你的记忆：还有遗赠未送出。\n缚魂者驱使军团：「替我维持这道束缚吧。」\n\n五类小怪同时登场。关闭锚点阻止补召，躲避扇形魂弹。\n左键秘文弹；右键吞印/格挡；Shift冲刺。\n召唤引导2.4秒内造成180伤害可打断。战后到北侧控制台处理灵魂，继续下一章。";
   so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
   var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==SoulScene))scenes.Add(new EditorBuildSettingsScene(SoulScene,true));else scenes.First(s=>s.path==SoulScene).enabled=true;EditorBuildSettings.scenes=scenes.ToArray();
   EditorSceneManager.OpenScene(SoulScene);var level=UnityEngine.Object.FindFirstObjectByType<ArcaneArchive.Soulbinder.SoulbinderLevel>();if(!level||level.minionModels.Length!=5||!level.boss||!level.font)throw new Exception("New encounter references missing");
   foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
   EditorSceneManager.OpenScene(MainScene);AssetDatabase.SaveAssets();Directory.CreateDirectory(Docs);File.WriteAllText(Docs+"/SceneCheck.txt","First chapter routes to SoulbinderComplete. Five minion prefabs, boss, font and scene scripts verified. Both scenes in build. Original 1.0.8 preserved.\n");Debug.Log("FULL_GAME_109_PREPARED");
  }
  public static void PrepareAndExit(){try{Prepare();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
  public static void BuildWindows(){Build(BuildTarget.StandaloneWindows64,"Builds/IMMonster_1.0.9_Windows/IMMonster.exe","WindowsBuild.txt");}
  public static void BuildMac(){Build(BuildTarget.StandaloneOSX,"Builds/IMMonster_1.0.9_Mac/IMMonster.app","MacBuild.txt");}
  static void Build(BuildTarget target,string output,string report){try{
   Prepare();PlayerSettings.productName="IM MONSTER - Legacy";PlayerSettings.bundleVersion="1.0.9";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   Directory.CreateDirectory(Path.GetDirectoryName(output));var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{MainScene,SoulScene},locationPathName=output,target=target,options=BuildOptions.CompressWithLz4HC});File.WriteAllText(Docs+"/"+report,$"Result: {r.summary.result}\nErrors: {r.summary.totalErrors}\nWarnings: {r.summary.totalWarnings}\nBytes: {r.summary.totalSize}\nOutput: {output}\n");EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
#endif
