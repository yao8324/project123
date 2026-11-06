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
    /// <summary>Local art iteration over the latest six-move production assets. Never rebuilds combat data.</summary>
    public static class KnightOathStyleBuilder
    {
        public const string Root = "Assets/Art/Bosses/Knight/OathStyle";
        public const string Output = "Docs/KnightStyle20261106";
        const string V5 = KnightProductionBuilder.Root;
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static T Save<T>(T asset, string path) where T : Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old)
            {
                if(asset is Mesh sourceMesh && old is Mesh targetMesh)
                {
                    targetMesh.Clear();targetMesh.indexFormat=sourceMesh.indexFormat;
                    targetMesh.vertices=sourceMesh.vertices;targetMesh.uv=sourceMesh.uv;targetMesh.normals=sourceMesh.normals;targetMesh.tangents=sourceMesh.tangents;
                    targetMesh.bindposes=sourceMesh.bindposes;if(sourceMesh.boneWeights.Length>0)targetMesh.boneWeights=sourceMesh.boneWeights;
                    targetMesh.subMeshCount=sourceMesh.subMeshCount;for(int i=0;i<sourceMesh.subMeshCount;i++)targetMesh.SetTriangles(sourceMesh.GetTriangles(i),i);
                    targetMesh.bounds=sourceMesh.bounds;targetMesh.UploadMeshData(false);
                }
                else if(asset is Material sourceMaterial && old is Material targetMaterial)
                {targetMaterial.shader=sourceMaterial.shader;targetMaterial.CopyPropertiesFromMaterial(sourceMaterial);}
                else EditorUtility.CopySerialized(asset, old);
                Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;
            }
            AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static Material Material(string name, Color color, float metal, float smooth, bool glow = false)
        {
            if (materials.TryGetValue(name, out var found)) return found;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name = name;
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * .2f); }
            return materials[name] = Save(m, Root + "/Materials/" + name + ".mat");
        }
        static Material Remap(Material source, bool arena)
        {
            string name = source.name.Replace("Oath_", ""); string key = "Oath_" + name;
            if (materials.TryGetValue(key, out var found)) return found;
            var m = new Material(source); m.name = key; string n = name.ToLowerInvariant();
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white; float alpha = c.a;
            if (arena)
            {
                if (n.Contains("floor")) c = new Color(.25f, .28f, .31f);
                else if (n.Contains("recessed")) c = new Color(.24f, .27f, .31f);
                else if (n.Contains("ivory")) c = new Color(.57f, .59f, .55f);
                else if (n.Contains("brass") || n.Contains("gold")) c = new Color(.37f, .27f, .13f);
            }
            else
            {
                if (n.Contains("silver")) { c = new Color(.57f, .64f, .70f); m.SetFloat("_Metallic", .78f); m.SetFloat("_Smoothness", .46f); }
                else if (n.Contains("ivory")) c = new Color(.72f, .69f, .58f);
                else if (n.Contains("veil")) { c = new Color(.42f, .43f, .40f); m.SetFloat("_Smoothness", .16f); }
                else if (n.Contains("black") || n.Contains("joint")) c = new Color(.055f, .075f, .095f);
                else if (n.Contains("gold")) c = new Color(.57f, .39f, .14f);
                if (n.Contains("light")) m.SetColor("_EmissionColor", new Color(.28f, .17f, .035f));
            }
            c.a = alpha; m.SetColor("_BaseColor", c);
            return materials[key] = Save(m, Root + "/Materials/" + key + ".mat");
        }

        // Geometric authoring remains editable as meshes. Rigid armor weights use existing skeleton indices.
        sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<BoneWeight> weights = new List<BoneWeight>();
            public readonly List<List<int>> triangles;
            public Matrix4x4 matrix = Matrix4x4.identity; public int bone, vaultVertices;
            public Geometry(int slots) { triangles = Enumerable.Range(0, slots).Select(i => new List<int>()).ToList(); }
            int Vertex(Vector3 v) { int i = vertices.Count; vertices.Add(matrix.MultiplyPoint3x4(v)); uv.Add(new Vector2(v.x, v.y)); weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1 }); return i; }
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, int slot, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0) { var swap = b; b = c; c = swap; }
                triangles[slot].AddRange(new[] { Vertex(a), Vertex(b), Vertex(c) });
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int slot, Vector3 normal)
            { Triangle(a, b, c, slot, normal); Triangle(a, c, d, slot, normal); }
            public void Box(Vector3 p, Vector3 size, int slot, Quaternion rotation = default(Quaternion))
            {
                if (rotation.x == 0 && rotation.y == 0 && rotation.z == 0 && rotation.w == 0) rotation = Quaternion.identity;
                var previous = matrix; matrix *= Matrix4x4.TRS(p, rotation, size);
                Vector3[] v = { new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f) };
                Quad(v[4],v[5],v[6],v[7],slot,Vector3.forward);Quad(v[1],v[0],v[3],v[2],slot,Vector3.back);
                Quad(v[0],v[4],v[7],v[3],slot,Vector3.left);Quad(v[5],v[1],v[2],v[6],slot,Vector3.right);
                Quad(v[3],v[7],v[6],v[2],slot,Vector3.up);Quad(v[0],v[1],v[5],v[4],slot,Vector3.down); matrix = previous;
            }
            public void Line(Vector3 a, Vector3 b, float width, int slot)
            { Box((a+b)/2, new Vector3(width,width,Vector3.Distance(a,b)), slot, Quaternion.LookRotation((b-a).normalized)); }
            public void Plate(Vector2[] outline, float z, float depth, int slot)
            {
                Vector2 center = outline.Aggregate(Vector2.zero, (a,b)=>a+b)/outline.Length;
                for (int i=0;i<outline.Length;i++)
                {
                    var a=new Vector3(outline[i].x,outline[i].y,z);var n=outline[(i+1)%outline.Length];var b=new Vector3(n.x,n.y,z);
                    Triangle(a,b,new Vector3(center.x,center.y,z+.025f),slot,Vector3.forward);
                    Quad(a-Vector3.forward*depth,b-Vector3.forward*depth,b,a,slot,new Vector3((a+b).x/2-center.x,(a+b).y/2-center.y,0));
                }
            }
            public void Arc(Vector3 center, float radius, float width, float start, float sweep, int steps, int slot, bool ground=false)
            {
                for(int i=0;i<steps;i++)
                {
                    float a=(start+sweep*i/steps)*Mathf.Deg2Rad,b=(start+sweep*(i+1)/steps)*Mathf.Deg2Rad;
                    Func<float,float,Vector3> p=(angle,r)=>center+(ground?new Vector3(Mathf.Sin(angle)*r,0,Mathf.Cos(angle)*r):new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,0));
                    Quad(p(a,radius-width/2),p(b,radius-width/2),p(b,radius+width/2),p(a,radius+width/2),slot,ground?Vector3.up:Vector3.forward);
                }
            }
            public void Crest(Vector3 center,float scale,int metal,int light,bool ground=false)
            {
                Arc(center,scale,.07f*scale,112,316,40,metal,ground);
                Vector3 up=ground?Vector3.forward:Vector3.up, right=Vector3.right;
                Line(center-up*scale*1.3f,center+up*scale*.8f,.07f*scale,metal);
                Line(center+up*scale*.75f-right*scale*.35f,center+up*scale*1.08f,.05f*scale,metal);
                Line(center+up*scale*1.08f,center+up*scale*.75f+right*scale*.35f,.05f*scale,metal);
                Box(center+up*scale*.15f,new Vector3(.09f*scale,ground?.008f:.35f*scale,ground?.35f*scale:.01f),light);
            }
            public Mesh Mesh(string name, Mesh original=null)
            {
                var mesh=new Mesh { name=name,indexFormat=IndexFormat.UInt32 };int offset=0;
                if(original)
                {
                    offset=original.vertexCount;vertices.InsertRange(0,original.vertices);uv.InsertRange(0,original.uv);weights.InsertRange(0,original.boneWeights);
                }
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=triangles.Count+(original?original.subMeshCount:0);
                if(original)for(int i=0;i<original.subMeshCount;i++)mesh.SetTriangles(original.GetTriangles(i),i);
                for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i].Select(n=>n+offset).ToArray(),i+(original?original.subMeshCount:0));
                if(original){mesh.bindposes=original.bindposes;mesh.boneWeights=weights.ToArray();}
                mesh.RecalculateNormals();
                if(vaultVertices>0){var normals=mesh.normals;for(int i=0;i<vaultVertices;i++){var v=vertices[i];normals[i]=-new Vector3(v.x/(14.45f*14.45f),(v.y-6.05f)/(4.45f*4.45f),v.z/(14.45f*14.45f)).normalized;}mesh.normals=normals;}
                mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
            }
        }

        static void Armor()
        {
            var model=PrefabUtility.LoadPrefabContents(V5+"/KnightProduction.prefab");
            try
            {
                var visual=model.GetComponent<KnightVisuals>();var rig=visual.productionRig;
                var silver=Material("Oath_PlanishedSilver",new Color(.60f,.66f,.70f),.78f,.44f);
                var gold=Material("Oath_WornEngraving",new Color(.49f,.33f,.12f),.76f,.39f);
                var seal=Material("Oath_QuietSeal",new Color(.7f,.49f,.19f),.45f,.36f,true);
                var slots=new List<KnightOathPresentation.SealSlot>();
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    string lod=skin.name.Contains("LOD1")?"LOD1":"LOD0";
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(V5+"/Meshes/KnightSkin_"+lod+".asset");
                    if(!original)throw new InvalidOperationException("Missing canonical V5 skin "+lod);
                    var g=new Geometry(3);
                    Action<Transform> bind=t=>{g.matrix=skin.transform.worldToLocalMatrix*t.localToWorldMatrix;g.bone=Array.IndexOf(skin.bones,t);if(g.bone<0)throw new InvalidOperationException("Missing armor attachment bone "+t.name);};
                    bind(visual.torso);
                    var plate=new[]{new Vector2(-.28f,.32f),new Vector2(-.32f,.85f),new Vector2(-.21f,1.07f),new Vector2(0,1.16f),new Vector2(.21f,1.07f),new Vector2(.32f,.85f),new Vector2(.28f,.32f),new Vector2(0,.13f)};
                    g.Plate(plate,.425f,.035f,0);
                    for(int i=0;i<plate.Length;i++)g.Line(new Vector3(plate[i].x,plate[i].y,.46f),new Vector3(plate[(i+1)%plate.Length].x,plate[(i+1)%plate.Length].y,.46f),.014f,1);
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<(lod=="LOD0"?2:1);i++)g.Line(new Vector3(side*(.20f+i*.045f),.35f,.465f),new Vector3(side*(.22f+i*.045f),.91f,.465f),.008f,1);
                    g.Crest(new Vector3(0,.69f,.49f),.17f,1,2);
                    bind(rig.head);
                    g.Line(new Vector3(-.14f,.10f,.271f),new Vector3(0,.16f,.271f),.009f,1);
                    g.Line(new Vector3(0,.16f,.271f),new Vector3(.14f,.10f,.271f),.009f,1);
                    for(int side=-1;side<=1;side+=2)g.Line(new Vector3(side*.13f,.075f,.272f),new Vector3(side*.10f,-.06f,.272f),.008f,1);
                    foreach(var shin in new[]{visual.leftShin,visual.rightShin}){bind(shin);g.Plate(new[]{new Vector2(-.11f,-.08f),new Vector2(0,.02f),new Vector2(.11f,-.08f),new Vector2(.065f,-.33f),new Vector2(0,-.40f),new Vector2(-.065f,-.33f)},.225f,.02f,0);g.Line(new Vector3(0,-.1f,.26f),new Vector3(0,-.36f,.26f),.012f,1);}
                    var combined=g.Mesh("OathSkin_"+lod,original);skin.sharedMesh=Save(combined,Root+"/Meshes/OathSkin_"+lod+".asset");
                    skin.sharedMaterials=skin.sharedMaterials.Take(original.subMeshCount).Select(m=>Remap(m,false)).Concat(new[]{silver,gold,seal}).ToArray();skin.localBounds=skin.sharedMesh.bounds;
                    slots.Add(new KnightOathPresentation.SealSlot{renderer=skin,materialIndex=original.subMeshCount+2});
                }
                foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>Remap(m,false)).ToArray();
                var presentation=model.GetComponent<KnightOathPresentation>();if(!presentation)presentation=model.AddComponent<KnightOathPresentation>();presentation.seals=slots.ToArray();visual.oathPresentation=presentation;
                PrefabUtility.SaveAsPrefabAsset(model,V5+"/KnightProduction.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(model);}
        }
        static void Forecourt()
        {
            var arena=PrefabUtility.LoadPrefabContents(V5+"/KnightArenaProduction.prefab");
            try
            {
                var runtime=arena.GetComponent<BossArena>();var old=arena.transform.Find("Oath architecture");if(old)Object.DestroyImmediate(old.gameObject);
                foreach(var r in arena.GetComponentsInChildren<MeshRenderer>(true))if(!r.transform.IsChildOf(runtime.boss)&&!r.transform.IsChildOf(runtime.effectRoot))r.sharedMaterials=r.sharedMaterials.Select(m=>Remap(m,true)).ToArray();
                var stone=Material("Oath_VaultSlate",new Color(.13f,.17f,.22f),.05f,.24f);
                var inlay=Material("Oath_QuietFloorBronze",new Color(.31f,.24f,.14f),.55f,.32f);
                var glass=Material("Oath_DeepBlueGlass",new Color(.055f,.12f,.16f),.15f,.40f,true);
                var wood=Material("Oath_ArchiveCase",new Color(.13f,.10f,.075f),.05f,.2f);
                var cord=Material("Oath_TransferBlue",new Color(.075f,.25f,.34f),.03f,.25f);
                var g=new Geometry(5);
                for(int sector=0;sector<32;sector++)for(int band=0;band<5;band++)
                {
                    Func<int,int,Vector3> p=(s,b)=>{float a=s*Mathf.PI*2/32,h=b*Mathf.PI/10;return new Vector3(Mathf.Sin(a)*14.45f*Mathf.Cos(h),6.05f+4.45f*Mathf.Sin(h),Mathf.Cos(a)*14.45f*Mathf.Cos(h));};
                    var a0=p(sector,band);var b0=p(sector+1,band);var c0=p(sector+1,band+1);var d0=p(sector,band+1);
                    g.Quad(a0,b0,c0,d0,0,-(a0+b0+c0+d0)/4);
                }
                g.vaultVertices=g.vertices.Count;
                foreach(float radius in new[]{2.55f,6.8f,10.9f})g.Arc(new Vector3(0,.018f,0),radius,.022f,0,360,128,1,true);
                g.Crest(new Vector3(0,.025f,0),1.35f,1,1,true);
                for(int side=-1;side<=1;side+=2)g.Line(new Vector3(side*.72f,.018f,-11.9f),new Vector3(side*.72f,.018f,11.6f),.022f,1);
                for(int i=0;i<24;i++){float a=i*Mathf.PI/12;var p=new Vector3(Mathf.Sin(a)*10.9f,.022f,Mathf.Cos(a)*10.9f);g.Line(p,p+p.normalized*.26f,.033f,1);}
                foreach(float degrees in new[]{-112.5f,-67.5f,67.5f,112.5f})
                {
                    float a=degrees*Mathf.Deg2Rad;var center=new Vector3(Mathf.Sin(a)*15.1f,3.35f,Mathf.Cos(a)*15.1f);
                    g.matrix=Matrix4x4.TRS(center,Quaternion.LookRotation(new Vector3(-center.x,0,-center.z)),Vector3.one);
                    var edge=new[]{new Vector2(-.58f,-1.15f),new Vector2(.58f,-1.15f),new Vector2(.58f,.48f),new Vector2(.30f,1.02f),new Vector2(0,1.37f),new Vector2(-.30f,1.02f),new Vector2(-.58f,.48f)};
                    g.Plate(edge,.015f,.06f,2);for(int i=0;i<edge.Length;i++)g.Line(new Vector3(edge[i].x,edge[i].y,.055f),new Vector3(edge[(i+1)%edge.Length].x,edge[(i+1)%edge.Length].y,.055f),.033f,1);
                    g.Crest(new Vector3(0,.05f,.06f),.30f,1,1);g.Line(new Vector3(0,-1.1f,.06f),new Vector3(0,1.25f,.06f),.019f,1);
                }
                g.matrix=Matrix4x4.identity;
                for(int side=-1;side<=1;side+=2)
                {
                    g.Line(new Vector3(side*.07f,1.25f,13.21f),new Vector3(side*.07f,3.50f,13.21f),.012f,1);
                    g.Line(new Vector3(side*.32f,3.45f,13.21f),new Vector3(0,3.78f,13.21f),.018f,1);
                }
                for(int n=0;n<2;n++)
                {
                    var p=new Vector3(-4+n*1.4f,.82f,-12.6f);g.Box(p,new Vector3(.80f,.43f,.48f),3);
                    for(int side=-1;side<=1;side+=2)g.Box(p+new Vector3(side*.27f,0,0),new Vector3(.035f,.46f,.51f),1);
                    g.Box(p+Vector3.up*.225f,new Vector3(.035f,.012f,.51f),4);g.Box(p+new Vector3(0,0,.25f),new Vector3(.035f,.43f,.012f),4);
                }
                var added=new GameObject("Oath architecture");added.transform.SetParent(arena.transform,false);
                added.AddComponent<MeshFilter>().sharedMesh=Save(g.Mesh("OathVaultInlayAndTransferCases"),Root+"/Meshes/OathVaultInlayAndTransferCases.asset");
                added.AddComponent<MeshRenderer>().sharedMaterials=new[]{stone,inlay,glass,wood,cord};
                foreach(var light in arena.GetComponentsInChildren<Light>())
                {
                    if(light.name.Contains("silver key")){light.intensity=110;light.color=new Color(.88f,.93f,1);}
                    if(light.name.Contains("warm fill")){light.intensity=25;light.color=new Color(1,.82f,.61f);}
                }
                PrefabUtility.SaveAsPrefabAsset(arena,V5+"/KnightArenaProduction.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(arena);}
        }
        [MenuItem("Arcane Archive/Knight/Apply Oath style pass")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before editing assets.");
            foreach(var path in new[]{Root,Root+"/Meshes",Root+"/Materials",Output})Directory.CreateDirectory(path);
            AssetDatabase.Refresh();materials.Clear();Armor();Forecourt();AssetDatabase.SaveAssets();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(V5+"/KnightProduction.prefab");var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            File.WriteAllText(Output+"/ArtManifest.json",JsonUtility.ToJson(new ArtManifest{source="lixiang 60f452b / 1.0.8",model=V5+"/KnightProduction.prefab",arena=V5+"/KnightArenaProduction.prefab",boneCount=skins[0].bones.Length,bodyRendererComponents=model.GetComponentsInChildren<Renderer>(true).Length,lodTriangles=skins.Select(s=>s.sharedMesh.triangles.Length/3).ToArray(),arenaLights=3},true));
            Debug.Log("KNIGHT_OATH_STYLE_BUILT");
        }
        [Serializable] class ArtManifest{public string source,model,arena;public int boneCount,bodyRendererComponents,arenaLights;public int[] lodTriangles;}
        [Serializable] class WindowsReport { public string result, executable; public int errors, warnings; public ulong bytes; public float seconds; }
        public static void BuildWindows()
        {
            string destination="Builds/KnightOathStyle/IMMonster-Knight.exe";Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{KnightProductionBuilder.TestScene},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
            File.WriteAllText(Output+"/WindowsBuild.json",JsonUtility.ToJson(new WindowsReport{result=report.summary.result.ToString(),executable=destination,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=(float)report.summary.totalTime.TotalSeconds},true));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Oath style Windows build failed");
        }
    }
}
