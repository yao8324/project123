using System;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
 public static class CombatUnificationBuilder
 {
  const string ViewPrefab="Assets/Art/PlayerSwordShield/Prefabs/MainCombatView.prefab";
  public static void FinalizeView()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
   var view=Object.FindFirstObjectByType<SwordShieldView>();
   if(!PrefabUtility.SaveAsPrefabAsset(view.gameObject,ViewPrefab))throw new Exception("View prefab export failed");
   EditorSceneManager.OpenScene(SealedDuelArenaAcceptance.ScenePath);
   if(AssetDatabase.IsValidFolder("Assets/CombatUnification"))AssetDatabase.DeleteAsset("Assets/CombatUnification");
   AssetDatabase.SaveAssets();
  }
  public static void PatchEffectMaterials()
  {
   var arena=AssetDatabase.LoadAssetAtPath<BossArena>(SealedDuelArenaAcceptance.ArenaPath);
   var ribbon=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ForbiddenWarden/CombatV2/Materials/WardenSpellRibbon.mat");
   var particles=AssetDatabase.LoadAssetAtPath<MeleeLoadout>("Assets/Art/PlayerSwordShield/OathboundLoadout.asset").skillParticleEffect;
   if(!ribbon || !particles)throw new Exception("Missing Warden effect materials");
   foreach(var path in new[]{AssetDatabase.GetAssetPath(arena.laserEffect),AssetDatabase.GetAssetPath(arena.fractureEffect)})
   {
    var root=PrefabUtility.LoadPrefabContents(path);
    try{
     foreach(var effect in root.GetComponentsInChildren<LaserBeamVisuals>(true)){effect.detailMaterial=ribbon;effect.particleMaterial=particles;}
     foreach(var effect in root.GetComponentsInChildren<GroundFractureVisuals>(true)){effect.detailMaterial=ribbon;effect.particleMaterial=particles;}
     PrefabUtility.SaveAsPrefabAsset(root,path);
    }finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   AssetDatabase.SaveAssets();
  }
  public static void BuildWindows()
  {
   string path="Builds/MainCombat108_20261106/IMMonster.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));
   var result=UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4});
   Directory.CreateDirectory(Output);File.WriteAllText(Output+"/windows-build.txt",result.summary.result+" errors="+result.summary.totalErrors+" bytes="+result.summary.totalSize+" path="+Path.GetFullPath(path));
   if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed");
  }
  public const string Output="Docs/CombatUnification20261106";
  [MenuItem("Arcane Archive/Unify main combat with 1.0.8")]
  public static void Apply()
  {
   SealedDuelArenaAcceptance.GuardSceneWork();PatchEffectMaterials();
   var prior=EditorSceneManager.GetSceneManagerSetup();
   SwordShieldView template;
   var savedView=AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefab);
   if(savedView){template=savedView.GetComponent<SwordShieldView>();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
   else{
    var source=EditorSceneManager.OpenScene("Assets/CombatUnification/Editor/Reference108.unity",OpenSceneMode.Single);
    template=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SwordShieldView>(true)).Single();
   }
   var loadout=AssetDatabase.LoadAssetAtPath<MeleeLoadout>("Assets/Art/PlayerSwordShield/OathboundLoadout.asset");
   if(!loadout || loadout.skills.Length!=4 || loadout.skills.Any(s=>!s.icon || !s.cooldownIcon) || !loadout.swordPrefab || !loadout.shieldPrefab)throw new Exception("Incomplete 1.0.8 loadout");
   var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
   var layers=tags.FindProperty("layers");
   if(LayerMask.NameToLayer("TemporalAfterimages")<0){if(layers.GetArrayElementAtIndex(30).stringValue!="")throw new Exception("Layer 30 occupied");layers.GetArrayElementAtIndex(30).stringValue="TemporalAfterimages";tags.ApplyModifiedProperties();}
   var supplement=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/MeleeChineseSupplement.asset");
   foreach(string path in new[]{"Assets/Scenes/MilanLibrary_Magic_V6.unity",SealedDuelArenaAcceptance.ScenePath})
   {
    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
    var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FirstPersonController>(true)).Single();
    var camera=player.GetComponentInChildren<Camera>();
    var melee=player.GetComponent<PlayerMelee>();if(!melee)melee=player.gameObject.AddComponent<PlayerMelee>();melee.loadout=loadout;
    var old=camera.transform.Find("MageHand_ViewModel");if(old)old.gameObject.SetActive(false);
    var existing=camera.transform.Find("SwordShield_ViewModel");if(existing)Object.DestroyImmediate(existing.gameObject);
    var clone=Object.Instantiate(template.gameObject,camera.transform,false);clone.name="SwordShield_ViewModel";
    var view=clone.GetComponent<SwordShieldView>();view.melee=melee;view.viewCamera=camera;
    var presentation=camera.transform.Find("FirstPersonPresentationCamera");
    if(!presentation)throw new Exception("Missing presentation camera: "+path);
    view.presentationCamera=presentation.GetComponent<Camera>();melee.view=view;
    foreach(var item in clone.GetComponentsInChildren<Transform>(true))item.gameObject.layer=LayerMask.NameToLayer("FirstPersonViewModel");
    view.ResetPose();
    var encounter=player.GetComponent<BossEncounter>();
    var menu=new SerializedObject(player.GetComponent<RecoveryMenu>());
    foreach(var font in new[]{encounter.combatFont,menu.FindProperty("uiFont").objectReferenceValue as TMP_FontAsset})
     if(font && supplement && font!=supplement && !font.fallbackFontAssetTable.Contains(supplement))
     {font.fallbackFontAssetTable.Add(supplement);EditorUtility.SetDirty(font);}
    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
    EditorSceneManager.CloseScene(scene,true);
   }
   AssetDatabase.SaveAssets();if(prior.Any(s=>s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(prior);else EditorSceneManager.OpenScene(SealedDuelArenaAcceptance.ScenePath);
   Directory.CreateDirectory(Output);File.WriteAllText(Output+"/scene-setup.txt","Main library and sealed arena equipped with authored 1.0.8 sword/shield view, four skills, shared stamina and equipment menu. Existing worlds and launch/story scenes preserved.");
   Debug.Log("COMBAT_UNIFICATION_SETUP_PASS");
  }
 }
}



