#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ArcaneArchive.Player;
namespace MilanAcceptance
{
    public sealed class HandReviewFrames:MonoBehaviour
    {
        [Serializable] class ViewSpec {public string name;public float[] position,target;}
        [Serializable] class ViewList {public ViewSpec[] views;}
        IEnumerator Start()
        {
            AudioListener.volume=0;
            var dir=Environment.GetEnvironmentVariable("MILAN_HAND_REVIEW_OUTPUT");
            Directory.CreateDirectory(dir);
            var eye=Camera.main;eye.aspect=1280f/720f;
            var view=eye.GetComponentInChildren<FirstPersonViewModel>();
            view.enabled=false;
            var animator=view.GetComponentInChildren<Animator>();
            string[] clips={"Idle","Walk","Sprint","Jump","Land","Crouch"};
            for(int i=0;i<clips.Length;i++)
            {
                animator.Play("Base Layer."+clips[i],0,.25f);
                for(int frame=0;frame<8;frame++)yield return null;
                var rt=RenderTexture.GetTemporary(1280,720,24);
                var old=RenderTexture.active;
                try
                {
                    RenderPipeline.SubmitRenderRequest(eye,new RenderPipeline.StandardRequest{destination=rt});
                    RenderTexture.active=rt;
                    var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                    File.WriteAllBytes(Path.Combine(dir,clips[i]+".png"),tex.EncodeToPNG());Destroy(tex);
                }
                finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
            }
            string config=Environment.GetEnvironmentVariable("MILAN_REVIEW_CAMERA_CONFIG");
            if(!string.IsNullOrEmpty(config))
            {
                view.gameObject.SetActive(false);
                foreach(var v in JsonUtility.FromJson<ViewList>(File.ReadAllText(config)).views)
                {
                    Vector3 pos=new Vector3(v.position[0],v.position[1],v.position[2]);
                    Vector3 target=new Vector3(v.target[0],v.target[1],v.target[2]);
                    eye.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(target-pos));
                    for(int frame=0;frame<3;frame++)yield return null;
                    var rt=RenderTexture.GetTemporary(1600,900,24);var old=RenderTexture.active;
                    try
                    {
                        RenderPipeline.SubmitRenderRequest(eye,new RenderPipeline.StandardRequest{destination=rt});
                        RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
                        tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
                        File.WriteAllBytes(Path.Combine(dir,v.name+".png"),tex.EncodeToPNG());Destroy(tex);
                    }
                    finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
                }
            }
            File.WriteAllText(Path.Combine(dir,"capture_scope.json"),"{\"playMode\":true,\"offscreen\":true,\"controllerDisabledForNoCursorInterference\":true,\"sixClips\":true,\"movementAcceptance\":false}");
            Debug.Log("MILAN_HAND_REVIEW_SIX_RUNTIME_FRAMES_COMPLETE");
            EditorApplication.Exit(0);
        }
    }
}

#endif
