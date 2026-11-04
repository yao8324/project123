using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
using ArcaneArchive.Core;
namespace ArcaneArchive.Visuals.Editor
{
 public static class CrystalArenaBuilder
 {
  const string R=SealedDuelArenaBuilder.Root+"/CrystalMechanic";
  const string D="Docs/SealedDuelArena/CrystalStage";
  static Material iron,bronze,stone,amethyst,dim,glow,energy;
  static Mesh link,crystal,ring;
  static GameObject Node(string name,Transform parent,Vector3 p){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;return g;}
  static Material Mat(string name,Color color,float metal,float rough,Color emission)
  {
   string path=R+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",rough);m.globalIlluminationFlags=emission.maxColorComponent>0?MaterialGlobalIlluminationFlags.BakedEmissive:MaterialGlobalIlluminationFlags.EmissiveIsBlack;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);EditorUtility.SetDirty(m);return m;
  }
  static Mesh Save(Mesh m,string name){m.name=name;string path=R+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,path);return m;}
  static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material mat,Vector3 pos,Vector3 scale,Quaternion rot)
  {var g=Node(name,parent,pos);g.transform.localScale=scale;g.transform.localRotation=rot;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
  static GameObject Primitive(string name,Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,bool collision=false)
  {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
  static Mesh FacetCrystal()
  {
   var v=new List<Vector3>();var tri=new List<int>();
   Action<Vector3,Vector3,Vector3> add=(a,b,c)=>{int n=v.Count;v.AddRange(new[]{a,b,c});tri.AddRange(new[]{n,n+1,n+2});};
   int sides=7;for(int i=0;i<sides;i++){
    float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
    Vector3 a0=new Vector3(Mathf.Cos(a)*.31f,.2f,Mathf.Sin(a)*.31f),b0=new Vector3(Mathf.Cos(b)*.31f,.2f,Mathf.Sin(b)*.31f);
    Vector3 a1=new Vector3(Mathf.Cos(a)*.45f,1.4f,Mathf.Sin(a)*.45f),b1=new Vector3(Mathf.Cos(b)*.45f,1.4f,Mathf.Sin(b)*.45f);
    add(a0,a1,b1);add(a0,b1,b0);add(a1,new Vector3(.08f,2.15f,-.04f),b1);add(b0,Vector3.zero,a0);
   }
   var m=new Mesh{name="FacetedAmethyst"};m.SetVertices(v);m.SetUVs(0,v.Select(p=>new Vector2(Mathf.Atan2(p.z,p.x)/(Mathf.PI*2),p.y*.65f)).ToList());m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();return m;
  }
  static Mesh Ellipse(float x,float y,float tube,int n=20)
  {var pts=new Vector3[n+1];for(int i=0;i<=n;i++){float a=i*2*Mathf.PI/n;pts[i]=new Vector3(Mathf.Cos(a)*x,Mathf.Sin(a)*y,0);}return SealedDuelArenaGeometry.Tube(pts,tube,8);}
  
  static void PrepareEnergyAndCrystal()
  {
   var tex=new Texture2D(128,64,TextureFormat.RGBA32,false);tex.wrapMode=TextureWrapMode.Clamp;
   for(int y=0;y<64;y++)for(int x=0;x<128;x++){float v=Mathf.Abs((y-31.5f)/31.5f);float alpha=Mathf.Pow(1-v,4);tex.SetPixel(x,y,new Color(.65f,.28f,1,alpha*(.72f+.28f*Mathf.Sin(x*.25f))));}tex.Apply();
   string path=R+"/EnergyRibbon.asset";var old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(old){EditorUtility.CopySerialized(tex,old);Object.DestroyImmediate(tex);tex=old;}else AssetDatabase.CreateAsset(tex,path);
   energy=Mat("Supply_Energy",Color.white,0,0,Color.black);energy.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");energy.SetTexture("_BaseMap",tex);energy.SetColor("_BaseColor",new Color(1.4f,.65f,2,1));energy.SetFloat("_Surface",1);energy.SetFloat("_Blend",2);energy.SetFloat("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);energy.SetFloat("_DstBlend",(int)UnityEngine.Rendering.BlendMode.One);energy.SetFloat("_ZWrite",0);energy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");energy.renderQueue=3000;
   var vein=new Texture2D(256,256,TextureFormat.RGBA32,true);vein.wrapMode=TextureWrapMode.Repeat;
   for(int y=0;y<256;y++)for(int x=0;x<256;x++){float n=Mathf.PerlinNoise(x*.047f,y*.047f);float f=Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(n-.5f)*28),4);vein.SetPixel(x,y,Color.Lerp(new Color(.17f,.025f,.32f),new Color(.85f,.35f,1),f));}vein.Apply();
   path=R+"/AmethystVeins.asset";old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(old){EditorUtility.CopySerialized(vein,old);Object.DestroyImmediate(vein);vein=old;}else AssetDatabase.CreateAsset(vein,path);
   amethyst.SetTexture("_BaseMap",vein);amethyst.SetTexture("_EmissionMap",vein);amethyst.SetColor("_BaseColor",new Color(.6f,.3f,.9f));amethyst.SetColor("_EmissionColor",new Color(.9f,.42f,1)*2.5f);amethyst.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;amethyst.EnableKeyword("_EMISSION");EditorUtility.SetDirty(amethyst);EditorUtility.SetDirty(energy);
  }
  static void MakeAssembly(Transform parent,CrystalArenaVisuals visual,int i,float angle)
  {
   float a=angle*Mathf.Deg2Rad;var origin=Node("Crystal_"+(i+1),parent,new Vector3(Mathf.Sin(a)*15.35f,0,Mathf.Cos(a)*15.35f)).transform;visual.crystals[i]=origin;
   Primitive("Basalt_Foundation",origin,PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(1.65f,.12f,1.65f),stone,true);
   Primitive("Bronze_Socket",origin,PrimitiveType.Cylinder,new Vector3(0,.30f,0),new Vector3(1.13f,.10f,1.13f),bronze);
   for(int r=0;r<3;r++)MeshObject("Etched_Base_Ring",origin,ring,glow,new Vector3(0,.248f+r*.003f,0),Vector3.one*(.65f+r*.13f),Quaternion.Euler(90,0,0));
   for(int r=0;r<4;r++)MeshObject("Bronze_Socket_Moulding",origin,ring,bronze,new Vector3(0,.27f+r*.055f,0),new Vector3(.59f,.59f,2.2f),Quaternion.Euler(90,0,0));
   for(int j=0;j<16;j++){float t=j*Mathf.PI/8;var panel=Primitive("Socket_Engraved_Rib",origin,PrimitiveType.Cube,new Vector3(Mathf.Sin(t)*.55f,.37f,Mathf.Cos(t)*.55f),new Vector3(.025f,.22f,.035f),bronze);panel.transform.localRotation=Quaternion.Euler(0,j*22.5f,0);}
   for(int j=0;j<6;j++){
    float t=j*60*Mathf.Deg2Rad;var claw=Primitive("Socket_Claw",origin,PrimitiveType.Cube,new Vector3(Mathf.Sin(t)*.50f,.55f,Mathf.Cos(t)*.50f),new Vector3(.11f,.57f,.16f),bronze);claw.transform.localRotation=Quaternion.Euler(0,j*60,18);
   }
   var archMesh=Save(SealedDuelArenaGeometry.Tube(SealedDuelArenaGeometry.Arch(.105f,0,.13f,.12f,8),.014f,6),"Socket_Gothic_Arch");
   for(int j=0;j<12;j++){float t=j*Mathf.PI/6;MeshObject("Socket_Gothic_Engraving",origin,archMesh,bronze,new Vector3(Mathf.Sin(t)*.58f,.29f,Mathf.Cos(t)*.58f),Vector3.one,Quaternion.Euler(0,j*30,0));}
   var intact=Node("Intact_Amethyst",origin,new Vector3(0,.42f,0));visual.intactCrystals[i]=intact;
   foreach(var poweredRing in origin.Cast<Transform>().Where(t=>t.name=="Etched_Base_Ring").ToArray())poweredRing.SetParent(intact.transform,true);
   MeshObject("Main_Faceted_Crystal",intact.transform,crystal,amethyst,Vector3.zero,Vector3.one,Quaternion.Euler(0,25*i,0));
   var edges=new List<CombineInstance>();
   for(int j=0;j<7;j++){float t=j*Mathf.PI*2/7;var pts=new[]{new Vector3(Mathf.Cos(t)*.313f,.2f,Mathf.Sin(t)*.313f),new Vector3(Mathf.Cos(t)*.453f,1.4f,Mathf.Sin(t)*.453f),new Vector3(.08f,2.15f,-.04f)};edges.Add(new CombineInstance{mesh=SealedDuelArenaGeometry.Tube(pts,.006f,5),transform=Matrix4x4.identity});}
   var edgeMesh=new Mesh();edgeMesh.CombineMeshes(edges.ToArray());foreach(var edge in edges)Object.DestroyImmediate(edge.mesh);
   MeshObject("Crystal_Luminous_Facet_Edges",intact.transform,Save(edgeMesh,"Crystal_Facet_Edges"),glow,Vector3.zero,Vector3.one,Quaternion.Euler(0,25*i,0));
   for(int j=0;j<4;j++){float t=j*Mathf.PI*.5f;MeshObject("Satellite_Shard",intact.transform,crystal,amethyst,new Vector3(Mathf.Sin(t)*.37f,0,Mathf.Cos(t)*.37f),new Vector3(.3f,.36f,.3f),Quaternion.Euler(12,j*90,18));}
   var broken=Node("Dim_Broken_Remnants",origin,new Vector3(0,.30f,0));visual.brokenCrystals[i]=broken;
   for(int j=0;j<7;j++){float t=j*2.4f;MeshObject("Dead_Shard",broken.transform,crystal,dim,new Vector3(Mathf.Sin(t)*.5f,.04f,Mathf.Cos(t)*.5f),new Vector3(.25f,.22f,.25f),Quaternion.Euler(70,j*51,23));}broken.SetActive(false);
   var chainRoot=Node("Floating_Spiral_Chain_Protection",origin,Vector3.zero).transform;visual.chains[i]=chainRoot;
   // Two interwoven rising spirals, alternating link planes. Merge links per spiral for two draw calls.
   for(int strand=0;strand<2;strand++){
    var combines=new List<CombineInstance>();int count=62;
    for(int j=0;j<count;j++){
     float t=j/(float)(count-1);float theta=t*Mathf.PI*3.2f+strand*Mathf.PI;
     Vector3 pos=new Vector3(Mathf.Cos(theta)*.81f,.62f+t*2.15f,Mathf.Sin(theta)*.81f);
     Vector3 tangent=new Vector3(-Mathf.Sin(theta)*.81f,2.15f/(Mathf.PI*3.2f),Mathf.Cos(theta)*.81f).normalized;
     Quaternion q=Quaternion.FromToRotation(Vector3.up,tangent)*Quaternion.Euler(0,j%2*90,0);
     combines.Add(new CombineInstance{mesh=link,transform=Matrix4x4.TRS(pos,q,Vector3.one)});
    }
    var mesh=new Mesh{name="Interlocked_Spiral_"+strand};mesh.CombineMeshes(combines.ToArray());mesh.RecalculateBounds();MeshObject("Spiral_"+strand,chainRoot,Save(mesh,"Spiral_"+strand),iron,Vector3.zero,Vector3.one,Quaternion.identity);
   }
   var runeRoot=Node("Ascending_Purple_Runes",origin,Vector3.zero).transform;visual.runes[i]=runeRoot;
   for(int j=0;j<14;j++){
    float theta=j*2.399f;var glyph=Node("Rune_"+j,runeRoot,new Vector3(Mathf.Cos(theta)*1.02f,.6f+j*.16f,Mathf.Sin(theta)*1.02f));glyph.transform.localRotation=Quaternion.Euler(0,-theta*Mathf.Rad2Deg,0);
    MeshObject("Rune_Diamond",glyph.transform,ring,glow,Vector3.zero,new Vector3(.085f,.15f,.085f),Quaternion.identity);
    Primitive("Rune_Stem",glyph.transform,PrimitiveType.Cube,Vector3.zero,new Vector3(.018f,.31f,.018f),glow);
   }
   var lamp=Node("Purple_Crystal_Light",origin,new Vector3(0,1.6f,0)).AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(.53f,.14f,1);lamp.range=5.8f;lamp.intensity=1.15f;lamp.shadows=LightShadows.None;visual.crystalLights[i]=lamp;
   var beam=Node("Supply_Stream_"+(i+1),parent,Vector3.zero).AddComponent<LineRenderer>();beam.sharedMaterial=energy;beam.textureMode=LineTextureMode.Stretch;beam.positionCount=32;beam.useWorldSpace=true;beam.startWidth=.24f;beam.endWidth=.11f;beam.numCornerVertices=3;beam.numCapVertices=3;beam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;visual.supply[i]=beam;
  }
  [MenuItem("IM Monster/Sealed Arena/Build crystal visual stage")]
  public static void Build()
  {
   SealedDuelArenaAcceptance.GuardSceneWork();Directory.CreateDirectory(R);Directory.CreateDirectory(D);AssetDatabase.Refresh();
   iron=Mat("Dark_Iron_Links",new Color(.16f,.18f,.23f),.85f,.48f,Color.black);bronze=Mat("Aged_Bronze_Socket",new Color(.30f,.19f,.095f),.72f,.42f,Color.black);stone=Mat("Deep_Basalt_Base",new Color(.08f,.09f,.13f),.1f,.24f,Color.black);amethyst=Mat("Amethyst_Facets",new Color(.23f,.055f,.42f),.48f,.85f,new Color(.22f,.025f,.45f)*.65f);dim=Mat("Dead_Amethyst",new Color(.075f,.065f,.10f),.25f,.5f,Color.black);glow=Mat("Purple_Rune_Energy",new Color(.5f,.14f,.9f),.2f,.5f,new Color(.65f,.12f,1)*1.8f);
   PrepareEnergyAndCrystal();
   link=Save(Ellipse(.072f,.12f,.025f),"Iron_Link");ring=Save(Ellipse(1,1,.014f,32),"Rune_Ring");crystal=Save(FacetCrystal(),"Crystal_Mesh");
   var root=PrefabUtility.LoadPrefabContents(SealedDuelArenaAcceptance.ArenaPath);
   try{
    var old=root.transform.Find("Crystal_Power_Sanctums");if(old)Object.DestroyImmediate(old.gameObject);
    var visual=Node("Crystal_Power_Sanctums",root.transform,Vector3.zero).AddComponent<CrystalArenaVisuals>();visual.bossTarget=root.GetComponent<BossArena>().boss;visual.linkFragmentMesh=link;visual.crystalFragmentMesh=crystal;visual.ironFragmentMaterial=iron;visual.crystalFragmentMaterial=amethyst;
    visual.crystals=new Transform[3];visual.chains=new Transform[3];visual.runes=new Transform[3];visual.intactCrystals=new GameObject[3];visual.brokenCrystals=new GameObject[3];visual.crystalLights=new Light[3];visual.supply=new LineRenderer[3];
    var network=visual.gameObject.AddComponent<BossPowerNetwork>();network.visuals=visual;PrepareSelectedAudio(visual.gameObject);network.targets=new CrystalCombatTarget[3];
    for(int i=0;i<3;i++)MakeAssembly(visual.transform,visual,i,new[]{-60f,60f,180f}[i]);for(int i=0;i<3;i++){
        var hit=Node("Attackable_Chain_Crystal_"+i,visual.crystals[i],new Vector3(0,1.6f,0));
        var collider=hit.AddComponent<CapsuleCollider>();collider.radius=.92f;collider.height=2.9f;collider.isTrigger=true;
        var target=hit.AddComponent<CrystalCombatTarget>();target.network=network;target.index=i;network.targets[i]=target;
    }
    visual.Animate(0);PrefabUtility.SaveAsPrefabAsset(root,SealedDuelArenaAcceptance.ArenaPath);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
   AssetDatabase.SaveAssets();VerifyAndCapture();Debug.Log("CRYSTAL_VISUAL_STAGE_COMPLETE");
  }
  static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
  static void PrepareSelectedAudio(GameObject root)
  {
   var audio=root.AddComponent<ArenaCombatAudio>();const string dir="Assets/Audio/Boss/Selected20261104/";
   var map=new System.Collections.Generic.Dictionary<string,string>{{"guardBlock","GuardBlock"},{"guardRaise","GuardRaise"},{"armorHit","ArmorHit"},{"heavyArmorHit","EmpoweredArmorHit"},{"playerHurt","PlayerHurt"},{"meleeSwingReserved","MeleeSwingReserved"},{"crystalBreak","CrystalBreak"},{"crystalDebris","CrystalDebris"},{"chainHit","ChainHit"},{"chainBreak","ChainBreak"},{"chainLoop","ChainFrictionLoop"},{"crystalLoop","CrystalHumLoop"},{"volleyRelease","VolleyRelease"},{"volleyCharge","VolleyCharge"},{"groundFracture","GroundFracture"},{"shockwave","Shockwave"},{"laserCharge","LaserCharge"},{"rush","Rush"}};
   foreach(var pair in map){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(dir+pair.Value+".wav");if(!clip)throw new Exception("Missing selected audio "+pair.Value);typeof(ArenaCombatAudio).GetField(pair.Key).SetValue(audio,clip);}
  }
  public static void BuildAndNative(){Build();BuildNative();}
  public static void BuildNative()
  {
   SealedDuelArenaAcceptance.GuardSceneWork();EditorSceneManager.OpenScene(SealedDuelArenaAcceptance.ScenePath);
   var e=Object.FindFirstObjectByType<BossEncounter>();PrepareCombatFont(e);if(!e.GetComponent<ArcaneArchive.Visuals.Acceptance.CrystalNativeProbe>())e.gameObject.AddComponent<ArcaneArchive.Visuals.Acceptance.CrystalNativeProbe>();
   EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);SealedDuelArenaBuilder.BuildMac();
  }
  static void PrepareCombatFont(BossEncounter e)
  {
   const string path=R+"/CrystalChineseFallback.asset";
   var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
   if(!font){
    var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Contest20261107/Fonts/NotoSansCJKsc-Regular.otf");
    font=TMPro.TMP_FontAsset.CreateFontAsset(source,48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
    font.name="CrystalChineseFallback";AssetDatabase.CreateAsset(font,path);
    AssetDatabase.AddObjectToAsset(font.material,font);
    foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
   }
   // Populate every currently used Chinese UI character, while keeping the source for future text.
   string chars=string.Concat(Directory.GetFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
   chars=new string(chars.Where(c=>c>=0x4e00&&c<=0x9fff).Distinct().ToArray());
   Require(font.TryAddCharacters(chars,out string missing),"Missing CJK glyphs: "+missing);
   if(!e.combatFont.fallbackFontAssetTable.Contains(font))e.combatFont.fallbackFontAssetTable.Add(font);
   EditorUtility.SetDirty(font);EditorUtility.SetDirty(e.combatFont);AssetDatabase.SaveAssets();
  }
  public static void VerifyAndCapture()
  {
   SealedDuelArenaAcceptance.GuardSceneWork();var setup=EditorSceneManager.GetSceneManagerSetup();
   try{VerifyAndCaptureInternal();}
   finally{if(setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
  static void VerifyAndCaptureInternal()
  {
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SealedDuelArenaAcceptance.ArenaPath));var v=root.GetComponentInChildren<CrystalArenaVisuals>();v.ResetVisuals();v.Animate(2);
   Require(v.intactCrystals.All(c=>c.transform.Cast<Transform>().Count(t=>t.name=="Etched_Base_Ring")==3),"Base sigils must stop with their crystal");
   Require(v.supply.Length==3 && v.chains.Length==3,"Need exactly three independent assemblies");
   v.SetCrystalBroken(0);Require(!v.crystalBroken[0],"Protected crystal cannot break visually");v.SetChainBroken(0);Require(v.supply[0].enabled && v.crystalLights[0].enabled,"Chain break must preserve supply");v.SetCrystalBroken(0);Require(!v.supply[0].enabled && v.supply[1].enabled && !v.intactCrystals[0].activeSelf && v.brokenCrystals[0].activeSelf,"Only corresponding supply stops");v.ResetVisuals();Require(v.supply.All(x=>x.enabled) && v.chains.All(x=>x.gameObject.activeSelf),"Reset restores all");
   Physics.SyncTransforms();int n=0;for(int i=0;i<72;i++){float a=i*5*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var start=-dir*14;var hits=Physics.CapsuleCastAll(start+Vector3.up*.4f,start+Vector3.up*1.45f,.3f,dir,28).Where(x=>!x.collider.transform.IsChildOf(root.GetComponent<BossArena>().boss));Require(!hits.Any(),"Crystal foundation blocks central diameter "+i);n++;}
   var c=new GameObject("Crystal_Review_Camera").AddComponent<Camera>();c.nearClipPlane=.1f;c.farClipPlane=80;c.fieldOfView=66;c.clearFlags=CameraClearFlags.Skybox;
   Capture(c,new Vector3(0,4.7f,-12),new Vector3(0,1.6f,2),"01_arena_crystals.png");Capture(c,new Vector3(-10.4f,2.8f,4),new Vector3(-13.29f,1.5f,7.675f),"02_chain_crystal.png");
   v.SetChainBroken(0);v.SetCrystalBroken(0);Capture(c,new Vector3(-10.4f,2.8f,4),new Vector3(-13.29f,1.1f,7.675f),"03_dim_remnants.png");
   File.WriteAllText(D+"/verification.json","{\"visualStateAssertions\":6,\"clearDiameterSweeps\":"+n+",\"compiled\":true,\"scope\":\"static visual states and collision sweeps; runtime results stored in CrystalCombat\"}");Object.DestroyImmediate(root);Object.DestroyImmediate(c.gameObject);
  }
  static void Capture(Camera c,Vector3 pos,Vector3 look,string name)
  {c.transform.position=pos;c.transform.LookAt(look);var rt=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(D+"/"+name,tex.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}}
 }
}
