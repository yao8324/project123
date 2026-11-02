using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using IMMonster.SceneArt;
using Object = UnityEngine.Object;

public static partial class SealChamberStudy
{
    const string Work = Root + "/MaintenanceW20261028";
    const string Evidence = "Docs/ART-W20261028-01";
    static Material cyan, blue, bone, lettering;
    [MenuItem("Art Studies/Maintenance/Build weekly deliverable")]
    public static void BuildMaintenance()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        ShaderUtil.allowAsyncCompilation=false;
        foreach(string folder in new[]{"Meshes","Materials","Prefabs","Scenes"})Directory.CreateDirectory(Work+"/"+folder);
        Directory.CreateDirectory(Evidence+"/Captures");AssetDatabase.Refresh();
        stone=LoadBase("01_AgedLimestone");stoneLight=LoadBase("02_CutStone");dark=LoadBase("04_Obsidian");
        brass=LoadBase("05_AgedBrass");patina=LoadBase("06_OxidisedBrass");
        var sourceCube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=sourceCube.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(sourceCube);cylinder=CylinderMesh(16);
        cyan=WeeklyMat("Node_Cyan",new Color(.10f,.52f,.62f),.2f,.35f,true);
        blue=WeeklyMat("Archive_Cord_Blue",new Color(.035f,.16f,.43f),.04f,.15f);
        bone=WeeklyMat("Archive_Paper",new Color(.73f,.68f,.54f),0,.15f);
        var existingPaper=ArchiveLibraryMaterial("MAT_Old_Paper");bone.CopyPropertiesFromMaterial(existingPaper);EditorUtility.SetDirty(bone);
        lettering=WeeklyMat("Archive_Ink",new Color(.025f,.035f,.05f),0,.1f);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        // Scene changes unload transient meshes; create the procedural rod mesh afterwards.
        cylinder=CylinderMesh(16);
        var nodeAsset=BuildSupplyNode();var doorAsset=BuildPassage();var cordAsset=BuildCord();var badgeAsset=BuildBadge();var recordAsset=BuildRecord();
        var sourceHall=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Layout/Prefabs/SealChamber_LayoutCandidate.prefab");
        if(!sourceHall)throw new InvalidOperationException("Original layout asset missing.");
        var hall=(GameObject)PrefabUtility.InstantiatePrefab(sourceHall);hall.name="MaintenanceHall_ART_W20261028_01";
        // All changes are overrides in the new weekly candidate; historical originals remain available.
        var walls=hall.transform.Find("WallBays_24");int bay=0;
        foreach(Transform wall in walls)
        {
            if(bay==12)wall.gameObject.SetActive(false);
            else AddWallEnvelope(wall);
            bay++;
        }
        Instance(doorAsset,hall.transform,new Vector3(0,0,-15.1f),180);
        var nodes=Node("MaintenanceNodes",hall.transform).transform;
        int number=0;
        foreach(float angle in new[]{45f,135f,270f})
        {
            number++;var g=Instance(nodeAsset,nodes,RadialPoint(14.52f,angle),angle);
            g.name="SupplyNode_"+number;var anchor=g.GetComponent<MaintenanceArtAnchor>();
            anchor.anchorId="MAINT_NODE_0"+number;anchor.displayLabel="供能节点 "+number;
            AddLabel("NODE "+number,g.transform,new Vector3(0,1.27f,-.195f),.06f,bone);
        }
        var refs=hall.transform.Find("PlacementMarkers");
        Node("STORY_MAINTENANCE_ENTER_Spawn",refs,new Vector3(0,.08f,-13.30f));
        Node("STORY_ARCHIVE_RETURN_Exit",refs,new Vector3(0,.08f,-16.9f));
        Node("ArchiveSideDoor_Link",refs,new Vector3(0,0,-15.1f));
        var exitAnchor=hall.transform.Find("MaintenancePassage").gameObject.AddComponent<MaintenanceArtAnchor>();
        exitAnchor.anchorId="MAINTENANCE_TO_ARCHIVE";exitAnchor.displayLabel="返回档案馆";
        exitAnchor.interactionPoint=Node("ExitInteraction",exitAnchor.transform,new Vector3(0,1.55f,-.35f)).transform;
        EnsureMaintenanceHitVolume(exitAnchor.transform,new Vector3(0,1.1f,-.35f),new Vector3(2.35f,2.2f,.08f));
        PrefabUtility.SaveAsPrefabAsset(hall,Work+"/Prefabs/MaintenanceHall.prefab");
        var entry=BuildArchiveEntryKit();
        // A separate review gallery keeps the foreshadowing props out of the central activity area.
        var gallery=Node("REVIEW_ONLY_ArchiveProps",null,new Vector3(24,0,0));
        Box("ReviewTable",gallery.transform,new Vector3(0,.45f,0),new Vector3(2.1f,.9f,1.0f),dark);
        Instance(cordAsset,gallery.transform,new Vector3(-.55f,.92f,-.1f),0);
        Instance(badgeAsset,gallery.transform,new Vector3(0,.92f,0),0);
        Instance(recordAsset,gallery.transform,new Vector3(.52f,.92f,0),0);
        Instance(entry,gallery.transform,new Vector3(0,0,2),0);
        SetupWeeklyReview();
        var cam=Node("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.nearClipPlane=.03f;cam.farClipPlane=100;cam.fieldOfView=70;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.032f,.045f);cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        cam.transform.position=new Vector3(0,1.65f,-12);cam.transform.LookAt(new Vector3(0,1.9f,2));
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Work+"/Scenes/MaintenanceArtReview.unity");
        WriteContract(hall);ValidateMaintenance(hall);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();CaptureMaintenance();
        Debug.Log("[MaintenanceArt] BUILD_COMPLETE");
    }
    static Material LoadBase(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
    static Material ArchiveLibraryMaterial(string name)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilanLibraryV6/Materials/"+name+".mat");
        if(!material)throw new InvalidOperationException("Existing library material dependency missing: "+name);
        return material;
    }
    static Material WeeklyMat(string name,Color color,float metal,float smooth,bool emissive=false)
    {
        string path=Work+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);
        if(emissive){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.65f);}EditorUtility.SetDirty(mat);return mat;
    }
    static void AddWallEnvelope(Transform wall)
    {
        AddBox(wall,"SkirtCollision_Envelope",new Vector3(0,.65f,-.4875f),new Vector3(4.05f,1.30f,.325f));
        AddBox(wall,"TraceryCollision_Envelope",new Vector3(0,4.65f,-.5325f),new Vector3(4.05f,6.70f,.415f));
    }
    static BoxCollider AddBox(Transform parent,string name,Vector3 center,Vector3 size)
    {var g=Node(name,parent);var c=g.AddComponent<BoxCollider>();c.center=center;c.size=size;return c;}
    static GameObject Instance(GameObject asset,Transform parent,Vector3 pos,float yaw)
    {var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,yaw,0);return g;}
    static Vector3 RadialPoint(float radius,float angle)
    {float a=angle*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);}
    static void AddLabel(string text,Transform parent,Vector3 position,float size,Material unused)
    {
        var label=Node("Label_"+text.Replace(' ','_'),parent,position).AddComponent<TextMesh>();
        label.text=text;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=64;label.characterSize=size*.22f;
        label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=unused==lettering?new Color(.025f,.035f,.05f):new Color(.85f,.79f,.61f);
        label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
    }
    static GameObject SaveWeekly(GameObject root,string name,bool merge=true)
    {
        if(merge)WeeklyMerge(root.transform,name);
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Work+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(root);return asset;
    }
    static void WeeklyMerge(Transform root,string prefix)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            string materialKey=prefix=="ArchiveRecord" && group.Key.name=="IV4_BookLeather_4"?"FolderCover":prefix=="ArchiveRecord" && group.Key.name=="IV4_Oxidised_Brass"?"Archive_Brass":prefix=="ElaineBadge" && group.Key.name=="IV4_Oxidised_Brass"?"BrassCase":group.Key.name;
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=prefix+"_"+materialKey};
            mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            string path=Work+"/Meshes/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            Shape(mesh.name,root,mesh,group.Key,Vector3.zero,Vector3.one);
        }
        foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
    }
    static GameObject BuildSupplyNode()
    {
        var g=Node("SupplyNode");var p=g.transform;
        Box("BackPlate",p,new Vector3(0,.77f,.06f),new Vector3(.74f,1.54f,.12f),patina);
        Box("StoneHousing",p,new Vector3(0,.77f,0),new Vector3(.66f,1.48f,.18f),dark);
        foreach(float x in new[]{-.31f,.31f})Box("Frame",p,new Vector3(x,.77f,-.11f),new Vector3(.035f,1.42f,.06f),brass);
        foreach(float y in new[]{.08f,.99f,1.45f})Box("Frame",p,new Vector3(0,y,-.11f),new Vector3(.66f,.04f,.06f),brass);
        for(int i=0;i<3;i++)
        {
            Box("PowerChannel",p,new Vector3((i-1)*.13f,.66f,-.12f),new Vector3(.035f,.82f,.04f),cyan);
            Band(Node("Coupling",p,new Vector3((i-1)*.13f,.24f,-.12f)).transform,0,.06f,.08f,brass);
        }
        Box("ControlPanel",p,new Vector3(0,1.17f,-.15f),new Vector3(.48f,.31f,.07f),patina);
        WeeklyMerge(p,"SupplyNode");
        AddBox(p,"NodeBodyCollision",new Vector3(0,.77f,0),new Vector3(.74f,1.54f,.34f));
        var anchor=g.AddComponent<MaintenanceArtAnchor>();anchor.anchorId="MAINT_NODE_TEMPLATE";anchor.displayLabel="供能节点";
        anchor.interactionPoint=Node("InteractionPoint",p,new Vector3(0,1.15f,-.22f)).transform;
        anchor.indicator=g.GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterial==cyan);
        return SaveWeekly(g,"SupplyNode",false);
    }
    static GameObject BuildPassage()
    {
        var g=Node("MaintenancePassage");var p=g.transform;
        foreach(int sign in new[]{-1,1})
        {
            Box("DoorJamb",p,new Vector3(sign*1.69f,2.60f,0),new Vector3(.67f,5.2f,.94f),stoneLight);
            Box("DoorTrim",p,new Vector3(sign*1.29f,2.45f,-.54f),new Vector3(.09f,4.90f,.10f),brass);
            AddBox(p,"JambCollision",new Vector3(sign*1.69f,2.6f,0),new Vector3(.80f,5.2f,1.10f));
        }
        Box("UpperMasonry",p,new Vector3(0,6.6f,0),new Vector3(4.05f,2.8f,.94f),stone);
        Box("Lintel",p,new Vector3(0,5.05f,-.10f),new Vector3(4.05f,.30f,1.10f),stoneLight);
        AddBox(p,"UpperCollision",new Vector3(0,6.48f,-.05f),new Vector3(4.05f,3.04f,1.12f));
        var apron=PassageApron();string apronPath=Work+"/Meshes/PassageApron.asset";
        var savedApron=AssetDatabase.LoadAssetAtPath<Mesh>(apronPath);
        if(savedApron){EditorUtility.CopySerialized(apron,savedApron);Object.DestroyImmediate(apron);apron=savedApron;apron.UploadMeshData(false);EditorUtility.SetDirty(apron);}else AssetDatabase.CreateAsset(apron,apronPath);
        Shape("TransitionFloor",p,apron,dark,Vector3.zero,Vector3.one);
        Node("TransitionFloorCollision",p).AddComponent<MeshCollider>().sharedMesh=apron;
        foreach(int sign in new[]{-1,1})
        {Box("PassageSide",p,new Vector3(sign*1.39f,1.8f,1.7f),new Vector3(.22f,3.6f,2.4f),stone);AddBox(p,"PassageSideCollision",new Vector3(sign*1.39f,1.8f,1.7f),new Vector3(.22f,3.6f,2.4f));}
        Box("TransitionEnd",p,new Vector3(0,1.8f,2.95f),new Vector3(2.8f,3.6f,.20f),stone);
        AddBox(p,"TransitionEndCollision",new Vector3(0,1.8f,2.95f),new Vector3(2.8f,3.6f,.20f));
        Box("TransitionCeiling",p,new Vector3(0,3.7f,1.7f),new Vector3(3.0f,.20f,2.6f),stone);
        AddBox(p,"TransitionCeilingCollision",new Vector3(0,3.7f,1.7f),new Vector3(3.0f,.20f,2.6f));
        Box("MarkerPlate",p,new Vector3(0,4.50f,-.56f),new Vector3(1.7f,.38f,.06f),patina);
        WeeklyMerge(p,"MaintenancePassage");AddLabel("ARCHIVE / RETURN",p,new Vector3(0,4.5f,-.61f),.065f,bone);
        Node("ArrivalFacingIn",p,new Vector3(0,.08f,-1.8f));Node("FadeTransferExit",p,new Vector3(0,.08f,1.8f));
        return SaveWeekly(g,"MaintenancePassage",false);
    }
    static GameObject BuildArchiveEntryKit()
    {
        var g=Node("ArchiveEntrySignAndAnchors");var p=g.transform;
        Box("Sign",p,new Vector3(0,2.85f,-.05f),new Vector3(1.7f,.40f,.07f),patina);
        WeeklyMerge(p,"ArchiveEntry");AddLabel("LOWER MAINTENANCE",p,new Vector3(0,2.85f,-.095f),.055f,bone);
        var anchor=g.AddComponent<MaintenanceArtAnchor>();anchor.anchorId="ARCHIVE_TO_MAINTENANCE";anchor.displayLabel="前往下层维护厅";
        anchor.interactionPoint=Node("InteractionPoint",p,new Vector3(0,1.55f,-.4f)).transform;
        EnsureMaintenanceHitVolume(p,new Vector3(0,1.2f,-.36f),new Vector3(2.0f,2.4f,.08f));
        Node("ArchiveReturnSpawn",p,new Vector3(0,.08f,-1.6f)).transform.localRotation=Quaternion.Euler(0,180,0);return SaveWeekly(g,"ArchiveEntryKit",false);
    }
    static Mesh PassageApron()
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int i=0;i<=20;i++)
        {
            float x=Mathf.Lerp(-1.275f,1.275f,i/20f),edge=Mathf.Sqrt(15.3f*15.3f-x*x)-15.1f+.001f;
            v.Add(new Vector3(x,0,edge));v.Add(new Vector3(x,0,2.85f));uv.Add(new Vector2(x,edge));uv.Add(new Vector2(x,2.85f));
            if(i<20){int k=i*2;t.AddRange(new[]{k,k+1,k+2,k+2,k+1,k+3});}
        }
        var m=new Mesh{name="PassageApron"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();return m;
    }
    static void EnsureMaintenanceHitVolume(Transform root,Vector3 center,Vector3 size)
    {
        var child=root.Find("InteractionHitVolume");var box=child?child.GetComponent<BoxCollider>():null;
        if(!box)box=Node("InteractionHitVolume",root).AddComponent<BoxCollider>();
        box.isTrigger=true;box.center=center;box.size=size;
    }
    [MenuItem("Art Studies/Maintenance/Update interaction targets only")]
    public static void UpdateMaintenanceInteractionTargets()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play Mode and save the current scene first.");
        foreach(string name in new[]{"ArchiveEntryKit","MaintenanceHall"})
        {
            string path=Work+"/Prefabs/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(name=="ArchiveEntryKit")EnsureMaintenanceHitVolume(root.transform,new Vector3(0,1.2f,-.36f),new Vector3(2.0f,2.4f,.08f));
                else EnsureMaintenanceHitVolume(root.transform.Find("MaintenancePassage"),new Vector3(0,1.1f,-.35f),new Vector3(2.35f,2.2f,.08f));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var hall=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Work+"/Prefabs/MaintenanceHall.prefab"));
        ValidateMaintenance(hall);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Work+"/Scenes/MaintenanceArtReview.unity");
        Debug.Log("[MaintenanceArt] INTERACTION_TARGETS_UPDATED");
    }
    static GameObject BuildCord()
    {
        var g=Node("BlueArchiveCord");var p=g.transform;
        for(int strand=0;strand<2;strand++)for(int i=0;i<36;i++)
        {
            float a=i*Mathf.PI*2/36,b=(i+1)*Mathf.PI*2/36;float r=.115f+strand*.007f;
            Rod("BlueCord",p,new Vector3(Mathf.Cos(a)*r,.014f+strand*.004f,Mathf.Sin(a)*r*.60f),new Vector3(Mathf.Cos(b)*r,.014f+strand*.004f,Mathf.Sin(b)*r*.60f),.006f,blue);
        }
        Rod("Tail",p,new Vector3(.05f,.02f,0),new Vector3(.24f,.02f,-.07f),.007f,blue);
        Rod("Tail",p,new Vector3(.05f,.02f,0),new Vector3(.19f,.02f,.10f),.007f,blue);
        return SaveWeekly(g,"BlueArchiveCord");
    }
    static GameObject BuildBadge()
    {
        var g=Node("ElaineWorkBadge");var p=g.transform;
        string oldCase=Work+"/Meshes/ElaineBadge_05_AgedBrass.asset",newCase=Work+"/Meshes/ElaineBadge_BrassCase.asset";
        if(AssetDatabase.LoadAssetAtPath<Mesh>(oldCase) && !AssetDatabase.LoadAssetAtPath<Mesh>(newCase))AssetDatabase.MoveAsset(oldCase,newCase);
        Box("BrassCase",p,new Vector3(0,.009f,0),new Vector3(.19f,.018f,.27f),ArchiveLibraryMaterial("IV4_Oxidised_Brass"));
        Box("IvoryInsert",p,new Vector3(0,.021f,0),new Vector3(.166f,.006f,.244f),bone);
        Box("BlueStripe",p,new Vector3(0,.025f,.084f),new Vector3(.166f,.003f,.022f),blue);
        WeeklyMerge(p,"ElaineBadge");
        var label=Node("BadgePrint",p,new Vector3(0,.029f,-.005f));label.transform.localRotation=Quaternion.Euler(90,0,0);
        AddLabel("ELAINE VEIL\nARCHIVIST\nARCHIVE / 014",label.transform,Vector3.zero,.0105f,lettering);
        return SaveWeekly(g,"ElaineWorkBadge",false);
    }
    static GameObject BuildRecord()
    {
        var g=Node("ArchiveRecord");var p=g.transform;
        string oldCover=Work+"/Meshes/ArchiveRecord_04_Obsidian.asset",newCover=Work+"/Meshes/ArchiveRecord_FolderCover.asset";
        if(AssetDatabase.LoadAssetAtPath<Mesh>(oldCover) && !AssetDatabase.LoadAssetAtPath<Mesh>(newCover))AssetDatabase.MoveAsset(oldCover,newCover);
        Box("LeatherFolder",p,new Vector3(0,.016f,0),new Vector3(.28f,.032f,.36f),ArchiveLibraryMaterial("IV4_BookLeather_4"));
        for(int i=0;i<4;i++)Box("Paper",p,new Vector3(0,.035f+i*.003f,0),new Vector3(.256f,.002f,.334f),bone);
        for(int i=0;i<7;i++)Box("PrintedLine",p,new Vector3(-.01f,.049f,-.10f+i*.027f),new Vector3(i%3==0?.13f:.20f,.0008f,.0012f),lettering);
        var cornerMetal=ArchiveLibraryMaterial("IV4_Oxidised_Brass");
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
        {Box("CoverCorner",p,new Vector3(x*.111f,.034f,z*.172f),new Vector3(.051f,.004f,.009f),cornerMetal);Box("CoverCorner",p,new Vector3(x*.132f,.034f,z*.151f),new Vector3(.009f,.004f,.051f),cornerMetal);}
        Band(Node("BlueWaxSeal",p,new Vector3(.095f,.051f,.125f)).transform,0,.020f,.004f,blue);
        Band(Node("SealImpression",p,new Vector3(.095f,.054f,.125f)).transform,0,.0105f,.0015f,cornerMetal);
        return SaveWeekly(g,"ArchiveRecord");
    }
    static void SetupWeeklyReview()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.30f,.33f,.44f);
        RenderSettings.ambientEquatorColor=new Color(.19f,.18f,.24f);RenderSettings.ambientGroundColor=new Color(.10f,.09f,.12f);
        var rig=Node("REVIEW_ONLY_Lighting");var key=Node("ReviewKey",rig.transform).AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.5f;key.color=new Color(.75f,.8f,1);key.transform.rotation=Quaternion.Euler(48,-25,0);key.shadows=LightShadows.Soft;
        var vol=rig.AddComponent<Volume>();vol.isGlobal=true;vol.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Settings/ReviewVolume.asset");
    }
    [Serializable] class AnchorRecord {public string id;public Vector3 localPosition;public Vector3 localForward;}
    [Serializable] class Contract {public string task="ART-W20261028-01";public string runtimeProject="1.0.6";public string requiredUnity="6000.3.25f1";public string testedUnity="6000.3.24f1 (user authorized installed version)";public float groundY=0,flatRadius=14;public AnchorRecord[] anchors;public Vector3 archiveKitWorldPosition=new Vector3(6f,0,-.2f);public float archiveKitYaw=180;public string archiveDoor="Exterior_V3_DarkHistoricalDoorWood_385 (right inner side door x=6)";public string note="Anchors are relative to MaintenanceHall root. Archive kit uses the existing right side door, not the front entrance. Bind input and story states in program. RequestInteraction does not advance story or teleport. Keep old eight point lights disabled; candidate contains six lights.";}
    static void WriteContract(GameObject hall)
    {
        var records=hall.GetComponentsInChildren<MaintenanceArtAnchor>().Select(a=>new AnchorRecord{id=a.anchorId,localPosition=a.transform.localPosition,localForward=a.transform.forward}).ToList();
        foreach(Transform m in hall.transform.Find("PlacementMarkers"))records.Add(new AnchorRecord{id=m.name,localPosition=m.localPosition,localForward=m.forward});
        WriteArtText(Evidence+"/handoff.json",JsonUtility.ToJson(new Contract{anchors=records.ToArray()},true));
    }
    static void ValidateMaintenance(GameObject hall)
    {
        Physics.SyncTransforms();int wallTests=0,wallPassed=0,floorTests=0,floorPassed=0,clearTests=0,clearPassed=0;
        for(int i=0;i<24;i++)if(i!=12)foreach(float y in new[]{.25f,.7f,1.2f,1.7f,2.7f})foreach(float x in new[]{-1.0f,0,1.0f})
        {
            wallTests++;var q=Quaternion.Euler(0,i*15,0);var start=q*new Vector3(x,y,13.6f);var direction=q*Vector3.forward;
            if(Physics.Raycast(start,direction,out var hit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&Vector3.Dot(hit.point,direction)<14.57f)wallPassed++;
        }
        for(int i=0;i<360;i++)foreach(float radius in new[]{13.5f,14.0f,14.2f})
        {floorTests++;var p=RadialPoint(radius,i);if(Physics.Raycast(p+Vector3.up*3,Vector3.down,out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y)<.002f)floorPassed++;}
        for(int i=0;i<360;i++)
        {clearTests++;var p=RadialPoint(14,i);if(!Physics.CheckCapsule(p+Vector3.up*.29f,p+Vector3.up*1.48f,.28f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))clearPassed++;}
        int doorPass=0;
        for(int i=0;i<17;i++){Vector3 p=new Vector3(0,0,-13.4f-i*.25f);if(!Physics.CheckCapsule(p+Vector3.up*.29f,p+Vector3.up*1.48f,.28f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&Physics.Raycast(p+Vector3.up,Vector3.down,out var hit,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y)<.002f)doorPass++;}
        int missing=hall.GetComponentsInChildren<MeshFilter>().Count(m=>!m.sharedMesh||m.sharedMesh.vertexCount==0)+hall.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader));
        foreach(string path in AssetDatabase.FindAssets("t:Mesh",new[]{Work+"/Meshes"}).Select(AssetDatabase.GUIDToAssetPath))
            if(AssetDatabase.LoadAssetAtPath<Mesh>(path).vertexCount==0)throw new Exception("Empty deliverable mesh: "+path);
        int lights=hall.GetComponentsInChildren<Light>().Length;
        string report="{\"unity\":\""+Application.unityVersion+"\",\"wallEnvelopePassed\":"+wallPassed+",\"wallEnvelopeTotal\":"+wallTests+",\"outerFloorPassed\":"+floorPassed+",\"outerFloorTotal\":"+floorTests+",\"R14ClearPassed\":"+clearPassed+",\"R14ClearTotal\":"+clearTests+",\"doorPassed\":"+doorPass+",\"doorTotal\":17,\"missingReferences\":"+missing+",\"candidateLights\":"+lights+"}";
        WriteArtText(Evidence+"/static-validation.json",report);
        if(wallPassed!=wallTests||floorPassed!=floorTests||clearPassed!=clearTests||doorPass!=17||missing!=0||lights!=6)throw new Exception("Maintenance geometry check failed: "+report);
    }
    [MenuItem("Art Studies/Maintenance/Capture weekly deliverable")]
    public static void CaptureMaintenance()
    {
        ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.OpenScene(Work+"/Scenes/MaintenanceArtReview.unity");var cam=Camera.main;
        try
        {
            WeeklyCapture(cam,"01_hall");
            cam.transform.position=new Vector3(-3,2.1f,-11);cam.transform.LookAt(new Vector3(0,2.1f,-15.1f));WeeklyCapture(cam,"02_passage");
            cam.transform.position=RadialPoint(11.8f,45)+Vector3.up*1.35f;cam.transform.LookAt(RadialPoint(14.52f,45)+Vector3.up*.95f);WeeklyCapture(cam,"03_supply_node");
            cam.transform.position=new Vector3(24.65f,1.85f,-.95f);cam.transform.LookAt(new Vector3(24,.94f,0));cam.fieldOfView=48;WeeklyCapture(cam,"04_archive_props");
            var hall=GameObject.Find("MaintenanceHall_ART_W20261028_01");
            cam.fieldOfView=70;cam.transform.position=new Vector3(-3,2.1f,-11);cam.transform.LookAt(new Vector3(0,2.1f,-15.1f));WeeklyCapture(cam,"06_after_door");
            hall.SetActive(false);var before=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Layout/Prefabs/SealChamber_LayoutCandidate.prefab"));
            WeeklyCapture(cam,"05_before_door");Object.DestroyImmediate(before);hall.SetActive(true);
        }
        finally{EditorSceneManager.OpenScene(Work+"/Scenes/MaintenanceArtReview.unity");}
    }
    static void WriteArtText(string path,string text){WriteArtCapture(path,System.Text.Encoding.UTF8.GetBytes(text));}
    static void WriteArtLines(string path,IEnumerable<string> lines){WriteArtText(path,string.Join(Environment.NewLine,lines)+Environment.NewLine);}
    static void WriteArtCapture(string path,byte[] bytes)
    {
        // Replace the file atomically so an open image preview cannot leave a partial capture.
        string temporary=path+".capture-tmp";
        try{File.WriteAllBytes(temporary,bytes);if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    static void WeeklyCapture(Camera c,string name)
    {
        var old=c.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        try{rt.Create();c.targetTexture=rt;for(int i=0;i<3;i++)c.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();WriteArtCapture(Evidence+"/Captures/"+name+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}
        finally{c.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
    }
}
