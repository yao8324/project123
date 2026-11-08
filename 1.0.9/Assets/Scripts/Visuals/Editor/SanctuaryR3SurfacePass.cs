using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
    public static class SanctuaryR3SurfacePass
    {
        const string Root="Assets/Art/Bosses/Knight/SanctuaryR3";
        static T Save<T>(T value,string path) where T:Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);return old;}AssetDatabase.CreateAsset(value,path);return value;}
        [MenuItem("Tools/Knight/R3/Apply Material Lighting Pass")]
        public static void Run()
        {
            int oldAA=QualitySettings.antiAliasing;var oldGraphics=GraphicsSettings.defaultRenderPipeline;var oldQuality=QualitySettings.renderPipeline;
            try{
                Directory.CreateDirectory(Root+"/Lighting");AssetDatabase.Refresh();
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/BossTests/Knight_Sanctuary_R3_Preview.unity");
                foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"})){
                    var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));string n=m.name;
                    if(n.Contains("Gold")||n.Contains("Brass")){m.SetColor("_BaseColor",new Color(.72f,.46f,.19f));m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",.64f);m.SetTexture("_BaseMap",null);}
                    if(n.Contains("Bluegrey")||n.Contains("FloorVariation")){m.SetColor("_BaseColor",new Color(.90f,.94f,1f));m.SetFloat("_Smoothness",.26f);}
                    if(n.Contains("Ivory_Marble")){m.SetColor("_BaseColor",new Color(.88f,.84f,.74f));m.SetFloat("_Smoothness",.34f);}
                    if(n.Contains("Armor_Ivory")){m.SetColor("_BaseColor",new Color(.74f,.72f,.64f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.45f);}
                    if(n.Contains("Gunmetal")||n.Contains("ShadowSteel")){m.SetColor("_BaseColor",new Color(.13f,.15f,.18f));m.SetFloat("_Metallic",.85f);m.SetFloat("_Smoothness",.42f);}
                    if(n.Contains("Limestone")){m.SetColor("_BaseColor",new Color(.88f,.85f,.79f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.22f);}
                    string stem=n.Contains("Limestone")?"Limestone":n.Contains("Ivory_Marble")?"Marble":n.Contains("Bluegrey")||n.Contains("FloorVariation")?"Slate":n.Contains("Gold")||n.Contains("Brass")?"Brass":null;
                    if(stem!=null){var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Bosses/Knight/Sanctuary20261107/Textures/"+stem+"_Normal.png");m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",stem=="Brass"?.08f:.12f);m.EnableKeyword("_NORMALMAP");}
                    if((n.Contains("Wood")||n.Contains("Walnut")||n.Contains("Leather"))&&m.GetTexture("_BaseMap")){m.SetColor("_BaseColor",Color.white);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.35f);}
                    // R3-specific maps carry their own base color and smoothness; avoid multiplying dark metal twice.
                    string revised=n.Contains("Ivory_Marble")?"Marble":n.Contains("Bluegrey")||n.Contains("FloorVariation")?"Slate":n.Contains("Gold")||n.Contains("Brass")?"Gold":n.Contains("Gunmetal")?"GateSteel":null;
                    if(revised!=null){
                        m.SetTexture("_BaseMap",LoadMap(revised,"Albedo"));m.SetColor("_BaseColor",Color.white);
                        m.SetTexture("_MetallicGlossMap",LoadMap(revised,"Mask"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);m.SetFloat("_SmoothnessTextureChannel",0);m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                        m.SetTexture("_BumpMap",LoadMap(revised,"Normal"));m.SetFloat("_BumpScale",revised=="GateSteel"?.35f:.15f);m.EnableKeyword("_NORMALMAP");
                    }
                    EditorUtility.SetDirty(m);
                }
                var existing=GameObject.Find("R3_Atmosphere");if(existing)Object.DestroyImmediate(existing);
                var root=new GameObject("R3_Atmosphere");
                var source=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Art/MilanLibraryV6/Settings/MilanV6_URP.asset");
                var pipe=Object.Instantiate(source);pipe.name="R3 Sanctuary URP";pipe.renderScale=1;pipe.msaaSampleCount=4;pipe.shadowDistance=80;pipe.supportsHDR=true;
                var settings=new SerializedObject(pipe);settings.FindProperty("m_ReflectionProbeBlending").boolValue=true;settings.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;settings.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=8;settings.ApplyModifiedPropertiesWithoutUndo();pipe=Save(pipe,Root+"/Lighting/R3URP.asset");
                GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;
                var owner=root.AddComponent<KnightSanctuaryLighting>();owner.pipeline=pipe;
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);var adjust=profile.Add<ColorAdjustments>();adjust.postExposure.Override(.15f);adjust.saturation.Override(-3);
                profile=Save(profile,Root+"/Lighting/R3Volume.asset");foreach(var c in profile.components)if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,profile);
                var volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=40;volume.sharedProfile=profile;
                foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){
                    if(light.name=="R3_InteriorFill"){light.intensity=120;light.color=new Color(.74f,.83f,1);light.range=48;}
                    if(light.type==LightType.Directional){light.intensity=.75f;light.shadows=LightShadows.Soft;light.shadowBias=.04f;light.shadowNormalBias=.2f;}
                }
                for(int side=-1;side<=1;side+=2)for(int z=-20;z<=28;z+=12){var l=new GameObject("R3_WarmMaintenanceFill").AddComponent<Light>();l.transform.SetParent(root.transform);l.transform.position=new Vector3(side*31.2f,3.15f,z);l.type=LightType.Point;l.range=7;l.intensity=12;l.color=new Color(1,.43f,.15f);l.shadows=LightShadows.None;}
                foreach(var p in new[]{new Vector3(-5,6,-12),new Vector3(5,6,10),new Vector3(0,8,28)}){var fill=new GameObject("R3_NaveBounceFill").AddComponent<Light>();fill.transform.SetParent(root.transform);fill.transform.position=p;fill.type=LightType.Point;fill.range=35;fill.intensity=180;fill.color=new Color(.94f,.91f,.83f);fill.shadows=LightShadows.None;}
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.5f,.52f,.55f);RenderSettings.ambientEquatorColor=new Color(.31f,.30f,.29f);RenderSettings.ambientGroundColor=new Color(.23f,.22f,.21f);RenderSettings.reflectionIntensity=1;
                var probe=new GameObject("R3_RoomReflection").AddComponent<ReflectionProbe>();probe.transform.SetParent(root.transform);probe.transform.position=new Vector3(0,8,0);probe.size=new Vector3(66,38,74);probe.center=new Vector3(0,10,0);probe.boxProjection=true;probe.intensity=1;probe.resolution=128;probe.mode=ReflectionProbeMode.Baked;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.20f,.22f,.25f);probe.nearClipPlane=.1f;probe.farClipPlane=120;
                string probePath=Root+"/Lighting/R3RoomReflection.exr";
                if(!Lightmapping.BakeReflectionProbe(probe,probePath))throw new Exception("Reflection probe bake failed");AssetDatabase.ImportAsset(probePath,ImportAssetOptions.ForceSynchronousImport);probe.bakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(probePath);
                var camera=Object.FindFirstObjectByType<SanctuaryR3Preview>().view;camera.allowHDR=true;camera.allowMSAA=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.FastApproximateAntialiasing;
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                camera.transform.position=new Vector3(0,2,-30);camera.transform.LookAt(new Vector3(0,12,20));camera.fieldOfView=70;Capture(camera,"Unity_Hall_SurfaceR3.png");
                camera.transform.position=new Vector3(3,2.5f,6);camera.transform.LookAt(new Vector3(0,1.6f,0));camera.fieldOfView=48;Capture(camera,"Unity_Knight_SurfaceR3.png");
                camera.transform.position=new Vector3(-24,3,-18);camera.transform.LookAt(new Vector3(-31.4f,3,-12));camera.fieldOfView=60;Capture(camera,"Unity_Niche_SurfaceR5.png");
                camera.transform.position=new Vector3(5,8,23);camera.transform.LookAt(new Vector3(0,9,35));camera.fieldOfView=60;Capture(camera,"Unity_Gate_SurfaceR5.png");
                camera.transform.position=new Vector3(-24,15,-18);camera.transform.LookAt(new Vector3(-29.4f,14.7f,-12));camera.fieldOfView=60;Capture(camera,"Unity_Gallery_SurfaceR6.png");
                camera.transform.position=new Vector3(.8f,2.1f,3);camera.transform.LookAt(new Vector3(0,1.8f,0));camera.fieldOfView=28;Capture(camera,"Unity_Knight_DetailR7.png");
                File.WriteAllText("Docs/SanctuaryR3Build/SurfacePass.txt","R3 material/lighting pass; not final visual acceptance.\nR5 maps: sparse warped mineral veins; metallic R and smoothness A masks; albedo sRGB and mask linear; gate and wood avoid double dark tint. Scene-space baked reflection cubemap, dedicated URP 4xMSAA and ACES.\nLighting values are renderer settings, not verified scene lux.\n");Debug.Log("R3_SURFACE_PASS_COMPLETE");
            }finally{GraphicsSettings.defaultRenderPipeline=oldGraphics;QualitySettings.renderPipeline=oldQuality;QualitySettings.antiAliasing=oldAA;}
        }
        static Texture2D LoadMap(string stem,string kind)
        {
            string path=Root+"/Textures/"+stem+"_"+kind+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Missing R3 map "+path);
            bool normal=kind=="Normal",srgb=kind=="Albedo";
            if(importer.sRGBTexture!=srgb||importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)||importer.wrapMode!=TextureWrapMode.Repeat){
                importer.sRGBTexture=srgb;importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void Capture(Camera camera,string name)
        {
            var rt=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,4);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes("Docs/SanctuaryR3Build/"+name,tex.EncodeToPNG());Object.DestroyImmediate(tex);camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
        }
    }
}
