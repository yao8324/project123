using System.Collections.Generic;
using ArcaneArchive.Interaction;
using ArcaneArchive.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneArchive.EditorTools
{
    public static class GrandEntranceBuilder
    {
        private const string Folder="Assets/Art/Models/GrandEntrance";
        private static Mesh cube;
        private sealed class Geometry
        {
            public readonly List<CombineInstance> pieces=new List<CombineInstance>();
            public void Box(Vector3 center,Vector3 size,float angle=0)
            {
                pieces.Add(new CombineInstance {mesh=cube,transform=Matrix4x4.TRS(center,Quaternion.Euler(0,0,angle),size)});
            }
            public void Stroke(Vector2 a,Vector2 b,float z,float width,float depth=.045f)
            {
                var d=b-a;Box(new Vector3((a.x+b.x)*.5f,(a.y+b.y)*.5f,z),new Vector3(d.magnitude,width,depth),Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            }
            public void Arc(Vector2 center,float radius,float start,float end,float z,float width)
            {
                int steps=Mathf.Max(8,Mathf.CeilToInt(Mathf.Abs(end-start)/2.5f));
                Vector2 prev=center+radius*new Vector2(Mathf.Cos(start*Mathf.Deg2Rad),Mathf.Sin(start*Mathf.Deg2Rad));
                for(int i=1;i<=steps;i++){float a=Mathf.Lerp(start,end,i/(float)steps)*Mathf.Deg2Rad;var next=center+radius*new Vector2(Mathf.Cos(a),Mathf.Sin(a));Stroke(prev,next,z,width);prev=next;}
            }
            public void Diamond(Vector2 center,float x,float y,float z,float width)
            {
                var v=new[]{center+Vector2.up*y,center+Vector2.right*x,center-Vector2.up*y,center-Vector2.right*x};
                for(int i=0;i<4;i++)Stroke(v[i],v[(i+1)%4],z,width);
            }
            public GameObject Save(string name,Transform parent,Material material)
            {
                var mesh=new Mesh {name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                mesh.CombineMeshes(pieces.ToArray(),true,true);mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name+".asset"));
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
            }
        }
        private static Material Material(string name,Color color,float metallic,float smoothness,Color emission)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}
            AssetDatabase.CreateAsset(m,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name+".mat"));return m;
        }
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
            if(GameObject.Find("GrandEntranceFrame"))return "Grand entrance already exists";
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/Models","GrandEntrance");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene,AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/MilanLibrary_Magic_V6_BeforeGrandEntrance.unity"),true);
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
            int widened=WidenOpening();
            var old=GameObject.Find("ArcaneEntranceDoor");Undo.RecordObject(old,"Replace entrance");old.name="_Backup_ArcaneEntranceDoor";old.SetActive(false);
            var root=new GameObject("ArcaneEntranceDoor");Undo.RegisterCreatedObjectUndo(root,"Build grand arcane entrance");root.transform.SetParent(old.transform.parent,false);root.transform.position=new Vector3(0,0,-.2f);
            var dark=Material("Archive_ObsidianWood",new Color(.024f,.045f,.047f),.32f,.43f,Color.black);
            var inset=Material("Archive_DeepTealEnamel",new Color(.028f,.105f,.12f),.55f,.5f,Color.black);
            var gold=Material("Archive_AntiqueGold",new Color(.65f,.43f,.15f),.72f,.42f,new Color(.055f,.027f,.006f));
            var rune=Material("Archive_AmethystSeal",new Color(.5f,.22f,.85f),.35f,.55f,new Color(.45f,.10f,1.0f)*1.8f);
            var pale=Material("Archive_PaleGoldRunes",new Color(.95f,.73f,.34f),.45f,.5f,new Color(.8f,.4f,.10f)*.65f);
            var frameMat=Material("Archive_FrameStone",new Color(.11f,.13f,.14f),.25f,.35f,Color.black);
            Transform[] hinges=new Transform[2];
            for(int side=-1;side<=1;side+=2)
            {
                string label=side<0?"Left":"Right";
                var h=new GameObject(label+"Hinge");h.transform.SetParent(root.transform,false);h.transform.localPosition=new Vector3(side*3.03f,0,0);hinges[side<0?0:1]=h.transform;
                // Author leaves around the full portal origin, then offset beneath their hinges.
                var leaf=new GameObject(label+"ArcaneLeaf");leaf.transform.SetParent(h.transform,false);leaf.transform.localPosition=new Vector3(-side*3.03f,0,0);
                float x=side*1.515f;
                var body=new Geometry();body.Box(new Vector3(x,3.02f,0),new Vector3(3.09f,6.08f,.34f));
                var bodyGo=body.Save(label+"_SolidDoor",leaf.transform,dark);var collider=bodyGo.AddComponent<BoxCollider>();collider.center=new Vector3(x,3.02f,0);collider.size=new Vector3(3.09f,6.08f,.34f);
                var panels=new Geometry();var metal=new Geometry();var magic=new Geometry();var detail=new Geometry();
                foreach(int face in new[]{-1,1})
                {
                    float z=face*.20f;float mz=face*.25f;
                    panels.Box(new Vector3(x,3.04f,z),new Vector3(2.66f,5.62f,.06f));
                    // Massive raised stile, rail and pointed-arch inset framing.
                    foreach(float edge in new[]{side*.16f,side*2.88f})metal.Box(new Vector3(edge,3.03f,mz),new Vector3(.085f,5.9f,.11f));
                    foreach(float y in new[]{.2f,5.88f})metal.Box(new Vector3(x,y,mz),new Vector3(2.86f,.105f,.11f));
                    var arch=new[]{new Vector2(side*.32f,.48f),new Vector2(side*2.7f,.48f),new Vector2(side*2.7f,4.85f),new Vector2(x,5.62f),new Vector2(side*.32f,4.85f),new Vector2(side*.32f,.48f)};
                    for(int j=0;j<arch.Length-1;j++)metal.Stroke(arch[j],arch[j+1],mz,.045f);
                    // A single seal splits precisely across the two leaves and travels with them.
                    float start=side<0?90:-90,end=side<0?270:90;var center=new Vector2(0,3.1f);
                    metal.Arc(center,1.74f,start,end,mz,.055f);metal.Arc(center,1.42f,start,end,mz,.035f);
                    magic.Arc(center,1.61f,start,end,face*.29f,.024f);
                    detail.Arc(center,.83f,start,end,face*.29f,.025f);
                    for(int j=0;j<=24;j++)
                    {
                        float a=Mathf.Lerp(start,end,j/24f)*Mathf.Deg2Rad;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var tangent=new Vector2(-dir.y,dir.x);
                        var v=center+dir*1.88f;metal.Stroke(center+dir*1.77f,v,mz,.025f);
                        if(j%2==0){magic.Stroke(v-tangent*.045f,v+dir*.09f,face*.29f,.018f);magic.Stroke(v+dir*.09f,v+tangent*.045f,face*.29f,.018f);}
                    }
                    // Radial points and the central vertical slit evoke a sealed astrolabe.
                    for(int j=0;j<8;j++)
                    {
                        float a=(j*45+22.5f)*Mathf.Deg2Rad;float dx=Mathf.Cos(a);if(dx*side<0)continue;
                        Vector2 tip=center+new Vector2(dx,Mathf.Sin(a))*1.34f;
                        Vector2 low=center+new Vector2(Mathf.Cos(a-.22f),Mathf.Sin(a-.22f))*.47f;
                        Vector2 high=center+new Vector2(Mathf.Cos(a+.22f),Mathf.Sin(a+.22f))*.47f;
                        detail.Stroke(low,tip,face*.3f,.035f);detail.Stroke(tip,high,face*.3f,.035f);
                    }
                    metal.Diamond(new Vector2(x,1f),.30f,.40f,mz,.035f);magic.Diamond(new Vector2(x,1f),.13f,.23f,face*.29f,.025f);
                    metal.Diamond(new Vector2(x,5.15f),.22f,.29f,mz,.035f);magic.Stroke(new Vector2(x,4.98f),new Vector2(x,5.33f),face*.29f,.028f);
                    // Gold corner filigree, mirror branches, and small engraved studs.
                    foreach(float y in new[]{.73f,5.35f})
                    {
                        for(int j=0;j<3;j++)
                        {
                            float u=.48f+j*.12f;metal.Stroke(new Vector2(side*u,y),new Vector2(side*(u+.19f),y+(y<2?.24f:-.24f)),mz,.028f);
                            metal.Stroke(new Vector2(side*(3.03f-u),y),new Vector2(side*(2.84f-u),y+(y<2?.24f:-.24f)),mz,.028f);
                        }
                    }
                    foreach(float y in new[]{.4f,1.5f,2.55f,3.65f,4.7f,5.65f})metal.Diamond(new Vector2(side*2.88f,y),.07f,.09f,mz+.015f*face,.026f);
                    // Knocker-like gold handle stays separate from the central seal.
                    metal.Arc(new Vector2(side*.46f,2.02f),.12f,0,360,face*.36f,.045f);
                }
                // Overlapping astragal closes the sightline along the central join from both sides.
                if(side<0)foreach(int face in new[]{-1,1})metal.Box(new Vector3(0,3.02f,face*.24f),new Vector3(.12f,6.08f,.10f));
                panels.Save(label+"_EnamelPanels",leaf.transform,inset);metal.Save(label+"_GoldArchitecture",leaf.transform,gold);magic.Save(label+"_AmethystInlay",leaf.transform,rune);detail.Save(label+"_SealStar",leaf.transform,pale);
            }
            var frame=new GameObject("GrandEntranceFrame");frame.transform.SetParent(root.transform,false);
            var stone=new Geometry();var frameGold=new Geometry();var frameRunes=new Geometry();
            foreach(int side in new[]{-1,1})
            {
                stone.Box(new Vector3(side*3.34f,3.15f,0),new Vector3(.58f,6.3f,.65f));
                stone.Box(new Vector3(side*3.34f,.27f,0),new Vector3(.78f,.54f,.82f));
                stone.Box(new Vector3(side*3.34f,6.13f,0),new Vector3(.8f,.40f,.82f));
                foreach(int face in new[]{-1,1})
                {
                    float z=face*.43f;
                    foreach(float dx in new[]{-.19f,.19f})frameGold.Box(new Vector3(side*3.34f+dx,3.15f,z),new Vector3(.05f,5.7f,.055f));
                    foreach(float y in new[]{.56f,1.5f,3f,4.5f,5.75f})frameGold.Box(new Vector3(side*3.34f,y,z),new Vector3(.52f,.065f,.06f));
                    frameGold.Diamond(new Vector2(side*3.34f,3.75f),.16f,.53f,z,.05f);
                    frameRunes.Diamond(new Vector2(side*3.34f,3.75f),.075f,.32f,z+face*.035f,.035f);
                }
            }
            stone.Box(new Vector3(0,6.30f,0),new Vector3(7.35f,.46f,.78f));
            stone.Box(new Vector3(0,6.57f,0),new Vector3(7.7f,.16f,.86f));
            foreach(int face in new[]{-1,1})
            {
                float z=face*.46f;
                frameGold.Box(new Vector3(0,6.13f,z),new Vector3(7.35f,.06f,.05f));
                frameGold.Box(new Vector3(0,6.48f,z),new Vector3(7.5f,.065f,.05f));
                frameGold.Diamond(new Vector2(0,6.3f),.57f,.27f,z,.04f);
                frameRunes.Diamond(new Vector2(0,6.3f),.24f,.15f,z+face*.03f,.035f);
                for(int j=-5;j<=5;j++){if(j==0)continue;frameGold.Diamond(new Vector2(j*.57f,6.3f),.045f,.075f,z,.02f);}
            }
            var frameGo=stone.Save("PortalStonework",frame.transform,frameMat);var frameCollider=frameGo.AddComponent<MeshCollider>();frameCollider.sharedMesh=frameGo.GetComponent<MeshFilter>().sharedMesh;
            frameGold.Save("PortalGoldCornice",frame.transform,gold);frameRunes.Save("PortalAmethystWards",frame.transform,rune);
            var door=root.AddComponent<AutomaticEntranceDoor>();var so=new SerializedObject(door);
            so.FindProperty("player").objectReferenceValue=Object.FindFirstObjectByType<FirstPersonController>().transform;
            so.FindProperty("leftHinge").objectReferenceValue=hinges[0];so.FindProperty("rightHinge").objectReferenceValue=hinges[1];
            so.FindProperty("openingAngle").floatValue=90;so.FindProperty("approachHalfWidth").floatValue=4.2f;so.FindProperty("openDistance").floatValue=6f;so.FindProperty("closeDistance").floatValue=8f;so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Grand entrance built; widened facade meshes: "+widened;
        }
        private static int WidenOpening()
        {
            int count=0;
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer || !filter.sharedMesh)continue;
                var b=renderer.bounds;
                if(b.max.z>=2f || b.min.x>=4f || b.max.x<=-4f || b.min.y>9f || b.max.y<.2f)continue;
                if(!filter.name.StartsWith("Structure_") && !filter.name.StartsWith("Exterior_") && !filter.name.StartsWith("V3_ReferenceExterior_"))continue;
                var original=filter.sharedMesh;var vertices=original.vertices;bool changed=false;
                for(int j=0;j<vertices.Length;j++)
                {
                    Vector3 v=filter.transform.TransformPoint(vertices[j]);float a=Mathf.Abs(v.x);
                    if(a>=4 || v.y>9 || v.z>1 || v.z< -5.7f)continue;
                    float delta=a<=2f?a*.56f:(4f-a)*.56f;
                    float fade=1f-Mathf.Clamp01((v.y-7)/2);
                    v.x+=Mathf.Sign(v.x)*delta*fade;vertices[j]=filter.transform.InverseTransformPoint(v);changed|=delta*fade>.0001f;
                }
                if(!changed)continue;
                var mesh=Object.Instantiate(original);mesh.name=original.name+"_GrandPortal";mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+mesh.name+".asset"));
                Undo.RecordObject(filter,"Widen central entrance");filter.sharedMesh=mesh;
                foreach(var col in filter.GetComponents<MeshCollider>()){Undo.RecordObject(col,"Widen central collider");if(col.sharedMesh==original)col.sharedMesh=mesh;}
                count++;
            }
            return count;
        }
    }
}
