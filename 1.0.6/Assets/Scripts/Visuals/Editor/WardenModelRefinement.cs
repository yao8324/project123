using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    public static partial class BossEncounterBuilder
    {
        const string DetailFolder = Root + "/Meshes/DetailV2";

        [MenuItem("Arcane Archive/Boss/Refine Warden model only")]
        public static void RefineWardenModel()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before editing the model.");
            Directory.CreateDirectory(DetailFolder);AssetDatabase.Refresh();
            gold=LoadDetailMaterial("AgedGold");cloth=LoadDetailMaterial("VioletVelvet");
            paper=LoadDetailMaterial("AncientParchment");ink=LoadDetailMaterial("BindingLeather");
            darkStone=LoadDetailMaterial("Obsidian");violet=LoadDetailMaterial("VioletSeal");
            var ivory=Mat("MaskIvory",new Color(.76f,.72f,.60f),.12f,.35f);
            var bone=Mat("BoneEdges",new Color(.42f,.38f,.30f),.25f,.32f);
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/ForbiddenWarden.prefab");
            try
            {
                var rig=root.GetComponent<WardenVisuals>();
                BuildMaskDetails(rig,ivory);
                BuildBodyDetails(rig,bone);
                BuildHandDetails(rig.leftArm,-1);BuildHandDetails(rig.rightArm,1);
                BuildWingDetails(rig.leftWing,-1);BuildWingDetails(rig.rightWing,1);
                BuildCodexDetails(rig);
                // Keep original rig transforms, collision volumes and nested prefab identity.
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/ForbiddenWarden.prefab");
                ExportObj(root,Root+"/Source/ForbiddenWarden.obj");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            Debug.Log("[Boss] Refined mask, cowl, armour, articulated hands, layered folios and heart codex. Original collision and animation rig retained.");
        }

        static Material LoadDetailMaterial(string name)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
            if(!material)throw new InvalidOperationException("Missing base material: "+name);
            return material;
        }
        static Transform DetailGroup(Transform parent)
        {
            var old=parent.Find("DetailV2");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            return Node("DetailV2",parent).transform;
        }
        static void HideOriginal(Transform parent,params string[] names)
        {
            foreach(Transform child in parent)if(names.Contains(child.name))child.gameObject.SetActive(false);
        }
        static GameObject DetailShape(string name,Transform parent,Mesh mesh,Material material,Vector3 position=default)
        {
            var go=Node(name,parent,position);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        // Collapse added static ornament by material within each animated part. No extra colliders.
        static void FinishDetails(Transform group,string part)
        {
            var filters=group.GetComponentsInChildren<MeshFilter>();
            var materials=filters.Select(f=>f.GetComponent<MeshRenderer>().sharedMaterial).Distinct().ToArray();
            var transient=new HashSet<Mesh>(filters.Select(f=>f.sharedMesh).Where(m=>!AssetDatabase.Contains(m)));
            foreach(var material in materials)
            {
                var instances=filters.Where(f=>f.GetComponent<MeshRenderer>().sharedMaterial==material)
                    .Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=group.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(instances,true,true);mesh.RecalculateBounds();
                string assetPath=DetailFolder+"/"+part+"_"+material.name+".asset";mesh.name=part+"_"+material.name;
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}
                else AssetDatabase.CreateAsset(mesh,assetPath);
                var go=Node(part+"_"+material.name,group);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            }
            foreach(var filter in filters)if(filter)UnityEngine.Object.DestroyImmediate(filter.gameObject);
            foreach(var mesh in transient)if(mesh)UnityEngine.Object.DestroyImmediate(mesh);
        }
        static void DetailCurve(string name,Transform parent,Vector3[] points,float radius,Material material)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();const int sides=8;
            for(int i=0;i<points.Length;i++)
            {
                var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var normal=Vector3.Cross(tangent,Mathf.Abs(tangent.z)<.9f?Vector3.forward:Vector3.up).normalized;
                var binormal=Vector3.Cross(tangent,normal);
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides;vertices.Add(points[i]+radius*(normal*Mathf.Cos(a)+binormal*Mathf.Sin(a)));
                    if(i==points.Length-1)continue;int k=i*sides+j,n=i*sides+(j+1)%sides;
                    triangles.AddRange(new[]{k,n,k+sides,n,n+sides,k+sides});
                }
            }
            DetailShape(name,parent,Mesh(vertices,triangles),material);
        }
        static void DetailOval(string name,Transform parent,Vector3 center,float rx,float ry,Material material,float thickness=.012f,bool edgeOn=false)
        {
            int segments=rx<.05f?12:32;var points=new Vector3[segments+1];
            for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/segments;points[i]=center+(edgeOn?new Vector3(0,Mathf.Sin(a)*ry,Mathf.Cos(a)*rx):new Vector3(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry,0));}
            DetailCurve(name,parent,points,thickness,material);
        }
        static void Chain(Transform parent,Vector3 a,Vector3 b,int links)
        {
            for(int i=0;i<links;i++)DetailOval("OathLink",parent,Vector3.Lerp(a,b,i/(float)(links-1)),.028f,.042f,gold,.008f,i%2==1);
        }
        static void Rivet(Transform parent,Vector3 p,float size=.035f)
        {DetailBall("Rivet",parent,p,new Vector3(size,size,size*.6f),gold);}
        static void DetailBall(string name,Transform parent,Vector3 p,Vector3 size,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();const int rings=6,sides=12;
            for(int i=0;i<=rings;i++)for(int j=0;j<=sides;j++)
            {
                float latitude=i*Mathf.PI/rings,longitude=j*Mathf.PI*2/sides;
                v.Add(Vector3.Scale(size,new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude)))*.5f);
                if(i==rings||j==sides)continue;int k=i*(sides+1)+j;t.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});
            }
            DetailShape(name,parent,Mesh(v,t),material,p);
        }

        static Mesh CurvedVisage(int sign,float inset)
        {
            float[] ys={-.48f,-.30f,-.08f,.18f,.35f,.52f};
            float[] widths={.025f,.16f,.26f,.30f,.24f,.025f};
            float[] depths={.12f,.20f,.255f,.25f,.19f,.09f};
            var vertices=new List<Vector3>();var triangles=new List<int>();const int columns=6;
            for(int i=0;i<ys.Length;i++)for(int j=0;j<=columns;j++)
            {
                float u=j/(float)columns;float halfGap=.020f+Mathf.Sin(i*1.7f)*.006f;
                float x=Mathf.Lerp(halfGap,widths[i]*inset,u);
                vertices.Add(new Vector3(sign*x,ys[i]*inset,depths[i]-.07f*u*u+(inset<1?.012f:0)));
                if(i==ys.Length-1 || j==columns)continue;int k=i*(columns+1)+j;
                if(sign>0)triangles.AddRange(new[]{k,k+1,k+columns+1,k+1,k+columns+2,k+columns+1});
                else triangles.AddRange(new[]{k,k+columns+1,k+1,k+1,k+columns+1,k+columns+2});
            }
            return Mesh(vertices,triangles);
        }
        static void BuildMaskDetails(WardenVisuals rig,Material ivory)
        {
            HideOriginal(rig.mask,"GothicMask","MaskInlay","SealedSight","Brow");
            var g=DetailGroup(rig.mask);
            DetailShape("RecessedVoid",g,Plate(new[]{new Vector2(0,.51f),new Vector2(.29f,.28f),new Vector2(.25f,-.2f),new Vector2(0,-.48f),new Vector2(-.25f,-.2f),new Vector2(-.29f,.28f)},.12f),darkStone,new Vector3(0,0,.07f));
            foreach(int side in new[]{-1,1})
            {
                DetailShape("CastGoldFace",g,CurvedVisage(side,1),gold);
                DetailShape("IvoryFace",g,CurvedVisage(side,.87f),ivory);
                DetailCurve("EngravedBrow",g,new[]{new Vector3(side*.025f,.12f,.27f),new Vector3(side*.11f,.19f,.26f),new Vector3(side*.21f,.20f,.23f)},.013f,gold);
                DetailCurve("CheekScar",g,new[]{new Vector3(side*.14f,-.02f,.26f),new Vector3(side*.19f,-.12f,.25f),new Vector3(side*.10f,-.29f,.22f)},.009f,ink);
                Rivet(g,new Vector3(side*.22f,.31f,.20f),.027f);
            }
            DetailCurve("BrokenSealSight",g,new[]{new Vector3(0,.38f,.205f),new Vector3(-.008f,.19f,.25f),new Vector3(.005f,.04f,.255f),new Vector3(-.006f,-.17f,.23f),new Vector3(0,-.36f,.18f)},.009f,violet);
            for(int i=0;i<3;i++)DetailCurve("MuteStaple",g,new[]{new Vector3(-.047f,-.14f-i*.065f,.26f-i*.018f),new Vector3(.047f,-.14f-i*.065f,.26f-i*.018f)},.011f,gold);
            FinishDetails(g,"Mask");
        }
        static Mesh OpenCowl()
        {
            float[] y={3.10f,3.38f,3.8f,4.13f,4.35f};float[] width={.39f,.52f,.53f,.44f,.10f};
            var v=new List<Vector3>();var t=new List<int>();const int n=40;
            for(int i=0;i<y.Length;i++)for(int j=0;j<=n;j++)
            {
                float a=Mathf.Lerp(150,390,j/(float)n)*Mathf.Deg2Rad;
                v.Add(new Vector3(Mathf.Cos(a)*width[i],y[i],Mathf.Sin(a)*width[i]*.85f));
                if(i==y.Length-1||j==n)continue;int k=i*(n+1)+j;t.AddRange(new[]{k,k+1,k+n+1,k+1,k+n+2,k+n+1});
            }
            return Mesh(v,t);
        }
        static void BuildBodyDetails(WardenVisuals rig,Material bone)
        {
            var g=DetailGroup(rig.floatingBody);DetailShape("OpenCowl",g,OpenCowl(),cloth);
            foreach(int s in new[]{-1,1})
            {
                DetailCurve("CowlPiping",g,new[]{new Vector3(s*.34f,3.1f,.18f),new Vector3(s*.45f,3.38f,.22f),new Vector3(s*.46f,3.8f,.23f),new Vector3(s*.38f,4.13f,.19f),new Vector3(s*.085f,4.35f,.04f)},.019f,gold);
                for(int i=0;i<3;i++)
                {
                    var p=DetailShape("OverlappingShoulderScale",g,Plate(new[]{new Vector2(-.23f,-.14f),new Vector2(.23f,-.14f),new Vector2(.19f,.19f),new Vector2(0,.32f),new Vector2(-.19f,.19f)},.065f),i==1?bone:gold,new Vector3(s*(.72f+i*.16f),3.22f-i*.14f,.24f));
                    p.transform.localRotation=Quaternion.Euler(0,s*18,-s*(18+i*12));Rivet(g,p.transform.localPosition+new Vector3(0,.03f,.06f),.045f);
                }
                DetailCurve("StoleGiltEdge",g,new[]{new Vector3(s*.56f,2.4f,.53f),new Vector3(s*.68f,1.65f,.55f),new Vector3(s*.75f,.9f,.54f),new Vector3(s*.68f,.67f,.55f)},.015f,gold);
                for(int i=0;i<3;i++)
                {
                    float x=s*(.56f+i*.10f);float y=1.92f-i*.10f;
                    Chain(g,new Vector3(x,2.30f,.56f),new Vector3(x,y,.59f),6+i);
                    var seal=DetailShape("DanglingOathTag",g,Plate(new[]{new Vector2(-.05f,.1f),new Vector2(.05f,.1f),new Vector2(.06f,-.08f),new Vector2(0,-.14f),new Vector2(-.06f,-.08f)},.025f),paper,new Vector3(x,y-.10f,.59f));
                    seal.transform.localRotation=Quaternion.Euler(0,0,s*(8+i*11));
                    DetailBall("OathWax",g,new Vector3(x,y-.07f,.61f),new Vector3(.055f,.055f,.025f),ink);
                }
            }
            for(int i=0;i<7;i++)
            {
                var p=DetailShape("SpineBinding",g,Plate(new[]{new Vector2(-.13f,.09f),new Vector2(.13f,.09f),new Vector2(.18f,-.02f),new Vector2(0,-.14f),new Vector2(-.18f,-.02f)},.07f),gold,new Vector3(0,2.95f-i*.23f,-.43f-i*.014f));
                p.transform.localRotation=Quaternion.Euler(0,180,0);
            }
            FinishDetails(g,"Body");
        }
        static void BuildHandDetails(Transform arm,int side)
        {
            HideOriginal(arm,"Gauntlet");var g=DetailGroup(arm);
            DetailShape("ArticulatedPalm",g,Plate(new[]{new Vector2(-.11f,.16f),new Vector2(.11f,.16f),new Vector2(.14f,-.04f),new Vector2(.09f,-.20f),new Vector2(-.09f,-.20f),new Vector2(-.14f,-.04f)},.13f),gold,new Vector3(0,-1.30f,.05f));
            DetailShape("PalmLeatherInset",g,Plate(new[]{new Vector2(0,.11f),new Vector2(.075f,0),new Vector2(0,-.13f),new Vector2(-.075f,0)},.025f),ink,new Vector3(0,-1.29f,.13f));
            for(int finger=0;finger<4;finger++)for(int joint=0;joint<2;joint++)
            {
                float x=(finger-1.5f)*.065f,y=-1.46f-joint*.16f;
                DetailBall("Knuckle",g,new Vector3(x,y,.064f+joint*.027f),new Vector3(.060f,.054f,.058f),gold);
                DetailCurve("FingerInlay",g,new[]{new Vector3(x,y-.025f,.097f),new Vector3(x,y-.10f,.108f)},.008f,darkStone);
            }
            for(int i=0;i<5;i++)Rivet(g,new Vector3((i-2)*.095f,-1.06f,.30f),.035f);
            DetailOval("CuffSigil",g,new Vector3(0,-.91f,.27f),.085f,.12f,gold,.012f);
            FinishDetails(g,side<0?"HandL":"HandR");
        }
        static Mesh FolioLeaf(int side,float length,int seed)
        {
            var v=new List<Vector3>();var t=new List<int>();const int n=20;
            for(int i=0;i<=n;i++)
            {
                float u=i/(float)n,x=.23f+u*length,w=.18f+.08f*Mathf.Sin(u*Mathf.PI);
                float z=.09f*Mathf.Sin(u*Mathf.PI*1.8f+seed*.35f);
                float tear=i==n?((seed%2==0)?.055f:-.025f):0;
                v.Add(new Vector3(side*x,-w,z));v.Add(new Vector3(side*(x-tear),w,z+.025f));
                if(i<n){int k=i*2;t.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            }
            return Mesh(v,t);
        }
        static void BuildWingDetails(Transform wing,int side)
        {
            var g=DetailGroup(wing);
            for(int i=0;i<5;i++)
            {
                float length=2.02f-i*.14f;
                var page=DetailShape("LayeredFolio",g,FolioLeaf(side,length,i),paper,new Vector3(0,0,-.06f-i*.014f)).transform;
                page.localRotation=Quaternion.Euler(0,side*(i-2)*4,side*(-15+i*18));
                for(int line=0;line<9;line++)
                {
                    float u=.18f+line*.073f,x=.23f+u*length,z=.09f*Mathf.Sin(u*Mathf.PI*1.8f+i*.35f)+.02f;
                    float end=(line%3==0)?.045f:.12f;
                    DetailCurve("Inscription",page,new[]{new Vector3(side*x,-.12f,z),new Vector3(side*x,end,z+.014f)},.005f,ink);
                }
                DetailCurve("BoundFolioEdge",page,new[]{new Vector3(side*.27f,-.18f,.02f),new Vector3(side*.27f,.18f,.045f)},.014f,gold);
                Rivet(page,new Vector3(side*.29f,.13f,.055f),.034f);
            }
            FinishDetails(g,side<0?"WingL":"WingR");
        }
        static void BuildCodexDetails(WardenVisuals rig)
        {
            var cover=DetailGroup(rig.chestCover);
            foreach(int sx in new[]{-1,1})foreach(int sy in new[]{-1,1})
            {
                var pos=new Vector3(.36f+sx*.28f,sy*.40f,.064f);
                DetailShape("CornerProtector",cover,Plate(new[]{new Vector2(-.06f,-.07f),new Vector2(.06f,-.07f),new Vector2(.06f,.07f),new Vector2(0,.10f),new Vector2(-.06f,.07f)},.022f),gold,pos);Rivet(cover,pos+Vector3.forward*.018f,.03f);
            }
            DetailCurve("PointedSealFrame",cover,new[]{new Vector3(.18f,-.32f,.086f),new Vector3(.18f,.17f,.086f),new Vector3(.36f,.35f,.086f),new Vector3(.54f,.17f,.086f),new Vector3(.54f,-.32f,.086f)},.012f,gold);
            DetailOval("CoverSeal",cover,new Vector3(.36f,0,.10f),.11f,.16f,gold,.012f);
            FinishDetails(cover,"CodexCover");
            var g=DetailGroup(rig.chestCover.parent);
            for(int i=0;i<12;i++)DetailCurve("IndividualPageEdge",g,new[]{new Vector3(.328f,-.42f,.012f+i*.015f),new Vector3(.328f,.42f,.012f+i*.015f)},.004f,ink);
            for(int i=0;i<4;i++)
                Primitive("SpineBand",PrimitiveType.Cube,g,new Vector3(-.38f,-.34f+i*.23f,.075f),new Vector3(.09f,.042f,.33f),gold);
            DetailOval("CoreAperture",g,new Vector3(0,0,.242f),.13f,.19f,gold,.018f);
            DetailOval("InnerCoreSeal",g,new Vector3(0,0,.255f),.09f,.13f,violet,.009f);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Rivet(g,new Vector3(Mathf.Cos(a)*.16f,Mathf.Sin(a)*.22f,.245f),.027f);}
            FinishDetails(g,"CodexBody");
        }
    }
}
