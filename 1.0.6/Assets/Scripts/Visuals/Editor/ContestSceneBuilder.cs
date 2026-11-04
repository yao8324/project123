using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using ArcaneArchive.Interaction;
using ArcaneArchive.Contest;
using IMMonster.SceneArt;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    public static class ContestSceneBuilder
    {
        public const string ScenePath="Assets/Scenes/IMMonsterContest20261107.unity";
        const string Root="Assets/Contest20261107";
        const string Art="Assets/Art/SealChamberStudy/";
        const string Weekly=Art+"MaintenanceW20261028/Prefabs/";
        static Material brass,wood,paper,cloth,black,blue,magic;
        [MenuItem("IM Monster/Build contest story scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory("Docs/Contest20261107");AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            var original=Object.FindFirstObjectByType<BossEncounter>();if(!original)throw new Exception("Base player/encounter missing");
            var player=original.GetComponent<FirstPersonController>();var library=original.libraryEnvironment;var camera=player.GetComponentInChildren<Camera>();
            foreach(var name in new[]{"RecoveryMenu","UIInputStateManager","BossEncounter","PlayerCombat","BookPreviewInteractor","PlayerInteractor"})
                foreach(var component in player.GetComponents<MonoBehaviour>().Where(c=>c && c.GetType().Name==name).ToArray())Object.DestroyImmediate(component);
            foreach(var component in Object.FindObjectsByType<UIInputStateManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(component);
            foreach(var model in player.GetComponentsInChildren<FirstPersonViewModel>(true))if(model.transform!=player.transform)model.gameObject.SetActive(false);
            var game=new GameObject("IMMonsterContestStory").AddComponent<ContestStoryGame>();game.player=player;game.view=camera;game.library=library;
            game.cameraPivot=(Transform)new SerializedObject(player).FindProperty("cameraPivot").objectReferenceValue;
            game.gameObject.AddComponent<ContestBuildProbe>();
            game.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKsc-Regular.otf");game.contentFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/StoryContent.json");
            if(!game.font || !game.contentFile)throw new Exception("Contest font or content missing");
            var fontImporter=(TrueTypeFontImporter)AssetImporter.GetAtPath(Root+"/Fonts/NotoSansCJKsc-Regular.otf");fontImporter.fontTextureCase=FontTextureCase.Dynamic;fontImporter.includeFontData=true;fontImporter.SaveAndReimport();
            var overlay=Instance(Art+"ArchiveWorkstation20261102/Prefabs/ArchiveWorkstation.prefab",library.transform,new Vector3(5.8f,.836f,8.4f));
            game.workstation=overlay.GetComponent<ArchiveWorkstationArt>();BindArt(overlay);
            foreach(var group in new[]{game.workstation.intactProps,game.workstation.disturbedProps})
            {
                var from=group.transform.Find("ArchiveSlot_01/Cord_Sealed");var to=group.transform.Find("ArchiveSlot_03");
                if(from && to)from.SetParent(to,false);
            }
            var entry=Instance(Weekly+"ArchiveEntryKit.prefab",library.transform,new Vector3(6,0,-.2f),Quaternion.Euler(0,180,0));BindArt(entry);
            var hall=Instance(Weekly+"MaintenanceHall.prefab",null,new Vector3(160,0,0));hall.name="ContestMaintenanceHall";BindArt(hall);game.hall=hall;hall.SetActive(false);
            brass=LibraryMat("IV4_Oxidised_Brass");wood=LibraryMat("IV4_Walnut_QuarterSawn");paper=LibraryMat("MAT_Old_Paper");blue=AssetDatabase.LoadAssetAtPath<Material>(Art+"MaintenanceW20261028/Materials/Archive_Cord_Blue.mat");
            cloth=Material("CoveredLinen",new Color(.18f,.19f,.18f),0,.12f);black=Material("TornFabric",new Color(.055f,.025f,.028f),0,.05f);
            magic=Material("SealEcho",new Color(.18f,.28f,.46f),.15f,.3f,true);
            var story=Node("ContestStoryProps",library.transform,Vector3.zero);
            BuildBody(game,story.transform);BuildConduit(game,story.transform);BuildCase(game,story.transform);BuildPalm(game,camera.transform);
            player.transform.SetPositionAndRotation(new Vector3(5.2f,.08f,7.1f),Quaternion.identity);
            camera.transform.localRotation=Quaternion.Euler(24,0,0);
            Physics.SyncTransforms();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            AssetDatabase.SaveAssets();ValidateScene(game);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            Debug.Log("[Contest] Scene assembled "+ScenePath);
        }
        static GameObject Instance(string path,Transform parent,Vector3 position,Quaternion? rotation=null)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Missing prefab "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(position,rotation??Quaternion.identity);return go;
        }
        static void BindArt(GameObject root)
        {
            foreach(var anchor in root.GetComponentsInChildren<MaintenanceArtAnchor>(true))
            {var target=anchor.GetComponent<ContestWorldTarget>()??anchor.gameObject.AddComponent<ContestWorldTarget>();target.id=anchor.anchorId;target.aimPoint=anchor.interactionPoint;}
        }
        static void BuildBody(ContestStoryGame game,Transform parent)
        {
            var root=Node("CoveredArchivist",parent,new Vector3(5.15f,.018f,10.85f));game.body=root;
            var sheet=Node("FoldedCover",root.transform,Vector3.zero);sheet.AddComponent<MeshFilter>().sharedMesh=ClothMesh();sheet.AddComponent<MeshRenderer>().sharedMaterial=cloth;
            Rounded("CoveredHead",root.transform,new Vector3(-.73f,.17f,0),new Vector3(.34f,.34f,.31f),cloth);
            foreach(int side in new[]{-1,1})Rounded("Shoe",root.transform,new Vector3(.88f,.075f,side*.13f),new Vector3(.31f,.15f,.16f),LibraryMat("IV4_Oxblood_Leather"));
            var collision=root.AddComponent<BoxCollider>();collision.center=new Vector3(0,.17f,0);collision.size=new Vector3(1.78f,.32f,.65f);
            // A sleeve and a slit in fabric show position, without a face or gore.
            Rounded("OxbloodSleeve",root.transform,new Vector3(-.10f,.20f,-.33f),new Vector3(.57f,.13f,.18f),LibraryMat("IV4_Oxblood_Leather"));
            var wound=Box("TornAbdomen",root.transform,new Vector3(.19f,.235f,-.25f),new Vector3(.20f,.025f,.095f),black);wound.transform.localRotation=Quaternion.Euler(0,-23,0);
            for(int i=0;i<4;i++)Box("TornEdge",root.transform,new Vector3(.12f+i*.05f,.251f,-.30f+i*.012f),new Vector3(.013f,.012f,.07f),cloth);
            var badge=Instance(Weekly+"ElaineWorkBadge.prefab",root.transform,root.transform.TransformPoint(new Vector3(-.29f,.268f,-.31f)));badge.transform.localRotation=Quaternion.Euler(0,-12,0);badge.transform.localScale=Vector3.one*.85f;
            var evidence=Node("EvidenceTargets",root.transform,Vector3.zero);game.bodyEvidence=evidence;
            badge.transform.SetParent(evidence.transform,true);
            Target("REVEAL_BADGE",evidence.transform,new Vector3(-.29f,.40f,-.31f),new Vector3(.24f,.09f,.22f));
            Target("REVEAL_WOUND",evidence.transform,new Vector3(.19f,.39f,-.25f),new Vector3(.26f,.08f,.19f));
            var bundle=Instance(Weekly+"ArchiveRecord.prefab",root.transform,root.transform.TransformPoint(new Vector3(.36f,.27f,.04f)));bundle.name="SingleSealedBatch";bundle.transform.localScale=Vector3.one*1.15f;bundle.transform.localRotation=Quaternion.Euler(0,-10,0);game.sealedBundle=bundle;
            foreach(float z in new[]{-.09f,.09f})Box("BlueBinding",bundle.transform,new Vector3(0,.058f,z),new Vector3(.295f,.006f,.014f),blue);
            Box("CrossBinding",bundle.transform,new Vector3(-.04f,.061f,0),new Vector3(.013f,.005f,.38f),blue);
            var knot=Node("SameHandTiedKnot",bundle.transform,new Vector3(-.04f,.07f,-.09f));
            foreach(int side in new[]{-1,1}){var loop=Box("BlueLoop",knot.transform,new Vector3(side*.024f,0,0),new Vector3(.053f,.007f,.013f),blue);loop.transform.localRotation=Quaternion.Euler(0,side*35,0);}
            Target("REVEAL_CORD",evidence.transform,new Vector3(.36f,.42f,.04f),new Vector3(.36f,.09f,.42f));
            var incident=Instance(Weekly+"ArchiveRecord.prefab",parent,new Vector3(4.03f,.035f,10.8f),Quaternion.Euler(0,18,0));incident.name="IncidentRecord";game.incidentObject=incident;
            Target("ARCHIVE_INCIDENT_RECORD",incident.transform,new Vector3(0,.07f,0),new Vector3(.32f,.09f,.39f));
            game.body.SetActive(false);evidence.SetActive(false);incident.SetActive(false);
        }
        static void BuildConduit(ContestStoryGame game,Transform parent)
        {
            var root=Node("DamagedLocalConduit",parent,new Vector3(4.0f,0,5.0f));game.conduit=root;
            Box("StoneFoot",root.transform,new Vector3(0,.11f,0),new Vector3(.65f,.22f,.52f),cloth,true);
            Box("WoodHousing",root.transform,new Vector3(0,.56f,0),new Vector3(.42f,.80f,.32f),wood,true);
            foreach(float x in new[]{-.24f,.24f})Box("BrassRim",root.transform,new Vector3(x,.61f,-.18f),new Vector3(.035f,.84f,.04f),brass);
            Box("ControlFace",root.transform,new Vector3(0,.82f,-.19f),new Vector3(.43f,.30f,.05f),brass);
            var emblem=Node("CrackedCoupling",root.transform,new Vector3(0,.83f,-.225f));Ring(emblem.transform,.10f,magic,0);
            Target("RESCUE_CONDUIT",root.transform,new Vector3(0,.83f,-.28f),new Vector3(.47f,.39f,.08f));
            game.sealRenderers=root.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==magic).ToArray();root.SetActive(false);
        }
        static void BuildCase(ContestStoryGame game,Transform parent)
        {
            var root=Node("ArchiveTransferCase",parent,new Vector3(5.0f,0,3.7f));game.transferCase=root;
            Box("Pedestal",root.transform,new Vector3(0,.39f,0),new Vector3(.68f,.78f,.50f),wood,true);
            Box("CaseBottom",root.transform,new Vector3(0,.83f,0),new Vector3(.72f,.10f,.50f),LibraryMat("IV4_Oxblood_Leather"));
            foreach(int sign in new[]{-1,1})
            {Box("SideRim",root.transform,new Vector3(sign*.35f,.94f,0),new Vector3(.04f,.22f,.51f),brass);Box("EndRim",root.transform,new Vector3(0,.94f,sign*.24f),new Vector3(.68f,.22f,.035f),brass);}
            var lid=Node("Lid",root.transform,new Vector3(0,1.055f,.25f));Box("LeatherLid",lid.transform,new Vector3(0,0,-.25f),new Vector3(.72f,.035f,.53f),LibraryMat("IV4_Oxblood_Leather"));
            Box("LidBand",lid.transform,new Vector3(0,.022f,-.25f),new Vector3(.07f,.015f,.54f),brass);lid.transform.localRotation=Quaternion.Euler(-24,0,0);
            Target("ARCHIVE_TRANSFER_CASE",root.transform,new Vector3(0,1.1f,-.15f),new Vector3(.73f,.2f,.55f));
        }
        static void BuildPalm(ContestStoryGame game,Transform camera)
        {
            var root=Node("PlayersLeftPalm",camera,new Vector3(-.23f,-.19f,.46f));root.transform.localRotation=Quaternion.Euler(-18,-8,-18);game.palm=root;
            var glove=Material("RecoveredGlove",new Color(.075f,.085f,.11f),.1f,.24f);
            Box("Palm",root.transform,Vector3.zero,new Vector3(.105f,.135f,.026f),glove);
            for(int i=0;i<4;i++)Box("Finger",root.transform,new Vector3((i-1.5f)*.027f,.10f+(i==1||i==2?.011f:0),0),new Vector3(.021f,.093f,.024f),glove);
            var thumb=Box("Thumb",root.transform,new Vector3(-.072f,.018f,0),new Vector3(.024f,.083f,.025f),glove);thumb.transform.localRotation=Quaternion.Euler(0,0,-42);
            var echo=Node("ThreeGapSeal",root.transform,new Vector3(0,0,-.016f));Ring(echo.transform,.037f,magic,0);
            root.SetActive(false);
        }
        static void Ring(Transform parent,float radius,Material material,float z)
        {
            for(int i=0;i<48;i++)
            {
                if(i%16<3)continue;float angle=i*7.5f*Mathf.Deg2Rad;
                var part=Box("SealArc",parent,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,z),new Vector3(radius*.17f,radius*.075f,radius*.06f),material);
                part.transform.localRotation=Quaternion.Euler(0,0,i*7.5f+90);
            }
        }
        static GameObject Node(string name,Transform parent,Vector3 local)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=local;return go;}
        static GameObject Box(string name,Transform parent,Vector3 point,Vector3 size,Material mat,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=point;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static GameObject Rounded(string name,Transform parent,Vector3 point,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=point;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        static void Target(string id,Transform parent,Vector3 point,Vector3 size)
        {
            var go=Node(id,parent,point);var target=go.AddComponent<ContestWorldTarget>();target.id=id;target.aimPoint=go.transform;var collider=go.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=size;
        }
        static Material LibraryMat(string name)
        {var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilanLibraryV6/Materials/"+name+".mat");if(!mat)throw new Exception("Missing original material "+name);return mat;}
        static Material Material(string name,Color color,float metallic,float smoothness,bool emission=false)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.6f);}
            EditorUtility.SetDirty(m);return m;
        }
        static Mesh ClothMesh()
        {
            const int nx=36,nz=18;var vertices=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int x=0;x<=nx;x++)for(int z=0;z<=nz;z++)
            {
                float px=(float)x/nx*1.95f-.975f,width=.45f*(1-.35f*Mathf.Pow(Mathf.Abs(px)/.975f,3));
                float pz=((float)z/nz*2-1)*width;
                float shape=Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(pz)/width),.65f);
                float body=.24f*Mathf.Exp(-px*px/.75f),head=.13f*Mathf.Exp(-(px+.64f)*(px+.64f)/.035f);
                float fold=.028f*Mathf.Sin(px*28+pz*9)+.014f*Mathf.Sin(pz*45-px*10);
                int n=x*(nz+1)+z;vertices[n]=new Vector3(px,.025f+shape*(body+head+fold),pz);uv[n]=new Vector2(x/(float)nx,z/(float)nz);
                if(x<nx && z<nz){int a=n,b=n+nz+1;triangles.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
            }
            var mesh=new Mesh{name="CoveredArchivist_FoldedSheet"};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Root+"/Meshes/CoveredArchivist.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        [Serializable] class Probe {public Vector3 point;public bool floor;public string[] overlaps;}
        [Serializable] class SceneReport {public string unity,scene;public int players,targets,hallPointLights;public Probe[] probes;}
        static void ValidateScene(ContestStoryGame game)
        {
            game.body.SetActive(true);game.bodyEvidence.SetActive(true);game.conduit.SetActive(true);Physics.SyncTransforms();
            var cc=game.player.GetComponent<CharacterController>();var probes=new List<Probe>();
            foreach(var p in new[]{new Vector3(5.2f,.08f,7.1f),new Vector3(5.05f,.08f,7),new Vector3(6,.08f,1.4f),new Vector3(4.275f,.08f,9.85f),new Vector3(5.5f,.08f,9.85f),new Vector3(4,.08f,3.8f),new Vector3(5,.08f,2.5f)})
            {
                float half=cc.height*.5f-cc.radius;Vector3 center=p+cc.center;
                var overlaps=Physics.OverlapCapsule(center-Vector3.up*half,center+Vector3.up*half,cc.radius+.02f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(game.player.transform)).Select(c=>c.name).Distinct().ToArray();
                probes.Add(new Probe{point=p,floor=Physics.Raycast(p+Vector3.up,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),overlaps=overlaps});
            }
            var report=new SceneReport{unity=Application.unityVersion,scene=ScenePath,players=Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None).Length,targets=Object.FindObjectsByType<ContestWorldTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,hallPointLights=game.hall.GetComponentsInChildren<Light>(true).Count(l=>l.enabled && l.type==LightType.Point),probes=probes.ToArray()};
            File.WriteAllText("Docs/Contest20261107/scene-assembly.json",JsonUtility.ToJson(report,true));
            if(report.players!=1 || report.hallPointLights!=6)throw new Exception("Contest scene ownership/light validation failed");
            game.body.SetActive(false);game.bodyEvidence.SetActive(false);game.conduit.SetActive(false);
        }
        [MenuItem("IM Monster/Build Windows contest player")]
        public static void PrepareAndBuild(){Prepare();BuildWindows();}
        public static void BuildWindows()
        {
            string folder=Environment.GetEnvironmentVariable("IM_CONTEST_OUTPUT");if(string.IsNullOrEmpty(folder))folder=Path.GetFullPath("Builds/IMMonsterContest20261107");
            Directory.CreateDirectory(folder);string previousName=PlayerSettings.productName,previousVersion=PlayerSettings.bundleVersion;
            BuildReport report;
            try
            {
                PlayerSettings.productName="IM MONSTER";PlayerSettings.bundleVersion="2026.11.3.1";
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(folder,"IMMonster.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
            }
            finally{PlayerSettings.productName=previousName;PlayerSettings.bundleVersion=previousVersion;}
            File.WriteAllText("Docs/Contest20261107/build-report.json",JsonUtility.ToJson(new BuildResult{result=report.summary.result.ToString(),errors=(int)report.summary.totalErrors,warnings=(int)report.summary.totalWarnings,size=(long)report.summary.totalSize,seconds=(float)report.summary.totalTime.TotalSeconds,unity=Application.unityVersion},true));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Contest build failed: "+report.summary.result);
            File.Copy(Root+"/Fonts/OFL.txt",Path.Combine(folder,"NotoFont-OFL.txt"),true);
            File.Copy(Root+"/Fonts/NOTICE.txt",Path.Combine(folder,"NotoFont-NOTICE.txt"),true);
            Debug.Log("[Contest] Windows player built "+folder);
        }
        [Serializable] class BuildResult {public string result,unity;public int errors,warnings;public long size;public float seconds;}
    }
}
