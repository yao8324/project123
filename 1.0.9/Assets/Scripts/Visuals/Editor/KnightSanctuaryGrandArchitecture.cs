using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using G=ArcaneArchive.Visuals.Editor.KnightSanctuaryGeometry;

namespace ArcaneArchive.Visuals.Editor
{
    public static partial class KnightSanctuaryBuilder
    {
        // Five vaulted bays create depth down the nave. The combat floor remains a clear circle.
        const float VaultSpring=14.1f, VaultRise=9.2f, ShellRadius=15.15f;
        static float NaveWidth(float z)=>Mathf.Sqrt(Mathf.Max(3.8f,ShellRadius*ShellRadius-z*z));
        static float VaultY(float x,float z)
        {
            float u=Mathf.Clamp01(Mathf.Abs(x)/NaveWidth(z));
            float longitudinal=VaultSpring+VaultRise*Mathf.Sqrt(Mathf.Max(0,4-(1+u)*(1+u)))/Mathf.Sqrt(3);
            return Mathf.Max(longitudinal,LunetteY(z));
        }
        static float LunetteY(float z)
        {
            if(z<=-12 || z>=12)return VaultSpring;
            float local=Mathf.Repeat(z+12,6),u=Mathf.Abs(local-3)/3;
            return VaultSpring+7*Mathf.Sqrt(Mathf.Max(0,4-(1+u)*(1+u)))/Mathf.Sqrt(3);
        }
        static Vector3 RoofPoint(float x,float z,float inset=.22f)=>new Vector3(x,VaultY(x,z)-inset,z);

        static Vector3 LeafPoint(Vector3 radial,Vector3 across,float radius,float y,float u,float v)
        {
            float height=y+.43f*u;
            float bell=Mathf.Lerp(.98f,1.27f,Mathf.InverseLerp(-.18f,.27f,height));
            float relief=.035f+.065f*Mathf.Sin(u*Mathf.PI)+.025f*(1-v*v);
            float width=.235f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.70f)*(1+.13f*Mathf.Cos(u*Mathf.PI*12));
            return radial*(radius*bell+relief)+across*(v*width)+Vector3.up*height;
        }
        static void GoldLeaf(Transform parent,Vector3 radial,Vector3 across,float radius,float y)
        {
            const int rows=24,cols=8;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int face=0;face<2;face++)for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){
                float u=row/(float)rows,v=col/(float)cols*2-1;
                vertices.Add(LeafPoint(radial,across,radius,y,u,v)-radial*(face*.018f));
                uv.Add(new Vector2(col/(float)cols*.4f,u*.43f));
            }
            int stride=(rows+1)*(cols+1);
            for(int face=0;face<2;face++)for(int row=0;row<rows;row++)for(int col=0;col<cols;col++){
                int a=face*stride+row*(cols+1)+col,b=a+cols+1;
                if(face==0)triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});else triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            for(int row=0;row<rows;row++)foreach(int col in new[]{0,cols}){
                int a=row*(cols+1)+col,b=a+cols+1;triangles.AddRange(new[]{a,b,a+stride,b,b+stride,a+stride});
            }
            var mesh=new Mesh{name="Sculpted chased gold acanthus leaf"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();
            G.Place("Solid antique-gold acanthus leaf",parent,Vector3.zero,mesh,goldRelief);
            for(int side=-1;side<=1;side+=2)for(int branch=0;branch<3;branch++){
                float u=.23f+branch*.19f;var points=new Vector3[7];
                for(int j=0;j<points.Length;j++){float q=j/6f;points[j]=LeafPoint(radial,across,radius,y,u+q*.13f,side*q*.85f)+radial*.008f;}
                G.Tube("Chased gold lateral leaf vein",parent,points,.008f,goldEdge,5);
            }
        }
        static void LeafCrown(Transform parent,Vector3 p,float radius)
        {
            var t=G.Node("Double acanthus capital",parent,p).transform;
            G.Lathe("Carved bell and abacus",t,Vector3.zero,new[]{
                new Vector2(radius*.84f,-.35f),new Vector2(radius*.88f,-.23f),
                new Vector2(radius*.98f,-.18f),new Vector2(radius*1.20f,.18f),
                new Vector2(radius*1.27f,.27f),new Vector2(radius*1.27f,.38f),
                new Vector2(radius*1.12f,.43f)},goldRelief,48);
            foreach(float h in new[]{-.28f,.25f,.42f}){
                float r=radius*(h<0?.9f:1.25f);
                G.Lathe("Broad chased golden capital band",t,new Vector3(0,h,0),new[]{new Vector2(r,-.045f),new Vector2(r+.025f,-.025f),new Vector2(r+.025f,.025f),new Vector2(r,.045f)},goldRelief,48);
                G.Ring("Polished gold collar edge",t,new Vector3(0,h+.04f,0),r,.018f,goldEdge);
            }
            for(int row=0;row<2;row++)for(int n=0;n<12;n++){
                float a=(n*30+row*15)*Mathf.Deg2Rad;
                var radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                var across=new Vector3(radial.z,0,-radial.x);
                float y=-.29f+row*.25f;
                var center=radial*radius*.94f+Vector3.up*y;
                var tip=radial*radius*1.39f+Vector3.up*(y+.43f);
                GoldLeaf(t,radial,across,radius,y);
                var vein=new Vector3[13];
                for(int j=0;j<vein.Length;j++){
                    float u=j/12f;vein[j]=LeafPoint(radial,across,radius,y,u,0)+radial*.009f;
                }
                G.Tube("Raised gold leaf midrib",t,vein,.012f,goldEdge,6);
            }
        }
        static Material carvedLeaf,goldRelief,goldEdge;
        static void GrandPier(Transform parent,Vector3 p)
        {
            var t=G.Node("Monumental bundled pier",parent,p*.98f).transform;
            G.Lathe("Octagonal load-bearing plinth",t,Vector3.zero,new[]{new Vector2(1.01f,0),new Vector2(1.01f,.18f),new Vector2(.92f,.23f),new Vector2(.92f,.43f),new Vector2(.78f,.55f),new Vector2(.76f,.85f)},ivory,8);
            Shaft(t,0,0,14.08f,.41f,false);
            for(int n=0;n<8;n++){
                float a=n*Mathf.PI/4;float x=Mathf.Sin(a)*.53f,z=Mathf.Cos(a)*.53f;
                Shaft(t,x,z,13.95f,.145f,false);
                G.Ring("Shaft socket collar",t,new Vector3(x,1.05f,z),.17f,.035f,brass);
            }
            LeafCrown(t,new Vector3(0,13.95f,0),.76f);
            LeafCrown(t,new Vector3(0,8.8f,0),.68f);
            foreach(float h in new[]{1.0f,8.4f,9.3f,13.5f}){
                G.Ring("Pier layered stone collar",t,new Vector3(0,h,0),.76f,.095f,ivory);
                G.Ring("Pier recessed gold collar",t,new Vector3(0,h+.15f,0),.72f,.028f,brass);
            }
            G.Combine(t,"GrandPier_"+p.x.ToString("F2")+"_"+p.z.ToString("F2"));
        }
        static void DeepRib(Transform parent,string label,Vector3[] path,float radius)
        {
            G.Tube(label+" load-bearing stone",parent,path,radius,ivory,10);
            G.Tube(label+" recessed inner roll",parent,path.Select(v=>v-Vector3.up*radius*.78f).ToArray(),radius*.54f,ivory,8);
            G.Tube(label+" narrow gold fillet",parent,path.Select(v=>v-Vector3.up*radius*1.48f).ToArray(),Mathf.Max(.02f,radius*.14f),goldEdge,8);
        }
        static void CeilingBoss(Transform parent,Vector3 p,float scale)
        {
            var t=G.Node("Carved floral vault keystone",parent,p).transform;t.localScale=Vector3.one*scale;
            G.Lathe("Pendant keystone",t,Vector3.zero,new[]{new Vector2(.16f,-.48f),new Vector2(.30f,-.32f),new Vector2(.55f,-.15f),new Vector2(.64f,0),new Vector2(.55f,.20f)},ivory,16);
            G.Ring("Boss gold corona",t,new Vector3(0,-.15f,0),.52f,.045f,brass);
            for(int n=0;n<8;n++){
                float a=n*Mathf.PI/4;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var s=new Vector3(d.z,0,-d.x);
                G.Tube("Eight-petal carved boss",t,new[]{d*.22f-Vector3.up*.3f,d*.57f+s*.16f-Vector3.up*.20f,d*.75f,d*.57f-s*.16f-Vector3.up*.20f,d*.22f-Vector3.up*.3f},.075f,ivory,8);
            }
        }
        static void GrandVault(Transform parent)
        {
            var t=G.Node("Five-bay pointed crossing vault",parent).transform;
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int rows=96,cols=64;
            for(int r=0;r<=rows;r++){
                float z=Mathf.Lerp(-15,15,r/(float)rows),width=NaveWidth(z);
                for(int c=0;c<=cols;c++){float x=Mathf.Lerp(-width,width,c/(float)cols);v.Add(RoofPoint(x,z,-.16f));uv.Add(new Vector2(x*.6f,z*.6f));}
            }
            for(int r=0;r<rows;r++)for(int c=0;c<cols;c++){int a=r*(cols+1)+c;triangles.AddRange(new[]{a,a+1,a+cols+1,a+1,a+cols+2,a+cols+1});}
            var shell=new Mesh{name="Pointed stone vault webs"};shell.SetVertices(v);shell.SetTriangles(triangles,0);shell.SetUVs(0,uv);shell.RecalculateNormals();shell.RecalculateTangents();
            G.Place("Deep masonry vault webs",t,Vector3.zero,shell,stone);
            // Principal transverse arches have blockwork, two rolls and a deep intrados.
            foreach(float z in new[]{-12f,-6f,0f,6f,12f}){
                float width=NaveWidth(z);
                var path=G.ArchPath(width,VaultSpring,VaultRise,z,96);
                DeepRib(t,"Principal transverse pointed arch",path.Select(q=>q-Vector3.up*.25f).ToArray(),.34f);
                Voussoirs(t,width,VaultSpring+.08f,VaultRise,z,.50f);
                CeilingBoss(t,new Vector3(0,VaultSpring+VaultRise-.45f,z),1.1f);
                GrandPier(parent,new Vector3(-width,0,z));GrandPier(parent,new Vector3(width,0,z));
            }
            // Diagonal groins meet at each bay centre. Secondary ribs form diamonds and nested stars.
            for(int bay=0;bay<4;bay++){
                float z0=-12+bay*6,z1=z0+6;
                foreach(int side in new[]{-1,1}){
                    var diagonal=new Vector3[97];
                    for(int j=0;j<diagonal.Length;j++){
                        float u=j/96f,z=Mathf.Lerp(z0,z1,u),x=side*Mathf.Lerp(-NaveWidth(z0),NaveWidth(z1),u);
                        diagonal[j]=RoofPoint(Mathf.Clamp(x,-NaveWidth(z),NaveWidth(z)),z,.28f);
                    }
                    DeepRib(t,"Diagonal crossing groin",diagonal,.23f);
                    // A secondary branch springs from each pier into the opposite quarter of the ceiling.
                    foreach(float end in new[]{.33f,.67f}){
                        var branch=new Vector3[49];
                        for(int j=0;j<branch.Length;j++){
                            float u=j/48f,z=Mathf.Lerp(z0,z0+6*end,u),x=side*NaveWidth(z)*(1-u);
                            branch[j]=RoofPoint(x,z,.27f);
                        }
                        DeepRib(t,"Secondary tierceron",branch,.115f);
                    }
                }
                CeilingBoss(t,RoofPoint(0,(z0+z1)*.5f,.4f),.85f);
                foreach(int side in new[]{-1,1}){
                    var points=new[]{RoofPoint(0,z0+1.8f,.35f),RoofPoint(side*NaveWidth(z0+3)*.35f,z0+3,.35f),RoofPoint(0,z0+4.2f,.35f)};
                    DeepRib(t,"Diamond lierne link",points,.095f);
                }
            }
            // Real side lunettes meet the longitudinal vault along these groin curves.
            for(int bay=0;bay<4;bay++)foreach(int side in new[]{-1,1}){
                var seam=new Vector3[65];
                for(int j=0;j<seam.Length;j++){
                    float z=-12+bay*6+j/64f*6;
                    float q=(LunetteY(z)-VaultSpring)/VaultRise;
                    float x=side*NaveWidth(z)*(Mathf.Sqrt(Mathf.Max(0,4-3*q*q))-1);
                    seam[j]=RoofPoint(x,z,.30f);
                }
                DeepRib(t,"Lunette groin seam",seam,.19f);
            }
            DeepRib(t,"Longitudinal crown ridge",new[]{RoofPoint(0,-15,.3f),RoofPoint(0,15,.3f)},.16f);
            // Fine masonry courses make the large webs read as built stone, not a smooth tent.
            for(float z=-14.4f;z<14.5f;z+=2.4f){
                float w=NaveWidth(z);var path=new Vector3[65];
                for(int j=0;j<path.Length;j++){float x=Mathf.Lerp(-w,w,j/64f);path[j]=RoofPoint(x,z,-.10f);}
                G.Tube("Vault masonry bed joint",t,path,.022f,stone,6);
            }
            foreach(int side in new[]{-1,1}){
                var capRoot=G.Node("Vault end masonry",t,new Vector3(0,0,side*15)).transform;
                if(side<0)capRoot.localRotation=Quaternion.Euler(0,180,0);
                float w=NaveWidth(15);var boundary=G.ArchPath(w,VaultSpring,VaultRise,0,64).ToList();
                boundary.Add(new Vector3(w,9,0));boundary.Add(new Vector3(-w,9,0));
                G.Fan("Pointed end wall",capRoot,boundary.ToArray(),stone);
                Quatrefoil(capRoot,new Vector3(0,20.7f,-.05f),.65f);
            }
            G.Combine(t,"GrandVault");
        }
        static void Quatrefoil(Transform t,Vector3 p,float r)
        {
            for(int k=0;k<4;k++){
                float a=k*Mathf.PI*.5f;
                G.Ring("Carved quatrefoil lobe",t,p+new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*r*.55f,r*.53f,.028f,ivory,true);
            }
        }
        static void UpperGallery(Transform parent)
        {
            var source=new GameObject("Deep upper gallery and clerestory");var t=source.transform;
            Box("Upper masonry backing",t,0,11.9f,.65f,4.0f,5.0f,1.0f,stone);
            // A real projecting gallery has a dark rear wall, front columns, balustrade and a stepped cornice.
            Box("Gallery shadow recess",t,0,10.8f,.11f,3.15f,2.25f,.08f,stoneDark);
            for(int k=0;k<4;k++)Box("Layered gallery corbel ledge",t,0,9.45f+k*.13f,-.25f-k*.06f,4.05f,.13f,.75f+k*.19f,ivory,.025f);
            for(int k=-1;k<=1;k++){
                float x=k*1.08f;
                var niche=G.Node("Deep triforium lancet",t,new Vector3(x,0,0)).transform;
                G.Arch("Gallery pointed arch",niche,.47f,11.0f,.64f,.075f,-.58f,ivory);
                foreach(int side in new[]{-1,1}){
                    var col=G.Node("Gallery colonette",niche,new Vector3(side*.47f,9.9f,-.58f)).transform;
                    G.Lathe("Turned gallery colonette",col,Vector3.zero,new[]{new Vector2(.075f,0),new Vector2(.055f,.10f),new Vector2(.055f,1.05f),new Vector2(.085f,1.12f)},ivory,12);
                }
                Quatrefoil(t,new Vector3(x,10.06f,-.80f),.22f);
                Box("Gallery parapet sill",t,x,9.82f,-.8f,1.08f,.09f,.2f,ivory,.02f);
            }
            for(int k=0;k<3;k++)Box("Clerestory string course",t,0,11.85f+k*.11f,-.35f,4.05f,.10f,.65f+k*.12f,ivory);
            G.Ring("Upper wall recessed gold emblem",t,new Vector3(0,13.05f,-.15f),.53f,.038f,brass,true,25,280);
            Box("Wall emblem finial",t,0,13.92f,-.15f,.055f,.7f,.06f,brass,.01f);
            for(int n=-8;n<=8;n++){
                Box("Carved cornice dentil",t,n*.235f,14.32f,-.30f,.13f,.22f,.35f,ivory,.018f);
                if(n%2==0)Quatrefoil(t,new Vector3(n*.235f,11.98f,-.83f),.10f);
            }
            Box("Vault springing cornice",t,0,14.5f,-.1f,4.05f,.23f,1.0f,ivory,.04f);
            G.Combine(t,"UpperGallery");
            var prefab=PrefabUtility.SaveAsPrefabAsset(source,Root+"/Modules/UpperGallery.prefab");UnityEngine.Object.DestroyImmediate(source);
            for(int i=0;i<24;i++){
                if(i==0||i==1||i==23||i==11||i==12||i==13)continue;
                float a=i*15*Mathf.Deg2Rad;var bay=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
                bay.name="Upper gallery bay "+i.ToString("D2");bay.transform.localPosition=new Vector3(Mathf.Sin(a)*15.05f,0,Mathf.Cos(a)*15.05f);bay.transform.localRotation=Quaternion.Euler(0,i*15,0);
            }
        }
        static void HighLunetteWindows(Transform parent)
        {
            var t=G.Node("Eight monumental clerestory lunettes",parent).transform;
            for(int bay=0;bay<4;bay++)foreach(int side in new[]{-1,1}){
                float start=-12+bay*6;
                var vertices=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();var arch=new Vector3[65];
                for(int j=0;j<=64;j++){
                    float z=start+j/64f*6,x=side*(NaveWidth(z)+.10f),top=LunetteY(z)+.16f;
                    vertices.Add(new Vector3(x,VaultSpring-.15f,z));vertices.Add(new Vector3(x,top,z));
                    uv.Add(new Vector2(z*.5f,0));uv.Add(new Vector2(z*.5f,(top-VaultSpring)*.5f));
                    arch[j]=new Vector3(x-side*.20f,top-.12f,z);
                    if(j<64){int a=j*2;if(side>0)tris.AddRange(new[]{a,a+3,a+1,a,a+2,a+3});else tris.AddRange(new[]{a,a+1,a+3,a,a+3,a+2});}
                }
                var mesh=new Mesh{name="Curved pointed clerestory glazing"};mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();
                G.Place("Monumental opal window",t,Vector3.zero,mesh,glass);
                DeepRib(t,"High window carved hood",arch,.24f);
                for(int n=-3;n<=3;n++){
                    float z=start+3+n*.72f,x=side*(NaveWidth(z)-.12f),height=LunetteY(z)-.2f;
                    G.Tube("Tall clerestory stone mullion",t,new[]{new Vector3(x,VaultSpring,z),new Vector3(x,height,z)},n%2==0?.075f:.035f,n%2==0?ivory:brass,8);
                }
                for(int row=1;row<=8;row++){
                    float y=VaultSpring+row*.65f;var pts=new List<Vector3>();
                    for(int j=0;j<=48;j++){
                        float z=start+j/48f*6;if(LunetteY(z)>y+.10f)pts.Add(new Vector3(side*(NaveWidth(z)-.14f),y,z));
                    }
                    if(pts.Count>1)G.Tube("High window horizontal lead",t,pts.ToArray(),.018f,brass,6);
                }
                var rose=G.Node("Clerestory tracery rose",t,new Vector3(side*(NaveWidth(start+3)-.3f),18.8f,start+3)).transform;
                rose.localRotation=Quaternion.Euler(0,side*90,0);
                G.Ring("Window rose stone frame",rose,Vector3.zero,.86f,.07f,ivory,true);
                Quatrefoil(rose,Vector3.zero,.63f);
            }
            G.Combine(t,"HighLunettes");
        }
        static void GrandPortal(Transform parent,bool south)
        {
            var old=parent.Find(south?"South entrance threshold":"North sealing gate");
            old.localScale=new Vector3(1.65f,1.55f,1);
            var t=G.Node(south?"South monumental portal crown":"North monumental portal crown",parent,new Vector3(0,0,south?-14.1f:14.1f)).transform;
            if(south)t.localRotation=Quaternion.Euler(0,180,0);
            for(int layer=0;layer<3;layer++){
                float w=7.5f+layer*.41f;
                var path=G.ArchPath(w,10.25f,6.8f+layer*.25f,-.65f+layer*.25f,96);
                DeepRib(t,"Monumental portal archivolt",path,.22f);
                Voussoirs(t,w,10.25f,6.8f+layer*.25f,-.46f+layer*.25f,.38f);
            }
            // Crocket leaves grow from the outer hood; four-leaf rosettes fill its spandrels.
            var rim=G.ArchPath(8.35f,10.25f,7.4f,-.45f,36);
            for(int n=1;n<rim.Length-1;n+=2){
                var q=rim[n];var stem=q+new Vector3(Mathf.Sign(q.x)*.15f,.28f,-.05f);
                G.Tube("Portal crocketed leaf",t,new[]{q,stem,stem+new Vector3(-Mathf.Sign(q.x)*.12f,.14f,-.03f)},.075f,ivory,8);
            }
            foreach(int side in new[]{-1,1}){
                var q=G.Node("Portal clustered buttress",t,new Vector3(side*8.3f,0,-.05f)).transform;
                Shaft(q,0,0,14.0f,.4f);Shaft(q,side*.43f,-.16f,11.4f,.18f);Shaft(q,-side*.38f,-.18f,11.4f,.15f);
                LeafCrown(q,new Vector3(0,13.85f,0),.60f);
                for(int level=0;level<3;level++)Quatrefoil(t,new Vector3(side*7.2f,11.6f+level*.8f,-.70f),.25f);
            }
            G.Combine(t,south?"GrandSouthPortal":"GrandNorthPortal");
        }
        static void GrandArchitecture(Transform parent)
        {
            carvedLeaf=Surface("17_Carved_Leaf_Stone",Color.white,"Marble",0,.35f,true);
            goldRelief=Surface("18_Antique_Gold_Relief",Color.white,"Brass",.93f,.57f,true);
            goldEdge=Surface("19_Polished_Gold_Edges",new Color(1.12f,1.09f,1.02f),"Brass",.95f,.65f);
            UpperGallery(parent);GrandVault(parent);HighLunetteWindows(parent);GrandPortal(parent,false);GrandPortal(parent,true);
        }
    }
}
