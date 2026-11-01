using System;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    public static class WardenCombatV2Builder
    {
        public const string Root = "Assets/Art/ForbiddenWarden/CombatV2";
        static Material gold, violet, vellum;
        static GameObject Node(string name, Transform parent = null)
        { var go = new GameObject(name); if (parent) go.transform.SetParent(parent, false); return go; }
        static Material Material(string name, Color color, float metallic, float smooth, float emission = 0)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Cull", 0);
            if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * emission); }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(m); return m;
        }
        static GameObject Model(string name, Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx");
            if (!source) throw new InvalidOperationException("Missing model " + name);
            // FBX empties carry 100x unit conversion and an axis rotation. Animate a metre-space wrapper.
            var wrapper = Node(name + " | metre pivot", parent);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
            go.transform.SetParent(wrapper.transform, false);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name.Contains("Amethyst") ? violet : m.name.Contains("Vellum") ? vellum : gold).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
            return wrapper;
        }
        static LineRenderer Line(string name, Transform parent, Material material, float width, bool world, int count)
        {
            var line = Node(name, parent).AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = world; line.positionCount = count;
            line.startWidth = width; line.endWidth = width * .3f;
            line.numCornerVertices = 3; line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }
        static Transform[] Motes(Transform parent, int count)
        {
            var items = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Gilt mote " + i;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(parent, false);
                go.transform.localScale = Vector3.one * .018f; go.transform.localRotation = Quaternion.Euler(45, 20, 45);
                var r = go.GetComponent<Renderer>(); r.sharedMaterial = i % 3 == 0 ? violet : gold;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; items[i] = go.transform;
            }
            return items;
        }
        static GameObject Save(GameObject go, string name)
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go); return asset;
        }
        static Material BeamMaterial(string name, Color color)
        {
            string texturePath=Root+"/Materials/SoftBeam.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(!texture)
            {
                texture=new Texture2D(32,64,TextureFormat.RGBA32,false,true);texture.name="Soft beam cross-section";
                texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
                for(int x=0;x<32;x++)for(int y=0;y<64;y++)
                {float edge=1-Mathf.Abs((y+.5f)/64*2-1);float cap=Mathf.SmoothStep(0,1,Mathf.Min((x+.5f)/32,1-(x+.5f)/32)*12);texture.SetPixel(x,y,new Color(1,1,1,edge*edge*cap));}
                texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetTexture("_BaseMap",texture);
            mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(int)BlendMode.One);
            mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;
            EditorUtility.SetDirty(mat);return mat;
        }
        static Transform Crystal(string name,Transform parent,Material material)
        {
            string path=Root+"/Models/SpellCrystal.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                mesh=new Mesh{name="Faceted spell focus"};mesh.vertices=new[]{Vector3.up,Vector3.down,Vector3.left*.55f,Vector3.forward*.55f,Vector3.right*.55f,Vector3.back*.55f};
                mesh.triangles=new[]{0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var go=Node(name,parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return go.transform;
        }
        [MenuItem("Arcane Archive/Boss/Connect animated Warden v2")]
        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "MilanLibrary_Magic_V6" || scene.isDirty) throw new InvalidOperationException("Open the saved MilanLibrary_Magic_V6 scene first.");
            foreach (var path in new[] { Root + "/Materials", Root + "/Prefabs" }) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            gold = Material("SpellOldGold", new Color(1, .59f, .20f), .45f, .6f, 2.2f);
            violet = Material("SpellAmethyst", new Color(.39f, .035f, 1), .1f, .4f, 2.4f);
            vellum = Material("SpellVellum", new Color(.64f, .43f, .2f), .12f, .4f, .5f);
            string pathModel = Root + "/Models/SilentWarden_Combat_v2.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(pathModel);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true; importer.optimizeGameObjects = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            var clips = importer.defaultClipAnimations;
            foreach (var c in clips)
            {
                int index = c.name.IndexOf("AA_", StringComparison.Ordinal);
                if (index >= 0) c.name = c.name.Substring(index);
                c.loopTime = c.name == "AA_Idle" || c.name == "AA_Glide" || c.name == "AA_RushDrive" || c.name == "AA_FractureRelease" || c.name == "AA_WaveRelease";
                c.lockRootPositionXZ = true; c.lockRootHeightY = true; c.lockRootRotation = true;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            var importedClips = AssetDatabase.LoadAllAssetsAtPath(pathModel).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (importedClips.Length != 17) throw new InvalidOperationException("Expected 17 animation clips, got " + importedClips.Length);
            var warden = Node("ForbiddenWarden_Animated");
            var body = Node("FloatingBody", warden.transform);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pathModel));
            model.transform.SetParent(body.transform, false); model.transform.localScale = Vector3.one * 1.18f;
            Color[] colors = { new Color(.46f,.28f,.10f),new Color(.76f,.56f,.24f),new Color(.035f,.027f,.055f),new Color(.62f,.64f,.72f),new Color(.055f,.026f,.10f),new Color(.16f,.035f,.052f),new Color(.68f,.55f,.35f),new Color(.77f,.71f,.56f),new Color(.07f,.035f,.018f),new Color(.46f,.12f,.86f),new Color(.34f,.025f,.04f),new Color(.015f,.01f,.025f) };
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                {
                    int n; if (!int.TryParse(m.name.Substring(0, 2), out n)) n = 1;
                    return Material("Warden_" + n.ToString("00"), colors[n - 1], n <= 4 ? .82f : 0, n <= 4 ? .58f : .25f, n == 10 ? 1.2f : 0);
                }).ToArray();
                renderer.updateWhenOffscreen = true;
            }
            var visuals = warden.AddComponent<WardenVisuals>();
            var driver = warden.AddComponent<WardenAnimationDriver>();
            visuals.floatingBody = body.transform; visuals.animatedRig = driver;
            driver.animator = model.GetComponent<Animator>();
            if (!driver.animator) driver.animator = model.AddComponent<Animator>();
            driver.animator.applyRootMotion = false; driver.clips = importedClips;
            var bones = model.GetComponentsInChildren<Transform>();
            driver.leftHand = bones.Single(t => t.name == "hand.L"); driver.rightHand = bones.Single(t => t.name == "hand.R");
            driver.castingSocket = Node("CastSocket", driver.rightHand).transform;
            visuals.leftArm = bones.Single(t => t.name == "upper_arm.L"); visuals.rightArm = bones.Single(t => t.name == "upper_arm.R");
            visuals.mask = bones.Single(t => t.name == "head");
            var capsule = warden.AddComponent<CapsuleCollider>(); capsule.center = new Vector3(0, 2.2f, 0); capsule.height = 3.9f; capsule.radius = .64f;
            var core = Node("CoreHitbox", warden.transform); core.transform.localPosition = new Vector3(0, 2.55f, .88f);
            var sphere = core.AddComponent<SphereCollider>(); sphere.isTrigger = true; sphere.radius = .3f;
            driver.rushRibbons = new[] { Line("Rush gold wake", warden.transform, gold, .045f, false, 18), Line("Rush violet wake", warden.transform, violet, .055f, false, 18) };
            foreach (var line in driver.rushRibbons) line.enabled = false;
            driver.phaseSeal = Model("WardenSealGate", warden.transform).transform; driver.phaseSeal.localPosition = Vector3.up * .12f; driver.phaseSeal.gameObject.SetActive(false);
            importedClips.Single(c => c.name == "AA_Idle").SampleAnimation(model, 0);
            var newWarden = Save(warden, "ForbiddenWardenRigged");

            var page = Node("ForbiddenPage_Illuminated"); var motion = page.AddComponent<WardenSpellMotion>(); motion.projectile = true;
            motion.ornament = Model("WardenForbiddenPage", page.transform).transform;
            motion.ornament.localRotation = Quaternion.Euler(90, 0, 0); motion.ornament.localScale = Vector3.one * 1.55f;
            motion.trail = Line("Amethyst wake", page.transform, violet, .045f, true, 14); motion.motes = Motes(page.transform, 5);
            var newPage = Save(page, "ForbiddenPageIlluminated");
            var bolt=UnityEngine.Object.Instantiate(newPage);bolt.name="PlayerGiltPage";bolt.transform.localScale=Vector3.one*.6f;
            foreach(var renderer in bolt.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>gold).ToArray();
            var newBolt=Save(bolt,"PlayerGiltPage");
            var ring = Node("RunicShockCrown"); motion = ring.AddComponent<WardenSpellMotion>();
            motion.ornament = Node("Slow orbit", ring.transform).transform;
            Model("WardenShockCrown", motion.ornament).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var newRing = Save(ring, "RunicShockCrown");
            var impact = Node("IlluminatedImpact"); motion = impact.AddComponent<WardenSpellMotion>();
            motion.ornament = Node("Scattered seal", impact.transform).transform;
            Model("WardenSealGate", motion.ornament).transform.localScale = Vector3.one * .65f;
            motion.motes = Motes(impact.transform, 12); var newImpact = Save(impact, "IlluminatedImpact");

            var laserRoot = Node("IlluminatedLaser"); var laser = laserRoot.AddComponent<LaserBeamVisuals>();
            laser.outerBeam=Line("Soft amethyst beam",laserRoot.transform,BeamMaterial("BeamViolet",new Color(.55f,.09f,1.4f,.42f)),.6f,true,2);
            laser.coreBeam=Line("Luminous core",laserRoot.transform,BeamMaterial("BeamGold",new Color(2.2f,1.25f,.45f,.85f)),.16f,true,2);
            laser.chargeRing=Node("Charge pivot",laserRoot.transform).transform;
            laser.chargeCore=Crystal("Charge crystal",laserRoot.transform,violet);
            laser.impactCore=Crystal("Impact crystal",laserRoot.transform,gold);
            laser.ornateGate = Model("WardenSealGate", laserRoot.transform).transform;
            laser.violetOrbit = laser.ornateGate.GetComponentsInChildren<Transform>().First(t => t.name.Contains("Spell_Amethyst"));
            laser.goldHelix = Line("Gold helical script", laserRoot.transform, gold, .018f, true, 96);
            laser.violetHelix = Line("Violet helical script", laserRoot.transform, violet, .025f, true, 96);
            var newLaser = Save(laserRoot, "IlluminatedLaser");
            var newFracture = WardenRiftBuilder.BuildAsset();

            var chamber = PrefabUtility.LoadPrefabContents(Root + "/Prefabs/SealChamberRigged.prefab");
            GameObject newArena;
            try
            {
                var arena = chamber.GetComponent<BossArena>(); var oldBoss = arena.boss;
                if(!chamber.GetComponent<WardenArenaPostProcessing>())chamber.AddComponent<WardenArenaPostProcessing>();
                var volume=chamber.GetComponentInChildren<Volume>();
                if(!volume)volume = Node("Spell bloom", chamber.transform).AddComponent<Volume>();
                volume.isGlobal = true; volume.priority = 20;
                string profilePath = Root + "/Materials/SpellBloom.asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
                UnityEngine.Rendering.Universal.Bloom bloom;
                if (!profile.TryGet(out bloom)) { bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true); AssetDatabase.AddObjectToAsset(bloom, profile); }
                bloom.intensity.Override(.42f); bloom.threshold.Override(1.25f); bloom.scatter.Override(.48f);
                EditorUtility.SetDirty(profile); EditorUtility.SetDirty(bloom); volume.sharedProfile = profile;
                var boss = (GameObject)PrefabUtility.InstantiatePrefab(newWarden, oldBoss.parent);
                boss.transform.SetLocalPositionAndRotation(oldBoss.localPosition, oldBoss.localRotation);
                boss.transform.localScale = oldBoss.localScale;
                arena.boss = boss.transform; arena.model = boss.GetComponent<WardenVisuals>();
                UnityEngine.Object.DestroyImmediate(oldBoss.gameObject);
                arena.pageProjectile = newPage; arena.ringEffect = newRing; arena.impactEffect = newImpact;
                arena.boltEffect=newBolt;arena.rushEffect=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/RushTelegraph.prefab").GetComponent<RushTelegraphVisuals>();
                arena.laserEffect = newLaser.GetComponent<LaserBeamVisuals>(); arena.fractureEffect = newFracture.GetComponent<GroundFractureVisuals>();
                newArena = PrefabUtility.SaveAsPrefabAsset(chamber, Root + "/Prefabs/SealChamberRigged.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(chamber); }
            var encounter = UnityEngine.Object.FindFirstObjectByType<BossEncounter>();
            Undo.RecordObject(encounter, "Connect Warden combat v2"); encounter.arenaPrefab = newArena.GetComponent<BossArena>();
            EditorUtility.SetDirty(encounter); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); return "Connected rigged Warden and illuminated spells without legacy prefab dependencies.";
        }
    }
}
