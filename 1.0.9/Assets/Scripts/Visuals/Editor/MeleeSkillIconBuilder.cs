using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEngine;

namespace ArcaneArchive.Visuals.Editor
{
    // Original vector glyphs, rasterized into portable UI sprites. No external art dependency.
    public static class MeleeSkillIconBuilder
    {
        const string Root="Assets/UI/MeleeSkills";
        const string Sources="Docs/CombatHUD20261029/IconSources";
        static readonly Color Ink=new Color(.95f,.91f,.80f),Gold=new Color(.89f,.64f,.29f);
        static readonly Color[] Accents={new Color(1,.65f,.24f),new Color(.45f,.85f,.75f),new Color(.54f,.75f,1),new Color(.77f,.52f,1)};
        [MenuItem("Arcane Archive/UI/Build melee skill icons")]
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building icons.");
            Directory.CreateDirectory(Root);Directory.CreateDirectory(Sources);AssetDatabase.Refresh();
            var data=AssetDatabase.LoadAssetAtPath<MeleeLoadout>("Assets/Art/PlayerSwordShield/OathboundLoadout.asset");
            if(!data)throw new InvalidOperationException("Sword and shield loadout is missing.");
            string[] names={"01_Oathbreaker","02_VeiledGuard","03_ShadowThrust","04_SealOrbit"};
            var sheet=new Texture2D(1024,256,TextureFormat.RGBA32,false);
            for(int i=0;i<4;i++)
            {
                var art=new Canvas256();Backdrop(art,Accents[i]);
                if(i==0)Heavy(art);else if(i==1)Shield(art);else if(i==2)Thrust(art);else Orbit(art);
                string path=Root+"/"+names[i];
                art.Save(path+".png",false);art.Save(path+"_Gray.png",true);
                File.WriteAllText(Sources+"/"+names[i]+".svg",art.Svg(),new UTF8Encoding(false));
                data.skills[i].icon=Import(path+".png");data.skills[i].cooldownIcon=Import(path+"_Gray.png");data.skills[i].iconAccent=Accents[i];
                sheet.SetPixels(i*256,0,256,256,art.Pixels);
            }
            var ring=new Canvas256();ring.Arc(128,128,118,0,360,7,Color.white);
            ring.Save(Root+"/CooldownRing.png",false);data.skillTimerRing=Import(Root+"/CooldownRing.png");
            sheet.Apply();File.WriteAllBytes("Docs/CombatHUD20261029/SkillIcons.png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();return "Four coloured and grey icon pairs, clockwise ring and editable SVG sources saved.";
        }
        static Sprite Import(string path)
        {
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.sRGBTexture=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Color Fade(Color c,float a){c.a=a;return c;}
        static void Backdrop(Canvas256 c,Color accent)
        {
            c.Disc(128,128,117,new Color(.065f,.052f,.083f));
            c.Disc(128,128,110,new Color(.10f,.078f,.12f));
            for(int i=100;i>=22;i-=3)c.Disc(128,128,i,Fade(accent,.010f));
            c.Arc(128,128,111,0,360,2,Fade(Gold,.52f));
            c.Arc(128,128,100,210,314,1.3f,Fade(accent,.45f));
            c.Arc(128,128,100,30,134,1.3f,Fade(accent,.45f));
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;Vector2 p=new Vector2(128+Mathf.Cos(a)*105,128+Mathf.Sin(a)*105);
                c.Line(2.5f,Fade(Gold,.60f),p.x-Mathf.Cos(a)*3,p.y-Mathf.Sin(a)*3,p.x+Mathf.Cos(a)*3,p.y+Mathf.Sin(a)*3);
            }
        }
        static void Heavy(Canvas256 c)
        {
            c.Arc(132,110,65,148,241,10,Fade(Accents[0],.18f));c.Arc(132,110,65,150,241,3,Accents[0]);
            c.Polygon(Ink,117,62,139,62,142,139,128,181,114,139);
            c.Polygon(Gold,117,62,127,73,128,181,114,139);
            c.Line(2,new Color(1,.98f,.91f),131,73,133,139,128,166);
            c.Polygon(Gold,91,53,105,48,119,52,137,52,151,48,165,53,153,65,136,60,120,60,103,65);
            c.Polygon(new Color(.46f,.24f,.16f),120,29,136,29,136,49,120,49);c.Line(3,Gold,120,32,136,32,120,40,136,40);
            c.Polygon(Ink,128,19,138,26,128,32,118,26);
            c.Line(6,Fade(Accents[0],.3f),82,190,106,185,113,199,130,188,140,201,153,187,176,190);
            c.Line(3,Gold,82,190,106,185,113,199,130,188,140,201,153,187,176,190);
            c.Line(3,Ink,91,165,104,177);c.Line(3,Ink,165,162,152,175);c.Line(3,Gold,128,204,128,215);
        }
        static void Shield(Canvas256 c)
        {
            c.Polygon(Gold,128,42,180,61,176,121,163,149,128,181,93,149,80,121,76,61);
            c.Polygon(Accents[1]*.65f,128,52,170,68,166,119,154,142,128,168,102,142,90,119,86,68);
            c.Polygon(new Color(.085f,.15f,.17f),128,62,159,74,157,117,147,134,128,154,109,134,99,117,97,74);
            c.Line(3,Ink,92,70,128,57,164,70);c.Line(2,Fade(Ink,.65f),99,131,128,159,157,131);
            c.Polygon(Ink,99,106,113,94,128,89,143,94,157,106,143,116,128,122,113,116);
            c.Polygon(new Color(.13f,.18f,.20f),106,106,128,95,150,106,128,117);
            c.Polygon(Accents[1],128,94,136,106,128,119,120,106);c.Disc(129,103,3,Ink);
            c.Line(4,Accents[1],56,85,43,85);c.Line(4,Accents[1],200,85,213,85);
            c.Line(3,Ink,57,116,44,125);c.Line(3,Ink,199,116,212,125);
            c.Line(3,Gold,70,149,61,163);c.Line(3,Gold,186,149,195,163);
            c.Arc(128,124,76,43,137,3,Fade(Accents[1],.65f));
        }
        static void Thrust(Canvas256 c)
        {
            c.Line(5,Fade(Accents[2],.45f),48,154,94,108);c.Line(3,Accents[2],48,133,75,106);
            c.Line(4,Accents[2],99,204,144,158);c.Line(3,Fade(Ink,.7f),126,199,158,167);
            c.Polygon(Ink,110,126,149,69,188,43,165,85,126,142);
            c.Polygon(Accents[2],110,126,149,69,188,43,122,132);
            c.Line(2,Color.white,130,126,174,61);
            c.Polygon(Gold,92,118,103,121,137,155,139,167,129,163,95,129);
            c.Polygon(new Color(.32f,.22f,.30f),99,139,115,154,87,181,72,165);
            c.Line(3,Gold,82,157,99,171);c.Line(3,Gold,89,150,106,164);
            c.Polygon(Ink,69,166,87,183,77,192,59,175);
            c.Line(4,Accents[2],168,109,191,86,201,78,198,99);
            c.Line(2,Ink,155,51,166,40,179,37);
        }
        static void Orbit(Canvas256 c)
        {
            Crescent(c,128,128,70,54,-155,43,Accents[3]);
            Crescent(c,128,128,70,54,25,223,Gold);
            c.Arc(128,128,84,-158,28,2.5f,Fade(Accents[3],.75f));
            c.Arc(128,128,84,22,208,2.5f,Fade(Gold,.75f));
            c.Polygon(Ink,128,102,148,128,128,154,108,128);
            c.Polygon(Accents[3],128,111,140,128,128,145,116,128);
            c.Line(2,Gold,88,128,98,128);c.Line(2,Gold,158,128,168,128);
            c.Line(2,Ink,128,87,128,96);c.Line(2,Ink,128,160,128,169);
            c.Polygon(Ink,189,57,193,67,203,71,193,75,189,85,185,75,175,71,185,67);
            c.Polygon(Gold,61,174,64,181,71,184,64,187,61,194,58,187,51,184,58,181);
        }
        static void Crescent(Canvas256 c,float cx,float cy,float outer,float inner,float from,float to,Color color)
        {
            var p=new List<float>();
            for(int i=0;i<=26;i++){float a=Mathf.Lerp(from,to,i/26f)*Mathf.Deg2Rad;float r=Mathf.Lerp(inner,outer,Mathf.Sin(i/26f*Mathf.PI*.5f));p.Add(cx+Mathf.Cos(a)*r);p.Add(cy+Mathf.Sin(a)*r);}
            float end=(to+15)*Mathf.Deg2Rad;p.Add(cx+Mathf.Cos(end)*(outer+8));p.Add(cy+Mathf.Sin(end)*(outer+8));
            for(int i=26;i>=0;i--){float a=Mathf.Lerp(from,to,i/26f)*Mathf.Deg2Rad;p.Add(cx+Mathf.Cos(a)*inner);p.Add(cy+Mathf.Sin(a)*inner);}
            c.Polygon(color,p.ToArray());c.Arc(cx,cy,outer-2,from+64,to-5,2,Ink);
        }
        sealed class Canvas256
        {
            public readonly Color[] Pixels=new Color[256*256];
            readonly StringBuilder svg=new StringBuilder();
            static string F(float n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
            static string Hex(Color c)=>"#"+ColorUtility.ToHtmlStringRGB(c);
            void Blend(int x,int y,Color c,float coverage)
            {
                if(x<0 || y<0 || x>=256 || y>=256)return;
                int index=(255-y)*256+x;Color old=Pixels[index];float alpha=Mathf.Clamp01(c.a*coverage),sum=alpha+old.a*(1-alpha);
                if(sum<=0)return;
                Pixels[index]=new Color((c.r*alpha+old.r*old.a*(1-alpha))/sum,(c.g*alpha+old.g*old.a*(1-alpha))/sum,(c.b*alpha+old.b*old.a*(1-alpha))/sum,sum);
            }
            public void Disc(float cx,float cy,float radius,Color color)
            {
                for(int y=Mathf.Max(0,Mathf.FloorToInt(cy-radius-1));y<=Mathf.Min(255,Mathf.CeilToInt(cy+radius+1));y++)
                    for(int x=Mathf.Max(0,Mathf.FloorToInt(cx-radius-1));x<=Mathf.Min(255,Mathf.CeilToInt(cx+radius+1));x++)
                        Blend(x,y,color,Mathf.Clamp01(radius+.5f-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(cx,cy))));
                svg.AppendLine("<circle cx='"+F(cx)+"' cy='"+F(cy)+"' r='"+F(radius)+"' fill='"+Hex(color)+"' opacity='"+F(color.a)+"'/>");
            }
            public void Line(float width,Color color,params float[] points)
            {
                var vectors=new Vector2[points.Length/2];for(int i=0;i<vectors.Length;i++)vectors[i]=new Vector2(points[i*2],points[i*2+1]);
                float minX=256,minY=256,maxX=0,maxY=0;foreach(var p in vectors){minX=Mathf.Min(minX,p.x);minY=Mathf.Min(minY,p.y);maxX=Mathf.Max(maxX,p.x);maxY=Mathf.Max(maxY,p.y);}
                float half=width*.5f;
                for(int y=Mathf.Max(0,Mathf.FloorToInt(minY-half-1));y<=Mathf.Min(255,Mathf.CeilToInt(maxY+half+1));y++)
                    for(int x=Mathf.Max(0,Mathf.FloorToInt(minX-half-1));x<=Mathf.Min(255,Mathf.CeilToInt(maxX+half+1));x++)
                    {
                        var sample=new Vector2(x+.5f,y+.5f);float distance=999;
                        for(int i=1;i<vectors.Length;i++){Vector2 a=vectors[i-1],delta=vectors[i]-a;float t=Mathf.Clamp01(Vector2.Dot(sample-a,delta)/Mathf.Max(.001f,delta.sqrMagnitude));distance=Mathf.Min(distance,Vector2.Distance(sample,a+delta*t));}
                        Blend(x,y,color,Mathf.Clamp01(half+.5f-distance));
                    }
                svg.Append("<polyline fill='none' stroke='"+Hex(color)+"' opacity='"+F(color.a)+"' stroke-width='"+F(width)+"' stroke-linecap='round' stroke-linejoin='round' points='");
                foreach(var p in vectors)svg.Append(F(p.x)+","+F(p.y)+" ");svg.AppendLine("'/>");
            }
            public void Arc(float cx,float cy,float radius,float from,float to,float width,Color color)
            {
                int count=Mathf.CeilToInt(Mathf.Abs(to-from)/5)+1;var points=new float[count*2];
                for(int i=0;i<count;i++){float a=Mathf.Lerp(from,to,i/(float)(count-1))*Mathf.Deg2Rad;points[i*2]=cx+Mathf.Cos(a)*radius;points[i*2+1]=cy+Mathf.Sin(a)*radius;}Line(width,color,points);
            }
            public void Polygon(Color color,params float[] points)
            {
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    int inside=0;for(int sample=0;sample<4;sample++)
                    {float px=x+(sample%2==0?.25f:.75f),py=y+(sample<2?.25f:.75f);bool hit=false;
                        for(int i=0,j=points.Length-2;i<points.Length;j=i,i+=2)
                            if((points[i+1]>py)!=(points[j+1]>py) && px<(points[j]-points[i])*(py-points[i+1])/(points[j+1]-points[i+1])+points[i])hit=!hit;
                        if(hit)inside++;
                    }
                    if(inside>0)Blend(x,y,color,inside*.25f);
                }
                svg.Append("<polygon fill='"+Hex(color)+"' opacity='"+F(color.a)+"' points='");for(int i=0;i<points.Length;i+=2)svg.Append(F(points[i])+","+F(points[i+1])+" ");svg.AppendLine("'/>");
            }
            public string Svg()=>"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 256 256'>\n"+svg+"</svg>\n";
            public void Save(string path,bool gray)
            {
                var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);var pixels=(Color[])Pixels.Clone();
                if(gray)for(int i=0;i<pixels.Length;i++){Color c=pixels[i];float l=(c.r*.2126f+c.g*.7152f+c.b*.0722f)*.64f;pixels[i]=new Color(l,l,l,c.a);}
                texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
