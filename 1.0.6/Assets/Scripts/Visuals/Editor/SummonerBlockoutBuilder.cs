using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    // Geometry and placement markers only; no dependency on combat or final art.
    public static class SummonerBlockoutBuilder
    {
        const string Root = "Assets/Art/Blockouts/SummonerRoom";
        const string ScenePath = "Assets/Scenes/Summoner_Blockout.unity";

        [MenuItem("Tools/IMMonster/Build Summoner Test Room")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floorMat = Material("Floor", new Color(.29f, .32f, .35f));
            var wallMat = Material("Walls", new Color(.48f, .51f, .54f));
            var playerMat = Material("PlayerMarker", new Color(.18f, .8f, .48f));
            var bossMat = Material("BossMarker", new Color(.65f, .25f, .9f));
            var root = new GameObject("Summoner_TestRoom");
            var geometry = Node("RoomGeometry_ReplaceWithFinalScene", root.transform, Vector3.zero);
            Box("Floor", geometry, new Vector3(0, -.25f, 0), new Vector3(25, .5f, 21), floorMat);
            Box("Wall_North", geometry, new Vector3(0, 3.5f, 10.25f), new Vector3(25, 7, .5f), wallMat);
            Box("Wall_South", geometry, new Vector3(0, 3.5f, -10.25f), new Vector3(25, 7, .5f), wallMat);
            Box("Wall_East", geometry, new Vector3(12.25f, 3.5f, 0), new Vector3(.5f, 7, 20), wallMat);
            Box("Wall_West", geometry, new Vector3(-12.25f, 3.5f, 0), new Vector3(.5f, 7, 20), wallMat);
            Box("Ceiling", geometry, new Vector3(0, 7.25f, 0), new Vector3(25, .5f, 21), wallMat);

            var markers = Node("PlacementMarkers", root.transform, Vector3.zero);
            Node("PlayerSpawn", markers, new Vector3(0, 0, -6));
            var bossSpawn = Node("BossSpawn", markers, new Vector3(0, 0, 5));
            bossSpawn.localRotation = Quaternion.Euler(0, 180, 0);
            var ground = Node("GroundSummonPoints", markers, Vector3.zero);
            var points = new[] { new Vector3(-7, 0, -4), new Vector3(7, 0, -4), new Vector3(-7, 0, 4), new Vector3(7, 0, 4) };
            for (int i = 0; i < points.Length; i++) Node("Ground_" + (i + 1), ground, points[i]);
            var walls = Node("WallSummonPoints", markers, Vector3.zero);
            var wallPoints = new[] { new Vector3(-11.95f, 1.5f, 0), new Vector3(11.95f, 1.5f, 0), new Vector3(-5, 1.5f, 9.95f), new Vector3(5, 1.5f, 9.95f) };
            for (int i = 0; i < wallPoints.Length; i++)
            {
                var marker = Node("Wall_" + (i + 1), walls, wallPoints[i]);
                marker.rotation = Quaternion.LookRotation(new Vector3(-marker.position.x, 0, -marker.position.z));
            }

            var placeholders = Node("VisualPlaceholders_ReplaceWithModels", root.transform, Vector3.zero);
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_1_8m_Green";
            player.transform.SetParent(placeholders, false);
            player.transform.localPosition = new Vector3(0, .9f, -6);
            player.transform.localScale = new Vector3(.6f, .9f, .6f);
            player.GetComponent<Renderer>().sharedMaterial = playerMat;
            Object.DestroyImmediate(player.GetComponent<Collider>());
            var boss = Box("Boss_3_6m_Purple", placeholders, new Vector3(0, 1.8f, 5), new Vector3(1.2f, 3.6f, .8f), bossMat);
            Object.DestroyImmediate(boss.GetComponent<Collider>());
            foreach (var position in points)
            {
                var pad = Box("GroundSummonMarker", placeholders, position + Vector3.up * .015f, new Vector3(1, .03f, 1), bossMat);
                Object.DestroyImmediate(pad.GetComponent<Collider>());
            }
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/SummonerTestRoom.prefab");
            Object.DestroyImmediate(root);
            root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/SummonerTestRoom.prefab"));
            root.name = "Summoner_TestRoom";

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .62f, .65f);
            RenderSettings.fog = false;
            var light = new GameObject("RoomLight").AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = new Vector3(0, 6, 0);
            light.range = 35;
            light.intensity = 4;
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(9.5f, 5.8f, -8.5f);
            camera.transform.LookAt(new Vector3(-1, 1.2f, 2));
            camera.fieldOfView = 72;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 60;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.gray;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            // Check the actual saved scene and collision bounds before delivery.
            EditorSceneManager.OpenScene(ScenePath);
            var room = GameObject.Find("Summoner_TestRoom");
            var geometryRoot = room.transform.Find("RoomGeometry_ReplaceWithFinalScene");
            if (geometryRoot.GetComponentsInChildren<BoxCollider>().Length != 6)
                throw new InvalidOperationException("Expected floor, ceiling and four collidable walls.");
            Physics.SyncTransforms();
            if (!Physics.Raycast(new Vector3(0, 1, 0), Vector3.down, 2))
                throw new InvalidOperationException("Floor collision missing.");
            foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up })
                if (!Physics.Raycast(new Vector3(0, 1, 0), direction, 15))
                    throw new InvalidOperationException("Room boundary missing: " + direction);
            Debug.Log("SUMMONER_BLOCKOUT_OK: saved scene/prefab, six colliders and five boundary raycasts verified. No combat AI included.");
        }

        static Transform Node(string name, Transform parent, Vector3 position)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = position;
            return node;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        static Material Material(string name, Color color)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) throw new InvalidOperationException("URP Lit shader missing.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
