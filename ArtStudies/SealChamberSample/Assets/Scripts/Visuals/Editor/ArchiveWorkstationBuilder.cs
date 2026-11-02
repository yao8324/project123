using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using IMMonster.SceneArt;
using Object = UnityEngine.Object;

public static partial class SealChamberStudy
{
    const string ArchiveWork = Root + "/ArchiveWorkstation20261102";
    const string ArchiveDocs = "Docs/ArchiveWorkstation20261102";
    const string ArchiveScene = ArchiveWork + "/Scenes/ArchiveWorkstationReview.unity";
    const float DeskTop = .836f;
    static readonly Vector3 DeskWorld = new Vector3(5.8f, DeskTop, 8.4f);
    static Material archiveWood, archiveLeather;

    [MenuItem("Art Studies/Archive/Build workstation candidate")]
    public static void BuildArchiveWorkstation()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
        ShaderUtil.allowAsyncCompilation=false;
        foreach(string folder in new[]{"Meshes","Prefabs","Scenes","ReviewContext"})Directory.CreateDirectory(ArchiveWork+"/"+folder);
        Directory.CreateDirectory(ArchiveDocs+"/Captures");AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
        cylinder=CylinderMesh(16);dark=LoadBase("04_Obsidian");brass=LoadBase("05_AgedBrass");patina=LoadBase("06_OxidisedBrass");
        blue=AssetDatabase.LoadAssetAtPath<Material>(Work+"/Materials/Archive_Cord_Blue.mat");
        bone=AssetDatabase.LoadAssetAtPath<Material>(Work+"/Materials/Archive_Paper.mat");
        lettering=AssetDatabase.LoadAssetAtPath<Material>(Work+"/Materials/Archive_Ink.mat");
        if(!blue||!bone||!lettering)throw new InvalidOperationException("Build or import the weekly maintenance assets first.");
        var overlay=Node("ArchiveWorkstation_ArtCandidate");var art=overlay.AddComponent<ArchiveWorkstationArt>();
        var normal=Node("State_Intact",overlay.transform);var after=Node("State_Disturbed",overlay.transform);
        art.intactProps=normal;art.disturbedProps=after;
        var loose=new List<GameObject>();var tied=new List<GameObject>();
        BuildArchiveState(normal.transform,false,loose,tied);BuildArchiveState(after.transform,true,loose,tied);
        art.looseSeals=loose.ToArray();art.tiedSeals=tied.ToArray();
        var markers=Node("StoryAnchors",overlay.transform).transform;
        Node("ARCHIVE_WORK_View",markers,new Vector3(0,1.65f-DeskTop,-1.1f));
        Node("ARCHIVE_AWAKEN_View_Proposed",markers,new Vector3(-.75f,1.1f-DeskTop,-1.4f));
        Node("ARCHIVE_RETURN_RecordTarget",markers,new Vector3(-.13f,.07f,.02f));
        Node("FUTURE_CordDetail_View",markers,new Vector3(-.50f,.45f,-.50f));
        Node("FUTURE_BadgeDetail_View",markers,new Vector3(-.62f,.30f,-.10f));
        art.ResetPresentation();
        PrefabUtility.SaveAsPrefabAsset(overlay,ArchiveWork+"/Prefabs/ArchiveWorkstation.prefab");
        overlay.transform.position=DeskWorld;
        BuildArchiveContext();SetupArchiveReview();
        var camera=Node("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=62;camera.nearClipPlane=.03f;camera.farClipPlane=50;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.030f,.04f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        camera.transform.position=DeskWorld+new Vector3(0,1.65f-DeskTop,-1.1f);camera.transform.LookAt(DeskWorld+new Vector3(0,.03f,.05f));
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ArchiveScene);
        WriteArchiveContract(overlay);ValidateArchiveWorkstation(overlay);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("[ArchiveWorkstation] BUILD_COMPLETE");
    }

    static void BuildArchiveState(Transform root,bool disturbed,List<GameObject> loose,List<GameObject> tied)
    {
        var record=AssetDatabase.LoadAssetAtPath<GameObject>(Work+"/Prefabs/ArchiveRecord.prefab");
        var badge=AssetDatabase.LoadAssetAtPath<GameObject>(Work+"/Prefabs/ElaineWorkBadge.prefab");
        var cord=AssetDatabase.LoadAssetAtPath<GameObject>(Work+"/Prefabs/BlueArchiveCord.prefab");
        if(!record||!badge||!cord)throw new Exception("Weekly prop prefab missing.");
        for(int i=0;i<3;i++)
        {
            var slot=Node("ArchiveSlot_0"+(i+1),root,new Vector3(-.13f+i*.33f,.004f,.02f));
            if(disturbed){slot.transform.localPosition+=new Vector3(i==0?-.035f:0,0,i==1?.035f:-.035f);slot.transform.localRotation=Quaternion.Euler(0,new[]{-13f,9f,-7f}[i],0);}
            Instance(record,slot.transform,Vector3.zero,0).name="Record_VisualPlaceholder";
            // The slot is a data binding key; book titles and archive contents are not authored here.
            ArchiveAnchor(slot,"ARCHIVE_RECORD_SLOT_0"+(i+1),"档案槽位 "+(i+1),new Vector3(0,.06f,0),new Vector3(.28f,.10f,.36f));
            var number=Node("SlotNumber",slot.transform,new Vector3(0,.053f,-.125f));number.transform.localRotation=Quaternion.Euler(90,0,0);
            AddLabel(new[]{"I","II","III"}[i],number.transform,Vector3.zero,.009f,lettering);
            if(i==0)
            {
                var bound=Node("Cord_Sealed",slot.transform);BuildTiedSeal(bound.transform);tied.Add(bound);
            }
        }
        var badgeGo=Instance(badge,root,new Vector3(-.56f,disturbed?.035f:.004f,.18f),disturbed?-9:0);badgeGo.name="ElaineBadge";
        if(disturbed)
        {
            badgeGo.transform.localRotation=Quaternion.Euler(0,-9,180);
            // The legacy text shader can show through the reverse side; explicitly hide the print.
            var print=badgeGo.transform.Find("BadgePrint");if(print)print.gameObject.SetActive(false);
        }
        ArchiveAnchor(badgeGo,"ARCHIVE_WORK_BADGE","工作牌",new Vector3(0,.04f,0),new Vector3(.20f,.08f,.28f));
        var looseGo=Instance(cord,root,new Vector3(-.59f,.004f,-.14f),disturbed?12:0);looseGo.name="Cord_Loose";loose.Add(looseGo);
        ArchiveAnchor(looseGo,"ARCHIVE_TIE_CORD","封档绳",new Vector3(.02f,.06f,0),new Vector3(.37f,.10f,.22f));
        // A tiny tabletop marker retains the work area's visual identity after the incident.
        Box("DeskMarker",root,new Vector3(-.61f,.012f,.315f),new Vector3(.22f,.018f,.045f),patina);
        if(disturbed)
        {
            var papers=Node("ScatteredPaper_NoBlockingCollision",root).transform;
            for(int i=0;i<5;i++)
            {
                var page=Node("LooseSheet_"+i,papers,new Vector3(-.55f+i*.22f,.002f-DeskTop,.86f+(i%2)*.28f));page.transform.localRotation=Quaternion.Euler(0,-32+i*17,0);
                Box("Paper",page.transform,Vector3.zero,new Vector3(.19f,.0015f,.26f),bone);
                for(int j=0;j<4;j++)Box("Line",page.transform,new Vector3(0,.001f,-.075f+j*.035f),new Vector3(.13f,.0005f,.002f),lettering);
            }
            ArchiveMerge(papers,"ScatteredPaper");
        }
    }

    static void ArchiveAnchor(GameObject target,string id,string label,Vector3 point,Vector3 box)
    {
        var anchor=target.AddComponent<MaintenanceArtAnchor>();anchor.anchorId=id;anchor.displayLabel=label;
        anchor.interactionPoint=Node("InteractionPoint",target.transform,point).transform;
        var hit=target.AddComponent<BoxCollider>();hit.isTrigger=true;hit.center=point*.5f;hit.size=box;
    }
    static void BuildTiedSeal(Transform root)
    {
        Box("SealAcross",root,new Vector3(0,.052f,0),new Vector3(.286f,.006f,.011f),blue);
        Box("SealAlong",root,new Vector3(0,.052f,0),new Vector3(.011f,.006f,.364f),blue);
        foreach(int side in new[]{-1,1})
        {
            Box("SideWrapX",root,new Vector3(side*.143f,.029f,0),new Vector3(.006f,.046f,.011f),blue);
            Box("SideWrapZ",root,new Vector3(0,.029f,side*.182f),new Vector3(.011f,.046f,.006f),blue);
        }
        for(int sign=-1;sign<=1;sign+=2)
            for(int i=0;i<16;i++)
            {float a=i*Mathf.PI*2/16,b=(i+1)*Mathf.PI*2/16;Rod("KnotLoop",root,new Vector3(sign*(.027f+Mathf.Cos(a)*.024f),.061f,Mathf.Sin(a)*.017f),new Vector3(sign*(.027f+Mathf.Cos(b)*.024f),.061f,Mathf.Sin(b)*.017f),.003f,blue);}
        ArchiveMerge(root,"TiedSeal");
        var anchor=root.gameObject.AddComponent<MaintenanceArtAnchor>();anchor.anchorId="ARCHIVE_SEAL_COMPLETE_MARKER";anchor.displayLabel="封档完成位置";
        anchor.interactionPoint=Node("SealObservationPoint",root,new Vector3(0,.08f,0)).transform;
    }
    static void ArchiveMerge(Transform root,string name)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            var mesh=new Mesh{name=name+"_"+group.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            string meshName=mesh.name;
            var saved=SaveArchiveMesh(mesh,ArchiveWork+"/Meshes/"+meshName+".asset");Shape(meshName,root,saved,group.Key,Vector3.zero,Vector3.one);
        }
        foreach(var filter in filters)Object.DestroyImmediate(filter.gameObject);
    }
    static Mesh SaveArchiveMesh(Mesh mesh,string path)
    {
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!old){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);old.UploadMeshData(false);EditorUtility.SetDirty(old);return old;
    }
    static void BuildArchiveContext()
    {
        // Only a cropped review reference is generated. The deliverable overlay contains no desk/floor/lights.
        var context=Node("REVIEW_ONLY_ExistingDeskReference");
        archiveWood=ArchiveContextMat("ExistingDeskWood",new Color(.23f,.105f,.052f),.05f,.27f);
        archiveLeather=ArchiveContextMat("ExistingDeskLeather",new Color(.13f,.026f,.035f),.05f,.3f);
        int n=0;foreach(int id in new[]{685,686,687})
        {
            string file="../../1.0.6/Assets/Art/MilanLibraryV6/Meshes/mesh_"+id.ToString("D4")+".mlmesh";
            var mesh=ReadArchiveReference(file);string path=ArchiveWork+"/ReviewContext/ExistingDesk_"+id+".asset";
            Shape("ExistingDesk_"+id,context.transform,SaveArchiveMesh(mesh,path),new[]{archiveWood,archiveLeather,brass}[n++],Vector3.zero,Vector3.one);
        }
        // Reference support is solely for preview placement probes; production retains its existing collision.
        AddBox(context.transform,"REFERENCE_ONLY_TableSupport",new Vector3(5.8f,.815f,8.4f),new Vector3(1.56f,.042f,.72f));
        Box("ReferenceFloor",context.transform,new Vector3(5.8f,-.11f,8.4f),new Vector3(7,.22f,6),LoadBase("02_CutStone"));
        AddBox(context.transform,"REFERENCE_ONLY_FloorSupport",new Vector3(5.8f,-.11f,8.4f),new Vector3(7,.22f,6));
    }
    static Material ArchiveContextMat(string name,Color color,float metal,float smooth)
    {
        string path=ArchiveWork+"/ReviewContext/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(mat);return mat;
    }
    static Mesh ReadArchiveReference(string file)
    {
        using var input=File.OpenRead(file);using var zip=new GZipStream(input,CompressionMode.Decompress);using var r=new BinaryReader(zip);
        if(r.ReadUInt32()!=0x314D4C4D)throw new InvalidDataException("MLM1 expected");int vc=r.ReadInt32(),ic=r.ReadInt32();
        var vertices=new Vector3[vc];var normals=new Vector3[vc];var uv=new Vector2[vc];
        for(int i=0;i<vc;i++){vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}
        var indices=new int[ic];for(int i=0;i<ic;i++)indices[i]=r.ReadInt32();
        var mesh=new Mesh{name=Path.GetFileNameWithoutExtension(file),indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();return mesh;
    }
    static void SetupArchiveReview()
    {
        var rig=Node("REVIEW_ONLY_Lighting");
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.21f,.24f,.30f);RenderSettings.ambientEquatorColor=new Color(.11f,.105f,.12f);RenderSettings.ambientGroundColor=new Color(.08f,.06f,.05f);
        var key=Node("ReviewKey",rig.transform).AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.7f;key.color=new Color(1,.86f,.68f);key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(48,-32,0);
        var fill=Node("ReviewFill",rig.transform).AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.55f;fill.color=new Color(.55f,.7f,1);fill.transform.rotation=Quaternion.Euler(38,135,0);
    }
    [Serializable] class ArchiveRecordContract {public string id;public Vector3 localInteractionPosition;public bool disturbed;}
    [Serializable] class ArchiveContract
    {
        public string status="User-authorized additional art candidate, not a Client-assigned task or accepted production integration";
        public string unity="6000.3.24f1",runtimeScene="MilanLibrary_Magic_V6",existingDesk="IV4_W04_Table chunks mesh_0685/0686/0687";
        public Vector3 rootWorld=DeskWorld,playerWorkWorld=new Vector3(5.8f,.08f,7.3f),sideDoorReturnWorld=new Vector3(6,.08f,1.4f);
        public float rootYaw=0;public ArchiveRecordContract[] anchors;
        public string note="Overlay only. Retain existing table and its collision. Three slot IDs bind planner-authored data. SetDisturbed preserves sealed state. No automatic story, input, light, teleport or identity changes. Badge is face-down after disturbance.";
    }
    static void WriteArchiveContract(GameObject overlay)
    {
        var anchors=overlay.GetComponentsInChildren<MaintenanceArtAnchor>(true).Where(a=>a.anchorId!="ARCHIVE_SEAL_COMPLETE_MARKER").Select(a=>new ArchiveRecordContract{id=a.anchorId,localInteractionPosition=overlay.transform.InverseTransformPoint(a.interactionPoint.position),disturbed=a.transform.IsChildOf(overlay.GetComponent<ArchiveWorkstationArt>().disturbedProps.transform)}).ToArray();
        File.WriteAllText(ArchiveDocs+"/placement.json",JsonUtility.ToJson(new ArchiveContract{anchors=anchors},true));
    }
    static void ValidateArchiveWorkstation(GameObject overlay)
    {
        var art=overlay.GetComponent<ArchiveWorkstationArt>();var checks=new List<string>();
        Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);checks.Add(text);};
        foreach(bool after in new[]{false,true})foreach(bool sealedState in new[]{false,true})
        {
            art.SetDisturbed(after);art.SetArchiveSealed(sealedState);Physics.SyncTransforms();
            check(art.intactProps.activeSelf!=art.disturbedProps.activeSelf,"Exclusive presentation state after="+after+", sealed="+sealedState);
            check(art.IsArchiveSealed==sealedState,"Seal presentation preserved");
            var active=overlay.GetComponentsInChildren<MaintenanceArtAnchor>().Where(a=>a.anchorId.StartsWith("ARCHIVE_RECORD_SLOT_")).ToArray();
            check(active.Length==3 && active.Select(a=>a.anchorId).Distinct().Count()==3,"Exactly three unique active record slots");
            foreach(var anchor in active)
            {
                var foot=anchor.transform.position;check(Physics.Raycast(foot+Vector3.up*.003f,Vector3.down,out var hit,.08f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y-DeskTop)<.002f,"Table support: "+anchor.anchorId);
                foreach(float x in new[]{-.14f,.14f})foreach(float z in new[]{-.18f,.18f})
                {var corner=anchor.transform.TransformPoint(new Vector3(x,.003f,z));check(Physics.Raycast(corner,Vector3.down,out var edge,.08f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&Mathf.Abs(edge.point.y-DeskTop)<.002f,"Record corner supported: "+anchor.anchorId);}
                check(Vector3.Distance(DeskWorld+new Vector3(0,1.65f-DeskTop,-1.1f),anchor.interactionPoint.position)<2.3f,"Record interaction within range");
            }
            check(overlay.GetComponentsInChildren<Collider>().All(c=>c.isTrigger),"Presentation adds no solid blocker");
            if(after)
            {
                var badge=art.disturbedProps.transform.Find("ElaineBadge");check(Vector3.Dot(badge.up,Vector3.up)<-.99f,"Post-awakening badge faces down");
                check(badge.GetComponentsInChildren<TextMesh>().Length==0,"No active identity print behind post-awakening badge");
                foreach(var mesh in art.disturbedProps.transform.Find("ScatteredPaper_NoBlockingCollision").GetComponentsInChildren<MeshFilter>())check(mesh.GetComponent<Renderer>().bounds.min.z>DeskWorld.z+.6f,"Loose sheets stay behind the desk approach");
            }
        }
        art.SetArchiveSealed(true);art.SetDisturbed(false);art.SetDisturbed(true);check(art.IsArchiveSealed,"Disturbance does not erase completed seal presentation");
        check(overlay.GetComponentsInChildren<Light>(true).Length==0,"No extra production lights in overlay");
        check(overlay.GetComponentsInChildren<MeshFilter>(true).All(f=>f.sharedMesh&&f.sharedMesh.vertexCount>0),"No missing or empty meshes");
        art.ResetPresentation();
        File.WriteAllText(ArchiveDocs+"/validation.json","{\"unity\":\""+Application.unityVersion+"\",\"checks\":"+checks.Count+",\"passed\":true,\"scope\":\"Art presentation, desk support, trigger layout and state checks; no production controller or story validation\"}");
        File.WriteAllLines(ArchiveDocs+"/validation-checks.txt",checks);
    }
    [MenuItem("Art Studies/Archive/Capture workstation candidate")]
    public static void CaptureArchiveWorkstation()
    {
        ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.OpenScene(ArchiveScene);var art=Object.FindFirstObjectByType<ArchiveWorkstationArt>();var cam=Camera.main;
        try
        {
            art.ResetPresentation();SetArchiveReviewLighting(false);ArchiveCapture(cam,"01_intact_first_person");
            art.SetArchiveSealed(true);ArchiveCapture(cam,"02_seal_complete");
            art.SetDisturbed(true);SetArchiveReviewLighting(true);ArchiveCapture(cam,"03_disturbed_same_view");
            cam.transform.position=DeskWorld+new Vector3(-1.7f,1.35f,-2.0f);cam.transform.LookAt(DeskWorld+new Vector3(0,-.23f,.35f));ArchiveCapture(cam,"04_disturbed_overview");
            art.ResetPresentation();SetArchiveReviewLighting(false);cam.transform.position=DeskWorld+new Vector3(-.57f,.40f,-.26f);cam.transform.LookAt(DeskWorld+new Vector3(-.53f,.02f,.08f));cam.fieldOfView=48;ArchiveCapture(cam,"05_badge_and_cord");
        }
        finally{EditorSceneManager.OpenScene(ArchiveScene);}
    }
    public static void SetArchiveReviewLighting(bool disturbed)
    {
        if(EditorSceneManager.GetActiveScene().path!=ArchiveScene)return;
        var rig=GameObject.Find("REVIEW_ONLY_Lighting");if(!rig)return;
        var key=rig.transform.Find("ReviewKey").GetComponent<Light>();key.color=disturbed?new Color(.65f,.76f,1):new Color(1,.86f,.68f);key.intensity=disturbed?1.25f:1.7f;
        var fill=rig.transform.Find("ReviewFill").GetComponent<Light>();fill.color=disturbed?new Color(.43f,.56f,.85f):new Color(.55f,.7f,1);fill.intensity=disturbed?.40f:.55f;
    }
    static void ArchiveCapture(Camera camera,string name)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();
        try
        {camera.targetTexture=rt;for(int i=0;i<3;i++)camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(ArchiveDocs+"/Captures/"+name+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
    }
}
