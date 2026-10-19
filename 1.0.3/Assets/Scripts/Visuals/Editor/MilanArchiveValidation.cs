using System;
using System.IO;
using System.Linq;
using UnityEditor;
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
            GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;
            // The archive opens on its new demonstration while retaining the previous entry.
            var prior=EditorBuildSettings.scenes.Where(x=>x.path!=ScenePath).ToArray();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(prior).ToArray();
            var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
            var renderers=all.SelectMany(g=>g.GetComponents<MeshRenderer>()).ToArray();
            var filters=all.SelectMany(g=>g.GetComponents<MeshFilter>()).ToArray();
            var motions=all.SelectMany(g=>g.GetComponents<MilanMagicMotion>()).ToArray();
            var deps=AssetDatabase.GetDependencies(ScenePath,true);
            var input=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            var player=all.Select(g=>g.GetComponent<FirstPersonController>()).First(x=>x);
            bool latestController=new SerializedObject(player).FindProperty("crouchHeight")!=null;
            var motion=motions.First();var position=motion.transform.localPosition;var rotation=motion.transform.localRotation;var scale=motion.transform.localScale;
            motion.Sample(0);var a=motion.transform.localToWorldMatrix;motion.Sample(2.75f);var b=motion.transform.localToWorldMatrix;
            bool changes=!a.Equals(b);motion.transform.localPosition=position;motion.transform.localRotation=rotation;motion.transform.localScale=scale;
            var glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilanLibraryV6/Materials/V5_Magic_StarBlue.mat");
            var result=new Result {
                unityVersion=Application.unityVersion,scene=ScenePath,rendererCount=renderers.Length,animationCount=motions.Length,
                dependencyCount=deps.Length,missingDependencies=deps.Where(p=>!File.Exists(p)&&!Directory.Exists(p)&&!p.StartsWith("Packages/")).ToArray(),
                missingScripts=all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),
                missingMaterials=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m)),missingMeshes=filters.Count(f=>!f.sharedMesh),
                triangles=filters.Where(f=>f.sharedMesh).Sum(f=>(long)f.sharedMesh.GetIndexCount(0)/3),
                latestCrouchInput=input&&input.FindAction("Player/Crouch")!=null,latestCrouchController=latestController,
                magicAnimationChanges=changes,glowEnabled=glow&&glow.IsKeywordEnabled("_EMISSION"),
                note="Independent archive project: real Unity import and compile, scene dependency checks and animation sampling. Earlier Play-mode evidence is included separately; this is not a target-device performance test."
            };
            result.pass=result.missingScripts==0&&result.missingMaterials==0&&result.missingMeshes==0&&result.missingDependencies.Length==0&&result.rendererCount==889&&result.animationCount==47&&result.triangles==15770344&&result.latestCrouchInput&&result.latestCrouchController&&changes&&result.glowEnabled;
            Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,JsonUtility.ToJson(result,true));
            if(!result.pass)throw new InvalidOperationException("Archive validation failed; see "+Output);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            MilanV6SceneBuilder.Validate();MilanV6SceneBuilder.Capture();
            Debug.Log("MILAN_ARCHIVE_VALIDATION_PASS "+JsonUtility.ToJson(result));
        }
    }
}
