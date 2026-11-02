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

// Standalone art study. Generates only its own assets; no dependency on the game scripts.
public static partial class SealChamberStudy
{
    const string Root = "Assets/Art/SealChamberStudy";
    static Material stone, stoneLight, mortar, dark, brass, patina, glow, violet;
    static Mesh cube, cylinder;

    [MenuItem("Art Studies/Seal Chamber/Build sample and capture")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        ShaderUtil.allowAsyncCompilation = false;
        foreach (var folder in new[] {"Materials", "Meshes", "Textures", "Prefabs", "Scenes", "Settings"})
            Directory.CreateDirectory(Root + "/" + folder);
        Directory.CreateDirectory("Docs/Captures");
        AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.companyName = "IM MONSTER";
        PlayerSettings.productName = "Seal Chamber Art Study";
        SetupPipeline();
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        cylinder = CylinderMesh(20);
        var grain = StoneTexture();
        stone = Material("01_AgedLimestone", new Color(.40f,.39f,.43f), .05f,.24f, grain);
        stoneLight = Material("02_CutStone", new Color(.51f,.49f,.51f), .03f,.28f,grain);
        mortar = Material("03_ShadowJoints", new Color(.115f,.105f,.14f),0,.10f,grain);
        dark = Material("04_Obsidian",new Color(.075f,.065f,.10f),.23f,.30f,grain);
        brass = Material("05_AgedBrass",new Color(.54f,.36f,.13f),.75f,.48f,grain);
        patina = Material("06_OxidisedBrass",new Color(.13f,.21f,.20f),.58f,.30f,grain);
        glow = Material("07_LanternAmber",new Color(1,.68f,.30f),0,.4f,null,new Color(2.7f,1.20f,.30f));
        violet = Material("08_QuietSeal",new Color(.34f,.18f,.51f),.1f,.3f,null,new Color(.35f,.10f,.64f));

        var baseline = Node("Baseline_GeometryReconstruction");
        Baseline(baseline.transform);
        Merge(baseline.transform, "Baseline");
        var candidate = Node("SealChamber_ArtSample");
        var wall = Node("Wall_4.05m_x_8m", candidate.transform);
        Wall(wall.transform); Merge(wall.transform,"Wall");
        BoxCollider wallCollider = wall.AddComponent<BoxCollider>();
        wallCollider.center = new Vector3(0,4,0); wallCollider.size = new Vector3(4.05f,8,.65f);
        var pier = Node("Pier_7.5m",candidate.transform,new Vector3(2.025f,0,-.64f));
        Pier(pier.transform); Merge(pier.transform,"Pier");
        BoxCollider pierCollider = pier.AddComponent<BoxCollider>();
        pierCollider.center = new Vector3(0,3.75f,0); pierCollider.size = new Vector3(.88f,7.5f,.88f);
        var floor = Node("Floor_4.05m_x_4.05m",candidate.transform);
        Floor(floor.transform); Merge(floor.transform,"Floor");
        BoxCollider floorCollider = floor.AddComponent<BoxCollider>();
        floorCollider.center = new Vector3(0,-.25f,-2.025f); floorCollider.size = new Vector3(4.05f,.5f,4.05f);
        var lantern = Node("Lantern_OptionalRealtimeLight",candidate.transform,new Vector3(-1.58f,2.65f,-.65f));
        Lantern(lantern.transform); Merge(lantern.transform,"Lantern");
        LightAt("AmberPool",lantern.transform,new Vector3(0,.2f,-.22f),LightType.Point,new Color(1,.58f,.27f),3.8f,5,false);
        foreach (var module in new[] {wall,pier,floor,lantern})
            PrefabUtility.SaveAsPrefabAsset(module,Root+"/Prefabs/"+module.name+".prefab");
        PrefabUtility.SaveAsPrefabAsset(candidate,Root+"/Prefabs/SealChamber_ArtSample.prefab");
        baseline.SetActive(false);
        var rig = Node("REVIEW_ONLY_StudioLighting");
        var key = LightAt("MoonKey",rig.transform,new Vector3(0,8,-3),LightType.Directional,new Color(.77f,.82f,1),1.55f,0,true);
        key.transform.rotation = Quaternion.Euler(36,-32,0);
        LightAt("SoftWarmFill",rig.transform,new Vector3(-4,5,-6),LightType.Point,new Color(1,.76f,.49f),7,12,false);
        LightAt("VioletEdge",rig.transform,new Vector3(3,6,1),LightType.Point,new Color(.54f,.40f,.85f),8,9,false);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.26f,.29f,.38f);
        RenderSettings.ambientEquatorColor = new Color(.14f,.13f,.18f);
        RenderSettings.ambientGroundColor = new Color(.08f,.07f,.09f);
        RenderSettings.fog = false;
        var camera = Node("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.022f,.027f,.041f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 80; camera.fieldOfView = 39;
        camera.transform.position = new Vector3(8.5f,5.8f,-15.0f);
        camera.transform.LookAt(new Vector3(.0f,3.5f,-.55f));
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var volume = rig.AddComponent<Volume>(); volume.isGlobal = true;
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Settings/ReviewVolume.asset");
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/Settings/ReviewVolume.asset");}
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}
        tone.mode.Override(TonemappingMode.ACES);
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
        bloom.intensity.Override(.18f); bloom.threshold.Override(1.2f);
        EditorUtility.SetDirty(profile);EditorUtility.SetDirty(tone);EditorUtility.SetDirty(bloom);
        volume.sharedProfile = profile;
        AssetDatabase.SaveAssets();
        Capture(camera,"01_candidate");
        candidate.SetActive(false); baseline.SetActive(true); Capture(camera,"00_baseline_reconstruction");
        baseline.SetActive(false); candidate.SetActive(true);
        var cameraPos = camera.transform.position; var cameraRot = camera.transform.rotation;
        camera.transform.position = new Vector3(3.8f,3.9f,-6.2f);
        camera.transform.LookAt(new Vector3(.15f,2.9f,-.25f)); Capture(camera,"02_detail");
        camera.transform.position = new Vector3(3.7f,5.0f,-7.5f);
        camera.transform.LookAt(new Vector3(0,.1f,-2)); Capture(camera,"03_floor");
        camera.transform.SetPositionAndRotation(cameraPos,cameraRot);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/SealChamber_Review.unity");
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(Root+"/Scenes/SealChamber_Review.unity",true)};
        Validate(candidate);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("[SealStudy] BUILD_AND_VALIDATION_COMPLETE");
    }

    [MenuItem("Art Studies/Seal Chamber/Capture saved review")]
    public static void CaptureSavedReview()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save your scene before capturing.");
        ShaderUtil.allowAsyncCompilation=false;
        EditorSceneManager.OpenScene(Root+"/Scenes/SealChamber_Review.unity");
        var candidate=GameObject.Find("SealChamber_ArtSample");
        var baseline=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Baseline_GeometryReconstruction");
        var camera=Camera.main;var pos=camera.transform.position;var rot=camera.transform.rotation;
        Directory.CreateDirectory("Docs/Captures");
        try
        {
            Capture(camera,"01_candidate");
            candidate.SetActive(false);baseline.SetActive(true);Capture(camera,"00_baseline_reconstruction");
            baseline.SetActive(false);candidate.SetActive(true);
            camera.transform.position=new Vector3(3.8f,3.9f,-6.2f);camera.transform.LookAt(new Vector3(.15f,2.9f,-.25f));Capture(camera,"02_detail");
            camera.transform.position=new Vector3(3.7f,5,-7.5f);camera.transform.LookAt(new Vector3(0,.1f,-2));Capture(camera,"03_floor");
        }
        finally
        {
            baseline.SetActive(false);candidate.SetActive(true);camera.transform.SetPositionAndRotation(pos,rot);
            // Reload rather than save the temporary capture state.
            EditorSceneManager.OpenScene(Root+"/Scenes/SealChamber_Review.unity");
        }
        Debug.Log("[SealStudy] SAVED_REVIEW_CAPTURES_COMPLETE");
    }

    static void SetupPipeline()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Settings/StudyRenderer.asset");
        if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,Root+"/Settings/StudyRenderer.asset");}
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/StudyURP.asset");
        if(!pipeline){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,Root+"/Settings/StudyURP.asset");}
        pipeline.name = "StudyURP"; pipeline.msaaSampleCount = 4; pipeline.shadowDistance = 35;
        pipeline.supportsCameraDepthTexture = true;
        pipeline.supportsCameraOpaqueTexture = true;
        EditorUtility.SetDirty(pipeline);
        GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
        QualitySettings.shadows = UnityEngine.ShadowQuality.All; QualitySettings.shadowResolution = UnityEngine.ShadowResolution.High;
    }
    static GameObject Node(string name,Transform parent=null,Vector3 position=default)
    { var g=new GameObject(name); g.transform.SetParent(parent,false); g.transform.localPosition=position; return g; }
    static GameObject Shape(string name,Transform p,Mesh m,Material mat,Vector3 pos,Vector3 scale,Quaternion rotation=default)
    {
        var g=Node(name,p,pos); g.transform.localScale=scale;
        g.transform.localRotation=rotation==default?Quaternion.identity:rotation;
        g.AddComponent<MeshFilter>().sharedMesh=m; g.AddComponent<MeshRenderer>().sharedMaterial=mat;
        return g;
    }
    static void Box(string n,Transform p,Vector3 at,Vector3 size,Material mat) => Shape(n,p,cube,mat,at,size);
    static void Rod(string n,Transform p,Vector3 a,Vector3 b,float r,Material mat)
        => Shape(n,p,cylinder,mat,(a+b)/2,new Vector3(r,(b-a).magnitude,r),Quaternion.FromToRotation(Vector3.up,b-a));
    static void Band(Transform p,float y,float radius,float height,Material mat)
        => Shape("Collar",p,cylinder,mat,new Vector3(0,y,0),new Vector3(radius,height,radius));
    static void Line(Transform p,IList<Vector3> points,float radius,Material mat)
    { for(int i=1;i<points.Count;i++)Rod("Moulding",p,points[i-1],points[i],radius,mat); }
    static List<Vector3> Arch(float halfWidth,float spring,float peak,float z)
    {
        var v=new List<Vector3> {new Vector3(-halfWidth,1.12f,z),new Vector3(-halfWidth,spring,z)};
        for(int i=1;i<=16;i++)
        {float t=i/16f; v.Add(new Vector3(-halfWidth*(1-t*t),Mathf.Lerp(spring,peak,t),z));}
        for(int i=15;i>=0;i--)
        {float t=i/16f; v.Add(new Vector3(halfWidth*(1-t*t),Mathf.Lerp(spring,peak,t),z));}
        v.Add(new Vector3(halfWidth,1.12f,z)); return v;
    }
    static void Diamond(Transform p,Vector3 pos,float width,float height,Material mat)
    {
        var points=new[] {pos+Vector3.up*height/2,pos+Vector3.right*width/2,pos-Vector3.up*height/2,pos-Vector3.right*width/2,pos+Vector3.up*height/2};
        Line(p,points,.022f,mat);
    }
    static void Wall(Transform p)
    {
        Box("MortarBacking",p,new Vector3(0,4,0),new Vector3(4.05f,8,.65f),mortar);
        for(int row=0;row<16;row++)
        {
            float left=-2.025f; int count=row%2==0?4:5; float width=4.05f/count;
            for(int col=0;col<count;col++)
                Box("DressedBlock",p,new Vector3(left+(col+.5f)*width,(row+.5f)*.5f,-.02f),new Vector3(width-.018f,.484f,.64f),row%5==0?stoneLight:stone);
        }
        var inset=Plate(Arch(1.30f,4.80f,6.82f,0).Select(v=>new Vector2(v.x,v.y)).Reverse().ToArray(),.09f);
        Shape("PointedRecessShadow",p,inset,dark,new Vector3(0,0,-.38f),Vector3.one);
        Line(p,Arch(1.39f,4.85f,6.97f,-.49f),.095f,stoneLight);
        Line(p,Arch(1.26f,4.78f,6.77f,-.55f),.031f,brass);
        Line(p,Arch(1.17f,4.71f,6.59f,-.48f),.042f,patina);
        Rod("CentralTracery",p,new Vector3(0,1.25f,-.49f),new Vector3(0,6.50f,-.49f),.026f,brass);
        for(int s=-1;s<=1;s+=2)
        {
            Rod("Tracery",p,new Vector3(s*1.16f,3.3f,-.49f),new Vector3(0,4.6f,-.49f),.025f,brass);
            Rod("Tracery",p,new Vector3(s*1.16f,4.65f,-.49f),new Vector3(0,5.75f,-.49f),.025f,brass);
        }
        Diamond(p,new Vector3(0,4.25f,-.61f),.58f,1.15f,brass);
        Diamond(p,new Vector3(0,4.25f,-.62f),.26f,.58f,violet);
        for(int i=0;i<5;i++)
        {
            Box("PlinthPanel",p,new Vector3((i-2)*.79f,.62f,-.42f),new Vector3(.74f,.76f,.19f),stoneLight);
            Box("PlinthInset",p,new Vector3((i-2)*.79f,.62f,-.525f),new Vector3(.58f,.57f,.035f),stone);
        }
        foreach(var band in new[] {new Vector3(0,.14f,-.30f),new Vector3(0,1.10f,-.37f),new Vector3(0,7.40f,-.25f),new Vector3(0,7.77f,-.30f)})
            Box("StoneCornice",p,band,new Vector3(4.05f,.17f,.35f),stoneLight);
        foreach(float y in new[] {1.20f,7.52f})Box("BrassStringCourse",p,new Vector3(0,y,-.51f),new Vector3(4.05f,.028f,.04f),brass);
        for(int i=-4;i<=4;i++)Box("CorniceDentil",p,new Vector3(i*.44f,7.63f,-.47f),new Vector3(.17f,.15f,.20f),stoneLight);
    }
    static void Pier(Transform p)
    {
        Box("SquareFoot",p,new Vector3(0,.10f,0),new Vector3(1.12f,.20f,1.12f),stone);
        Box("BevelStep",p,new Vector3(0,.25f,0),new Vector3(.98f,.10f,.98f),stoneLight);
        Band(p,.39f,.46f,.18f,stoneLight); Band(p,.51f,.40f,.055f,brass);
        Band(p,3.75f,.30f,6.43f,stone);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6; var off=new Vector3(Mathf.Sin(a)*.286f,0,Mathf.Cos(a)*.286f);
            Rod("ClusteredFlute",p,off+Vector3.up*.62f,off+Vector3.up*6.95f,.066f,stoneLight);
        }
        foreach(float y in new[] {.68f,3.66f,6.91f})Band(p,y,.36f,.055f,brass);
        Band(p,7.07f,.39f,.18f,patina); Band(p,7.21f,.46f,.12f,brass);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;var off=new Vector3(Mathf.Sin(a)*.36f,0,Mathf.Cos(a)*.36f);
            Rod("CapitalRib",p,off+Vector3.up*6.99f,off*1.45f+Vector3.up*7.38f,.046f,brass);
        }
        Box("CapitalAbacus",p,new Vector3(0,7.43f,0),new Vector3(1.10f,.14f,1.10f),stoneLight);
    }
    static void Floor(Transform p)
    {
        // The substrate stays below the tile tops to prevent coplanar flicker.
        Box("FloorMass",p,new Vector3(0,-.28f,-2.025f),new Vector3(4.05f,.44f,4.05f),dark);
        for(int x=0;x<6;x++)for(int z=0;z<6;z++)
        {
            var m=(x+2*z)%5==0?mortar:dark;
            Box("FlushStoneTile",p,new Vector3(-2.025f+(x+.5f)*.675f,-.025f,-(z+.5f)*.675f),new Vector3(.661f,.05f,.661f),m);
        }
        foreach(float x in new[] {-1.85f,1.85f})Box("BorderInlay",p,new Vector3(x,.001f,-2.025f),new Vector3(.018f,.002f,3.73f),brass);
        foreach(float z in new[] {-.17f,-3.88f})Box("BorderInlay",p,new Vector3(0,.001f,z),new Vector3(3.72f,.002f,.018f),brass);
        // Edge-only ornament keeps the central combat floor quiet and flush.
        foreach(float x in new[] {-1.67f,1.67f})foreach(float z in new[] {-.37f,-3.67f})
        {
            var pts=new[] {new Vector3(x,.002f,z+.14f),new Vector3(x+.10f,.002f,z),new Vector3(x,.002f,z-.14f),new Vector3(x-.10f,.002f,z),new Vector3(x,.002f,z+.14f)};
            Line(p,pts,.008f,brass);
        }
    }
    static void Lantern(Transform p)
    {
        Box("MountingPlate",p,new Vector3(0,.15f,.1f),new Vector3(.23f,.74f,.08f),patina);
        Rod("Bracket",p,new Vector3(0,-.15f,.03f),new Vector3(0,0,-.36f),.045f,brass);
        var body=Node("CagedLamp",p,new Vector3(0,.32f,-.36f)).transform;
        Band(body,-.22f,.20f,.07f,brass); Band(body,.25f,.23f,.08f,brass);
        Band(body,.34f,.13f,.11f,patina); Band(body,.44f,.055f,.12f,brass);
        Band(body,0,.085f,.30f,glow);
        for(int i=0;i<6;i++)
        {float a=i*Mathf.PI/3;var at=new Vector3(Mathf.Sin(a)*.16f,0,Mathf.Cos(a)*.16f);Rod("Cage",body,at-Vector3.up*.21f,at+Vector3.up*.24f,.017f,brass);}
    }
    static void Baseline(Transform p)
    {
        // Geometry reference reconstructed from 1.0.6 BossEncounterBuilder.BuildArena.
        // This is deliberately not represented as a screenshot of the full existing level.
        Box("Wall",p,new Vector3(0,4,0),new Vector3(4.05f,8,.65f),stone);
        var recess=Plate(new[] {new Vector2(-1.3f,1.2f),new Vector2(1.3f,1.2f),new Vector2(1.3f,4.8f),new Vector2(.65f,6.15f),new Vector2(0,6.8f),new Vector2(-.65f,6.15f),new Vector2(-1.3f,4.8f)},.05f);
        Shape("Recess",p,recess,dark,new Vector3(0,0,-.36f),Vector3.one);
        var path=new[] {new Vector3(-1.33f,1.1f,-.42f),new Vector3(-1.33f,4.8f,-.42f),new Vector3(-.68f,6.17f,-.42f),new Vector3(0,6.9f,-.42f),new Vector3(.68f,6.17f,-.42f),new Vector3(1.33f,4.8f,-.42f),new Vector3(1.33f,1.1f,-.42f)};
        Line(p,path,.085f,brass); Rod("Mullion",p,new Vector3(0,1.3f,-.44f),new Vector3(0,6.4f,-.44f),.04f,brass);
        Diamond(p,new Vector3(0,4.2f,-.48f),.35f,1.2f,violet);
        var col=Node("Pier",p,new Vector3(2.025f,0,-.64f)).transform;
        Band(col,.18f,.60f,.36f,stone);Band(col,3.7f,.32f,6.8f,stone);Band(col,7.25f,.55f,.50f,brass);
        Box("Floor",p,new Vector3(0,-.25f,-2.025f),new Vector3(4.05f,.5f,4.05f),dark);
    }
    static Material Material(string name,Color color,float metallic,float smoothness,Texture2D texture,Color emission=default)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
        m.SetTexture("_BaseMap",texture);m.SetTextureScale("_BaseMap",new Vector2(2,2));
        if(emission!=default){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}
        EditorUtility.SetDirty(m);return m;
    }
    static Texture2D StoneTexture()
    {
        const int n=256;var tex=new Texture2D(n,n,TextureFormat.RGB24,false);
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {
            float noise=Mathf.PerlinNoise(x/35f,y/35f)*.13f+Mathf.PerlinNoise(x/6f,y/6f)*.055f;
            float fleck=Mathf.PerlinNoise(x*.7f,y*.7f)>.71f?-.07f:0;
            float value=.80f+noise+fleck;tex.SetPixel(x,y,new Color(value,value*.985f,value*.97f));
        }
        tex.Apply();string path=Root+"/Textures/ProceduralStone.png";
        File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;
        importer.mipmapEnabled=true;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Mesh CylinderMesh(int sides)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int i=0;i<=sides;i++)for(int j=0;j<2;j++)
        {float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Sin(a),j-.5f,Mathf.Cos(a)));uv.Add(new Vector2(i/(float)sides,j));}
        for(int i=0;i<sides;i++){int k=i*2;t.AddRange(new[] {k,k+2,k+1,k+2,k+3,k+1});}
        for(int cap=0;cap<2;cap++)
        {
            int center=v.Count;v.Add(new Vector3(0,cap-.5f,0));uv.Add(Vector2.one*.5f);
            for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;var pt=new Vector3(Mathf.Sin(a),cap-.5f,Mathf.Cos(a));v.Add(pt);uv.Add(new Vector2(pt.x,pt.z)*.5f+Vector2.one*.5f);}
            for(int i=0;i<sides;i++)t.AddRange(cap==0?new[] {center,center+i+2,center+i+1}:new[] {center,center+i+1,center+i+2});
        }
        var mesh=new Mesh {name="SourceCylinder"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
    }
    static Mesh Plate(Vector2[] outline,float depth)
    {
        var v=new List<Vector3>();var t=new List<int>();int n=outline.Length;
        foreach(float z in new[] {-depth/2,depth/2})foreach(var p in outline)v.Add(new Vector3(p.x,p.y,z));
        for(int i=1;i<n-1;i++)t.AddRange(new[] {0,i+1,i,n,n+i,n+i+1});
        for(int i=0;i<n;i++){int j=(i+1)%n;t.AddRange(new[] {i,j,n+i,j,n+j,n+i});}
        var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.SetUVs(0,v.Select(p=>new Vector2(p.x,p.y)).ToList());m.RecalculateNormals();return m;
    }
    static void Merge(Transform root,string prefix)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>();
        var groups=filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
        foreach(var group in groups)
        {
            var mesh=new Mesh {name=prefix+"_"+group.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(group.Select(f=>new CombineInstance {mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            string path=Root+"/Meshes/"+mesh.name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            Shape(mesh.name,root,mesh,group.Key,Vector3.zero,Vector3.one);
        }
        foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
        foreach(var t in root.GetComponentsInChildren<Transform>().Reverse())
            if(t!=root && t.childCount==0 && t.GetComponents<Component>().Length==1)Object.DestroyImmediate(t.gameObject);
    }
    static Light LightAt(string name,Transform p,Vector3 pos,LightType type,Color color,float intensity,float range,bool shadow)
    {var light=Node(name,p,pos).AddComponent<Light>();light.type=type;light.color=color;light.intensity=intensity;light.range=range;light.shadows=shadow?LightShadows.Soft:LightShadows.None;return light;}
    static void Capture(Camera camera,string name)
    {
        var rt=new RenderTexture(1500,1200,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try
        {
            rt.Create();camera.targetTexture=rt;
            // Warm up the new SRP and per-material buffers before reading the final frame.
            for(int frame=0;frame<3;frame++)camera.Render();
            RenderTexture.active=rt;
            var image=new Texture2D(1500,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,1200),0,0);image.Apply();
            File.WriteAllBytes("Docs/Captures/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
    }
    static void Validate(GameObject candidate)
    {
        Physics.SyncTransforms();
        var filters=candidate.GetComponentsInChildren<MeshFilter>();
        int missingMeshes=filters.Count(f=>!f.sharedMesh);
        int missingMaterials=candidate.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader||!m.shader.isSupported));
        int scripts=candidate.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        int flat=0;
        for(int x=0;x<8;x++)for(int z=0;z<8;z++)
            if(Physics.Raycast(new Vector3(-1.4f+x*.4f,10,-.7f-z*.4f),Vector3.down,out var hit,11) && Mathf.Abs(hit.point.y)<.001f)flat++;
        long triangles=filters.Sum(f=>(long)f.sharedMesh.triangles.Length/3);
        if(missingMeshes+missingMaterials+scripts>0||flat!=64)throw new Exception("Study validation failed.");
        string report="{\n  \"unity\": \""+Application.unityVersion+"\",\n  \"missingMeshes\": "+missingMeshes+",\n  \"missingMaterialsOrShaders\": "+missingMaterials+",\n  \"missingScripts\": "+scripts+",\n  \"renderers\": "+filters.Length+",\n  \"triangles\": "+triangles+",\n  \"colliders\": "+candidate.GetComponentsInChildren<Collider>().Length+",\n  \"realtimeLightsInPrefab\": "+candidate.GetComponentsInChildren<Light>().Length+",\n  \"flatFloorSamplesPassed\": "+flat+",\n  \"flatFloorSamplesTotal\": 64\n}\n";
        File.WriteAllText("Docs/validation.json",report);
    }
}
