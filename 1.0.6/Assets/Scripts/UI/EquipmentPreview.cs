using ArcaneArchive.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArcaneArchive.UI
{
    public sealed class EquipmentPreview : MonoBehaviour, IDragHandler
    {
        GameObject studio;
        Transform turntable;
        RenderTexture texture;
        float yaw=180;
        readonly System.Collections.Generic.List<Material> displayMaterials=new System.Collections.Generic.List<Material>();
        public void Initialize(MeleeLoadout data)
        {
            int layer=LayerMask.NameToLayer("FirstPersonViewModel");
            studio=new GameObject("Equipment display studio");studio.transform.position=new Vector3(0,-500,0);
            turntable=new GameObject("Sword and shield display").transform;turntable.SetParent(studio.transform,false);
            var sword=Instantiate(data.swordPrefab,turntable);sword.transform.localPosition=new Vector3(-.25f,-.36f,0);
            var shield=Instantiate(data.shieldPrefab,turntable);shield.transform.localPosition=new Vector3(.29f,-.02f,0);
            foreach(var t in studio.GetComponentsInChildren<Transform>())t.gameObject.layer=layer;
            foreach(var r in studio.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode=ShadowCastingMode.Off;
                var materials=r.materials;
                foreach(var material in materials)
                {
                    displayMaterials.Add(material);material.EnableKeyword("_EMISSION");
                    if(material.GetColor("_EmissionColor").maxColorComponent<.4f)material.SetColor("_EmissionColor",material.GetColor("_BaseColor")*.30f);
                }
                r.sharedMaterials=materials;
            }
            var cameraObject=new GameObject("Equipment preview camera");cameraObject.transform.SetParent(studio.transform,false);
            cameraObject.transform.localPosition=new Vector3(0,.18f,-2.45f);cameraObject.transform.LookAt(studio.transform.position+Vector3.up*.16f);
            var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=32;camera.nearClipPlane=.1f;camera.farClipPlane=5;
            camera.cullingMask=1<<layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.085f,.071f,.080f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            texture=new RenderTexture(640,720,24,RenderTextureFormat.ARGB32){name="Live equipment preview"};texture.Create();camera.targetTexture=texture;
            GetComponent<RawImage>().texture=texture;
            Light(studio.transform,new Vector3(-1.2f,1.8f,-1.4f),new Color(1,.84f,.60f),12f,layer);
            Light(studio.transform,new Vector3(1,.4f,-.6f),new Color(.63f,.57f,1),7f,layer);
            turntable.localRotation=Quaternion.Euler(0,yaw,0);
        }
        static void Light(Transform parent,Vector3 position,Color color,float intensity,int layer)
        {
            var go=new GameObject("Display light");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=4;light.cullingMask=1<<layer;light.shadows=LightShadows.None;
        }
        public void OnDrag(PointerEventData data){yaw-=data.delta.x*.45f;if(turntable)turntable.localRotation=Quaternion.Euler(0,yaw,0);}
        void OnDisable(){if(studio)studio.SetActive(false);}
        void OnDestroy(){foreach(var material in displayMaterials)if(material)Destroy(material);if(studio)Destroy(studio);if(texture){texture.Release();Destroy(texture);}}
    }
}
