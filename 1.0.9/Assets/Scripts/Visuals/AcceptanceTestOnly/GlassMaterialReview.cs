#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ArcaneArchive.Player;
using ArcaneArchive.Visuals;
namespace MilanAcceptance
{
    public class GlassMaterialReview:MonoBehaviour
    {
        [Serializable] class View {public string name;public float[] position,target;}
        [Serializable] class Views {public View[] views;}
        [Serializable] class Row {public string variant,view;public double renderReadbackMs;}
        [Serializable] class Report {public string scope="Offscreen Unity Play Mode A/B, same source scene and camera; timings include synchronous GPU readback, not standalone FPS.";public int errors;public List<Row> rows=new List<Row>();}
        IEnumerator Start()
        {
            AudioListener.volume=0;Time.captureDeltaTime=1f/60f;
            var output=Environment.GetEnvironmentVariable("MILAN_GLASS_OUTPUT");Directory.CreateDirectory(output);var report=new Report();
            Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)report.errors++;};
            var eye=Camera.main;eye.aspect=1280f/720f;var hand=eye.GetComponentInChildren<FirstPersonViewModel>();if(hand)hand.gameObject.SetActive(false);
            foreach(var motion in FindObjectsByType<MilanMagicMotion>(FindObjectsSortMode.None))motion.enabled=false;
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilanLibraryV6/Materials/V3_FiguralPaintedGlass.mat");var mat=Instantiate(source);
            int renderers=0;foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {var a=renderer.sharedMaterials;bool changed=false;for(int i=0;i<a.Length;i++)if(a[i]==source){a[i]=mat;changed=true;}if(changed){renderer.sharedMaterials=a;renderers++;}}
            var tex=(Texture2D)source.GetTexture("_BaseMap");var native=new Texture2D(2,2,TextureFormat.RGBA32,true,false);native.LoadImage(File.ReadAllBytes("Assets/Art/MilanLibraryV6/Textures/Gothic_StainedGlass_Figural_V1.png"));native.filterMode=FilterMode.Trilinear;native.anisoLevel=8;
            var pipe=Instantiate((UniversalRenderPipelineAsset)(QualitySettings.renderPipeline??GraphicsSettings.defaultRenderPipeline));QualitySettings.renderPipeline=pipe;
            var data=eye.GetUniversalAdditionalCameraData();var volume=FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v=>v.isGlobal && v.sharedProfile && v.sharedProfile.TryGet<Bloom>(out _));var profile=volume ? volume.profile : null;
            Bloom bloom=null;ColorAdjustments color=null;if(profile){profile.TryGet<Bloom>(out bloom);profile.TryGet<ColorAdjustments>(out color);}var aa=data.antialiasing;float baseBloom=bloom?.intensity.value??0;float baseExposure=color?.postExposure.value??0;
            File.WriteAllText(Path.Combine(output,"baseline_settings.json"),JsonUtility.ToJson(new Settings{textureRuntimeBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex),textureWidth=tex.width,textureHeight=tex.height,textureFormat=tex.format.ToString(),filter=tex.filterMode.ToString(),anisotropy=tex.anisoLevel,mipCount=tex.mipmapCount,renderScale=pipe.renderScale,fog=RenderSettings.fog,glassRenderers=renderers,glassShader=source.shader.name,queue=source.renderQueue,surface=source.GetFloat("_Surface"),zwrite=source.GetFloat("_ZWrite"),bloom=baseBloom,exposure=baseExposure,volumePresent=volume!=null,postProcessing=data.renderPostProcessing,aa=aa.ToString()},true));
            string[] variants={"baseline","emission_off","emission_mapped","reflections_off","bloom_off","exposure_zero","scale_100","AA_off","native_texture","mapped_native","mapped_native_transparent"};
            var selectedVariants=Environment.GetEnvironmentVariable("MILAN_GLASS_VARIANTS");if(!string.IsNullOrEmpty(selectedVariants))variants=selectedVariants.Split(',');
            int repeats=int.TryParse(Environment.GetEnvironmentVariable("MILAN_GLASS_REPEATS"),out int repeatCount)?repeatCount:1;
            var views=JsonUtility.FromJson<Views>(File.ReadAllText(Environment.GetEnvironmentVariable("MILAN_GLASS_VIEWS"))).views;
            foreach(string variant in variants)
            {
                mat.CopyPropertiesFromMaterial(source);pipe.renderScale=.85f;data.antialiasing=aa;if(bloom!=null)bloom.intensity.value=baseBloom;if(color!=null)color.postExposure.value=baseExposure;
                if(variant=="legacy_white")mat.SetTexture("_EmissionMap",null);
                if(variant=="emission_off"){mat.SetColor("_EmissionColor",Color.black);mat.DisableKeyword("_EMISSION");}
                if(variant.Contains("mapped"))mat.SetTexture("_EmissionMap",tex);
                if(variant=="reflections_off"){mat.SetFloat("_EnvironmentReflections",0);mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");}
                if(variant=="bloom_off"&&bloom!=null)bloom.intensity.value=0;
                if(variant=="exposure_zero"&&color!=null)color.postExposure.value=0;
                if(variant=="scale_100")pipe.renderScale=1;
                if(variant=="AA_off")data.antialiasing=AntialiasingMode.None;
                if(variant.Contains("native")){mat.SetTexture("_BaseMap",native);if(variant.Contains("mapped"))mat.SetTexture("_EmissionMap",native);}
                if(variant.Contains("transparent"))
                {
                    var tint=mat.GetColor("_BaseColor");tint.a=.9f;mat.SetColor("_BaseColor",tint);mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",1);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",2);
                    mat.SetFloat("_SrcBlend",(float)BlendMode.One);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;
                }
                foreach(var view in views)
                for(int sample=0;sample<repeats;sample++)
                {
                    eye.transform.SetPositionAndRotation(V(view.position),Quaternion.LookRotation(V(view.target)-V(view.position)));
                    for(int i=0;i<4;i++)yield return null;
                    var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    var timer=System.Diagnostics.Stopwatch.StartNew();RenderPipeline.SubmitRenderRequest(eye,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();timer.Stop();
                    File.WriteAllBytes(Path.Combine(output,view.name+"__"+variant+(repeats>1?"_"+sample.ToString("D2"):"")+".png"),image.EncodeToPNG());report.rows.Add(new Row{variant=variant,view=view.name,renderReadbackMs=timer.Elapsed.TotalMilliseconds});Destroy(image);RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
                }
            }
            File.WriteAllText(Path.Combine(output,"summary.json"),JsonUtility.ToJson(report,true));Debug.Log("MILAN_GLASS_AB_COMPLETE errors="+report.errors);EditorApplication.Exit(report.errors==0?0:1);
        }
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        [Serializable] class Settings {public long textureRuntimeBytes;public int textureWidth,textureHeight,anisotropy,mipCount,glassRenderers,queue;public string textureFormat,filter,glassShader,aa;public float renderScale,surface,zwrite,bloom,exposure;public bool fog,volumePresent,postProcessing;}
    }
}
#endif
