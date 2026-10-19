using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ArcaneArchive.Player;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Run in an independent archive project with Unity -executeMethod.</summary>
    public static class MilanArchiveValidation
    {
        const string ScenePath = "Assets/Scenes/MilanLibrary_Magic_V6.unity";
        const string Output = "Docs/MilanV6Unity/archive_validation.json";
        [Serializable] sealed class Result
        {
            public bool pass, latestCrouchInput, latestCrouchController, magicAnimationChanges, glowEnabled;
            public bool collisionContract, playerEyeConfigured, handAnimationSetup, handOverlayConfigured;public int collisionMismatches,unsupportedShaders;public int expectedRendererCount,expectedAnimationCount,handRenderers;public long expectedTriangles;
            public string unityVersion, scene;
            public int rendererCount, animationCount, dependencyCount, missingScripts, missingMaterials, missingMeshes;
            public long triangles;
            public string[] missingDependencies;
            public string note;
        }
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Art/MilanLibraryV6/Settings/MilanV6_URP.asset");
            if(!pipe) throw new InvalidOperationException("Missing V6 URP asset");
            var manifest=JsonUtility.FromJson<MilanV6SceneBuilder.Manifest>(File.ReadAllText("Assets/Art/MilanLibraryV6/manifest.json"));
            var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
            var renderers=all.SelectMany(g=>g.GetComponents<MeshRenderer>()).ToArray();
            var filters=all.SelectMany(g=>g.GetComponents<MeshFilter>()).ToArray();
            var skinned=all.SelectMany(g=>g.GetComponents<SkinnedMeshRenderer>()).ToArray();
            var allRenderers=all.SelectMany(g=>g.GetComponents<Renderer>()).ToArray();
            var motions=all.SelectMany(g=>g.GetComponents<MilanMagicMotion>()).ToArray();
            var deps=AssetDatabase.GetDependencies(ScenePath,true);
            var input=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            var player=all.Select(g=>g.GetComponent<FirstPersonController>()).First(x=>x);
            bool latestController=new SerializedObject(player).FindProperty("crouchHeight")!=null;
            // Sample every environment animation and restore its original pose, without saving the scene.
            bool changes=motions.Length>0;
            foreach(var motion in motions)
            {
                var position=motion.transform.localPosition;var rotation=motion.transform.localRotation;var scale=motion.transform.localScale;
                try
                {
                    motion.Sample(0);var initial=motion.transform.localToWorldMatrix;bool thisChanges=false;
                    foreach(float t in new[]{.125f,.75f,2.75f,6.125f}){motion.Sample(t);thisChanges|=!initial.Equals(motion.transform.localToWorldMatrix);}
                    changes &= thisChanges;
                }
                finally{motion.transform.localPosition=position;motion.transform.localRotation=rotation;motion.transform.localScale=scale;}
            }
            var camera=player.GetComponentInChildren<Camera>();
            int collisionMismatches=0;
            for(int i=0;i<manifest.chunks.Length;i++)
            {
                var chunk=manifest.chunks[i];var go=all.FirstOrDefault(x=>x.name==chunk.name+"_"+i);
                if(!go){collisionMismatches++;continue;}
                bool needsCollider=chunk.collisionRole=="walkable"||chunk.collisionRole=="barrier";
                var collider=go.GetComponent<MeshCollider>();
                if((collider!=null)!=needsCollider)collisionMismatches++;
                if(needsCollider && collider)
                {
                    var expectedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/MilanLibraryV6/Meshes/"+chunk.file);
                    var filter=go.GetComponent<MeshFilter>();
                    if(!collider.enabled || collider.isTrigger || collider.convex || !expectedMesh || !filter || collider.sharedMesh!=expectedMesh || filter.sharedMesh!=expectedMesh)collisionMismatches++;
                }
                if((go.GetComponent<MeshRenderer>()!=null)==chunk.collisionOnly)collisionMismatches++;
                if((go.GetComponent<GroundSurface>()!=null)!=(chunk.collisionRole=="walkable"))collisionMismatches++;
            }
            var viewmodel=player.GetComponentInChildren<FirstPersonViewModel>(true);
            var animator=viewmodel?viewmodel.GetComponentInChildren<Animator>(true):null;
            var handController=animator?animator.runtimeAnimatorController as AnimatorController:null;
            bool handSetup=viewmodel && viewmodel.enabled && animator && animator.enabled && handController && handController.layers.Length>0 && !animator.applyRootMotion && animator.avatar && animator.avatar.isValid;
            handSetup &= skinned.All(r=>r.sharedMesh && r.rootBone && r.bones.Length>0 && r.bones.All(b=>b) && r.sharedMaterials.Length==r.sharedMesh.subMeshCount);
            if(handSetup)
            {
                var viewSerialized=new SerializedObject(viewmodel);
                handSetup=viewSerialized.FindProperty("movement").objectReferenceValue==player && viewSerialized.FindProperty("viewCamera").objectReferenceValue==camera && viewSerialized.FindProperty("animator").objectReferenceValue==animator;
                var states=handController.layers[0].stateMachine.states;
                handSetup &= new[]{"Idle","Walk","Sprint","Jump","Land","Crouch"}.All(name=>states.Count(st=>st.state.name==name && st.state.motion is AnimationClip)==1);
            }
            var overlay=player.GetComponentsInChildren<Camera>(true).FirstOrDefault(c=>c.name=="FirstPersonPresentationCamera");
            int handLayer=LayerMask.NameToLayer("FirstPersonViewModel");
            var baseCameraData=camera?camera.GetComponent<UniversalAdditionalCameraData>():null;
            var overlayData=overlay?overlay.GetComponent<UniversalAdditionalCameraData>():null;
            var cameraStack=baseCameraData?baseCameraData.cameraStack:null;
            bool overlayBinding=viewmodel && new SerializedObject(viewmodel).FindProperty("presentationCamera").objectReferenceValue==overlay;
            bool handOverlay=camera && overlay && overlay.enabled && overlay.gameObject.activeInHierarchy && overlayBinding && handLayer>=0 && overlay.transform.parent==camera.transform && overlay.transform.localPosition.sqrMagnitude<.000001f && Quaternion.Angle(overlay.transform.localRotation,Quaternion.identity)<.01f && Vector3.Distance(overlay.transform.localScale,Vector3.one)<.001f && overlayData && overlayData.renderType==CameraRenderType.Overlay && overlayData.clearDepth && cameraStack!=null && cameraStack.Contains(overlay) && (camera.cullingMask & (1<<handLayer))==0 && overlay.cullingMask==(1<<handLayer) && skinned.All(r=>r.gameObject.layer==handLayer);
            var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilanLibraryV6/Materials/V5_Magic_StarBlue.mat");
            var result=new Result {
                unityVersion=Application.unityVersion,scene=ScenePath,rendererCount=renderers.Length,animationCount=motions.Length,
                expectedRendererCount=manifest.chunks.Count(c=>!c.collisionOnly),expectedAnimationCount=manifest.animations.Length,expectedTriangles=manifest.triangleCount,
                collisionContract=manifest.formatVersion>=2 && manifest.repairRevision=="walkability_v1",collisionMismatches=collisionMismatches,playerEyeConfigured=camera&&camera.transform.parent==player.transform&&Vector3.Distance(camera.transform.localPosition,new Vector3(0,1.60f,0))<.001f&&Quaternion.Angle(camera.transform.localRotation,Quaternion.identity)<.1f&&Mathf.Abs(camera.fieldOfView-65f)<.01f,
                handAnimationSetup=handSetup,handOverlayConfigured=handOverlay,unsupportedShaders=allRenderers.Sum(r=>r.sharedMaterials.Count(mat=>mat&&(!mat.shader||!mat.shader.isSupported))),
                handRenderers=player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,
                dependencyCount=deps.Length,missingDependencies=deps.Where(p=>!File.Exists(p)&&!Directory.Exists(p)&&!p.StartsWith("Packages/")).ToArray(),
                missingScripts=all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),
                missingMaterials=allRenderers.Sum(r=>r.sharedMaterials.Count(m=>!m)),missingMeshes=filters.Count(f=>!f.sharedMesh)+skinned.Count(r=>!r.sharedMesh),
                triangles=filters.Where(f=>f.sharedMesh).Sum(f=>(long)f.sharedMesh.GetIndexCount(0)/3),
                latestCrouchInput=input&&input.FindAction("Player/Crouch")!=null,latestCrouchController=latestController,
                magicAnimationChanges=changes,glowEnabled=glow&&glow.IsKeywordEnabled("_EMISSION"),
                note="Editor structural checks only. All environment animations sampled; expected counts come from current export manifest. This does not prove Unity Play movement, collision paths, eye height above ground, target-device performance or hand clipping."
            };
            result.pass=result.missingScripts==0&&result.missingMaterials==0&&result.missingMeshes==0&&result.missingDependencies.Length==0&&result.rendererCount==result.expectedRendererCount&&result.animationCount==result.expectedAnimationCount&&result.triangles==result.expectedTriangles&&result.collisionContract&&result.collisionMismatches==0&&result.playerEyeConfigured&&result.handRenderers==2&&result.handAnimationSetup&&result.handOverlayConfigured&&result.unsupportedShaders==0&&result.latestCrouchInput&&result.latestCrouchController&&changes&&result.glowEnabled;
            Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,JsonUtility.ToJson(result,true));
            if(!result.pass)throw new InvalidOperationException("Archive validation failed; see "+Output);
            MilanV6SceneBuilder.Validate();
            Debug.Log("MILAN_ARCHIVE_VALIDATION_PASS "+JsonUtility.ToJson(result));
        }
    }
}
