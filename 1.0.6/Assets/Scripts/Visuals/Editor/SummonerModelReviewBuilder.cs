using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    // Visual-only prefab preparation. Does not install AI or replace combat scenes.
    public static class SummonerModelReviewBuilder
    {
        const string Skeleton = "Assets/Art/Minions/Skeleton";
        const string Boss = "Assets/Art/Bosses/Summoner";
        const string Undead = "Assets/Art/Minions/Undead";
        const string Projectiles = "Assets/Art/Minions/Projectiles";
        const string Review = "Assets/Scenes/Summoner_ModelReview.unity";
        [Serializable] class Entry
        {
            public string name;
            public float[] color;
            public float[] baseColor;
            public float metallic;
            public float smoothness;
            public float roughness;
            public float emission;
        }
        [Serializable] class Manifest { public Entry[] materials; }

        [MenuItem("Tools/IMMonster/Build Summoner Model Review")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            Prepare(Skeleton + "/Models/Skeleton_Machete.fbx", Skeleton,
                File.ReadAllText("Docs/SkeletonMinion20261106/materials.json"), false);
            var allMaterials = File.ReadAllText("Docs/SkeletonMinion20261106/materials-all.json");
            foreach (var variant in new[] { "Archer", "Hammer", "SwordShield" })
                Prepare(Skeleton + "/Models/Skeleton_" + variant + ".fbx", Skeleton, allMaterials, false);
            Prepare(Undead + "/Models/Undead_Winged.fbx", Undead, allMaterials, false);
            foreach (var prop in new[] { "Skeleton_Arrow", "Undead_Fireball", "Boss_PurpleSkull", "Boss_MagicDagger" })
            {
                Prepare(Projectiles + "/Models/" + prop + ".fbx", Projectiles, allMaterials, false, false);
                MakeStaticPrefab(prop);
            }
            Prepare(Boss + "/Models/Soulbinder_OverlordStudy.fbx", Boss,
                "{\"materials\":" + File.ReadAllText("Docs/SummonerModel20261106/material-manifest.json") + "}", true);
            var skeletonPrefab = MakePrefab(Skeleton + "/Models/Skeleton_Machete.fbx", Skeleton, "Skeleton_Machete_Visual", "Skeleton_Idle");
            var bossPrefab = MakePrefab(Boss + "/Models/Soulbinder_OverlordStudy.fbx", Boss, "Soulbinder_Visual", "Soulbinder_Idle_Study");
            var archerPrefab = MakePrefab(Skeleton + "/Models/Skeleton_Archer.fbx", Skeleton, "Skeleton_Archer_Visual", "Skeleton_Idle");
            var hammerPrefab = MakePrefab(Skeleton + "/Models/Skeleton_Hammer.fbx", Skeleton, "Skeleton_Hammer_Visual", "Skeleton_Idle");
            var shieldPrefab = MakePrefab(Skeleton + "/Models/Skeleton_SwordShield.fbx", Skeleton, "Skeleton_SwordShield_Visual", "Skeleton_Idle");
            var undeadPrefab = MakePrefab(Undead + "/Models/Undead_Winged.fbx", Undead, "Undead_Winged_Visual", "Undead_Hover", true);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Summoner_Blockout.unity");
            var placeholder = GameObject.Find("Boss_3_6m_Purple");
            if (placeholder) placeholder.SetActive(false);
            var playerMarker = GameObject.Find("Player_1_8m_Green");
            if (playerMarker) playerMarker.SetActive(false);
            foreach (var marker in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (marker.name == "GroundSummonMarker") marker.gameObject.SetActive(false);
            Place(bossPrefab, "Boss_Model", new Vector3(0, 0, 5), 180);
            Place(skeletonPrefab, "MacheteSkeleton_Armored", new Vector3(-5, 0, 1), 180);
            var bare = Place(skeletonPrefab, "SharedSkeleton_Unarmored", new Vector3(-5.3f, 0, 5), 180);
            foreach (var renderer in bare.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (renderer.name.StartsWith("Armor_") || renderer.name == "Machete_Weapon") renderer.gameObject.SetActive(false);
            Place(archerPrefab, "ArcherSkeleton", new Vector3(-2.5f, 0, 1), 180);
            Place(hammerPrefab, "HammerSkeleton", new Vector3(0, 0, 1), 180);
            Place(shieldPrefab, "SwordShieldSkeleton", new Vector3(2.5f, 0, 1), 180);
            Place(undeadPrefab, "WingedUndead", new Vector3(5, 0, 1), 180);
            var camera = Object.FindFirstObjectByType<Camera>();
            camera.transform.position = new Vector3(0, 3.2f, -8);
            camera.transform.LookAt(new Vector3(0, 1.7f, 2));
            camera.fieldOfView = 50;
            var key = new GameObject("ModelReview_Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.3f;
            key.transform.rotation = Quaternion.Euler(40, -25, 0);
            // Save as a separate scene so the plain blockout stays available.
            EditorSceneManager.SaveScene(scene, Review, true);
            AssetDatabase.SaveAssets();
            scene = EditorSceneManager.OpenScene(Review);
            foreach (var name in new[] { "Boss_Model", "MacheteSkeleton_Armored", "SharedSkeleton_Unarmored", "ArcherSkeleton", "HammerSkeleton", "SwordShieldSkeleton", "WingedUndead" })
            {
                var instance = GameObject.Find(name);
                if (!instance) throw new InvalidOperationException("Missing review instance: " + name);
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (renderers.Length == 0 || renderers.Any(r => r.sharedMesh == null || r.sharedMaterials.Any(m => !m || !m.shader)))
                    throw new InvalidOperationException("Missing model mesh/material: " + name);
                Debug.Log("MODEL_REVIEW_ASSET " + name + " skinnedMeshes=" + renderers.Length + " bounds=" + BoundsOf(instance));
            }
            Debug.Log("SUMMONER_MODEL_REVIEW_PASS: imported meshes/materials, idle controllers and saved prefab references verified. Visual scene only.");
        }

        public static void BuildAndCapture()
        {
            Build();
            Capture();
        }

        [MenuItem("Tools/IMMonster/Capture Summoner Model Review")]
        public static void Capture()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Review);
            var camera = Object.FindFirstObjectByType<Camera>();
            var texture = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Docs/SkeletonMinion20261106/Renders");
                File.WriteAllBytes("Docs/SkeletonMinion20261106/Renders/05_unity_room.png", pixels.EncodeToPNG());
                var skeleton = GameObject.Find("MacheteSkeleton_Armored");
                var eyes = skeleton.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Skeleton_GhostEyes");
                Debug.Log("SKELETON_FACING_CHECK eyesOffset=" + (eyes.bounds.center - skeleton.transform.position));
                if (eyes.bounds.center.z >= skeleton.transform.position.z)
                    throw new InvalidOperationException("Review skeleton must face the camera/player side (-Z).");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                texture.Release();
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(pixels);
            }
        }

        static void Prepare(string path, string folder, string json, bool boss, bool animated = true)
        {
            Directory.CreateDirectory(folder + "/Materials");
            Directory.CreateDirectory(folder + "/Prefabs");
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (!importer) throw new InvalidOperationException("Model missing: " + path);
            importer.animationType = animated ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.avatarSetup = animated ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.NoAvatar;
            importer.importAnimation = animated;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var entry in JsonUtility.FromJson<Manifest>(json).materials)
            {
                var color = boss ? entry.baseColor : entry.color;
                var materialPath = folder + "/Materials/" + entry.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                var tint = new Color(color[0], color[1], color[2], color[3]);
                material.SetColor("_BaseColor", tint);
                material.SetFloat("_Metallic", entry.metallic);
                material.SetFloat("_Smoothness", boss ? 1 - entry.roughness : entry.smoothness);
                if (tint.a < .99f)
                {
                    material.SetFloat("_Surface", 1);
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0);
                    material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    material.renderQueue = 3000;
                }
                if (entry.emission > 0)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", tint * entry.emission);
                }
                EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name), material);
            }
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = clip.name.Contains("Idle") || clip.name.Contains("Walk") || clip.name.Contains("Stun") || clip.name.Contains("Hover");
                clip.lockRootHeightY = true;
                clip.lockRootRotation = true;
                clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static GameObject MakePrefab(string modelPath, string folder, string name, string idle, bool keepHoverHeight = false)
        {
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            model.transform.SetParent(root.transform, false);
            // The exported Blender -Y facing is already Unity +Z.
            model.transform.localRotation = Quaternion.identity;
            var bounds = BoundsOf(root);
            if (!keepHoverHeight) model.transform.localPosition -= Vector3.up * bounds.min.y;
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var idleClip = clips.FirstOrDefault(c => c.name.EndsWith(idle));
            if (!idleClip) throw new InvalidOperationException("Missing idle clip: " + idle);
            string controllerPath = folder + "/Prefabs/" + name + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var existing in machine.states) machine.RemoveState(existing.state);
            foreach (var clip in clips)
            {
                var state = machine.AddState(clip.name); state.motion = clip;
                if (clip == idleClip) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            var animator = model.GetComponent<Animator>();
            if (!animator) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var result = PrefabUtility.SaveAsPrefabAsset(root, folder + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return result;
        }

        static void MakeStaticPrefab(string name)
        {
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Projectiles + "/Models/" + name + ".fbx"));
            model.transform.SetParent(root.transform, false);
            if (name == "Skeleton_Arrow" || name == "Undead_Fireball") model.transform.localRotation = Quaternion.Euler(90, 0, 0);
            PrefabUtility.SaveAsPrefabAsset(root, Projectiles + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        static GameObject Place(GameObject prefab, string name, Vector3 position, float angle)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name; instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, angle, 0));
            return instance;
        }

        static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
