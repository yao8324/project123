using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using ArcaneArchive.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>V5 authoring source. Builds new assets, retaining the complete V4 model and numeric configuration.</summary>
    public static class KnightProductionBuilder
    {
        public const string Root = "Assets/Art/Bosses/Knight/ProductionV5";
        public const string Config = Root + "/KnightProduction.asset";
        public const string TestScene = "Assets/Scenes/BossTests/Knight_Production_Test.unity";
        public const string Output = "Docs/KnightDesign20261105/ProductionV5";
        static readonly Dictionary<Material, Material> remapped = new Dictionary<Material, Material>();
        static Material stone, wall, ivory, brass, steel, trim, glow;
        static int meshId;

        static GameObject Node(string name, Transform parent, Vector3 position = default(Vector3))
        { var g = new GameObject(name); if (parent) g.transform.SetParent(parent, false); g.transform.localPosition = position; return g; }
        static T Save<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing) { EditorUtility.CopySerialized(asset, existing); Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static Mesh Finish(Mesh mesh, string name)
        {
            mesh.name = name; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
            return Save(mesh, Root + "/Meshes/" + (meshId++).ToString("D3") + "_" + name + ".asset");
        }
        static GameObject Geometry(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
        {
            var g = Node(name, parent, position); g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = material; return g;
        }
        static void Quad(List<int> t, int a, int b, int c, int d) { t.AddRange(new[] { a, b, c, a, c, d }); }
        static Mesh BeveledBox(string name, Vector3 size, float bevel)
        {
            float x = size.x / 2, y = size.y / 2, z = size.z / 2;
            float b = Mathf.Min(bevel, Mathf.Min(x, Mathf.Min(y, z)) * .8f);
            Vector2[] shape = { new Vector2(-x+b,-y), new Vector2(x-b,-y),new Vector2(x,-y+b),new Vector2(x,y-b),new Vector2(x-b,y),new Vector2(-x+b,y),new Vector2(-x,y-b),new Vector2(-x,-y+b) };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int row = 0; row < 4; row++) foreach (var p in shape)
            {
                float inset = row == 0 || row == 3 ? b : 0;
                v.Add(new Vector3(p.x * (1 - inset / x), p.y * (1 - inset / y), row == 0 ? -z : row == 1 ? -z + b : row == 2 ? z - b : z));
            }
            v.Add(new Vector3(0, 0, -z)); v.Add(new Vector3(0, 0, z));
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8; t.AddRange(new[] { 32, j, i, 33, 24 + i, 24 + j });
                for (int row = 0; row < 3; row++) Quad(t, row * 8 + i, row * 8 + j, (row + 1) * 8 + j, (row + 1) * 8 + i);
            }
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.uv = v.Select(p => new Vector2(p.x + p.z * .3f, p.y + p.z * .3f)).ToArray();
            return Finish(mesh, name);
        }
        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, float bevel = .04f)
        { return Geometry(name, parent, position, BeveledBox(name, size, bevel), material); }
        static Mesh Lathe(string name, Vector2[] profile, int segments = 16)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            for (int row = 0; row < profile.Length; row++) for (int n = 0; n <= segments; n++)
            { float a = n * Mathf.PI * 2 / segments; v.Add(new Vector3(Mathf.Cos(a) * profile[row].x, profile[row].y, Mathf.Sin(a) * profile[row].x)); uv.Add(new Vector2(n / (float)segments, profile[row].y * .5f)); }
            for (int row = 0; row < profile.Length - 1; row++) for (int n = 0; n < segments; n++) Quad(t, row * (segments+1)+n, (row+1)*(segments+1)+n, (row+1)*(segments+1)+n+1, row*(segments+1)+n+1);
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.SetUVs(0, uv); return Finish(mesh, name);
        }
        static GameObject Arch(string name, Transform parent, Vector3 center, float halfWidth, float spring, float rise, float width, float depth, Material material)
        {
            const int steps = 28; var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float x = halfWidth * (i * 2f / steps - 1), u = x / halfWidth;
                float y = spring + rise * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(u)), .65f);
                float previousX = halfWidth * ((Mathf.Max(0, i-1)) * 2f / steps - 1);
                float nextX = halfWidth * ((Mathf.Min(steps, i+1)) * 2f / steps - 1);
                float previousY = spring + rise * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(previousX / halfWidth)), .65f);
                float nextY = spring + rise * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(nextX / halfWidth)), .65f);
                Vector3 tangent = new Vector3(nextX-previousX, nextY-previousY, 0).normalized;
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0) * width / 2;
                Vector3 p = new Vector3(x, y, 0);
                foreach (var q in new[] { p-side-Vector3.forward*depth/2, p+side-Vector3.forward*depth/2, p+side+Vector3.forward*depth/2, p-side+Vector3.forward*depth/2 })
                { v.Add(q); uv.Add(new Vector2(i / (float)steps * 4, q.z)); }
                if (i > 0) for (int n = 0; n < 4; n++) Quad(t, (i-1)*4+n, i*4+n, i*4+(n+1)%4, (i-1)*4+(n+1)%4);
            }
            Quad(t, 3, 2, 1, 0); Quad(t, steps*4,steps*4+1,steps*4+2,steps*4+3);
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.SetUVs(0, uv);
            return Geometry(name, parent, center, Finish(mesh, name), material);
        }
        static Material Surface(string name, Color color, float metal, float smooth, bool stoneGrain = false, bool emissive = false)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name = name;
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            string texturePath = Root + "/Textures/" + (stoneGrain ? "StoneGrain.png" : "MetalGrain.png");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            m.SetTextureScale("_BaseMap", new Vector2(stoneGrain ? 2 : 1, stoneGrain ? 2 : 1));
            if(stoneGrain)
            {
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MilanLibraryV6/Textures/IV4_Interior_AgedLimestone_BaseColor.png"));
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MilanLibraryV6/Textures/IV4_Interior_AgedLimestone_Normal.png"));
                m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.32f);
            }
            if (emissive) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(.15f,.08f,.02f)); }
            return Save(m, Root + "/Materials/" + name + ".mat");
        }
        static void Textures()
        {
            foreach (bool grain in new[] { false, true })
            {
                var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false); var colors = new Color32[256*256];
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    float noise = Mathf.PerlinNoise(x*(grain?.061f:.19f), y*(grain?.061f:.022f));
                    byte value = (byte)Mathf.Clamp(230 + noise * 25, 0, 255); colors[y*256+x] = new Color32(value,value,value,255);
                }
                texture.SetPixels32(colors); texture.Apply(); string path = Root + "/Textures/" + (grain ? "StoneGrain.png" : "MetalGrain.png");
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true; importer.SaveAndReimport();
            }
        }
        static Material Remap(Material source)
        {
            if (remapped.TryGetValue(source, out var material)) return material;
            material = new Material(source); material.name = source.name + "_Production";
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", source.name.Contains("Silver") ? .43f : source.name.Contains("Ivory") ? .24f : .36f);
            if (material.HasProperty("_BaseMap") && !source.name.Contains("Light") && !source.name.Contains("Veil")) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/MetalGrain.png"));
            material = Save(material, Root + "/Materials/" + material.name + ".mat"); remapped[source] = material; return material;
        }

        static Transform Foot(Transform shin, string name)
        {
            var shoe=shin.Cast<Transform>().FirstOrDefault(t=>t.name.Contains("Sabatons"));
            var foot = Node(name, shin, shoe?shoe.localPosition:new Vector3(0,-.59f,.12f)).transform;
            foreach (Transform child in shin.Cast<Transform>().ToArray()) if (child.name.Contains("Sabatons")) child.SetParent(foot, true);
            return foot;
        }
        static void NormalizeRigScale(KnightVisuals visual)
        {
            // The V4 authoring root used non-uniform scale. Bake it into the mesh and bind positions;
            // analytic limb constraints require orthonormal bones, rather than sheared parent matrices.
            var transforms=visual.visualRoot.GetComponentsInChildren<Transform>(true);
            var positions=transforms.Select(t=>t.position).ToArray();var rotations=transforms.Select(t=>t.rotation).ToArray();
            var filters=visual.visualRoot.GetComponentsInChildren<MeshFilter>(true);
            var matrices=filters.Select(f=>f.transform.localToWorldMatrix).ToArray();
            for(int i=0;i<transforms.Length;i++){transforms[i].localScale=Vector3.one;transforms[i].SetPositionAndRotation(positions[i],rotations[i]);}
            for(int i=0;i<filters.Length;i++)
            {
                var source=filters[i].sharedMesh;var mesh=Object.Instantiate(source);
                Matrix4x4 matrix=filters[i].transform.worldToLocalMatrix*matrices[i];
                mesh.vertices=source.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                mesh.normals=source.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                mesh.RecalculateBounds();mesh.RecalculateTangents();filters[i].sharedMesh=Save(mesh,Root+"/Meshes/Normalized_"+i.ToString("D3")+".asset");
            }
            visual.visualRoot.localPosition+=Vector3.down*.055f;
        }
        static Transform[] PrepareRig(KnightVisuals visual, out KnightRigConstraints constraints)
        {
            constraints = visual.gameObject.AddComponent<KnightRigConstraints>(); constraints.visual = visual;
            constraints.rightGrip = Node("Grip_R", visual.weapon, new Vector3(0,.14f,-.08f)).transform;
            constraints.leftGrip = Node("Grip_L", visual.weapon, new Vector3(0,.70f,0)).transform;
            constraints.leftFoot = Foot(visual.leftShin, "Foot_L"); constraints.rightFoot = Foot(visual.rightShin, "Foot_R");
            constraints.head = Node("Head", visual.torso, new Vector3(0,1.52f,0)).transform;
            foreach (Transform child in visual.torso.Cast<Transform>().ToArray()) if (child != constraints.head && (child.name.Contains("helmet") || child.name.Contains("faceplate") || child.name.Contains("eye slit") || child.name.Contains("coronet crest"))) child.SetParent(constraints.head, true);
            var bones = new List<Transform> { visual.visualRoot,visual.torso,constraints.head,visual.rightArm,visual.rightForearm,visual.rightHand,visual.leftArm,visual.leftForearm,visual.leftHand,visual.leftThigh,visual.leftShin,visual.rightThigh,visual.rightShin,constraints.leftFoot,constraints.rightFoot,visual.halo };
            constraints.ribbons = new KnightRigConstraints.RibbonChain[visual.sealRibbons.Length];
            for (int chain = 0; chain < visual.sealRibbons.Length; chain++)
            {
                var ribbon = visual.sealRibbons[chain]; bones.Add(ribbon);
                var mesh = ribbon.GetComponentInChildren<MeshFilter>().sharedMesh; var vertices = mesh.vertices;
                var chainBones = new List<Transform>(); Transform parent = ribbon;
                for (int i = 1; i <= 6; i++)
                {
                    int ring = Mathf.Min((vertices.Length / 2) - 1, i * 3);
                    var bone = Node("Ribbon_" + chain.ToString("D2") + "_" + i, parent).transform;
                    bone.position = ribbon.TransformPoint((vertices[ring*2]+vertices[ring*2+1])/2); chainBones.Add(bone); bones.Add(bone); parent = bone;
                }
                constraints.ribbons[chain] = new KnightRigConstraints.RibbonChain { bones = chainBones.ToArray() };
            }
            visual.productionRig = constraints;
            // Resolved attachment contract, independent of the boss's combat collider.
            Node("SealEmitter", visual.leftHand, new Vector3(0,-.05f,.15f)); Node("ChestSeal", visual.torso, new Vector3(0,.64f,.4f));
            Node("HaloRoot", visual.halo); Node("FootMark_L", constraints.leftFoot, new Vector3(0,-.105f,0)); Node("FootMark_R", constraints.rightFoot, new Vector3(0,-.105f,0));
            return bones.ToArray();
        }
        static bool IsBody(MeshFilter filter, KnightVisuals v)
        {
            if (filter.transform.IsChildOf(v.weapon) || filter.transform.IsChildOf(v.wardShield.transform)) return false;
            return !v.refinedEffects.readyBlades.Any(g => filter.transform.IsChildOf(g.transform));
        }
        static BoneWeight Weights(MeshFilter filter, Vector3 worldVertex, Transform[] bones, KnightVisuals visual)
        {
            var ribbon = visual.sealRibbons.FirstOrDefault(r => filter.transform.IsChildOf(r));
            if (ribbon)
            {
                var candidates = bones.Select((bone,index)=>new {bone,index}).Where(p=>p.bone==ribbon || p.bone.IsChildOf(ribbon)).OrderBy(p=>(p.bone.position-worldVertex).sqrMagnitude).Take(2).ToArray();
                float a = Vector3.Distance(candidates[0].bone.position,worldVertex), b = Vector3.Distance(candidates[1].bone.position,worldVertex);
                float w = b / Mathf.Max(.00001f,a+b); return new BoneWeight { boneIndex0=candidates[0].index,weight0=w,boneIndex1=candidates[1].index,weight1=1-w };
            }
            var parent = filter.transform; while (parent && !bones.Contains(parent)) parent=parent.parent;
            return new BoneWeight { boneIndex0=Mathf.Max(0,Array.IndexOf(bones,parent)),weight0=1 };
        }
        static SkinnedMeshRenderer Skin(GameObject model, KnightVisuals visual, MeshFilter[] pieces, Transform[] bones, string name)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var weights = new List<BoneWeight>();
            var materials = pieces.Select(p=>p.GetComponent<MeshRenderer>().sharedMaterial).Distinct().ToArray();
            var triangles = materials.Select(m=>new List<int>()).ToArray();
            foreach (var piece in pieces)
            {
                Mesh mesh = piece.sharedMesh; Matrix4x4 matrix = model.transform.worldToLocalMatrix * piece.transform.localToWorldMatrix;
                Matrix4x4 normalMatrix = matrix.inverse.transpose; int offset = vertices.Count;
                for (int i=0;i<mesh.vertexCount;i++)
                {
                    vertices.Add(matrix.MultiplyPoint3x4(mesh.vertices[i])); normals.Add(normalMatrix.MultiplyVector(mesh.normals[i]).normalized);
                    uv.Add(mesh.uv.Length==mesh.vertexCount?mesh.uv[i]:Vector2.zero);weights.Add(Weights(piece,piece.transform.TransformPoint(mesh.vertices[i]),bones,visual));
                }
                int material = Array.IndexOf(materials,piece.GetComponent<MeshRenderer>().sharedMaterial);
                triangles[material].AddRange(mesh.triangles.Select(i=>i+offset));
            }
            var result = new Mesh { name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
            result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.boneWeights=weights.ToArray();
            result.bindposes=bones.Select(b=>b.worldToLocalMatrix*model.transform.localToWorldMatrix).ToArray();result.subMeshCount=materials.Length;
            for(int i=0;i<triangles.Length;i++)result.SetTriangles(triangles[i],i);result.RecalculateBounds();result.RecalculateTangents();
            result=Save(result,Root+"/Meshes/"+name+".asset");var renderer=Node(name,model.transform).AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh=result;renderer.sharedMaterials=materials;renderer.bones=bones;renderer.rootBone=visual.visualRoot;
            renderer.localBounds=new Bounds(new Vector3(0,1.5f,0),new Vector3(7,8,7));renderer.quality=SkinQuality.Bone2;renderer.updateWhenOffscreen=false;return renderer;
        }
        static void CombineStatic(Transform root, string prefix)
        {
            var pieces=root.GetComponentsInChildren<MeshFilter>(true).Where(p=>p.GetComponent<MeshRenderer>() && p.GetComponent<MeshRenderer>().enabled).ToArray();
            foreach(var group in pieces.GroupBy(p=>p.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var mesh=new Mesh {indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Select(p=>new CombineInstance {mesh=p.sharedMesh,transform=root.worldToLocalMatrix*p.transform.localToWorldMatrix}).ToArray(),true,true);
                var saved=Save(mesh,Root+"/Meshes/"+prefix+"_"+group.Key.name+".asset");
                Geometry(prefix+" "+group.Key.name,root,Vector3.zero,saved,group.Key);
            }
            foreach(var piece in pieces){Object.DestroyImmediate(piece.GetComponent<MeshRenderer>());Object.DestroyImmediate(piece);}
        }

        static GameObject BuildModel()
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Bosses/Knight/Refined/KnightRefined.prefab");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(original);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.name="Sealed Knight Production V5";
            var visual=model.GetComponent<KnightVisuals>();
            NormalizeRigScale(visual);
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterial=Remap(renderer.sharedMaterial);
            var bones=PrepareRig(visual,out var constraints);
            // Structural fasteners, visor frame and fluted greaves are restrained, non-emissive details.
            var rivet=Lathe("ArmorRivet",new[]{new Vector2(.015f,-.012f),new Vector2(.024f,0),new Vector2(.015f,.02f)},8);
            for(int side=-1;side<=1;side+=2)for(int n=0;n<7;n++)
            {
                var item=Geometry("Cuirass fastener",visual.torso,new Vector3(side*(.30f-n*.013f),.91f-n*.1f,.36f),rivet,brass);item.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            foreach(var shin in new[]{visual.leftShin,visual.rightShin})for(int n=-1;n<=1;n++)Box("Greave flute",shin,new Vector3(n*.063f,-.34f,.20f),new Vector3(.018f,.42f,.018f),brass,.006f);
            Box("Visor brow",constraints.head,new Vector3(0,.075f,.257f),new Vector3(.29f,.023f,.026f),steel,.007f);
            PrefabUtility.SaveAsPrefabAsset(model,Root+"/KnightAuthoring.prefab");
            ExportSource(model,null);
            var filters=model.GetComponentsInChildren<MeshFilter>(true).Where(p=>IsBody(p,visual)).ToArray();
            var low=filters.Where(p=>!p.name.Contains("glyph")&&!p.name.Contains("fastener")&&!p.name.Contains("flute")&&!p.name.Contains("fin")&&!p.name.Contains("finger")).ToArray();
            var highSkin=Skin(model,visual,filters,bones,"KnightSkin_LOD0");var lowSkin=Skin(model,visual,low,bones,"KnightSkin_LOD1");
            foreach(var filter in filters){Object.DestroyImmediate(filter.GetComponent<MeshRenderer>());Object.DestroyImmediate(filter);}
            CombineStatic(visual.weapon,"Weapon");CombineStatic(visual.wardShield.transform,"Ward");
            var lod=model.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.22f,new Renderer[]{highSkin}),new LOD(.015f,new Renderer[]{lowSkin})});lod.localReferencePoint=new Vector3(0,1.8f,0);lod.size=5;
            var prefab=PrefabUtility.SaveAsPrefabAsset(model,Root+"/KnightProduction.prefab");Object.DestroyImmediate(model);return prefab;
        }

        static void Plaque(string name, Transform parent, Vector3 position, string text, TMP_FontAsset font)
        {
            Box(name,parent,position,new Vector3(1.1f,.55f,.07f),brass,.025f);
            var label=Node(name+" inscription",parent,position-Vector3.forward*.045f).AddComponent<TextMeshPro>();
            label.font=font;label.text=text;label.fontSize=.17f;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.72f,.69f,.57f);
            label.rectTransform.sizeDelta=new Vector2(1,.45f);label.transform.localRotation=Quaternion.Euler(0,180,0);label.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        static BossArena BuildArena(GameObject model, TMP_FontAsset font)
        {
            const string source="Assets/Art/Bosses/Knight/Refined/KnightArenaRefined.prefab";
            var arena=PrefabUtility.LoadPrefabContents(source);arena.name="Threshold Forecourt Production V5";var runtime=arena.GetComponent<BossArena>();
            Object.DestroyImmediate(runtime.boss.gameObject);var boss=(GameObject)PrefabUtility.InstantiatePrefab(model,arena.transform);runtime.boss=boss.transform;runtime.boss.localPosition=new Vector3(0,0,4);runtime.boss.localRotation=Quaternion.Euler(0,180,0);
            // Keep the original floor and perimeter collision; replace only visual architecture.
            foreach(var filter in arena.GetComponentsInChildren<MeshFilter>(true))
                if(!filter.transform.IsChildOf(runtime.boss) && !filter.transform.IsChildOf(runtime.effectRoot))filter.GetComponent<MeshRenderer>().enabled=false;
            foreach(var light in arena.GetComponentsInChildren<Light>())Object.DestroyImmediate(light.gameObject);
            var geometry=Node("Forecourt authoring geometry",arena.transform).transform;
            var floorMesh=new Mesh();const int segments=192;var v=new List<Vector3>{Vector3.zero};var tri=new List<int>();
            for(int n=0;n<=segments;n++){float a=n*Mathf.PI*2/segments;v.Add(new Vector3(Mathf.Cos(a)*14,0,Mathf.Sin(a)*14));}
            for(int n=0;n<segments;n++)tri.AddRange(new[]{0,n+2,n+1});floorMesh.SetVertices(v);floorMesh.SetTriangles(tri,0);floorMesh.uv=v.Select(p=>new Vector2(p.x*.18f,p.z*.18f)).ToArray();
            Geometry("Unbroken battle floor",geometry,new Vector3(0,.003f,0),Finish(floorMesh,"ForecourtFloor"),stone);
            for(int n=0;n<24;n++)
            {
                float angle=n*15f*Mathf.Deg2Rad;var bay=Node("Perimeter bay "+n.ToString("D2"),geometry,new Vector3(Mathf.Sin(angle)*14.8f,0,Mathf.Cos(angle)*14.8f)).transform;bay.localRotation=Quaternion.Euler(0,n*15f,0);
                Box("Pier plinth",bay,new Vector3(0,.22f,0),new Vector3(.82f,.44f,.72f),wall,.06f);
                for(int rib=-1;rib<=1;rib++)Geometry("Bundled pier rib",bay,new Vector3(rib*.18f,.44f,-.12f*Mathf.Abs(rib)),Lathe("PierRib",new[]{new Vector2(.12f,0),new Vector2(.15f,.12f),new Vector2(.11f,.25f),new Vector2(.11f,4.8f),new Vector2(.18f,4.95f),new Vector2(.15f,5.1f)},12),ivory);
                Box("Abacus capital",bay,new Vector3(0,5.55f,0),new Vector3(.92f,.22f,.8f),ivory,.05f);
                if(n!=0 && n!=12)
                {
                    Arch("Pointed arcade",bay,new Vector3(1.9f,0,.12f),1.65f,3.25f,2.3f,.15f,.23f,ivory);
                    Box("Recessed dark wall",bay,new Vector3(1.8f,2.5f,.6f),new Vector3(3.5f,5,.18f),wall,.04f);
                    Box("Wall dado",bay,new Vector3(1.8f,.75f,.36f),new Vector3(3.5f,.12f,.14f),brass,.025f);
                }
            }
            for(int side=-1;side<=1;side+=2)
            {
                Box("Gate load bearing pier",geometry,new Vector3(side*3.15f,2.65f,13.8f),new Vector3(.85f,5.3f,1.15f),ivory,.10f);
                if(side==1)for(int layer=0;layer<3;layer++)Arch("Gate archivolt "+layer,geometry,new Vector3(0,0,13.25f+layer*.19f),2.8f+layer*.24f,4.6f,2.7f+layer*.22f,.16f,.22f,layer==1?brass:ivory);
                Box("Heavy sealed door leaf",geometry,new Vector3(side*1.09f,2.4f,13.64f),new Vector3(2.15f,4.8f,.42f),steel,.08f);
                for(int panel=0;panel<3;panel++)Box("Door recessed panel",geometry,new Vector3(side*1.09f,.85f+panel*1.5f,13.4f),new Vector3(1.83f,1.28f,.06f),wall,.06f);
                Box("Door gold stile",geometry,new Vector3(side*.17f,2.4f,13.38f),new Vector3(.055f,4.5f,.055f),brass,.014f);
                // No source stone is visible behind this threshold: two staggered opaque layers.
                Box("Behind door baffle",geometry,new Vector3(side*2.7f,3.1f,17.2f),new Vector3(4.2f,6.2f,.6f),wall,.06f);
                for(int n=0;n<6;n++)Box("Outer supply joint",geometry,new Vector3(side*(4.2f+n*1.35f),.012f,11.3f-n*.12f),new Vector3(1.15f,.018f,.045f),brass,.006f);
                for(int n=0;n<3;n++)Box("Repair clamp",geometry,new Vector3(side*3.8f,.6f+n*1.3f,13.1f),new Vector3(.23f,.13f,.3f),steel,.025f);
            }
            var seam=Box("Pressure seam",geometry,new Vector3(0,2.4f,13.37f),new Vector3(.025f,4.45f,.02f),glow,.005f);
            for(int n=0;n<4;n++){var rib=Arch("High vault load rib "+n,geometry,Vector3.zero,13.3f,5.6f,4.5f,.18f,.25f,ivory);rib.transform.localRotation=Quaternion.Euler(0,n*45,0);}
            // Flush stone joints read as large floor modules, without adding raised collision.
            for(int n=0;n<24;n++)
            {
                float a=n*15f*Mathf.Deg2Rad;
                var joint=Box("Flush radial stone joint",geometry,new Vector3(Mathf.Sin(a)*10.25f,.006f,Mathf.Cos(a)*10.25f),new Vector3(.012f,.004f,6.1f),wall,.001f);
                joint.transform.localRotation=Quaternion.Euler(0,n*15f,0);
            }
            var ring=Node("Dual permission rings",geometry,new Vector3(0,2.7f,13.32f)).transform;
            for(int radius=0;radius<2;radius++)for(int n=0;n<48;n++)
            {float a=n*Mathf.PI*2/48;var part=Box("Authority ring segment",ring,new Vector3(Mathf.Cos(a)*(1.07f+radius*.21f),Mathf.Sin(a)*(1.07f+radius*.21f),0),new Vector3(.14f,.018f,.026f),radius==0?brass:glow,.005f);part.transform.localRotation=Quaternion.Euler(0,0,n*7.5f+90);}
            Plaque("Evacuation manifest",geometry,new Vector3(-2.4f,1.45f,-12.5f),"OUTER ARCHIVE\nTRANSFER IN PROGRESS",font);
            Plaque("Maintenance record",geometry,new Vector3(3.9f,1.4f,13.2f),"ISOLATION ATTEMPTED\nOUTER SUPPLY UNSTABLE",font);
            for(int n=0;n<2;n++){var tray=Node("Empty transfer stand "+n,geometry,new Vector3(-4+n*1.4f,.55f,-12.6f));Box("Empty transfer tray",tray.transform,Vector3.zero,new Vector3(1.05f,.12f,.62f),steel,.025f);for(int s=-1;s<=1;s+=2)Box("Tray legs",tray.transform,new Vector3(s*.4f,-.3f,0),new Vector3(.06f,.6f,.45f),brass,.015f);}
            Light key=Node("Production silver key",arena.transform,new Vector3(-6,6,-4)).AddComponent<Light>();key.type=LightType.Spot;key.range=34;key.spotAngle=100;key.intensity=85;key.color=new Color(.87f,.91f,1);key.shadows=LightShadows.Soft;key.transform.LookAt(new Vector3(0,1.8f,4));
            Light fill=Node("Production warm fill",arena.transform,new Vector3(6,5,-1)).AddComponent<Light>();fill.type=LightType.Point;fill.range=24;fill.intensity=18;fill.color=new Color(1,.79f,.52f);
            Light rim=Node("Threshold cold rim",arena.transform,new Vector3(0,5,12.5f)).AddComponent<Light>();rim.type=LightType.Point;rim.range=18;rim.intensity=8;rim.color=new Color(.37f,.61f,1);
            var presentation=arena.AddComponent<KnightArenaPresentation>();presentation.thresholdLight=rim;
            // Keep per-renderer phase properties separate from the static material batches.
            var animated=Node("Animated threshold seals",arena.transform).transform;seam.transform.SetParent(animated,true);
            foreach(Transform child in ring.Cast<Transform>().ToArray())if(child.GetComponent<MeshRenderer>().sharedMaterial==glow)child.SetParent(animated,true);
            CombineStatic(animated,"Threshold");presentation.sealLines=animated.GetComponentsInChildren<Renderer>();
            PrefabUtility.SaveAsPrefabAsset(arena,Root+"/KnightArenaAuthoring.prefab");ExportSource(null,geometry.gameObject);
            CombineStatic(geometry,"Architecture");
            var prefab=PrefabUtility.SaveAsPrefabAsset(arena,Root+"/KnightArenaProduction.prefab").GetComponent<BossArena>();PrefabUtility.UnloadPrefabContents(arena);return prefab;
        }

        [Serializable] public class SourceBundle { public string unit="metre",version="V5";public SourceBone[] bones;public SourceMesh[] meshes; }
        [Serializable] public class SourceBone {public string name,parent;public float[] matrix;}
        [Serializable] public class SourceMesh {public string name,material;public float[] vertices,uv,matrix,color;public int[] triangles;public string bone;}
        static float[] Matrix(Matrix4x4 m){var values=new float[16];for(int row=0;row<4;row++)for(int col=0;col<4;col++)values[row*4+col]=m[row,col];return values;}
        static string PathOf(Transform t,Transform root){return AnimationUtility.CalculateTransformPath(t,root);}
        static void ExportSource(GameObject model,GameObject arena)
        {
            var source=model?model:arena;var root=source.transform;
            var bones=model?source.GetComponentsInChildren<Transform>(true):Array.Empty<Transform>();
            var data=new SourceBundle {bones=bones.Select(b=>new SourceBone{name=PathOf(b,root),parent=b.parent&&b!=root?PathOf(b.parent,root):null,matrix=Matrix(root.worldToLocalMatrix*b.localToWorldMatrix)}).ToArray(),meshes=source.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<MeshRenderer>()).Select(f=>{
                var mesh=f.sharedMesh;var material=f.GetComponent<MeshRenderer>().sharedMaterial;var color=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.gray;
                return new SourceMesh {name=PathOf(f.transform,root),material=material.name,vertices=mesh.vertices.SelectMany(p=>new[]{p.x,p.y,p.z}).ToArray(),uv=mesh.uv.SelectMany(p=>new[]{p.x,p.y}).ToArray(),triangles=mesh.triangles,matrix=Matrix(root.worldToLocalMatrix*f.transform.localToWorldMatrix),color=new[]{color.r,color.g,color.b,color.a},bone=f.transform.parent?PathOf(f.transform.parent,root):""};}).ToArray()};
            Directory.CreateDirectory("Blender/KnightProductionV5");File.WriteAllText("Blender/KnightProductionV5/"+(model?"Knight":"Forecourt")+"Source.json",JsonUtility.ToJson(data));
        }

        [MenuItem("Arcane Archive/Knight/Build V5 production rig and forecourt")]
        public static void Build()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the scene and exit Play Mode before building new assets.");
            foreach(var path in new[]{Root,Root+"/Materials",Root+"/Meshes",Root+"/Textures",Output})Directory.CreateDirectory(path);
            AssetDatabase.Refresh();meshId=0;remapped.Clear();Textures();
            stone=Surface("DarkLimestoneFloor",new Color(.12f,.14f,.17f),.05f,.27f,true);wall=Surface("RecessedWeatheredStone",new Color(.10f,.12f,.15f),.02f,.23f,true);
            ivory=Surface("IvoryLoadBearingStone",new Color(.54f,.53f,.47f),.02f,.29f,true);brass=Surface("OldBrassConnectors",new Color(.38f,.25f,.105f),.74f,.39f);
            steel=Surface("DoorBlackSteel",new Color(.065f,.083f,.11f),.65f,.32f);trim=Surface("QuietGoldTrim",new Color(.42f,.28f,.12f),.7f,.4f);glow=Surface("ThresholdEmission",new Color(.25f,.14f,.04f),.4f,.35f,false,true);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Bosses/Knight/Refined/KnightRefinedChinese.asset");
            var model=BuildModel();var arena=BuildArena(model,font);
            var source=AssetDatabase.LoadAssetAtPath<KnightDefinition>("Assets/Art/Bosses/Knight/Refined/KnightDefinitionRefined.asset");
            var configuration=Object.Instantiate(source);configuration.name="KnightProduction";configuration.arena=arena;configuration.AddMobilityMoves();configuration=Save(configuration,Config);
            var scene=EditorSceneManager.OpenScene(KnightTrialBuilder.ScenePath);var launcher=Object.FindFirstObjectByType<KnightTrialLauncher>();launcher.definition=configuration;launcher.font=font;launcher.storyGate=false;
            foreach(var menu in Object.FindObjectsByType<RecoveryMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))menu.bookKnight=configuration;
            EditorSceneManager.SaveScene(scene,TestScene);
            // Only rewire this development copy's existing book choice, retaining the Warden option and old trial.
            foreach(string path in new[]{"Assets/Scenes/MilanLibrary_Magic_V6.unity","Assets/Scenes/LibraryMain.unity"})
            {var main=EditorSceneManager.OpenScene(path);var menus=Object.FindObjectsByType<RecoveryMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None);foreach(var menu in menus){menu.bookKnight=configuration;EditorUtility.SetDirty(menu);}if(menus.Length>0)EditorSceneManager.SaveScene(main);}
            EditorSceneManager.OpenScene(TestScene);AssetDatabase.SaveAssets();
            File.WriteAllText(Output+"/BuildManifest.json",JsonUtility.ToJson(new BuildReport{unity=Application.unityVersion,sourceConfiguration="Refined/KnightDefinitionRefined.asset",productionConfiguration=Config,scene=TestScene,bones=model.GetComponentsInChildren<SkinnedMeshRenderer>(true)[0].bones.Length,renderers=model.GetComponentsInChildren<Renderer>(true).Length,triangles=model.GetComponentsInChildren<SkinnedMeshRenderer>(true)[0].sharedMesh.triangles.Length/3},true));
            Debug.Log("KNIGHT_V5_BUILD_COMPLETE");
        }
        [Serializable] class BuildReport{public string unity,sourceConfiguration,productionConfiguration,scene;public int bones,renderers,triangles;}
        public static void BuildWindows()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before building.");
            string destination="Builds/KnightProductionV5/IMMonster-KnightV5.exe";Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TestScene},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
            File.WriteAllText(Output+"/WindowsBuild.json",JsonUtility.ToJson(new WindowsReport{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=(float)report.summary.totalTime.TotalSeconds,executable=destination},true));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Windows build failed; inspect the build report.");
        }
        [Serializable] class WindowsReport{public string result,executable;public int errors,warnings;public ulong bytes;public float seconds;}
    }
}
