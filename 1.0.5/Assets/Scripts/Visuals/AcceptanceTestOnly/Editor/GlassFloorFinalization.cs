using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ArcaneArchive.Player;
using ArcaneArchive.Visuals;
using ArcaneArchive.Visuals.Editor;
namespace MilanAcceptance
{
 public static class GlassFloorFinalization
 {
  const string A="Assets/Art/MilanLibraryV6";
  public static void Refresh()
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var manifest=JsonUtility.FromJson<MilanV6SceneBuilder.Manifest>(File.ReadAllText(A+"/manifest.json"));
   var original=GameObject.Find("Milan Library — Magic V6");if(!original)throw new Exception("Generated geometry root missing");
   // Preserve every authored light and all player/UI/camera/environment state.
   var lights=original.GetComponentsInChildren<Light>(true).Select(l=>l.transform).ToArray();
   foreach(var light in lights)light.SetParent(null,true);
   UnityEngine.Object.DestroyImmediate(original);var root=new GameObject("Milan Library — Magic V6");
   foreach(var light in lights)light.SetParent(root.transform,true);
   var mats=manifest.materials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(A+"/Materials/"+m.name+".mat")).ToArray();
   if(mats.Any(m=>!m))throw new Exception("Unexpected new material; explicit review required");
   var groups=new Dictionary<string,Transform>();var animations=new Transform[manifest.animations.Length];
   for(int i=0;i<animations.Length;i++){var go=new GameObject(manifest.animations[i].name);go.transform.SetParent(root.transform,false);go.AddComponent<MilanMagicMotion>().poses=manifest.animations[i].poses;animations[i]=go.transform;}
   int index=0;foreach(var c in manifest.chunks)
   {
    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(A+"/Meshes/"+c.file);if(!mesh)throw new Exception("Missing mesh "+c.file);
    if(!groups.TryGetValue(c.group,out var parent)){parent=new GameObject(c.group).transform;parent.SetParent(root.transform,false);groups[c.group]=parent;}
    var go=new GameObject(c.name+"_"+index++);go.transform.SetParent(c.animation>=0?animations[c.animation]:parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
    if(!c.collisionOnly){var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mats[c.material];r.shadowCastingMode=manifest.materials[c.material].strength>1?ShadowCastingMode.Off:ShadowCastingMode.On;}
    if(c.animation<0)GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
    if(c.collisionRole!="none"){if(c.animation>=0)throw new Exception("Animated collider");go.AddComponent<MeshCollider>().sharedMesh=mesh;if(c.collisionRole=="walkable")go.AddComponent<GroundSurface>();}
   }
   ConfigureGlassImport();
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MILAN_GLASS_FLOOR_REFRESH_COMPLETE chunks="+index+" preservedLights="+lights.Length);
  }
  public static void ConfigureGlassImport()
  {
   string texturePath=A+"/Textures/Gothic_StainedGlass_Figural_V1.png";
   var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.npotScale=TextureImporterNPOTScale.ToNearest;importer.maxTextureSize=2048;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
   var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=true;platform.format=TextureImporterFormat.DXT1;platform.maxTextureSize=2048;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
   var glass=AssetDatabase.LoadAssetAtPath<Material>(A+"/Materials/V3_FiguralPaintedGlass.mat");glass.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(glass);
   AssetDatabase.SaveAssets();Debug.Log("MILAN_GLASS_COMPRESSION_CONFIGURED");
  }
  public static void Build()
  {
   string name=PlayerSettings.productName,version=PlayerSettings.bundleVersion;var target=UnityEditor.Build.NamedBuildTarget.Standalone;string id=PlayerSettings.GetApplicationIdentifier(target);
   try{PlayerSettings.productName="Milan Library Glass Floor 20261007";PlayerSettings.bundleVersion="2026.10.7.2";PlayerSettings.SetApplicationIdentifier(target,"com.arcanearchive.milan.glassfloor20261007");MilanAcceptanceBuild.BuildMacPlayer();}
   finally{PlayerSettings.productName=name;PlayerSettings.bundleVersion=version;PlayerSettings.SetApplicationIdentifier(target,id);AssetDatabase.SaveAssets();}
  }
 }
}
