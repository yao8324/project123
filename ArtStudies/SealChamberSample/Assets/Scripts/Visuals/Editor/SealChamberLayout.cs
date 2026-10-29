using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Scene-art assembly only. The candidate has no combat or player-controller dependency.
public static class SealChamberLayout
{
    const string Base = "Assets/Art/SealChamberStudy";
    const string Root = Base + "/Layout";
    const string ScenePath = Root + "/Scenes/SealChamber_LayoutReview.unity";
    const float CombatRadius = 14f, WallRadius = 15.1f, PierRadius = 14.9f;
    static readonly Dictionary<Material, List<CombineInstance>> parts = new Dictionary<Material, List<CombineInstance>>();

    [MenuItem("Art Studies/Seal Chamber/Build circular layout")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save your current scene first.");
        ShaderUtil.allowAsyncCompilation = false;
        foreach (string d in new[] {"Meshes", "Prefabs", "Scenes"}) Directory.CreateDirectory(Root + "/" + d);
        Directory.CreateDirectory("Docs/Layout/Captures"); AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var hall = Node("SealChamber_LayoutCandidate");
        var walls = Node("WallBays_24", hall.transform).transform;
        var piers = Node("Piers_24_OutsideCombatRadius", hall.transform).transform;
        var lights = Node("Lanterns_6", hall.transform).transform;
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15;
            Instantiate("Wall_4.05m_x_8m", walls, Radial(WallRadius, angle), angle);
            var pier = Instantiate("Pier_7.5m", piers, Radial(PierRadius, angle + 7.5f), angle + 7.5f);
            // The simple collider encloses the visible square base as well as the shaft.
            pier.GetComponent<BoxCollider>().size = new Vector3(1.12f, 7.5f, 1.12f);
            if (i % 4 == 0)
            {
                var lantern = Instantiate("Lantern_OptionalRealtimeLight", lights, Radial(14.45f, angle) + Vector3.up * 2.65f, angle);
                var point = lantern.GetComponentInChildren<Light>(); point.range = 7; point.intensity = 4.5f;
            }
        }
        var floor = Node("CircularFloor_30.6m", hall.transform);
        BuildFloor(floor.transform);
        var roof = Node("Vault_Optional", hall.transform);
        BuildVault(roof.transform);
        var markers = Node("PlacementMarkers", hall.transform).transform;
        Node("PlayerSpawn_Reference", markers, new Vector3(0, .02f, -10));
        Node("BossSpawn_Reference_NoActor", markers, new Vector3(0, 0, 4));
        Node("CombatArea_Radius14m", markers);
        PrefabUtility.SaveAsPrefabAsset(hall, Root + "/Prefabs/SealChamber_LayoutCandidate.prefab");
        SetupReviewEnvironment();
        var camera = Node("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.nearClipPlane = .1f; camera.farClipPlane = 150; camera.fieldOfView = 70;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.032f,.05f);
        var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        SetInterior(camera);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        Validate(hall);
        AssetDatabase.SaveAssets();
        // Reload persisted assets so captures reflect the same scene the reviewer opens.
        CaptureSaved();
        Debug.Log("[SealLayout] BUILD_AND_VALIDATION_COMPLETE");
    }

    static GameObject Node(string name, Transform parent = null, Vector3 position = default)
    { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = position; return g; }
    static Vector3 Radial(float radius, float degrees)
    { float a = degrees * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius); }
    static GameObject Instantiate(string name, Transform parent, Vector3 at, float angle)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Prefabs/" + name + ".prefab");
        if (!asset) throw new InvalidOperationException("Missing existing study module: " + name);
        var g = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        g.transform.localPosition = at; g.transform.localRotation = Quaternion.Euler(0, angle, 0);
        return g;
    }
    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Base + "/Materials/" + name + ".mat");
    static Mesh SaveMesh(Mesh fresh, string name)
    {
        string path = Root + "/Meshes/" + name + ".asset"; fresh.name = name;
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!saved) { AssetDatabase.CreateAsset(fresh, path); return fresh; }
        EditorUtility.CopySerialized(fresh, saved); saved.UploadMeshData(false); EditorUtility.SetDirty(saved);
        Object.DestroyImmediate(fresh); return saved;
    }
    static void Queue(Mesh mesh, Material material, Matrix4x4 matrix)
    {
        if (!material) throw new InvalidOperationException("Missing study material.");
        if (!parts.ContainsKey(material)) parts[material] = new List<CombineInstance>();
        parts[material].Add(new CombineInstance { mesh = mesh, transform = matrix });
    }
    static void Flush(Transform parent, string prefix)
    {
        foreach (var group in parts)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(group.Value.ToArray());
            mesh = SaveMesh(mesh, prefix + "_" + group.Key.name);
            var g = Node(mesh.name, parent); g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
        }
        foreach (var m in parts.Values.SelectMany(v => v).Select(c => c.mesh).Distinct()) Object.DestroyImmediate(m);
        parts.Clear();
    }
    static Mesh Annulus(float inner, float outer, float start, float end, float y, int steps)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.Lerp(start, end, i / (float)steps);
            foreach (float r in new[] {inner, outer})
            { var p = Radial(r,a) + Vector3.up*y; v.Add(p); uv.Add(new Vector2(p.x,p.z)*.3f); }
            if(i<steps){int k=i*2;t.AddRange(new[] {k,k+1,k+2,k+2,k+1,k+3});}
        }
        var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetUVs(0,uv); mesh.SetTriangles(t,0); mesh.RecalculateNormals(); return mesh;
    }
    static Mesh DiscSlab(float radius, float top, float bottom)
    {
        var v=new List<Vector3>(); var uv=new List<Vector2>(); var t=new List<int>(); const int sides=192;
        v.Add(new Vector3(0,top,0)); uv.Add(Vector2.zero);
        for(int i=0;i<=sides;i++){var p=Radial(radius,i*360f/sides)+Vector3.up*top;v.Add(p);uv.Add(new Vector2(p.x,p.z));}
        for(int i=0;i<sides;i++)t.AddRange(new[]{0,i+1,i+2});
        for(int i=0;i<sides;i++)
        {
            int k=v.Count;float a=i*360f/sides,b=(i+1)*360f/sides;
            v.Add(Radial(radius,a)+Vector3.up*bottom);v.Add(Radial(radius,a)+Vector3.up*top);
            v.Add(Radial(radius,b)+Vector3.up*bottom);v.Add(Radial(radius,b)+Vector3.up*top);
            uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.one});
            t.AddRange(new[]{k,k+2,k+1,k+2,k+3,k+1});
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
    }
    static void BuildFloor(Transform parent)
    {
        Material dark=Mat("04_Obsidian"), brass=Mat("05_AgedBrass");
        Queue(DiscSlab(15.3f,-.06f,-.5f),dark,Matrix4x4.identity);
        for(int row=0;row<10;row++)for(int cell=0;cell<72;cell++)
        {
            float inner=row*1.24f,outer=(row+1)*1.24f;
            float angle=cell*5, gap=.012f/Mathf.Max(outer,.5f)*Mathf.Rad2Deg;
            Queue(Annulus(inner+(row==0?0:.012f),outer-.012f,angle+gap,angle+5-gap,0,3),dark,Matrix4x4.identity);
        }
        Flush(parent,"FloorCore");
        // Each of these meshes is reused by 24 instances; sectors meet exactly every 15 degrees.
        var sector=Node("FloorSector_15deg_Source");
        for(int row=0;row<4;row++)for(int cell=0;cell<5;cell++)
        {
            float inner=12.4f+row*.725f,outer=inner+.725f;float gap=.012f/outer*Mathf.Rad2Deg;
            Queue(Annulus(inner+.012f,outer-.012f,-7.5f+cell*3+gap,-4.5f+cell*3-gap,0,3),dark,Matrix4x4.identity);
        }
        Flush(sector.transform,"FloorSector15");
        var sectorAsset=PrefabUtility.SaveAsPrefabAsset(sector,Root+"/Prefabs/FloorSector_15deg.prefab");
        Object.DestroyImmediate(sector);
        for(int i=0;i<24;i++)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(sectorAsset,parent);
            go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(0,i*15,0);
        }
        foreach(float radius in new[]{8.8f,12.4f})Queue(Annulus(radius,radius+.035f,0,360,.002f,360),brass,Matrix4x4.identity);
        Flush(parent,"QuietFloorInlays");
        var collider=Node("WalkSurface_Flat_Y0",parent).AddComponent<MeshCollider>();
        collider.sharedMesh=SaveMesh(DiscSlab(15.3f,0,-.5f),"WalkSurface_Collision");
    }
    static void BuildVault(Transform p)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        float[] y={8,9.8f,11.2f,12},r={15.3f,12,7,3.5f};const int sides=96;
        for(int row=0;row<4;row++)for(int i=0;i<=sides;i++)
        {
            var pos=Radial(r[row],i*360f/sides)+Vector3.up*y[row];v.Add(pos);uv.Add(new Vector2(i/(float)sides*8,row));
            if(row<3 && i<sides){int k=row*(sides+1)+i;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
        Queue(mesh,Mat("04_Obsidian"),Matrix4x4.identity);
        var oculus=Annulus(3.4f,3.6f,0,360,12,120);Flip(oculus);
        Queue(oculus,Mat("05_AgedBrass"),Matrix4x4.identity);
        var glass=Annulus(0,3.4f,0,360,12.08f,120);Flip(glass);
        Queue(glass,Mat("08_QuietSeal"),Matrix4x4.identity);
        var rib=TubeMesh();
        for(int i=0;i<12;i++)for(int row=0;row<3;row++)
        {
            Vector3 a=Radial(r[row]-.08f,i*30)+Vector3.up*(y[row]-.05f);
            Vector3 b=Radial(r[row+1]-.08f,i*30)+Vector3.up*(y[row+1]-.05f);
            Queue(rib,Mat("05_AgedBrass"),Matrix4x4.TRS((a+b)/2,Quaternion.FromToRotation(Vector3.up,b-a),new Vector3(.045f,(b-a).magnitude,.045f)));
        }
        Flush(p,"Vault");
    }
    static void Flip(Mesh mesh)
    {var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int k=t[i];t[i]=t[i+2];t[i+2]=k;}mesh.triangles=t;mesh.RecalculateNormals();}
    static Mesh TubeMesh()
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int n=12;
        for(int i=0;i<=n;i++)for(int end=0;end<2;end++)
        {var p=Radial(1,i*360f/n)+Vector3.up*(end-.5f);v.Add(p);uv.Add(new Vector2(i/(float)n,end));}
        for(int i=0;i<n;i++){int k=i*2;t.AddRange(new[]{k,k+2,k+1,k+2,k+3,k+1});}
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
    }
    static void SetupReviewEnvironment()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.30f,.33f,.44f);
        RenderSettings.ambientEquatorColor=new Color(.19f,.18f,.24f);
        RenderSettings.ambientGroundColor=new Color(.10f,.09f,.12f);RenderSettings.fog=false;
        var rig=Node("REVIEW_ONLY_Lighting");
        var moon=Node("MoonKey",rig.transform).AddComponent<Light>();moon.type=LightType.Directional;
        moon.color=new Color(.74f,.80f,1);moon.intensity=1.5f;moon.shadows=LightShadows.Soft;moon.transform.rotation=Quaternion.Euler(48,-25,0);
        foreach(int sign in new[]{-1,1})
        {
            var l=Node("SoftFill",rig.transform,new Vector3(sign*7,6,0)).AddComponent<Light>();
            l.type=LightType.Point;l.color=sign<0?new Color(1,.74f,.47f):new Color(.56f,.44f,.85f);l.intensity=16;l.range=22;
        }
        var vol=rig.AddComponent<Volume>();vol.isGlobal=true;vol.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Base+"/Settings/ReviewVolume.asset");
    }
    static void SetInterior(Camera c)
    {c.orthographic=false;c.fieldOfView=70;c.transform.position=new Vector3(0,1.65f,-10);c.transform.LookAt(new Vector3(0,3.3f,9));}

    [Serializable] sealed class Route {public string name;public Vector3[] points;}
    static Route[] Routes()
    {
        return new[]{
            new Route{name="SpawnToCentre",points=new[]{new Vector3(0,0,-10),new Vector3(0,0,0),new Vector3(0,0,8)}},
            new Route{name="LeftFlank",points=new[]{new Vector3(0,0,-10),new Vector3(-8,0,-7),new Vector3(-10,0,0),new Vector3(-8,0,7),new Vector3(0,0,10)}},
            new Route{name="RightFlank",points=new[]{new Vector3(0,0,-10),new Vector3(8,0,-7),new Vector3(10,0,0),new Vector3(8,0,7),new Vector3(0,0,10)}},
            new Route{name="Perimeter_R12m",points=Enumerable.Range(0,37).Select(i=>Radial(12,i*10)).ToArray()}
        };
    }
    [Serializable] sealed class Report
    {
        public string unity,scene;public float combatRadius,wallRadius,pierRadius,probeRadius=.28f,probeHeight=1.75f;
        public int wallCount,pierCount,lanternLights,renderers,missingMeshes,missingMaterials,missingScripts;
        public long triangles;public int floorPassed,floorTotal,clearancePassed,clearanceTotal,routeSegmentsPassed,routeSegmentsTotal,seamsPassed,seamsTotal;
        public string limitation="Static Physics probes in the art candidate; actual game controller, Boss encounter and target-device performance not tested.";
    }
    static void Validate(GameObject hall)
    {
        Physics.SyncTransforms();var filters=hall.GetComponentsInChildren<MeshFilter>();
        var report=new Report {unity=Application.unityVersion,scene=ScenePath,combatRadius=CombatRadius,wallRadius=WallRadius,pierRadius=PierRadius,
            wallCount=24,pierCount=24,lanternLights=hall.GetComponentsInChildren<Light>().Length,renderers=hall.GetComponentsInChildren<MeshRenderer>().Length,
            missingMeshes=filters.Count(f=>!f.sharedMesh),missingMaterials=hall.GetComponentsInChildren<MeshRenderer>().Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader||!m.shader.isSupported)),
            missingScripts=hall.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            triangles=filters.Sum(f=>(long)f.sharedMesh.triangles.Length/3)};
        for(int ring=0;ring<=28;ring++)for(int i=0;i<(ring==0?1:72);i++)
        {
            Vector3 p=Radial(ring*.5f,i*5);report.floorTotal++;report.clearanceTotal++;
            if(Physics.Raycast(p+Vector3.up*7.8f,Vector3.down,out var hit,8) && Mathf.Abs(hit.point.y)<.001f)report.floorPassed++;
            if(!Physics.CheckCapsule(p+Vector3.up*.29f,p+Vector3.up*1.48f,.28f))report.clearancePassed++;
        }
        foreach(var route in Routes())for(int i=1;i<route.points.Length;i++)
        {
            var start=route.points[i-1];var delta=route.points[i]-start;report.routeSegmentsTotal++;
            if(!Physics.CapsuleCast(start+Vector3.up*.29f,start+Vector3.up*1.48f,.28f,delta.normalized,delta.magnitude))report.routeSegmentsPassed++;
        }
        // Probe visual tile meshes at sector joins, independently of the continuous collision floor.
        var floor=hall.transform.Find("CircularFloor_30.6m");
        var tileFilters=floor.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("FloorSector15")).ToArray();
        var probeColliders=tileFilters.Select(f=>{var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;return c;}).ToArray();
        Physics.SyncTransforms();
        foreach(float radius in new[]{12.6f,13.2f,14,14.8f})for(int seam=0;seam<24;seam++)foreach(float offset in new[]{-.15f,.15f})
        {
            report.seamsTotal++;var p=Radial(radius,seam*15+7.5f+offset);
            var ray=new Ray(p+Vector3.up,Vector3.down);
            if(probeColliders.Any(c=>c.Raycast(ray,out _,2)))report.seamsPassed++;
        }
        foreach(var collider in probeColliders)Object.DestroyImmediate(collider);
        Directory.CreateDirectory("Docs/Layout");File.WriteAllText("Docs/Layout/validation.json",JsonUtility.ToJson(report,true));
        File.WriteAllText("Docs/Layout/routes.json","{\"routes\":["+string.Join(",",Routes().Select(r=>JsonUtility.ToJson(r)))+"]}");
        if(report.missingMeshes+report.missingMaterials+report.missingScripts>0 || report.floorPassed!=report.floorTotal ||
            report.clearancePassed!=report.clearanceTotal || report.routeSegmentsPassed!=report.routeSegmentsTotal || report.seamsPassed!=report.seamsTotal)
            throw new InvalidOperationException("Layout check failed; see Docs/Layout/validation.json.");
    }
    [MenuItem("Art Studies/Seal Chamber/Capture circular layout")]
    public static void CaptureSaved()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.OpenScene(ScenePath);
        var hall=GameObject.Find("SealChamber_LayoutCandidate");var roof=hall.transform.Find("Vault_Optional");var camera=Camera.main;
        try
        {
            SetInterior(camera);Capture(camera,"01_interior");
            roof.gameObject.SetActive(false);camera.transform.position=new Vector3(28,27,-29);camera.transform.LookAt(Vector3.up*2);camera.fieldOfView=52;
            Capture(camera,"02_assembly_roof_hidden");
            camera.orthographic=true;camera.orthographicSize=17;camera.transform.position=new Vector3(0,38,-.01f);camera.transform.LookAt(Vector3.zero);
            Capture(camera,"03_topdown");
            var guides=Node("REVIEW_ONLY_RouteGuides");
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.17f,.75f,.65f));
            foreach(var route in Routes())
            {
                var line=Node(route.name,guides.transform).AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
                line.positionCount=route.points.Length;line.SetPositions(route.points.Select(p=>p+Vector3.up*.055f).ToArray());
                line.startWidth=line.endWidth=.07f;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            }
            Capture(camera,"04_route_review");Object.DestroyImmediate(guides);Object.DestroyImmediate(material);
        }
        finally{EditorSceneManager.OpenScene(ScenePath);}
        Debug.Log("[SealLayout] CAPTURES_COMPLETE");
    }
    static void Capture(Camera camera,string name)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        try
        {
            rt.Create();camera.targetTexture=rt;for(int frame=0;frame<3;frame++)camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            File.WriteAllBytes("Docs/Layout/Captures/"+name+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
    }
}
