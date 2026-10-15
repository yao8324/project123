using System;
using System.Linq;
using ArcaneArchive.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArcaneArchive.Visuals.Editor
{
    public static class MilanFirstPersonSetup
    {
        private const string Folder = "Assets/Art/Characters/FirstPersonMage";
        private const string ModelPath = Folder + "/MageHand_Generic.fbx";
        private const string ControllerPath = Folder + "/MageHand.controller";
        private const string PrefabPath = Folder + "/MageHand_ViewModel.prefab";
        private const string InstanceName = "MageHand_ViewModel";
        private static readonly string[] States = { "Idle", "Walk", "Sprint", "Jump", "Land", "Crouch" };
        private static readonly string[] MaterialNames = { "Skin_Stylized_Warm", "Robe_Charcoal", "Cuff_Deep_Slate", "Trim_Muted_Brass", "Wand_Walnut", "Walnut_Carved_Ridges" };
        private static readonly Color[] Colors = {
            new Color(.50f,.265f,.135f), new Color(.025f,.031f,.04f), new Color(.047f,.053f,.063f),
            new Color(.23f,.15f,.055f), new Color(.092f,.041f,.017f), new Color(.155f,.077f,.031f)
        };

        // Called by the scene builder after its player/controller/camera have been created.
        // Regenerates only this owned prefab/controller/material set, preserving asset GUIDs.
        public static void Attach(GameObject player, Camera camera)
        {
            if (player == null || camera == null) throw new ArgumentNullException("A player and camera are required.");
            FirstPersonController movement = player.GetComponent<FirstPersonController>();
            if (movement == null || !camera.transform.IsChildOf(player.transform))
                throw new InvalidOperationException("The camera must belong to a player with FirstPersonController.");
            GameObject prefab = BuildPrefab(); // Validate assets before replacing an existing instance.
            Transform previous = camera.transform.Find(InstanceName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.transform);
            instance.name = InstanceName;
            SetAuthoredPose(instance.transform);
            FirstPersonViewModel view = instance.GetComponent<FirstPersonViewModel>();
            Camera overlay = AttachPresentationCamera(camera, instance);
            view.Configure(movement, camera, instance.GetComponentInChildren<Animator>(), overlay);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            EditorUtility.SetDirty(instance);
        }

        private static Camera AttachPresentationCamera(Camera camera, GameObject instance)
        {
            const string layerName = "FirstPersonViewModel";
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                var layers = tags.FindProperty("layers");
                for (int i = 31; i >= 8; i--)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    { layer = i; layers.GetArrayElementAtIndex(i).stringValue = layerName; break; }
                if (layer < 0) throw new InvalidOperationException("No free layer for the first-person viewmodel.");
                tags.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            { child.gameObject.layer = layer; PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject); }
            var baseData = camera.GetUniversalAdditionalCameraData();
            var stack = baseData.cameraStack;
            if (stack == null) throw new InvalidOperationException("The active URP renderer must support camera stacking.");
            Transform previous = camera.transform.Find("FirstPersonPresentationCamera");
            if (previous != null)
            { stack.Remove(previous.GetComponent<Camera>()); UnityEngine.Object.DestroyImmediate(previous.gameObject); }
            var go = new GameObject("FirstPersonPresentationCamera");
            go.transform.SetParent(camera.transform, false);
            Camera overlay = go.AddComponent<Camera>();
            overlay.CopyFrom(camera);
            overlay.cullingMask = 1 << layer;
            overlay.nearClipPlane = .005f;
            overlay.farClipPlane = 5f;
            overlay.clearFlags = CameraClearFlags.Depth;
            overlay.depth = camera.depth + 1;
            var data = overlay.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Overlay;
            data.renderPostProcessing = false;
            var serialized = new SerializedObject(data);
            serialized.FindProperty("m_ClearDepth").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            camera.cullingMask &= ~(1 << layer);
            stack.Add(overlay);
            EditorUtility.SetDirty(camera); EditorUtility.SetDirty(baseData);
            return overlay;
        }

        private static GameObject BuildPrefab()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing mage hand FBX: " + ModelPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.motionNodeName = "Root";
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                importer.SaveAndReimport();
                clips = importer.defaultClipAnimations;
            }
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string name = ShortName(clip.name);
                clip.name = name;
                clip.loopTime = name != "Jump" && name != "Land";
                clip.loopPose = clip.loopTime;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            AnimationClip[] animations = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            foreach (string state in States)
                if (animations.Count(c => ShortName(c.name) == state) != 1)
                    throw new InvalidOperationException("Expected exactly one mage animation: " + state);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader is required for the mage viewmodel.");
            Material[] materials = new Material[MaterialNames.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                string path = Folder + "/" + MaterialNames[i] + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                material.SetColor("_BaseColor", Colors[i].gamma); // Authored Blender colors are linear.
                material.SetFloat("_Metallic", i == 3 ? .55f : 0f);
                material.SetFloat("_Smoothness", i == 3 ? .52f : i == 0 ? .28f : i >= 4 ? .42f : .08f);
                EditorUtility.SetDirty(material);
                materials[i] = material;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (string name in States)
            {
                AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
                if (state == null) state = machine.AddState(name);
                state.motion = animations.Single(c => ShortName(c.name) == name);
                state.writeDefaultValues = false;
                if (name == "Idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);

            GameObject root = new GameObject(InstanceName);
            try
            {
                GameObject model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
                model.name = "MageHand_Model";
                Animator animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        string sourceName = slots[i] != null ? slots[i].name : "";
                        int index = Array.FindIndex(MaterialNames, n => sourceName == n || sourceName.StartsWith(n + ".", StringComparison.Ordinal));
                        if (index < 0) throw new InvalidOperationException("Unrecognized mage material slot: " + sourceName);
                        slots[i] = materials[index];
                    }
                    renderer.sharedMaterials = slots;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
                }
                root.AddComponent<FirstPersonViewModel>().Configure(null, null, animator);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Could not save mage viewmodel prefab.");
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static string ShortName(string name) => name.Substring(name.LastIndexOf('|') + 1);

        private static void SetAuthoredPose(Transform target)
        {
            // The FBX wand points along model +Y; the sleeve trails down/back/right.
            // Orient that physical grip directly in eye space, without a studio camera inverse.
            Vector3 modelWand = Vector3.up;
            Vector3 modelSleeveAcross = new Vector3(.27f, 0f, -.15f).normalized;
            Quaternion modelBasis = Quaternion.LookRotation(modelWand, Vector3.Cross(modelWand, modelSleeveAcross));
            Vector3 aim = new Vector3(-.10f, .70f, .71f).normalized;
            Vector3 sleeveAcross = Vector3.ProjectOnPlane(new Vector3(.62f, -.78f, 0f), aim).normalized;
            Quaternion eyeBasis = Quaternion.LookRotation(aim, Vector3.Cross(aim, sleeveAcross));
            target.localRotation = eyeBasis * Quaternion.Inverse(modelBasis);
            target.localScale = Vector3.one * .75f;
            target.localPosition = new Vector3(.22f, -.11f, .40f);

        }
    }
}
