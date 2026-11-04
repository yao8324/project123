using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
namespace ArcaneArchive.Visuals.Editor
{
 public static class FullDeliveryBuilder
 {
  public static void Build()
  {
   SealedDuelArenaAcceptance.GuardSceneWork();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var camera=new GameObject("LauncherCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.025f,.06f);camera.gameObject.AddComponent<AudioListener>();
   new GameObject("FullDeliveryLauncher").AddComponent<ArcaneArchive.UI.FullDeliveryLauncher>().font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Contest20261107/Fonts/NotoSansCJKsc-Regular.otf");
   const string path="Assets/Scenes/IMMonster_FullDelivery.unity";EditorSceneManager.SaveScene(scene,path);
   var scenes=new[]{path,ContestSceneBuilder.ScenePath,SealedDuelArenaAcceptance.ScenePath};
   EditorBuildSettings.scenes=Array.ConvertAll(scenes,p=>new EditorBuildSettingsScene(p,true));
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.GetFullPath("Builds/FullDelivery/IM MONSTER.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.CompressWithLz4});
   Directory.CreateDirectory("Docs/FullDelivery20261104");File.WriteAllText("Docs/FullDelivery20261104/build.txt",result.summary.result+" errors="+result.summary.totalErrors+" bytes="+result.summary.totalSize);
   if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Full delivery failed");
  }
 }
}
