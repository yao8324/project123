using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Updates only the encounter's spell assets; preserves the scene and rig hierarchy.</summary>
    public static class WardenRitualBuilder
    {
        const string Root = WardenCombatV2Builder.Root;
        const string Output = "Docs/BossDesign/ArcaneEffects20261027";
        static Material gold, violet, softViolet, softGold, shadow, ivory, echoGold, vellum;
        static Mesh crystal, crownMesh, innerMesh, shardMesh;
        sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> indices = new List<int>();
            public void Ribbon(IList<Vector3> points, float width, Vector3 normal)
            {
                int start = vertices.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    var tangent = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(0, i - 1)];
                    var side = Vector3.Cross(normal, tangent).normalized * width;
                    vertices.Add(points[i] - side); vertices.Add(points[i] + side);
                    uv.Add(new Vector2(i / (float)(points.Count - 1), 0)); uv.Add(new Vector2(i / (float)(points.Count - 1), 1));
                    if (i < points.Count - 1) { int n = start + i * 2; indices.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 }); }
                }
            }
            public Mesh Finish(string name)
            {
                var m = new Mesh { name = name }; m.SetVertices(vertices); m.SetUVs(0, uv); m.SetTriangles(indices, 0);
                m.colors = Enumerable.Repeat(Color.white, vertices.Count).ToArray(); m.RecalculateNormals(); m.RecalculateBounds(); return m;
            }
        }
        static Mesh SaveMesh(Mesh mesh, string name)
        {
            mesh.name = name;
            string path = Root + "/Models/Ritual/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); return old; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Material Ink(string name, Color tint, bool soft = false, bool opaqueInk = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Arcane Archive/Ritual Ink");
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Ritual Ink shader is unavailable");
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader; m.SetColor("_Tint", tint); m.SetFloat("_Soft", soft ? 1 : 0); m.SetFloat("_Fade", 1);
            m.SetFloat("_DstBlend", opaqueInk ? (int)BlendMode.OneMinusSrcAlpha : (int)BlendMode.One);
            m.renderQueue = opaqueInk ? 2999 : 3000; EditorUtility.SetDirty(m); return m;
        }
        static GameObject Node(string name, Transform parent = null)
        { var g = new GameObject(name); if (parent) g.transform.SetParent(parent, false); return g; }
        static Transform Part(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = Node(name, parent); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }
        static LineRenderer Line(string name, Transform parent, Material mat, float width, int points = 48)
        {
            var line = Node(name, parent).AddComponent<LineRenderer>(); line.sharedMaterial = mat; line.positionCount = points;
            line.useWorldSpace = true; line.startWidth = width; line.endWidth = .002f; line.numCapVertices = 3; line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.textureMode = LineTextureMode.Stretch;
            return line;
        }
        static GameObject Save(GameObject go, string name)
        { var asset = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab"); Object.DestroyImmediate(go); return asset; }
        static Vector3 Polar(float radius, float angle) => new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        static Mesh Ring(float radius, int marks)
        {
            var g = new Geometry();
            for (int sector = 0; sector < 7; sector++)
            {
                var points = new List<Vector3>();
                for (int j = 0; j <= 20; j++) points.Add(Polar(radius, (sector + j / 23f) * Mathf.PI * 2 / 7));
                g.Ribbon(points, .022f, Vector3.up);
            }
            for (int i = 0; i < marks; i++)
            {
                float a = i * Mathf.PI * 2 / marks;
                var radial = Polar(1, a); var tangent = Polar(1, a + Mathf.PI * .5f);
                var centre = radial * (radius + .085f);
                g.Ribbon(new[] { centre - radial * .04f - tangent * .027f, centre + radial * .065f, centre + tangent * .027f - radial * .04f }, .010f, Vector3.up);
                if (i % 3 != 0) g.Ribbon(new[] { centre - tangent * .035f, centre + tangent * .035f }, .009f, Vector3.up);
                if (i % 4 == 0) g.Ribbon(new[] { radial * (radius - .08f), radial * (radius - .2f) }, .010f, Vector3.up);
            }
            return g.Finish("Interrupted rings and forbidden glyphs");
        }
        static Mesh Shroud()
        {
            var g = new Geometry(); float[] y = { .12f, .55f, 1.4f, 2.3f, 2.85f, 3.18f };
            float[] r = { .82f, .65f, .42f, .40f, .83f, .29f }; const int sides = 20;
            for (int row = 0; row < y.Length; row++) for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, fold = i % 2 == 0 ? 1 : .89f;
                g.vertices.Add(new Vector3(Mathf.Cos(a) * r[row] * fold, y[row], Mathf.Sin(a) * r[row] * .48f - y[row] * .10f));
                g.uv.Add(new Vector2(i / (float)sides, row / (float)(y.Length - 1)));
                if (row < y.Length - 1)
                { int n = row * sides + i, next = row * sides + (i + 1) % sides; g.indices.AddRange(new[] { n, n + sides, next, next, n + sides, next + sides }); }
            }
            return g.Finish("Low polygon echo of the Warden robe");
        }
        static Mesh Seams()
        {
            var g = new Geometry();
            for (int i = -2; i <= 2; i++)
                g.Ribbon(new[] { new Vector3(i * .31f, .15f, .16f), new Vector3(i * .22f, .8f, .19f), new Vector3(i * .12f, 1.8f, .06f), new Vector3(i * .12f, 2.8f, -.09f) }, .013f, Vector3.forward);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 5; i++)
                    g.Ribbon(new[] { new Vector3(side * .65f, 2.85f, -.24f), new Vector3(side * (1.1f + i * .06f), 2.5f - i * .15f, -.25f), new Vector3(side * .91f, 2.2f - i * .13f, -.22f) }, .014f, Vector3.forward);
            return g.Finish("Ghost robe seams and torn page wings");
        }
        static Mesh Mask()
        {
            var g = new Geometry();
            g.vertices.AddRange(new[] { new Vector3(0,.37f,0),new Vector3(.22f,.22f,0),new Vector3(.18f,-.16f,0),new Vector3(0,-.35f,.01f),new Vector3(-.18f,-.16f,0),new Vector3(-.22f,.22f,0),new Vector3(0,.03f,.11f) });
            for(int i=0;i<7;i++)g.uv.Add(Vector2.one*.5f);
            for(int i=0;i<6;i++)g.indices.AddRange(new[]{i,(i+1)%6,6});
            return g.Finish("Faceted eyeless mask silhouette");
        }
        static Transform[] Scraps(Transform parent, int count)
        {
            var result = new Transform[count];
            for (int i = 0; i < count; i++) { result[i] = Part("Drifting inscription " + i, parent, shardMesh, i % 3 == 0 ? violet : gold); result[i].localScale = Vector3.one * .05f; }
            return result;
        }
        static GameObject BuildCrown()
        {
            var root = Node("Crown of forbidden pages"); var visual = root.AddComponent<WardenVolleyVisuals>();
            visual.outerRing = Part("Seven broken gold arcs", root.transform, crownMesh, gold);
            visual.innerRing = Part("Counter rotating violet scripture", root.transform, innerMesh, violet);
            visual.ringRenderers = root.GetComponentsInChildren<Renderer>(); return Save(root, "ForbiddenPageCrown");
        }
        static GameObject BuildWake()
        {
            var root = Node("World space Warden wake"); var wake = root.AddComponent<WardenRushWake>();
            wake.ribbons = new[] { Line("Left violet silk", root.transform, softViolet, .48f), Line("Right violet silk", root.transform, softViolet, .48f), Line("Left gold thread", root.transform, softGold, .075f), Line("Right gold thread", root.transform, softGold, .075f) };
            wake.scraps = Scraps(root.transform, 16); wake.trailRenderers = root.GetComponentsInChildren<Renderer>();
            var robe = SaveMesh(Shroud(), "EchoRobe"); var seams = SaveMesh(Seams(), "EchoInscriptions");
            var halo = SaveMesh(Ring(.43f, 16), "EchoHalo"); var mask = SaveMesh(Mask(), "EchoMask"); wake.ghosts = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var ghost = Node("Lingering silhouette " + i, root.transform).transform; wake.ghosts[i] = ghost;
                Part("Purple black shroud", ghost, robe, shadow); Part("Broken gold outline", ghost, seams, echoGold);
                var head = Part("Ivory mask echo", ghost, mask, ivory); head.localPosition = new Vector3(0, 3.46f, -.18f);
                var ring = Part("Fractured halo echo", ghost, halo, violet); ring.localPosition = new Vector3(0, 3.5f, -.35f); ring.localRotation = Quaternion.Euler(90, 0, 0);
            }
            return Save(root, "WardenRushWake");
        }
        static GameObject BuildImpact()
        {
            var root = Node("Unbinding page impact"); var impact = root.AddComponent<WardenPageImpact>();
            impact.seal = Part("Shattered seal", root.transform, innerMesh, gold);
            impact.scraps = Scraps(root.transform, 10); impact.renderers = root.GetComponentsInChildren<Renderer>(); return Save(root, "ForbiddenPageImpact");
        }
        static void UpgradePage()
        {
            string path = Root + "/Prefabs/ForbiddenPageIlluminated.prefab";
            var page = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var motion = page.GetComponent<WardenSpellMotion>(); motion.ornament.localScale = Vector3.one * 2.6f;
                foreach(var renderer in motion.ornament.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m.name.Contains("Vellum")?vellum:m.name.Contains("Amethyst") || m.name.Contains("Violet")?violet:gold).ToArray();
                if (motion.coreOrbit) Object.DestroyImmediate(motion.coreOrbit.gameObject);
                if (motion.coreTrail) Object.DestroyImmediate(motion.coreTrail.gameObject);
                motion.trail.sharedMaterial = softViolet; motion.trail.startWidth = .22f; motion.trail.endWidth = .002f;
                motion.coreTrail = Line("Fine gilt flight core", page.transform, softGold, .04f, 24);
                motion.trailLifetime = .16f; motion.trailLength = 2.4f;
                motion.coreOrbit = Node("Orbiting broken page seal", page.transform).transform;
                var orbit = Part("Violet encircling letters", motion.coreOrbit, innerMesh, violet); orbit.localScale = Vector3.one * .23f;
                var focus = Part("Ink dark nucleus", motion.coreOrbit, crystal, shadow); focus.localScale = new Vector3(.12f, .18f, .12f);
                PrefabUtility.SaveAsPrefabAsset(page, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(page); }
        }
        static void RefreshMotionRanges()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "/Models/SilentWarden_Combat_v2.fbx");
            var clips = importer.clipAnimations; var defaults = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                var source = defaults.FirstOrDefault(c => c.takeName == clip.takeName);
                if (source != null) { clip.firstFrame = source.firstFrame; clip.lastFrame = source.lastFrame; }
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
        }
        [MenuItem("Arcane Archive/Boss/Apply ritual effects 20261027")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating assets");
            Directory.CreateDirectory(Output); Directory.CreateDirectory(Root + "/Models/Ritual"); AssetDatabase.Refresh();
            gold = Ink("RitualGold", new Color(3.1f, 1.65f, .50f, .85f));
            violet = Ink("RitualViolet", new Color(.95f, .12f, 2.6f, .85f));
            softViolet = Ink("RitualVioletSilk", new Color(.57f, .045f, 1.3f, .65f), true);
            softGold = Ink("RitualGoldThread", new Color(2.5f, 1.4f, .5f, .85f), true);
            shadow = Ink("RitualShadow", new Color(.035f, .012f, .065f, .74f), false, true);
            ivory = Ink("RitualIvoryEcho", new Color(.95f, .79f, .49f, .54f));
            echoGold = Ink("RitualEchoGold", new Color(1.3f,.62f,.17f,.6f));
            string vellumPath=Root+"/Materials/RitualVellum.mat";vellum=AssetDatabase.LoadAssetAtPath<Material>(vellumPath);
            if(!vellum){vellum=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(vellum,vellumPath);}
            vellum.SetColor("_BaseColor",new Color(.82f,.66f,.40f));vellum.SetFloat("_Metallic",.05f);vellum.SetFloat("_Smoothness",.35f);vellum.SetFloat("_Cull",0);
            vellum.EnableKeyword("_EMISSION");vellum.SetColor("_EmissionColor",new Color(.55f,.31f,.10f));EditorUtility.SetDirty(vellum);
            crystal = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Models/SpellCrystal.asset");
            crownMesh = SaveMesh(Ring(1.56f, 42), "CrownOuter"); innerMesh = SaveMesh(Ring(1.04f, 28), "CrownInner");
            var shard = new Geometry(); shard.vertices.AddRange(new[] { new Vector3(-.5f, -.7f, 0), new Vector3(.4f, -.5f, 0), new Vector3(.6f, .6f, .15f), new Vector3(-.3f, .85f, .05f) });
            shard.uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }); shard.indices.AddRange(new[] { 0, 1, 2, 0, 2, 3 });
            shardMesh = SaveMesh(shard.Finish("Torn illuminated scrap"), "TornScript");
            UpgradePage(); var crown = BuildCrown(); var wake = BuildWake(); var impact = BuildImpact();
            WardenRiftBuilder.BuildAsset(); RefreshMotionRanges();
            string arenaPath = Root + "/Prefabs/SealChamberRigged.prefab";
            var chamber = PrefabUtility.LoadPrefabContents(arenaPath);
            try
            {
                var arena = chamber.GetComponent<BossArena>();
                arena.volleyEffect = crown.GetComponent<WardenVolleyVisuals>(); arena.rushWakeEffect = wake.GetComponent<WardenRushWake>(); arena.pageImpactEffect = impact.GetComponent<WardenPageImpact>();
                PrefabUtility.SaveAsPrefabAsset(chamber, arenaPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(chamber); }
            var d = AssetDatabase.LoadAssetAtPath<BossDefinition>("Assets/Art/ForbiddenWarden/WardenEncounter.asset");
            d.fractureWidth = 1.8f; d.fracturePhaseTwoWidth = 2.2f; d.fractureLeadTime = .8f;
            d.rushSpeed = 75; d.rushWakeDuration = .85f; d.volleyWarning = 1.25f;
            d.volleyCount = 6; d.volleyPhaseTwoCount = 8; d.volleyInterval = .05f; d.volleyReleaseDelay = .20f; d.volleyHaloRadius = 1.35f; d.projectileSpeed = 22;
            EditorUtility.SetDirty(d); AssetDatabase.SaveAssets();
            File.WriteAllText(Output + "/AssetBuild.txt", "Updated crown, world-space wake, pages, rifts and encounter parameters. Scene untouched.\n");
            Debug.Log("[Warden ritual] Updated spell prefabs and encounter parameters.");
        }
    }
}
