using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ArcaneArchive.Player;

namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class MilanV6SceneBuilder
    {
        const string A = "Assets/Art/MilanLibraryV6";
        const string ScenePath = "Assets/Scenes/MilanLibrary_Magic_V6.unity";
        const string D = "Docs/MilanV6Unity";
        [Serializable] public sealed class Mat {public string name,baseTexture,normalTexture,emissionTexture;public float[] color,emission;public float metallic,roughness,strength;}
        [Serializable] public sealed class Chunk {public string file,name,group,collisionRole;public bool collisionOnly;public int material,animation,vertices,triangles;public string[] sources;}
        [Serializable] public sealed class Anim {public string name;public MilanMagicMotion.Pose[] poses;}
        [Serializable] public sealed class Lamp {public string name,type;public float[] position,forward,color;public float energy,size;}
        [Serializable] public sealed class View {public string name;public float[] position,forward,up;public float fov;}
        [Serializable] public sealed class Manifest {public string repairRevision;public int formatVersion,sourceObjects,triangleCount;public Mat[] materials;public Chunk[] chunks;public Anim[] animations;public Lamp[] lights;public View[] cameras;}
        static double nextPoll;
        static MilanV6SceneBuilder() {EditorApplication.update += Poll; Application.logMessageReceived += LogError;}
        static void LogError(string condition,string trace,LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            Directory.CreateDirectory(D);
            File.AppendAllText(D+"/console_errors.log",DateTime.Now+" "+condition+"\n"+trace+"\n");
        }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll=EditorApplication.timeSinceStartup+2;
            string request="Tools/MilanV6/editor_request.txt";
            if (!File.Exists(request)) return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);
            try
            {
                switch(command)
                {
                    case "build":Build();break;
                    case "capture":Capture();break;
                    case "validate":Validate();break;
                    case "play":EditorApplication.isPlaying=true;break;
                    case "stop":EditorApplication.isPlaying=false;break;
                    case "open":EditorSceneManager.OpenScene(ScenePath);FrameView();break;
                    case "refine":Refine();break;
                    case "playcheck":PlayCheck();break;
                }
                File.WriteAllText(D+"/last_command.json",JsonUtility.ToJson(new CommandResult {command=command,success=true,time=DateTime.Now.ToString("O")},true));
            }
            catch(Exception e) {Debug.LogException(e);File.WriteAllText(D+"/last_command.json",JsonUtility.ToJson(new CommandResult {command=command,success=false,error=e.ToString()},true));}
        }
        [Serializable] class CommandResult {public string command,time,error;public bool success;}
        static Vector3 V(float[] a) => new Vector3(a[0],a[1],a[2]);
        static Color Col(float[] a) => new Color(a[0],a[1],a[2],a.Length>3?a[3]:1);
        static Manifest Read() => JsonUtility.FromJson<Manifest>(File.ReadAllText(A+"/manifest.json"));
        [MenuItem("Tools/Milan Library/Build V6 imported scene")]
        public static void Build()
        {
            var m=Read();
            if(m.formatVersion<2 || m.repairRevision!="walkability_v1")
                throw new InvalidOperationException("Run the approved native geometry repair and collision-aware exporter before building the repaired scene.");
            Directory.CreateDirectory(A+"/Materials");Directory.CreateDirectory(A+"/Settings");Directory.CreateDirectory(D);
            // Keep any user scene in memory. The new scene is created additively first.
            var previous=SceneManager.GetActiveScene();
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var open=SceneManager.GetSceneAt(i);
                if(open.path==ScenePath && open.isDirty)
                    throw new InvalidOperationException("Save the existing V6 scene changes before rebuilding.");
            }
            bool untitled=previous.IsValid() && string.IsNullOrEmpty(previous.path);
            if(untitled && previous.isDirty)
                throw new InvalidOperationException("Save the untitled scene before rebuilding V6.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,untitled && SceneManager.sceneCount==1?NewSceneMode.Single:NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SetupPipeline();
            var mats=new Material[m.materials.Length];
            for(int i=0;i<mats.Length;i++)
            {
                var d=m.materials[i];string path=A+"/Materials/"+d.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetColor("_BaseColor",Col(d.color).gamma);mat.SetFloat("_Metallic",d.metallic);mat.SetFloat("_Smoothness",1-Mathf.Clamp(d.roughness,.24f,.9f));
                mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;mat.enableInstancing=true;
                if(!string.IsNullOrEmpty(d.baseTexture))mat.SetTexture("_BaseMap",Texture(d.baseTexture,false));
                if(!string.IsNullOrEmpty(d.normalTexture)){mat.SetTexture("_BumpMap",Texture(d.normalTexture,true));mat.SetFloat("_BumpScale",.32f);mat.EnableKeyword("_NORMALMAP");}
                if(d.strength>.001f){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",(Col(d.emission)*Mathf.Min(d.strength,10)).gamma);}
                mat.SetTexture("_EmissionMap",string.IsNullOrEmpty(d.emissionTexture)?null:Texture(d.emissionTexture,false));
                mat.globalIlluminationFlags=d.strength>.001f?MaterialGlobalIlluminationFlags.BakedEmissive:MaterialGlobalIlluminationFlags.None;
                mats[i]=mat;EditorUtility.SetDirty(mat);
            }
            var root=new GameObject("Milan Library — Magic V6");var groups=new Dictionary<string,Transform>();
            var animations=new Transform[m.animations.Length];
            for(int i=0;i<animations.Length;i++)
            {
                var go=new GameObject(m.animations[i].name);go.transform.SetParent(root.transform,false);
                var motion=go.AddComponent<MilanMagicMotion>();motion.poses=m.animations[i].poses;animations[i]=go.transform;
            }
            int count=0;
            foreach(var c in m.chunks)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(A+"/Meshes/"+c.file);
                if(!mesh)throw new InvalidOperationException("Missing imported mesh: "+c.file);
                if(!groups.TryGetValue(c.group,out var parent)){parent=new GameObject(c.group).transform;parent.SetParent(root.transform,false);groups[c.group]=parent;}
                var go=new GameObject(c.name+"_"+count++);go.transform.SetParent(c.animation>=0?animations[c.animation]:parent,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                if(!c.collisionOnly)
                {
                    var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mats[c.material];
                    renderer.shadowCastingMode=m.materials[c.material].strength>1?ShadowCastingMode.Off:ShadowCastingMode.On;
                }
                if(c.animation<0)GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
                // New exports carry explicit physical roles, including invisible ramp/rail proxies.
                string role = c.collisionRole;
                if(m.formatVersion < 2)
                    role = c.animation<0 && new[]{"Structure","Gallery","Stairs","Exterior","V3_ReferenceExterior","Shelves","IV4_W02","IV4_W03"}.Contains(c.group) ? "walkable" : "none";
                if(role != "none" && role != "walkable" && role != "barrier")
                    throw new InvalidOperationException("Invalid collision role for " + c.name);
                if(c.collisionOnly && role == "none")throw new InvalidOperationException("Invisible proxy without collision role: " + c.name);
                if(role != "none")
                {
                    if(c.animation >= 0)throw new InvalidOperationException("Static collider on animated mesh: " + c.name);
                    go.AddComponent<MeshCollider>().sharedMesh=mesh;
                    if(role == "walkable")go.AddComponent<GroundSurface>();
                }
            }
            SetupLights(m,root.transform);
            var camObject=new GameObject("Main Camera");var cam=camObject.AddComponent<Camera>();cam.tag="MainCamera";cam.nearClipPlane=.08f;cam.farClipPlane=250;cam.allowHDR=true;
            cam.backgroundColor=new Color(.12f,.16f,.20f);cam.clearFlags=CameraClearFlags.SolidColor;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            cam.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cam.GetUniversalAdditionalCameraData().antialiasingQuality=AntialiasingQuality.High;
            camObject.AddComponent<AudioListener>();
            // Display cameras remain in the manifest for Capture; never inherit their player eye pose.
            cam.fieldOfView=65f;
            var player=new GameObject("Visitor — WASD + Mouse");player.transform.position=new Vector3(0,.08f,7.8f);
            var cc=player.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.28f;cc.center=new Vector3(0,.88f,0);cc.stepOffset=.28f;
            var controller=player.AddComponent<FirstPersonController>();
            cam.transform.SetParent(player.transform,false);
            cam.transform.localPosition=new Vector3(0,1.60f,0);
            cam.transform.localRotation=Quaternion.identity;
            var so=new SerializedObject(controller);so.FindProperty("inputActions").objectReferenceValue=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");so.FindProperty("cameraPivot").objectReferenceValue=cam.transform;so.ApplyModifiedPropertiesWithoutUndo();
            MilanFirstPersonSetup.Attach(player,cam);
            SetupVolume();
            MilanPerformanceSetup.ConfigureScene();
            player.AddComponent<ArcaneArchive.UI.RecoveryMenu>();
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.80f,.82f,.80f);RenderSettings.ambientEquatorColor=new Color(.55f,.50f,.41f);RenderSettings.ambientGroundColor=new Color(.28f,.23f,.16f);RenderSettings.ambientIntensity=1;
            RenderSettings.fog=false;RenderSettings.reflectionIntensity=.7f;MakeEnvironment();
            // A loaded scene at the same path prevents SaveScene from overwriting it.
            // Close only the clean old target after the replacement is fully built.
            for(int i=SceneManager.sceneCount-1;i>=0;i--)
            {
                var open=SceneManager.GetSceneAt(i);
                if(open!=scene && open.path==ScenePath)
                {
                    if(open.isDirty)throw new InvalidOperationException("Existing V6 scene changed during rebuild.");
                    EditorSceneManager.CloseScene(open,true);
                }
            }
            if(!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new InvalidOperationException("Could not save rebuilt V6 scene: "+ScenePath);
            AssetDatabase.SaveAssets();
            // Close only clean previous scenes, preserving any unsaved user work.
            if(previous.IsValid() && !previous.isDirty)EditorSceneManager.CloseScene(previous,true);
            if(!Application.isBatchMode)FrameView();Validate();
            Debug.Log("Milan V6 imported: "+m.sourceObjects+" source objects, "+m.chunks.Length+" renderer batches, "+m.triangleCount+" triangles.");
        }
        static Texture2D Texture(string file,bool normal)
        {
            string path=A+"/Textures/"+file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer && (normal && importer.textureType!=TextureImporterType.NormalMap || !normal && importer.textureType!=TextureImporterType.Default))
            {importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.maxTextureSize=2048;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void SetupPipeline()
        {
            string path=A+"/Settings/MilanV6_URP.asset";
            var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if(!pipe)
            {
                var current=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if(!current)throw new InvalidOperationException("The project requires an existing URP pipeline.");
                File.WriteAllText(D+"/previous_pipeline.json","{\"graphics\":\""+AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline)+"\",\"quality\":\""+AssetDatabase.GetAssetPath(QualitySettings.renderPipeline)+"\"}");
                pipe=UnityEngine.Object.Instantiate(current);AssetDatabase.CreateAsset(pipe,path);
                var renderer=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Library3D/Library3DRenderer.asset"));
                var rso=new SerializedObject(renderer);var mode=rso.FindProperty("m_RenderingMode");if(mode!=null)mode.intValue=2;rso.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(renderer,A+"/Settings/MilanV6_Renderer.asset");
                var so=new SerializedObject(pipe);var list=so.FindProperty("m_RendererDataList");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.FindProperty("m_DefaultRendererIndex").intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
            }
            pipe.supportsHDR=true;pipe.msaaSampleCount=2;pipe.renderScale=1;pipe.shadowDistance=65;
            GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;EditorUtility.SetDirty(pipe);
        }
        static void SetupLights(Manifest m,Transform root)
        {
            var group=new GameObject("V6 realtime lighting").transform;group.SetParent(root,false);
            foreach(var d in m.lights)
            {
                var go=new GameObject(d.name);go.transform.SetParent(group,false);go.transform.position=V(d.position);go.transform.rotation=Quaternion.LookRotation(V(d.forward));
                var l=go.AddComponent<Light>();l.color=Col(d.color).gamma;l.type=d.type=="AREA"?LightType.Spot:LightType.Point;
                l.range=d.type=="AREA"?35:Mathf.Clamp(Mathf.Sqrt(d.energy)*.7f,3,23);
                l.intensity=d.energy/80f;l.spotAngle=140;l.innerSpotAngle=80;l.shadows=LightShadows.None;
                if(d.name.StartsWith("Exterior")){l.range=110;l.intensity=d.energy/140;l.spotAngle=115;}
                if(d.name.StartsWith("Emblem")){l.intensity=8;l.range=18;}
            }
            var sun=new GameObject("Exterior daylight").AddComponent<Light>();sun.transform.SetParent(group,false);sun.type=LightType.Directional;sun.intensity=.6f;sun.color=new Color(1,.91f,.8f);sun.transform.rotation=Quaternion.Euler(48,-28,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.05f;sun.shadowNormalBias=.12f;RenderSettings.sun=sun;
        }
        static void SetupVolume()
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(A+"/Settings/MilanV6_Volume.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,A+"/Settings/MilanV6_Volume.asset");}
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.intensity.Override(.32f);bloom.threshold.Override(1.25f);bloom.scatter.Override(.6f);
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.35f);color.contrast.Override(8);color.saturation.Override(-4);
            var volumes=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Where(v=>v.name=="V6 warm magic grading").ToArray();
            var volume=volumes.FirstOrDefault()??new GameObject("V6 warm magic grading").AddComponent<Volume>();
            foreach(var duplicate in volumes.Skip(1))UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            volume.isGlobal=true;volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
        }
        static void MakeEnvironment()
        {
            string path=A+"/Settings/V6_InteriorReflection.cubemap";var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(!cube)
            {
                cube=new Cubemap(32,TextureFormat.RGBAHalf,true);cube.name="V6 warm interior environment";
                for(int face=0;face<6;face++)
                {
                    var pixels=new Color[1024];for(int i=0;i<pixels.Length;i++){float t=(i/32)/31f;pixels[i]=Color.Lerp(new Color(.035f,.022f,.013f),new Color(.38f,.30f,.21f),t);if(face==2)pixels[i]*=1.4f;}
                    cube.SetPixels(pixels,(CubemapFace)face);
                }
                cube.Apply(true);AssetDatabase.CreateAsset(cube,path);
            }
            RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=cube;
        }
        static void SetView(Camera c,View v){c.transform.position=V(v.position);c.transform.rotation=Quaternion.LookRotation(V(v.forward),V(v.up));c.fieldOfView=v.fov;}
        static void FrameView()
        {
            var c=Camera.main;if(!c)return;
            var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();view.in2DMode=false;view.orthographic=false;view.sceneLighting=true;view.LookAtDirect(c.transform.position+c.transform.forward*12,c.transform.rotation,12);view.Repaint();
            var gameType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            if(gameType!=null){var game=EditorWindow.GetWindow(gameType);game.position=new Rect(260,100,1150,740);game.Show();game.Focus();}
        }
        [MenuItem("Tools/Milan Library/Capture V6 views")]
        public static void Capture()
        {
            var m=Read();var c=Camera.main;if(!c)throw new InvalidOperationException("No main camera");
            var pos=c.transform.position;var rot=c.transform.rotation;float fov=c.fieldOfView;var old=c.targetTexture;var active=RenderTexture.active;
            var viewmodel=c.GetComponentInChildren<FirstPersonViewModel>(true);
            bool modelWasActive=viewmodel && viewmodel.gameObject.activeSelf;
            if(viewmodel)viewmodel.gameObject.SetActive(false);
            Directory.CreateDirectory(D+"/Evidence");
            try
            {
                foreach(var v in m.cameras.Where(v=>v.name=="CAM_V6_Overview"||v.name=="CAM_V6_Workzones"||v.name=="CAM_V6_Emblem"||v.name=="CAM_V3_FacadeDepthOblique"))
                {
                    SetView(c,v);var rt=new RenderTexture(1600,1067,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();c.targetTexture=rt;c.Render();RenderTexture.active=rt;
                    var tex=new Texture2D(1600,1067,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1067),0,0);tex.Apply();File.WriteAllBytes(D+"/Evidence/"+v.name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                }
            }
            finally{if(viewmodel)viewmodel.gameObject.SetActive(modelWasActive);c.transform.SetPositionAndRotation(pos,rot);c.fieldOfView=fov;c.targetTexture=old;RenderTexture.active=active;}
        }
        [Serializable] class Validation {public string scene,pipeline;public int renderers,missingMeshes,missingMaterials,unsupportedShaders,missingScripts,colliders,lights,animations;public long triangles;public bool camera,player,inputAsset;}
        public static void Validate()
        {
            var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
            var meshes=all.SelectMany(o=>o.GetComponents<MeshFilter>()).ToArray();var renderers=all.SelectMany(o=>o.GetComponents<MeshRenderer>()).ToArray();
            var v=new Validation{scene=scene.path,pipeline=GraphicsSettings.currentRenderPipeline?.name,renderers=renderers.Length,missingMeshes=meshes.Count(f=>!f.sharedMesh),missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m)),unsupportedShaders=renderers.Sum(r=>r.sharedMaterials.Count(m=>m&&!m.shader.isSupported)),missingScripts=all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),colliders=all.Sum(o=>o.GetComponents<Collider>().Length),lights=all.Sum(o=>o.GetComponents<Light>().Length),animations=all.Sum(o=>o.GetComponents<MilanMagicMotion>().Length),triangles=meshes.Where(f=>f.sharedMesh).Sum(f=>(long)f.sharedMesh.GetIndexCount(0)/3),camera=Camera.main!=null,player=all.Any(o=>o.GetComponent<FirstPersonController>()),inputAsset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions")!=null};
            File.WriteAllText(D+"/unity_validation.json",JsonUtility.ToJson(v,true));
        }
        static void Refine()
        {
            var m=Read();
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(A+"/Settings/MilanV6_Renderer.asset");
            var source=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Library3D/Library3DRenderer.asset");
            var so=new SerializedObject(renderer);var src=new SerializedObject(source);
            so.FindProperty("postProcessData").objectReferenceValue=src.FindProperty("postProcessData").objectReferenceValue;so.ApplyModifiedPropertiesWithoutUndo();renderer.SetDirty();
            foreach(var d in m.materials)
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(A+"/Materials/"+d.name+".mat");
                mat.SetColor("_BaseColor",Col(d.color).gamma);
                mat.globalIlluminationFlags=d.strength>.001f?MaterialGlobalIlluminationFlags.BakedEmissive:MaterialGlobalIlluminationFlags.None;
                if(d.strength>.001f){mat.SetColor("_EmissionColor",(Col(d.emission)*Mathf.Min(d.strength,10)).gamma);mat.EnableKeyword("_EMISSION");}
                EditorUtility.SetDirty(mat);
            }
            var lamps=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach(var d in m.lights){var l=lamps.FirstOrDefault(x=>x.name==d.name);if(l)l.color=Col(d.color).gamma;}
            RenderSettings.ambientSkyColor=new Color(.80f,.82f,.80f);RenderSettings.ambientEquatorColor=new Color(.55f,.50f,.41f);RenderSettings.ambientGroundColor=new Color(.28f,.23f,.16f);
            var cameraData=Camera.main.GetUniversalAdditionalCameraData();cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;cameraData.antialiasingQuality=AntialiasingQuality.High;
            DynamicGI.UpdateEnvironment();
            SetupVolume();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();FrameView();
        }
        [Serializable] class PlayResult {public bool isPlaying;public int frame;public float time;public Vector3 playerPosition;public int animatedObjects;public float animationMotion;public string[] shaderKeywords;}
        static void PlayCheck()
        {
            var motion=UnityEngine.Object.FindObjectsByType<MilanMagicMotion>(FindObjectsSortMode.None);
            float amount=motion.Sum(x=>x.transform.localPosition.magnitude+Quaternion.Angle(Quaternion.identity,x.transform.localRotation));
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            var mat=AssetDatabase.LoadAssetAtPath<Material>(A+"/Materials/V5_Magic_StarBlue.mat");
            var result=new PlayResult{isPlaying=EditorApplication.isPlaying,frame=Time.frameCount,time=Time.timeSinceLevelLoad,playerPosition=player?player.transform.position:Vector3.zero,animatedObjects=motion.Length,animationMotion=amount,shaderKeywords=mat.shaderKeywords};
            File.WriteAllText(D+"/play_check_"+(EditorApplication.isPlaying?"running":"editor")+".json",JsonUtility.ToJson(result,true));Validate();
        }
    }
}
