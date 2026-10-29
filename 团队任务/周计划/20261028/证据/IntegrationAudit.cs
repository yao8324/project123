using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using ArcaneArchive.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Temporary acceptance instrumentation. No production source is changed.
[InitializeOnLoad]
public static class IntegrationAudit
{
    const string Output = "AcceptanceEvidence";
    const string Hall = "Assets/Art/SealChamberStudy/Layout/Prefabs/SealChamber_LayoutCandidate.prefab";
    const string Arena = "Assets/Art/ForbiddenWarden/CombatV2/Prefabs/SealChamberRigged.prefab";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static BossEncounter encounter;
    static RecoveryMenu menu;
    static int phase;
    static double until;
    static Vector3 pausedPosition;
    static float pausedClock;
    static Dictionary<string, object> edge = new Dictionary<string, object>();
    static IntegrationAudit() { EditorApplication.update += Tick; }
    static void Save(string name, object data)
    { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented)); }
    public static void Prepare()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Hall);
        if (!prefab) throw new Exception("Art candidate missing after import");
        var hall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Physics.SyncTransforms();
        int floor = 0, clear = 0, total = 0;
        for (int ring = 0; ring <= 28; ring++) for (int i = 0; i < (ring == 0 ? 1 : 72); i++)
        {
            float angle = i * 5 * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * ring * .5f; total++;
            if (Physics.Raycast(p + Vector3.up * 7.8f, Vector3.down, out var hit, 8) && Mathf.Abs(hit.point.y) < .001f) floor++;
            if (!Physics.CheckCapsule(p + Vector3.up * .29f, p + Vector3.up * 1.48f, .28f)) clear++;
        }
        var renderers = hall.GetComponentsInChildren<MeshRenderer>(true);
        Save("art-import", new {
            unity = Application.unityVersion, floorPassed = floor, clearancePassed = clear, total,
            renderers = renderers.Length,
            missingMeshes = hall.GetComponentsInChildren<MeshFilter>(true).Count(x => !x.sharedMesh),
            invalidMaterials = renderers.Sum(x => x.sharedMaterials.Count(m => !m || !m.shader || !m.shader.isSupported)),
            missingScripts = hall.GetComponentsInChildren<Transform>(true).Sum(x => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),
            limitation = "Editor Physics probes after import into target project; not human play or target-device performance."
        });
        Object.DestroyImmediate(hall);
        var arena = PrefabUtility.LoadPrefabContents(Arena);
        try
        {
            var contract = arena.GetComponent<BossArena>();
            var children = Enumerable.Range(0, arena.transform.childCount).Select(i => arena.transform.GetChild(i).name).ToArray();
            var oldArchitecture = arena.transform.Find("Architecture");
            var oldLighting = arena.transform.Find("Lighting");
            if (!oldArchitecture || !oldLighting || !contract.spawn || !contract.boss || !contract.effectRoot) throw new Exception("Unexpected production arena contract");
            int oldPoints = oldLighting.GetComponentsInChildren<Light>(true).Count(x => x.type == LightType.Point);
            Object.DestroyImmediate(oldArchitecture.gameObject);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(prefab, arena.transform);
            art.transform.localPosition = Vector3.zero; art.transform.localRotation = Quaternion.identity; art.transform.localScale = Vector3.one;
            // Keep the existing moon key. Disable old point lights instead of stacking two rigs.
            foreach (var light in oldLighting.GetComponentsInChildren<Light>(true)) if (light.type == LightType.Point) light.enabled = false;
            Save("integration-assembly", new {
                unity = Application.unityVersion, originalRootChildren = children, radius = contract.radius,
                oldPoints, importedPoints = art.GetComponentsInChildren<Light>(true).Count(x => x.type == LightType.Point),
                enabledPointLights = arena.GetComponentsInChildren<Light>(true).Count(x => x.enabled && x.type == LightType.Point),
                floorStrategy = "Removed old Architecture and all of its colliders; imported one art hall at local identity.",
                preservedReferences = new { spawn = contract.spawn.name, boss = contract.boss.name, effects = contract.effectRoot.name, model = contract.model.name },
                limitation = "Temporary integration candidate. No production script, original source project, or remote game asset changed."
            });
            PrefabUtility.SaveAsPrefabAsset(arena, Arena);
        }
        finally { PrefabUtility.UnloadPrefabContents(arena); }
        AssetDatabase.SaveAssets();
        Debug.Log("[IntegrationAudit] PREPARE_COMPLETE");
    }
    public static void RunEdges()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
        SessionState.SetInt("IMMonster.EdgeAudit", 1);
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        int state = SessionState.GetInt("IMMonster.EdgeAudit", 0);
        if (state == 0) return;
        try
        {
            if (state == 1 && EditorApplication.isPlaying && !EditorApplication.isCompiling && Time.frameCount > 5)
            {
                encounter = Object.FindFirstObjectByType<BossEncounter>();
                menu = encounter.GetComponent<RecoveryMenu>();
                if (!encounter.Begin()) throw new Exception("Integrated encounter Begin failed");
                Application.runInBackground = true;
                SessionState.SetInt("IMMonster.EdgeAudit", 2); until = EditorApplication.timeSinceStartup + 3;
            }
            else if (state == 2 && EditorApplication.timeSinceStartup > until)
            {
                if (encounter.CurrentState != BossEncounter.State.Observe) throw new Exception("Expected production Observe state");
                var cc = encounter.GetComponent<CharacterController>();
                edge["controller"] = new { radius = cc.radius, height = cc.height, skinWidth = cc.skinWidth };
                cc.enabled = false; encounter.transform.position += Vector3.up * 1.2f; cc.enabled = true;
                typeof(FirstPersonController).GetField("verticalVelocity", Private).SetValue(encounter.GetComponent<FirstPersonController>(), 0f);
                menu.Open(); pausedPosition = encounter.transform.position; pausedClock = encounter.BattleTime;
                until = EditorApplication.timeSinceStartup + .4; SessionState.SetInt("IMMonster.EdgeAudit", 3);
            }
            else if (state == 3 && EditorApplication.timeSinceStartup > until)
            {
                edge["airbornePause"] = new { controlledSetup = "Real controller placed 1.2m airborne with zero vertical velocity before opening actual menu", startY = pausedPosition.y, endY = encounter.transform.position.y, deltaY = encounter.transform.position.y - pausedPosition.y, battleTimeDelta = encounter.BattleTime - pausedClock, isPaused = encounter.IsPaused };
                menu.Resume();
                encounter.enabled = false; // Isolate projectile methods from the normal attack scheduler.
                var view = encounter.View.transform; var f = view.forward;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Acceptance_ThinWall";
                wall.transform.SetPositionAndRotation(view.position + f * .35f, Quaternion.LookRotation(f)); wall.transform.localScale = new Vector3(3, 3, .08f); Physics.SyncTransforms();
                var list = (System.Collections.IList)typeof(BossEncounter).GetField("shots", Private).GetValue(encounter);
                encounter.FirePlayerBolt(1, false);
                var shot = list[list.Count - 1]; var type = shot.GetType(); var go = (GameObject)type.GetField("go").GetValue(shot);
                bool blocked = Physics.Linecast(view.position, go.transform.position, out var hit) && hit.collider.gameObject == wall;
                edge["muzzleWall"] = new { cameraToMuzzleBlockedByWall = blocked, muzzleDistance = Vector3.Distance(view.position, go.transform.position), wallDistance = .35f, wallThickness = .08f, spawnedBeyondWall = Vector3.Dot(go.transform.position - wall.transform.position, f) > .04f };
                typeof(BossEncounter).GetMethod("ClearAttacks", Private).Invoke(encounter, null);
                wall.transform.position = view.position + f * .7f; Physics.SyncTransforms();
                shot = Activator.CreateInstance(type, true);
                go = Object.Instantiate(encounter.Arena.pageProjectile, view.position + f * 1.4f, Quaternion.identity, encounter.Arena.effectRoot);
                type.GetField("go").SetValue(shot, go); type.GetField("velocity").SetValue(shot, -f * 22);
                type.GetField("enemy").SetValue(shot, true); type.GetField("life").SetValue(shot, 2f); type.GetField("damage").SetValue(shot, 5f); list.Add(shot);
                float before = encounter.Combat.Power;
                bool occluded = Physics.Linecast(view.position, go.transform.position, out hit) && hit.collider.gameObject == wall;
                bool absorbStarted = encounter.Combat.TryAbsorb();
                typeof(BossEncounter).GetMethod("TickShots", Private).Invoke(encounter, new object[] { .016f });
                edge["absorbWall"] = new { projectileBehindWall = occluded, absorbStarted, beforePower = before, afterPower = encounter.Combat.Power, powerDelta = encounter.Combat.Power - before, setup = "Injected one ordinary enemy projectile to isolate actual TickShots/CanAbsorb production methods; real wall collider." };
                Object.Destroy(wall);
                encounter.enabled = true; encounter.ReturnToLibrary();
                edge["unity"] = Application.unityVersion; edge["testKind"] = "Controlled runtime edge-case reproduction, not manual play.";
                Save("edge-reproduction", edge);
                SessionState.SetInt("IMMonster.EdgeAudit", 4); EditorApplication.isPlaying = false;
            }
            else if (state == 4 && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            { SessionState.SetInt("IMMonster.EdgeAudit", 0); Debug.Log("[IntegrationAudit] EDGE_REPRODUCTIONS_COMPLETE"); EditorApplication.Exit(0); }
        }
        catch (Exception ex)
        { Save("edge-audit-error", new { error = ex.ToString(), state }); SessionState.SetInt("IMMonster.EdgeAudit", 0); Debug.LogException(ex); EditorApplication.Exit(2); }
    }
}
