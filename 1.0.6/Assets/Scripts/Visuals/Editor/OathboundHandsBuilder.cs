using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>
    /// Original closed-grip gauntlets, authored in metres around the weapon grip.
    /// No MageHand mesh, bind pose or baked wand geometry is used. The two sides
    /// have separate vertex data and outward winding; every transform has scale +1.
    /// </summary>
    public static class OathboundHandsBuilder
    {
        const string Root = "Assets/Art/PlayerSwordShield";
        const string SourceRoot = "Blender/PlayerSwordShield/HandsV2";
        public static readonly Vector3 RightElbowBind = new Vector3(.20f,-.48f,-.52f);
        public struct Hands { public GameObject left, right; }
        enum Surface { Leather, Palm, Steel, Gold, Thread, Seal }

        public static Hands BuildPrefabs()
        {
            foreach (string folder in new[] { Root + "/Models", Root + "/Materials", Root + "/Prefabs", SourceRoot })
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var materials = Materials();
            var geometry = new Sculpt();
            BuildArmAndPalm(geometry);
            BuildFingers(geometry);
            BuildArmour(geometry);
            Mesh right = geometry.ToMesh("Oathbound_RightHand", false);
            Mesh left = geometry.ToMesh("Oathbound_LeftHand", true);
            ExportObj(right, "Oathbound_RightHand", materials);
            ExportObj(left, "Oathbound_LeftHand", materials);
            right = SaveMesh(right); left = SaveMesh(left);
            var result = new Hands { left = Prefab("Oathbound_LeftHand", left, materials), right = Prefab("Oathbound_RightHand", right, materials) };
            AssetDatabase.SaveAssets();
            return result;
        }

        static void BuildArmAndPalm(Sculpt g)
        {
            // One continuous oval loft from the cut-off hidden under the screen,
            // through the tapered forearm and wrist, into the metacarpal mass.
            g.Tube(new[] {
                V(.20f,-.48f,-.52f), V(.18f,-.43f,-.46f),
                V(.145f,-.34f,-.36f), V(.132f,-.31f,-.31f), V(.113f,-.265f,-.249f),
                V(.093f,-.213f,-.185f), V(.075f,-.16f,-.120f), V(.059f,-.119f,-.070f),
                V(.051f,-.091f,-.042f), V(.048f,-.063f,-.020f), V(.047f,-.032f,-.010f),
                V(.047f,-.001f,-.009f), V(.049f,.027f,-.012f), V(.052f,.043f,-.015f)
            }, new[] { .076f,.071f,.065f,.063f,.058f,.052f,.043f,.035f,.031f,.032f,.034f,.035f,.032f,.021f },
               new[] { .064f,.060f,.055f,.053f,.049f,.045f,.037f,.029f,.024f,.026f,.029f,.028f,.025f,.016f }, 24, Surface.Leather);

            // Supple heel pad under the thumb, and flatter reinforced palm.
            g.Ellipsoid(V(.054f,-.052f,-.033f), V(.032f,.043f,.019f), Quaternion.Euler(0,0,-10), Surface.Palm);
            g.Ellipsoid(V(.022f,-.026f,.007f), V(.018f,.055f,.024f), Quaternion.identity, Surface.Palm);
            // Seams follow the wrist rather than the old mesh's hard sleeve cut.
            for (int side = -1; side <= 1; side += 2)
            {
                var seam = new[] { V(.12f+side*.047f,-.29f,-.29f),V(.09f+side*.042f,-.22f,-.21f),V(.066f+side*.032f,-.15f,-.14f),V(.05f+side*.026f,-.10f,-.09f) };
                g.Tube(seam, Constant(seam.Length,.0013f), Constant(seam.Length,.0013f), 6, Surface.Thread);
            }
        }

        static void BuildFingers(Sculpt g)
        {
            // Four differentiated fingers wrap the Y-axis grip, with a readable
            // knuckle, proximal, middle and distal bend. The tapered fingertip
            // turns back toward the palm; each digit is a single smooth loft.
            float[] heights = { .024f,-.003f,-.030f,-.055f };
            float[] widths = { .0135f,.0141f,.0132f,.0116f };
            for (int finger=0; finger<4; finger++)
            {
                float y=heights[finger], r=widths[finger], shorter=finger==3?.006f:0;
                var points = new[] {
                    V(.062f,y,.001f), V(.058f,y+.001f,.020f), V(.047f,y+.001f,.037f),
                    V(.028f,y,.046f-shorter), V(.008f,y-.002f,.044f-shorter),
                    V(-.008f,y-.003f,.032f-shorter), V(-.018f,y-.004f,.017f),
                    V(-.018f,y-.005f,-.001f), V(-.010f,y-.006f,-.016f), V(.003f,y-.006f,-.023f),
                    V(.012f,y-.006f,-.023f)
                };
                g.Tube(Smooth(points,3), SmoothRadii(new[] { r*.95f,r,r*1.05f,r,r*.90f,r*.94f,r*.91f,r*.85f,r*.77f,r*.66f,.0025f },3),
                    SmoothRadii(new[] { r,r,r*.93f,r*.88f,r*.88f,r*.88f,r*.84f,r*.80f,r*.72f,r*.61f,.0025f },3), 14, Surface.Leather);
                // Rounded segmented plates leave the flexible joint creases open.
                g.Plate(V(.070f,y,.019f),Quaternion.Euler(0,-67,0),.023f,r*1.65f,.006f,Surface.Steel);
                g.Plate(V(.027f,y,.052f-shorter),Quaternion.Euler(0,180,0),.027f,r*1.55f,.005f,Surface.Steel);
                // The curled distal phalanx is the camera-facing surface in the
                // left-handed idle. Give it a short fitted plate, not a bare tube.
                g.Plate(V(-.003f,y-.006f,-.030f),Quaternion.Euler(0,15,0),.026f,r*1.52f,.0045f,Surface.Gold);
                g.Plate(V(-.003f,y-.006f,-.033f),Quaternion.Euler(0,15,0),.022f,r*1.26f,.004f,Surface.Steel);
                g.Ellipsoid(V(-.024f,y-.004f,.012f),V(.0047f,r*.78f,.009f),Quaternion.identity,Surface.Palm);
                g.Tube(new[] {V(-.024f,y-r*.60f,.019f),V(-.030f,y-r*.62f,.006f),V(-.025f,y-r*.59f,-.008f)},
                    Constant(3,.00115f),Constant(3,.00115f),6,Surface.Thread);
                g.Tube(new[] {V(.051f,y-r*.72f,.030f),V(.034f,y-r*.70f,.047f),V(.017f,y-r*.68f,.050f)},
                    Constant(3,.00085f),Constant(3,.00085f),5,Surface.Thread);
            }
            // Opposing thumb comes from the thenar pad, crosses the handle and
            // closes against the upper two digits, rather than forming a sixth bar.
            var thumb = new[] { V(.074f,-.045f,-.024f), V(.082f,-.024f,-.032f), V(.079f,-.001f,-.036f),
                V(.064f,.020f,-.040f),V(.042f,.034f,-.039f),V(.020f,.038f,-.034f),V(.003f,.033f,-.024f),V(-.005f,.025f,-.013f) };
            g.Tube(Smooth(thumb,4),SmoothRadii(new[] {.022f,.021f,.019f,.0165f,.015f,.0135f,.0115f,.004f},4),
                SmoothRadii(new[] {.021f,.020f,.018f,.015f,.014f,.012f,.010f,.004f},4),16,Surface.Leather);
            g.Ellipsoid(V(.064f,.020f,-.052f),V(.018f,.012f,.0045f),Quaternion.Euler(0,0,-30),Surface.Steel);
            g.Ellipsoid(V(.021f,.039f,-.043f),V(.014f,.010f,.0042f),Quaternion.Euler(0,0,15),Surface.Steel);
        }

        static void BuildArmour(Sculpt g)
        {
            // Back-of-hand plate is softly bevelled, fitted to the palm contour;
            // old gold is restricted to edges, rivets and small heraldic strokes.
            g.Plate(V(.067f,-.014f,-.034f),Quaternion.Euler(6,-33,-5),.061f,.077f,.010f,Surface.Steel);
            g.Plate(V(.067f,-.014f,-.040f),Quaternion.Euler(6,-33,-5),.047f,.063f,.003f,Surface.Gold);
            g.Plate(V(.067f,-.014f,-.043f),Quaternion.Euler(6,-33,-5),.040f,.057f,.003f,Surface.Steel);
            g.Ellipsoid(V(.064f,-.015f,-.050f),V(.007f,.016f,.004f),Quaternion.Euler(0,-25,0),Surface.Seal);
            foreach(float y in new[]{-.039f,.011f})
                g.Ellipsoid(V(.087f,y,-.038f),V(.0038f,.0038f,.0028f),Quaternion.identity,Surface.Gold);

            // Overlapping wrist lames bridge hand and vambrace without a seam of air.
            var wristDirection=V(.01f,-.07f,-.075f).normalized;
            for(int i=0;i<3;i++)
            {
                Vector3 center=V(.055f,-.099f,-.061f)+wristDirection*(i*.018f);
                g.Band(center,wristDirection,.039f+i*.004f,.030f+i*.0035f,.015f,Surface.Steel,24);
                g.Band(center-wristDirection*.005f,wristDirection,.040f+i*.004f,.031f+i*.0035f,.0022f,Surface.Gold,24);
            }
            Quaternion forearm=Quaternion.Euler(51,0,10);
            g.Plate(V(.103f,-.197f,-.241f),forearm,.100f,.174f,.017f,Surface.Steel);
            g.Plate(V(.103f,-.190f,-.247f),forearm,.087f,.156f,.003f,Surface.Gold);
            g.Plate(V(.103f,-.187f,-.250f),forearm,.079f,.146f,.004f,Surface.Steel);
            // A subdued central inlay echoes the book/eye vocabulary of the weapons.
            for(int i=0;i<3;i++)
            {
                Vector3 c=V(.103f,-.187f,-.254f)+forearm*V(0,.029f-i*.029f,0);
                g.Tube(new[]{c+forearm*V(-.011f,.011f,-.002f),c+forearm*V(0,0,-.002f),c+forearm*V(.011f,.011f,-.002f)},Constant(3,.0017f),Constant(3,.0017f),7,Surface.Gold);
            }
            Vector3 cuff=V(.188f,-.451f,-.486f),axis=V(-.08f,.5f,.6f).normalized;
            g.Band(cuff,axis,.075f,.063f,.036f,Surface.Palm,28);
            g.Band(cuff+axis*.018f,axis,.076f,.064f,.005f,Surface.Gold,28);
            g.Band(cuff-axis*.018f,axis,.076f,.064f,.005f,Surface.Gold,28);
        }

        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static float[] Constant(int count,float value){var a=new float[count];for(int i=0;i<count;i++)a[i]=value;return a;}
        static Vector3[] Smooth(Vector3[] p,int steps)
        {
            var result=new List<Vector3>();
            for(int i=0;i<p.Length-1;i++)for(int n=0;n<steps;n++)
            {
                float t=n/(float)steps;Vector3 a=p[Mathf.Max(0,i-1)],b=p[i],c=p[i+1],d=p[Mathf.Min(p.Length-1,i+2)];
                result.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
            result.Add(p[p.Length-1]);return result.ToArray();
        }
        static float[] SmoothRadii(float[] values,int steps)
        {
            var result=new List<float>();for(int i=0;i<values.Length-1;i++)for(int n=0;n<steps;n++)result.Add(Mathf.Lerp(values[i],values[i+1],n/(float)steps));
            result.Add(values[values.Length-1]);return result.ToArray();
        }

        sealed class Sculpt
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uvs=new List<Vector2>();
            readonly List<int>[] triangles={new List<int>(),new List<int>(),new List<int>(),new List<int>(),new List<int>(),new List<int>()};
            int Add(Vector3 p,Vector2 uv){vertices.Add(p);uvs.Add(uv);return vertices.Count-1;}
            void Tri(int a,int b,int c,Surface s){triangles[(int)s].Add(a);triangles[(int)s].Add(b);triangles[(int)s].Add(c);}
            public void Tube(Vector3[] centers,float[] radiusX,float[] radiusZ,int sides,Surface material)
            {
                int start=vertices.Count;Vector3 previousRight=Vector3.right;
                for(int row=0;row<centers.Length;row++)
                {
                    Vector3 tangent=(centers[Mathf.Min(row+1,centers.Length-1)]-centers[Mathf.Max(0,row-1)]).normalized;
                    Vector3 right=Vector3.ProjectOnPlane(previousRight,tangent).normalized;
                    if(right.sqrMagnitude<.001f)right=Vector3.Cross(tangent,Vector3.forward).normalized;
                    Vector3 up=Vector3.Cross(tangent,right).normalized;previousRight=right;
                    for(int side=0;side<=sides;side++)
                    {
                        float a=side*2*Mathf.PI/sides;
                        Add(centers[row]+right*(Mathf.Cos(a)*radiusX[row])+up*(Mathf.Sin(a)*radiusZ[row]),new Vector2(side/(float)sides,row/(float)(centers.Length-1)));
                    }
                }
                for(int row=0;row<centers.Length-1;row++)for(int side=0;side<sides;side++)
                {
                    int a=start+row*(sides+1)+side,b=a+1,c=b+sides+1,d=a+sides+1;
                    Tri(a,b,c,material);Tri(a,c,d,material);
                }
                int bottom=Add(centers[0],new Vector2(.5f,.5f)),top=Add(centers[centers.Length-1],new Vector2(.5f,.5f));
                for(int side=0;side<sides;side++)
                {
                    Tri(bottom,start+side+1,start+side,material);
                    int last=start+(centers.Length-1)*(sides+1);Tri(top,last+side,last+side+1,material);
                }
            }
            public void Ellipsoid(Vector3 center,Vector3 radii,Quaternion q,Surface material)
            {
                const int rows=10,sides=16;int start=vertices.Count;
                for(int y=0;y<=rows;y++)for(int x=0;x<=sides;x++)
                {
                    float phi=Mathf.PI*y/rows,a=2*Mathf.PI*x/sides;
                    Add(center+q*Vector3.Scale(radii,V(Mathf.Sin(phi)*Mathf.Cos(a),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(a))),new Vector2(x/(float)sides,y/(float)rows));
                }
                for(int y=0;y<rows;y++)for(int x=0;x<sides;x++)
                {
                    int a=start+y*(sides+1)+x,b=a+1,c=b+sides+1,d=a+sides+1;
                    if(y>0)Tri(a,b,c,material);if(y<rows-1)Tri(a,c,d,material);
                }
            }
            public void Band(Vector3 center,Vector3 axis,float radiusX,float radiusZ,float width,Surface material,int sides)
            {
                Tube(new[]{center-axis*width*.5f,center-axis*width*.3f,center+axis*width*.3f,center+axis*width*.5f},
                    new[]{radiusX*.97f,radiusX,radiusX,radiusX*.97f},new[]{radiusZ*.97f,radiusZ,radiusZ,radiusZ*.97f},sides,material);
            }
            public void Plate(Vector3 center,Quaternion rotation,float width,float height,float depth,Surface material)
            {
                // Rounded eight-sided panel: silhouette follows a wrist/palm,
                // with three bevel rings rather than a sharp primitive cube.
                Vector2[] outline={new Vector2(-.36f,-.5f),new Vector2(.36f,-.5f),new Vector2(.5f,-.30f),new Vector2(.46f,.32f),new Vector2(.27f,.5f),new Vector2(-.27f,.5f),new Vector2(-.46f,.32f),new Vector2(-.5f,-.30f)};
                int start=vertices.Count;
                float[] sizes={.87f,1,1,.85f},zs={depth*.5f,depth*.22f,-depth*.15f,-depth*.5f};
                for(int ring=0;ring<4;ring++)foreach(Vector2 p in outline)Add(center+rotation*V(p.x*width*sizes[ring],p.y*height*sizes[ring],zs[ring]),p+Vector2.one*.5f);
                for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++)
                {int a=start+ring*8+i,b=start+ring*8+(i+1)%8,c=b+8,d=a+8;Tri(a,c,b,material);Tri(a,d,c,material);}
                int back=Add(center+rotation*V(0,0,zs[0]),Vector2.one*.5f),front=Add(center+rotation*V(0,0,zs[3]),Vector2.one*.5f);
                for(int i=0;i<8;i++){Tri(back,start+i,start+(i+1)%8,material);Tri(front,start+24+(i+1)%8,start+24+i,material);}
            }
            public Mesh ToMesh(string name,bool mirror)
            {
                var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};var points=vertices.ToArray();
                if(mirror)for(int i=0;i<points.Length;i++)points[i].x=-points[i].x;
                mesh.vertices=points;mesh.uv=uvs.ToArray();mesh.subMeshCount=triangles.Length;
                for(int i=0;i<triangles.Length;i++)
                {var indices=triangles[i].ToArray();if(mirror)for(int t=0;t<indices.Length;t+=3){int k=indices[t+1];indices[t+1]=indices[t+2];indices[t+2]=k;}mesh.SetTriangles(indices,i);}
                mesh.RecalculateNormals();
                // UV seams and sphere poles share position but not vertex index.
                // Weld only their normals so close-up lighting has no longitudinal seam.
                var normals=mesh.normals;var sums=new Dictionary<Vector3Int,Vector3>();
                for(int i=0;i<points.Length;i++)
                {
                    var key=Vector3Int.RoundToInt(points[i]*1000000f);
                    sums.TryGetValue(key,out Vector3 sum);sums[key]=sum+normals[i];
                }
                for(int i=0;i<points.Length;i++)normals[i]=sums[Vector3Int.RoundToInt(points[i]*1000000f)].normalized;
                mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();
                Vector3 wrist=V(.051f,-.091f,-.042f),elbow=RightElbowBind;
                if(mirror){wrist.x=-wrist.x;elbow.x=-elbow.x;}
                Vector3 axis=(elbow-wrist).normalized;var weights=new BoneWeight[points.Length];
                for(int i=0;i<points.Length;i++)
                {
                    float u=Mathf.Clamp01(Vector3.Dot(points[i]-wrist,axis)/.08f);
                    // Blend only within eight centimetres of the wrist; the
                    // forearm and vambrace remain rigid on the elbow bone.
                    float weight=Mathf.SmoothStep(0,1,u);
                    // This region contains every finger (including curled tips),
                    // thumb and palm heel. Projection alone must never skin them
                    // to the elbow when the hand rotates across the body.
                    if(points[i].y>-.10f && points[i].z>-.095f)weight=0;
                    weights[i]=weight<=.5f
                        ? new BoneWeight { boneIndex0=0,weight0=1-weight,boneIndex1=1,weight1=weight }
                        : new BoneWeight { boneIndex0=1,weight0=weight,boneIndex1=0,weight1=1-weight };
                }
                mesh.boneWeights=weights;mesh.bindposes=new[]{Matrix4x4.identity,Matrix4x4.Translate(-elbow)};
                return mesh;
            }
        }

        static Material[] Materials()
        {
            Color[] colors={new Color(.105f,.076f,.063f),new Color(.055f,.039f,.039f),new Color(.18f,.19f,.215f),new Color(.48f,.29f,.10f),new Color(.34f,.25f,.15f),new Color(.26f,.055f,.46f)};
            float[] metals={0,0,.82f,.8f,0,.6f},smooth={.18f,.13f,.49f,.56f,.15f,.77f};
            var result=new Material[colors.Length];
            Texture2D grain=LeatherGrain(),normal=LeatherNormal();
            for(int i=0;i<result.Length;i++)
            {
                string path=Root+"/Materials/Gauntlet_"+((Surface)i)+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                m.SetColor("_BaseColor",colors[i]);m.SetFloat("_Metallic",metals[i]);m.SetFloat("_Smoothness",smooth[i]);
                m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",colors[i]*(i==5?.7f:.055f));
                if(i<2)
                {
                    m.SetTexture("_BaseMap",grain);m.SetTextureScale("_BaseMap",new Vector2(3,4));
                    m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.48f);m.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(m);result[i]=m;
            }
            return result;
        }
        static Texture2D LeatherGrain()
        {
            string path=Root+"/Materials/Gauntlet_LeatherGrain.png";
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);var colors=new Color32[128*128];var random=new System.Random(100629);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float pores=Mathf.PerlinNoise(x*.36f,y*.36f);byte level=(byte)Mathf.Clamp(145+pores*72+(float)random.NextDouble()*20,0,255);
                colors[y*128+x]=new Color32(level,level,level,255);
            }
            texture.SetPixels32(colors);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.sRGBTexture=true;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Texture2D LeatherNormal()
        {
            const int size=128;string path=Root+"/Materials/Gauntlet_LeatherNormal.png";
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
            var heights=new float[size*size];var pixels=new Color[size*size];var random=new System.Random(301029);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)heights[y*size+x]=Mathf.PerlinNoise(x*.38f,y*.38f)*.72f+(float)random.NextDouble()*.28f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size];
                float dy=heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x];
                Vector3 n=new Vector3(-dx*1.8f,-dy*1.8f,1).normalized;pixels[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            }
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.NormalMap;importer.convertToNormalmap=false;importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Mesh SaveMesh(Mesh mesh)
        {
            string path=Root+"/Models/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!existing){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            existing.Clear();existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;existing.boneWeights=mesh.boneWeights;existing.bindposes=mesh.bindposes;existing.subMeshCount=mesh.subMeshCount;
            for(int i=0;i<mesh.subMeshCount;i++)existing.SetTriangles(mesh.GetTriangles(i),i);existing.RecalculateBounds();existing.UploadMeshData(false);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;
        }
        static GameObject Prefab(string name,Mesh mesh,Material[] materials)
        {
            var go=new GameObject(name);
            var hand=new GameObject("Hand").transform;hand.SetParent(go.transform,false);
            var elbow=new GameObject("Elbow").transform;elbow.SetParent(go.transform,false);
            Vector3 bind=RightElbowBind;if(name.Contains("Left"))bind.x=-bind.x;elbow.localPosition=bind;
            var renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterials=materials;
            renderer.bones=new[]{hand,elbow};renderer.rootBone=hand;renderer.quality=SkinQuality.Bone2;
            renderer.updateWhenOffscreen=true;renderer.localBounds=new Bounds(Vector3.zero,Vector3.one*4);
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var result=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return result;
        }
        static void ExportObj(Mesh mesh,string name,Material[] materials)
        {
            var b=new StringBuilder("# Original closed-grip gauntlet, metre units; +Y blade, -Z forearm.\nmtllib Oathbound_Hands.mtl\n");
            Func<float,string> f=n=>n.ToString("0.######",CultureInfo.InvariantCulture);
            foreach(var p in mesh.vertices)b.AppendLine("v "+f(p.x)+" "+f(p.y)+" "+f(p.z));
            foreach(var p in mesh.uv)b.AppendLine("vt "+f(p.x)+" "+f(p.y));
            foreach(var p in mesh.normals)b.AppendLine("vn "+f(p.x)+" "+f(p.y)+" "+f(p.z));
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                b.AppendLine("usemtl "+((Surface)sub));int[] indices=mesh.GetTriangles(sub);
                for(int i=0;i<indices.Length;i+=3){b.Append("f");for(int j=0;j<3;j++){int n=indices[i+j]+1;b.Append(" "+n+"/"+n+"/"+n);}b.AppendLine();}
            }
            File.WriteAllText(SourceRoot+"/"+name+".obj",b.ToString(),new UTF8Encoding(false));
            b.Clear();for(int i=0;i<materials.Length;i++){Color c=materials[i].GetColor("_BaseColor");b.AppendLine("newmtl "+((Surface)i)+"\nKd "+f(c.r)+" "+f(c.g)+" "+f(c.b)+"\nKs 0.2 0.2 0.2\nNs 40\n");}
            File.WriteAllText(SourceRoot+"/Oathbound_Hands.mtl",b.ToString(),new UTF8Encoding(false));
            File.WriteAllText(SourceRoot+"/README.md","# Oathbound gauntlets V2\n\nOriginal procedural closed-grip hand/forearm meshes; no legacy MageHand geometry. The two OBJ files are editable static shapes in Blender (OBJ does not include skin weights). Keep the stated Unity coordinates: +Y blade, -Z toward the forearm. Meshes are in metres and the weapon grip is the origin.\n\nEach hand has four curled fingers, one opposing thumb, a continuous palm/wrist/forearm volume, fitted steel plates, gold trim and leather seams. Separate left/right vertex sets use positive transforms and outward winding. Unity prefabs have a two-bone SkinnedMeshRenderer: Hand at the grip origin and Elbow at (+/-0.20,-0.48,-0.52). Palm/fingers follow Hand rigidly; the forearm gradually blends to Elbow so the sleeve can stay connected off-screen during wrist rotation. There is no per-finger skeleton.\n\nReproduction source: Assets/Scripts/Visuals/Editor/OathboundHandsBuilder.cs. BuildPrefabs() regenerates these exports and Unity mesh/prefab/material assets.\n",new UTF8Encoding(false));
        }
    }
}
