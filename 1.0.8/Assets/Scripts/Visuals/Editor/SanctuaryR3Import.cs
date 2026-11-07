using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using ArcaneArchive.Visuals;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
    public static class SanctuaryR3Import
    {
        const string Root="Assets/Art/Bosses/Knight/SanctuaryR3";
        const string Output="Docs/SanctuaryR3Build";
        [Serializable] class Surface {public string name;public float[] color;public float metallic,roughness;public string texture;}
        [Serializable] class Surfaces {public Surface[] materials;}
        static Dictionary<string,Material> mats=new Dictionary<string,Material>();
        static Material MaterialFor(Material source)
        {
            string name=source?source.name:"R3_Fallback";
            if(mats.TryGetValue(name,out var found))return found;
            var path=Root+"/Materials/"+name.Replace('/','_')+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            Color col=source&&source.HasProperty("_Color")?source.color:new Color(.6f,.6f,.6f);
            m.SetColor("_BaseColor",col);m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",.35f);
            bool gold=name.Contains("Gold")||name.Contains("Brass");m.SetFloat("_Metallic",gold?1:0);
            if(gold)m.SetColor("_BaseColor",new Color(.65f,.40f,.13f));
            var json=Root+"/EnvironmentMaterials.json";
            if(File.Exists(json)){
                var entry=JsonUtility.FromJson<Surfaces>(File.ReadAllText(json)).materials.FirstOrDefault(x=>x.name==name);
                if(entry!=null){
                    m.SetColor("_BaseColor",new Color(entry.color[0],entry.color[1],entry.color[2],entry.color[3]));m.SetFloat("_Metallic",entry.metallic);m.SetFloat("_Smoothness",1-entry.roughness);
                    if(!string.IsNullOrEmpty(entry.texture)){
                        string stem=entry.texture.Split(new[]{"_Albedo"},StringSplitOptions.None)[0];
                        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Bosses/Knight/Sanctuary20261107/Textures/"+stem+"_Albedo.png");m.SetTexture("_BaseMap",tex);
                    }
                }
            }
            if(name.Contains("WarmGlass")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.35f,.06f)*2);}
            EditorUtility.SetDirty(m);mats[name]=m;return m;
        }
        static void Remap(GameObject root){foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(MaterialFor).ToArray();}
        static void Configure(string path,bool animated)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(importer && importer.animationType==(animated?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None) && importer.isReadable && (!animated || importer.avatarSetup==ModelImporterAvatarSetup.CreateFromThisModel))return;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.globalScale=1;importer.useFileScale=true;importer.importCameras=false;importer.importLights=false;
            importer.animationType=animated?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;importer.importAnimation=animated;importer.isReadable=true;
            if(animated){importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;var clips=importer.defaultClipAnimations;foreach(var clip in clips){clip.loopTime=false;clip.keepOriginalPositionXZ=true;clip.keepOriginalPositionY=true;clip.keepOriginalOrientation=true;}importer.clipAnimations=clips;}
            importer.SaveAndReimport();
        }
        [MenuItem("Tools/Knight/R3/Build First Pass Preview")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Output);mats.Clear();AssetDatabase.Refresh();
            Configure(Root+"/Sanctuary_R3_Environment.fbx",false);Configure(Root+"/Paladin_R3_Animated.fbx",true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var environment=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Sanctuary_R3_Environment.fbx"));environment.name="R3_56m_Environment_FirstPass";environment.transform.rotation=Quaternion.Euler(0,180,0);Remap(environment);
            var character=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Paladin_R3_Animated.fbx"));character.name="R3_Paladin_AnimationPreview";character.transform.rotation=Quaternion.Euler(0,180,0);Remap(character);
            var animator=character.GetComponent<Animator>();if(!animator)animator=character.AddComponent<Animator>();animator.applyRootMotion=true;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var all=AssetDatabase.LoadAllAssetsAtPath(Root+"/Paladin_R3_Animated.fbx").OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview__")).ToArray();
            var clips=new[]{"A01_SlowWalk","A02_Dash","A03_JumpAttack"}.Select(n=>all.FirstOrDefault(c=>c.name.Contains(n))).ToArray();
            if(clips.Any(c=>!c))throw new Exception("Missing R3 animation clips");
            var go=new GameObject("R3_PreviewCamera");var camera=go.AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(5,3,8);camera.transform.LookAt(new Vector3(0,1.5f,1));camera.fieldOfView=55;camera.nearClipPlane=.05f;camera.farClipPlane=200;camera.backgroundColor=new Color(.10f,.13f,.18f);go.AddComponent<AudioListener>();
            var preview=new GameObject("R3_PreviewControls").AddComponent<SanctuaryR3Preview>();preview.actor=animator;preview.clips=clips;preview.view=camera;
            var floor=new GameObject("R3_FloorCollision").AddComponent<BoxCollider>();floor.center=new Vector3(0,-.175f,0);floor.size=new Vector3(64,.35f,72);
            foreach(var pair in new[]{new Vector3(-32.6f,12,0),new Vector3(32.6f,12,0)}){var wall=new GameObject("R3_SideWallCollision").AddComponent<BoxCollider>();wall.center=pair;wall.size=new Vector3(1.2f,24,72);}
            new GameObject("Design_PlayerStart").transform.position=new Vector3(0,0,-22);new GameObject("Design_BossStart").transform.position=new Vector3(0,0,25);
            var sun=new GameObject("R3_PreviewKey").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.color=new Color(1,.91f,.78f);sun.transform.rotation=Quaternion.Euler(45,-35,0);sun.shadows=LightShadows.None;
            for(int side=-1;side<=1;side+=2)for(int z=-20;z<=20;z+=20){var fill=new GameObject("R3_InteriorFill").AddComponent<Light>();fill.type=LightType.Point;fill.transform.position=new Vector3(side*24,18,z);fill.range=45;fill.intensity=12;fill.color=new Color(.76f,.84f,1);fill.shadows=LightShadows.None;}
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.50f,.56f,.65f);RenderSettings.ambientEquatorColor=new Color(.33f,.36f,.40f);RenderSettings.ambientGroundColor=new Color(.12f,.11f,.10f);
            PrefabUtility.SaveAsPrefabAsset(character,Root+"/Paladin_R3_FirstPass.prefab");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/BossTests/Knight_Sanctuary_R3_Preview.unity");
            var renderers=environment.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            if(bounds.size.x<60 || bounds.size.x>80 || bounds.size.y<30 || bounds.size.y>45)throw new Exception("R3 imported scale invalid: "+bounds.size);
            string report="Stage: first engine import, NOT final acceptance\nEnvironment bounds: "+bounds+"\nEnvironment renderers: "+renderers.Length+"\nCharacter skinned meshes: "+character.GetComponentsInChildren<SkinnedMeshRenderer>().Length+"\n"+string.Join("\n",clips.Select(c=>c.name+" duration="+c.length));
            File.WriteAllText(Output+"/UnityImportReport.txt",report);Capture(camera,Output+"/Unity_Character_FirstPass.png");
            camera.transform.position=new Vector3(0,2,-30);camera.transform.LookAt(new Vector3(0,12,20));camera.fieldOfView=70;Capture(camera,Output+"/Unity_Hall_FirstPass.png");
            Debug.Log("R3_IMPORT_COMPLETE\n"+report);
        }
        static void Capture(Camera camera,string path)
        {
            var rt=RenderTexture.GetTemporary(1280,800,24);var prior=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);
        }
    }
}
