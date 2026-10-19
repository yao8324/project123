#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
namespace MilanAcceptance
{
    [Serializable] public sealed class ReviewCase
    {
        public string id,kind;
        public Vector3 start,normal,origin,target;
        public float yaw,minimumInside=.5f;
        public bool screenshot;
    }
    [Serializable] public sealed class ReviewSuite {public ReviewCase[] cases;}
    [Serializable] public sealed class ReviewResult
    {
        public string id,kind;public bool pass;public string[] failures;
        public Vector3 start,end;public float minInside,maxPenetration,retreatDistance,forwardDistance;
        public string[] contacts;
    }
    [Serializable] public sealed class ReviewResults
    {
        public string scope="Unity Play Mode; real FirstPersonController driven by synthetic InputSystem devices. Fixed 1/60 simulated delta; not FPS or human-input acceptance. Desktop cursor untouched.";
        public int total,failed,errors,exceptions;public bool completed;public List<ReviewResult> results=new List<ReviewResult>();
    }
    public sealed class ReviewPlayTests:MonoBehaviour
    {
        FirstPersonController player;CharacterController cc;Camera eye;RecoveryMenu menu;
        Keyboard keyboard;Mouse mouse;Key[] held=Array.Empty<Key>();Vector2 pointer,mouseDelta;ushort buttons;
        string dir,phase="setup";ReviewCase active;ReviewResult result;ReviewResults report=new ReviewResults();
        readonly List<string> failures=new List<string>();readonly HashSet<string> contacts=new HashSet<string>();
        StreamWriter trace;bool recording;float minInside,maxPen;int shot;bool preview;int previewFrame,previewTick;
        void Check(bool value,string message){if(!value)failures.Add(message);}
        IEnumerator Start()
        {
            AudioListener.volume=0;Time.captureDeltaTime=1f/60f;
            dir=Environment.GetEnvironmentVariable("MILAN_REVIEW_OUTPUT");Directory.CreateDirectory(dir);
            trace=new StreamWriter(Path.Combine(dir,"trajectory.csv"));trace.WriteLine("case,phase,frame,x,y,z,height,inside,grounded,jump,land,clip,requestedCursor,menu");
            Application.logMessageReceived+=Log;
            player=FindFirstObjectByType<FirstPersonController>();cc=player.GetComponent<CharacterController>();eye=Camera.main;
            menu=player.GetComponent<RecoveryMenu>();
            foreach(var d in InputSystem.devices.ToArray())if(d is Keyboard || d is Mouse)InputSystem.DisableDevice(d);
            keyboard=InputSystem.AddDevice<Keyboard>("ReviewKeyboard");mouse=InputSystem.AddDevice<Mouse>("ReviewMouse");keyboard.MakeCurrent();mouse.MakeCurrent();
            InputSystem.settings=UnityEngine.Object.Instantiate(InputSystem.settings);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.onBeforeUpdate+=Inject;
            player.gameObject.AddComponent<ReviewContactRecorder>().owner=this;
            var suite=JsonUtility.FromJson<ReviewSuite>(File.ReadAllText(Environment.GetEnvironmentVariable("MILAN_REVIEW_SUITE")));
            var survey=new List<string>{"case,offset,tangent,x,z,hitY,collider"};
            var allCases=JsonUtility.FromJson<ReviewSuite>(File.ReadAllText(Environment.GetEnvironmentVariable("MILAN_REVIEW_SURVEY")??Environment.GetEnvironmentVariable("MILAN_REVIEW_SUITE")));
            foreach(var c in allCases.cases.Where(c=>c.kind=="window"))
                foreach(float offset in new[]{0f,-.4f,.4f})foreach(float tangent in new[]{0f,-1f,1f})
                {
                    var point=c.start+c.normal*offset+Vector3.Cross(c.normal,Vector3.up)*tangent;
                    var hits=Physics.RaycastAll(point+Vector3.up*.3f,Vector3.down,8f).Where(h=>h.collider!=cc).OrderBy(h=>h.distance).ToArray();
                    survey.Add($"{c.id},{offset},{tangent},{point.x},{point.z},{(hits.Length>0?hits[0].point.y:-999)},{(hits.Length>0?hits[0].collider.name:"none")}");
                }
            File.WriteAllLines(Path.Combine(dir,"floor_survey.csv"),survey);
            if(Environment.GetEnvironmentVariable("MILAN_FLOOR_REVIEW")=="1")
            {
                var samples=new List<string>{"x,z,hitY,supported"};int misses=0;
                for(float x=-11.95f;x<12;x+=.1f)for(float z=.05f;z<6;z+=.1f)
                {bool hit=Physics.Raycast(new Vector3(x,6.08f,z),Vector3.down,out var h,.2f);bool ok=hit&&Mathf.Abs(h.point.y-6)<.025f;if(!ok)misses++;samples.Add($"{x},{z},{(hit?h.point.y:-999)},{ok}");}
                File.WriteAllLines(Path.Combine(dir,"new_floor_support_grid.csv"),samples);if(misses>0)Debug.LogError("New floor grid unsupported samples="+misses);
            }

            yield return Hold(.2f);
            Place(new Vector3(0,.08f,7.8f),0);yield return Hold(.1f);
            Vector3 inputStart=player.transform.position;yield return Hold(.2f,Key.W);
            if(Vector3.Distance(inputStart,player.transform.position)<.4f)throw new InvalidOperationException("Synthetic input calibration failed: controller did not walk.");
            yield return Hold(.2f,Key.C);
            if(!player.IsCrouching)throw new InvalidOperationException("Synthetic crouch calibration failed.");
            Debug.Log("MILAN_REVIEW_INPUT_CALIBRATION_PASSED");
            foreach(var c in suite.cases)
            {
                active=c;result=new ReviewResult{id=c.id,kind=c.kind,start=c.start};failures.Clear();contacts.Clear();minInside=float.PositiveInfinity;maxPen=0;
                Place(c.start,c.yaw);yield return Hold(.4f);
                Check(Mathf.Abs(player.transform.position.y-c.start.y)<.4f,"Initial floor/placement did not settle at expected level.");
                recording=true;
                if(c.kind=="window")yield return WindowCase();
                else if(c.kind=="table")yield return TableCase();
                else if(c.kind=="support")yield return SupportCase();
                else if(c.kind=="floor_route")yield return FloorRoute();
                else yield return FrontCase();
                recording=false;
                result.end=player.transform.position;result.minInside=float.IsInfinity(minInside)?0:minInside;result.maxPenetration=maxPen;
                result.contacts=contacts.ToArray();result.failures=failures.ToArray();result.pass=failures.Count==0;
                report.total++;if(!result.pass)report.failed++;report.results.Add(result);
                if(c.screenshot || !result.pass)Capture(c.id);
                Save();Debug.Log("MILAN_REVIEW_CASE "+c.id+" pass="+result.pass);
            }
            string[] states={"window_gap","stairs","jump","crouch","held_jump"};
            foreach(string state in states)yield return RecoveryCase(state);
            yield return HandMovement();
            report.completed=true;Save();trace.Dispose();trace=null;
            Debug.Log("MILAN_PLAYTEST_REVIEW_COMPLETE failed="+report.failed+" errors="+report.errors);
            EditorApplication.Exit(report.failed+report.errors+report.exceptions==0?0:1);
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error)report.errors++;if(type==LogType.Exception)report.exceptions++;}
        void Save(){File.WriteAllText(Path.Combine(dir,"summary.json"),JsonUtility.ToJson(report,true));trace?.Flush();}
        void Inject()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic || keyboard==null)return;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=pointer,delta=mouseDelta,buttons=buttons});
        }
        IEnumerator Hold(float seconds,params Key[] keys)
        {
            held=keys;float elapsed=0;int frames=0;
            while(elapsed<seconds && frames++<6000){yield return null;elapsed+=Time.deltaTime;}
        }
        void Place(Vector3 position,float yaw)
        {
            phase="settle";held=Array.Empty<Key>();buttons=0;mouseDelta=Vector2.zero;
            cc.enabled=false;player.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));cc.height=1.75f;cc.center=new Vector3(0,.88f,0);cc.enabled=true;
            eye.transform.localPosition=new Vector3(0,1.6f,0);eye.transform.localRotation=Quaternion.identity;
            foreach(string n in new[]{"verticalVelocity","pitch"})typeof(FirstPersonController).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,0f);
            foreach(string n in new[]{"jumpConsumed","isOnGroundSurface","wasOnGroundSurface","groundSampleInitialized"})typeof(FirstPersonController).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,false);
            player.SetGameplayInputEnabled(true);player.SetCursorLocked(true);player.WaitForReleasedControls();Physics.SyncTransforms();
        }
        IEnumerator Turn(float degrees)
        {
            held=Array.Empty<Key>();mouseDelta=new Vector2(degrees/.08f,0);yield return null;mouseDelta=Vector2.zero;yield return null;
        }
        IEnumerator WindowCase()
        {
            phase="normal_approach";yield return Hold(.8f,Key.W);
            phase="sprint_jump";yield return Hold(.18f,Key.W,Key.LeftShift,Key.Space);yield return Hold(.65f,Key.W,Key.LeftShift);
            phase="left_angle";yield return Turn(-35);yield return Hold(.35f,Key.W,Key.LeftShift);
            phase="right_angle";yield return Turn(70);yield return Hold(.35f,Key.W,Key.LeftShift);yield return Turn(-35);
            phase="crouch_probe";yield return Hold(.5f,Key.W,Key.C);yield return Hold(.2f);
            Check(minInside>=active.minimumInside,"Entered window/wall recess beyond safe inner plane.");
            Check(maxPen<.13f,"Deep controller overlap around window.");
            Vector3 blocked=player.transform.position;phase="retreat";yield return Hold(.35f,Key.S);
            result.retreatDistance=Vector3.Distance(blocked,player.transform.position);
            Check(result.retreatDistance>.2f,"Could not retreat from window contact.");
            phase="along_wall";Vector3 before=player.transform.position;yield return Hold(.35f,Key.D);
            Check(Vector3.Distance(before,player.transform.position)>.2f,"Could not move along wall after retreat.");
        }
        IEnumerator TableCase()
        {
            phase="standing_contact";yield return Hold(.7f,Key.W);float inside=Vector3.Dot(player.transform.position-active.origin,active.normal);
            Check(inside>=.12f,"Standing controller passed through the tabletop edge.");
            phase="retreat";Vector3 at=player.transform.position;yield return Hold(.25f,Key.S);result.retreatDistance=Vector3.Distance(at,player.transform.position);
            if(result.retreatDistance<=.15f)
            {
                phase="chair_constrained_side_exit";yield return Hold(.45f,Key.D);yield return Hold(.25f,Key.S);
                result.retreatDistance=Vector3.Distance(at,player.transform.position);
            }
            Check(result.retreatDistance>.15f,"Unable to exit table contact either backwards or sideways.");
            phase="jump_contact";yield return Hold(.16f,Key.W,Key.Space);yield return Hold(.65f,Key.W,Key.LeftShift);
            phase="crouch_contact";yield return Hold(.45f,Key.W,Key.C);
            phase="around_side";yield return Hold(.65f,Key.D);yield return Hold(.15f);
            Check(player.transform.position.y>-.25f,"Fell through table/floor.");Check(maxPen<.13f,"Deep overlap with table collider.");
        }
        IEnumerator SupportCase()
        {
            phase="tabletop_support";yield return Hold(.5f);
            Check(Mathf.Abs(player.transform.position.y-active.start.y)<.1f,"Tabletop did not support a standing controller.");
            phase="tabletop_jump";uint jumps=player.JumpSequence;yield return Hold(.15f,Key.Space);yield return Hold(.85f);
            Check(player.JumpSequence>jumps && Mathf.Abs(player.transform.position.y-active.start.y)<.1f,"Jump did not return to the tabletop.");
            Check(contacts.Any(n=>n.StartsWith("Playtest_Review_Collision")),"No contact with the new compound tabletop proxy.");
            Check(maxPen<.13f,"Deep overlap on tabletop.");
        }
        IEnumerator FloorRoute()
        {
            float minY=player.transform.position.y;float elapsed=0;int frames=0;
            while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(active.target.x,active.target.z))>.3f && elapsed<22 && frames++<2000)
            {
                var delta=active.target-player.transform.position;delta.y=0;
                float desired=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
                mouseDelta=new Vector2(Mathf.DeltaAngle(player.transform.eulerAngles.y,desired)/.08f,0);
                phase="floor_walk_sprint_jump_crouch";
                held=elapsed<.7f?new[]{Key.W}:elapsed<.85f?new[]{Key.W,Key.Space}:elapsed<1.5f?new[]{Key.W,Key.LeftShift}:elapsed<2.2f?new[]{Key.W,Key.C}:new[]{Key.W,Key.LeftShift};
                yield return null;elapsed+=Time.deltaTime;minY=Mathf.Min(minY,player.transform.position.y);
            }
            mouseDelta=Vector2.zero;yield return Hold(.45f);
            Check(Vector3.Distance(player.transform.position,active.target)<.5f,"Floor route did not reach endpoint.");
            Check(minY>Mathf.Min(active.start.y,active.target.y)-.32f,"Floor route fell below expected supporting level.");
            Check(maxPen<.13f,"Deep overlap along floor/stair connection.");
            result.forwardDistance=Vector3.Distance(active.start,player.transform.position);
        }
        IEnumerator FrontCase()
        {
            Vector3 before=player.transform.position;
            if(active.kind=="closed_door")
            {
                phase="door_block";yield return Hold(1.8f,Key.W,Key.LeftShift);yield return Hold(.2f,Key.W,Key.Space);yield return Hold(.5f,Key.W,Key.C);
                Check(player.transform.position.z<-.30f,"Secondary door admitted controller.");
                phase="retreat";Vector3 at=player.transform.position;yield return Hold(.5f,Key.S);result.retreatDistance=Vector3.Distance(at,player.transform.position);Check(result.retreatDistance>.5f,"Secondary door trapped controller.");
            }
            else if(active.kind=="main_door")
            {
                phase="enter";yield return Hold(2.4f,Key.W,Key.LeftShift);Check(player.transform.position.z>1f,"Main doorway inward route blocked.");Capture("main_inside");
                phase="leave";yield return Hold(1f,Key.S,Key.C);yield return Hold(2.2f,Key.S,Key.LeftShift);Check(player.transform.position.z<-6f,"Main doorway outward route blocked.");
            }
            else
            {
                phase="ramp_walk";yield return Hold(.65f,Key.W);
                phase="ramp_sprint_jump";yield return Hold(.16f,Key.W,Key.Space,Key.LeftShift);yield return Hold(.45f,Key.W,Key.LeftShift);
                phase="ramp_crouch";yield return Hold(.45f,Key.W,Key.C);yield return Hold(.35f);
                Check(player.transform.position.y>-.72f && player.transform.position.y<.35f,"Ramp did not provide continuous landing.");
                result.forwardDistance=Vector3.Distance(before,player.transform.position);Check(result.forwardDistance>2f,"Front apron/edge traversal obstructed.");
                phase="ramp_reverse";yield return Hold(.65f,Key.S);Check(player.transform.position.y>-.75f,"Fell at apron edge.");
            }
            Check(maxPen<.13f,"Deep overlap at front approach.");
        }
        public void Contact(ControllerColliderHit hit){if(hit.collider)contacts.Add(hit.collider.name);}
        void LateUpdate()
        {
            if(trace==null || player==null)return;
            float inside=active==null?0:Vector3.Dot(player.transform.position-active.origin,active.normal);
            if(recording)
            {
                minInside=Mathf.Min(minInside,inside);
                Vector3 center=player.transform.TransformPoint(cc.center);float half=cc.height*.5f-cc.radius;
                foreach(var hit in Physics.OverlapCapsule(center-Vector3.up*half,center+Vector3.up*half,cc.radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                    if(hit!=cc && !hit.transform.IsChildOf(player.transform) && Physics.ComputePenetration(cc,player.transform.position,player.transform.rotation,hit,hit.transform.position,hit.transform.rotation,out var direction,out float depth))maxPen=Mathf.Max(maxPen,depth);
            }
            var a=player.GetComponentInChildren<Animator>(true);var clips=a&&a.gameObject.activeInHierarchy?a.GetCurrentAnimatorClipInfo(0):Array.Empty<AnimatorClipInfo>();
            Vector3 p=player.transform.position;
            if(preview && ++previewTick%5==0)Capture("PreviewFrames/frame_"+(previewFrame++).ToString("D4"));
            trace.WriteLine(string.Join(",",active?.id??"ui",phase,Time.frameCount,p.x,p.y,p.z,cc.height,inside,cc.isGrounded,player.JumpSequence,player.LandSequence,clips.Length>0?clips[0].clip.name:"hidden",player.CursorIsLocked,menu&&menu.IsOpen));
        }
        IEnumerator Click(UnityEngine.UI.Button button)
        {
            Canvas.ForceUpdateCanvases();pointer=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pointer},hits);
            Check(hits.Any(h=>h.gameObject==button.gameObject),$"UI raycast does not reach actual button at {pointer}, screen {Screen.width}x{Screen.height}, hits {string.Join(";",hits.Select(h=>h.gameObject.name))}.");
            buttons=0;yield return null;buttons=1;yield return null;yield return null;buttons=0;yield return null;yield return null;
        }
        IEnumerator RecoveryCase(string state)
        {
            active=null;failures.Clear();phase="recovery_"+state;
            Vector3 position=state=="window_gap"?new Vector3(-12.45f,6.08f,21):state=="stairs"?new Vector3(13.44f,1.6f,16.8f):new Vector3(0,.08f,7.8f);
            Place(position,0);yield return Hold(.25f);
            if(state=="jump" || state=="held_jump")yield return Hold(.2f,Key.Space);
            if(state=="crouch")yield return Hold(.25f,Key.C);
            phase="escape_open";yield return Hold(.05f,Key.Escape);yield return Hold(.05f);
            Check(menu && menu.IsOpen && !player.GameplayInputEnabled && !player.CursorIsLocked,"Esc did not open clickable recovery menu with released cursor.");
            uint previous=menu.RespawnCount;
            if(state=="held_jump")held=new[]{Key.Space};
            for(int i=0;i<3;i++)yield return Click(menu.RespawnButton);
            Check(menu.RespawnCount==previous+3,$"Three synthetic pointer clicks expected 3 safe respawns, observed {menu.RespawnCount-previous}; position {player.transform.position}.");
            Check(Vector3.Distance(player.transform.position,new Vector3(0,.08f,7.8f))<.22f && cc.enabled,"Recovery did not reach safe spawn with enabled controller.");
            Check(!player.IsCrouching && Mathf.Abs(eye.transform.localPosition.y-1.6f)<.01f,"Recovery retained crouch/camera offset.");
            Check(menu.IsOpen && !player.GameplayInputEnabled,"Respawn unexpectedly closed menu or enabled movement.");
            Capture("recovery_"+state);
            uint jumps=player.JumpSequence;
            yield return Click(menu.ResumeButton);
            if(state=="held_jump"){yield return Hold(.2f,Key.Space);Check(player.JumpSequence==jumps,"Held jump was replayed after recovery.");}
            yield return Hold(.15f);
            Check(!menu.IsOpen && player.GameplayInputEnabled && player.CursorIsLocked,"Resume did not restore gameplay/requested cursor lock.");
            Vector3 before=player.transform.position;yield return Hold(.25f,Key.W);Check(Vector3.Distance(before,player.transform.position)>.5f,"Movement did not resume after release.");
            var row=new ReviewResult{id="recovery_"+state,kind="ui_pointer",pass=failures.Count==0,failures=failures.ToArray(),start=position,end=player.transform.position};
            report.results.Add(row);report.total++;if(!row.pass)report.failed++;Save();
        }
        IEnumerator HandMovement()
        {
            active=null;preview=true;previewFrame=previewTick=0;Directory.CreateDirectory(Path.Combine(dir,"PreviewFrames"));
            Place(new Vector3(0,.08f,7.8f),0);yield return Hold(.5f);HandFrame("Idle");
            phase="hand_walk";yield return Hold(.5f,Key.W);HandFrame("Walk");
            phase="hand_sprint";yield return Hold(.5f,Key.W,Key.LeftShift);HandFrame("Sprint");
            uint landed=player.LandSequence;
            phase="hand_jump";yield return Hold(.20f,Key.Space);HandFrame("Jump");
            held=Array.Empty<Key>();for(int i=0;i<120 && player.LandSequence==landed;i++)yield return null;
            yield return Hold(.14f);HandFrame("Land");
            phase="hand_crouch";yield return Hold(.8f,Key.C);HandFrame("Crouch");
            Place(new Vector3(-11.03f,6.08f,21),-90);yield return Hold(.3f);yield return Hold(.8f,Key.W);yield return Hold(.3f);HandFrame("NearWall");
            preview=false;
        }
        void HandFrame(string expected)
        {
            failures.Clear();var animator=player.GetComponentInChildren<Animator>();var clips=animator.GetCurrentAnimatorClipInfo(0);
            string actual=clips.Length>0?clips.OrderByDescending(c=>c.weight).First().clip.name:"missing";
            Check(expected=="NearWall" || actual.EndsWith(expected),$"Expected {expected}, dominant animation is {actual}.");
            float minX=10,maxX=-10,minY=10,maxY=-10;int centerSamples=0;
            foreach(var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();renderer.BakeMesh(mesh,true);var cameraVerts=mesh.vertices.Select(v=>eye.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();var verts=cameraVerts.Select(v=>eye.WorldToViewportPoint(eye.transform.TransformPoint(v))).ToArray();
                foreach(var v in verts){if(v.z<=0)continue;minX=Mathf.Min(minX,v.x);maxX=Mathf.Max(maxX,v.x);minY=Mathf.Min(minY,v.y);maxY=Mathf.Max(maxY,v.y);}
                Debug.Log($"MILAN_HAND_BAKE {expected} {renderer.name} scale={renderer.transform.lossyScale} bakedBounds={mesh.bounds}");
                var triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var polygon=new List<Vector3>{cameraVerts[triangles[i]],cameraVerts[triangles[i+1]],cameraVerts[triangles[i+2]]};
                    var clipped=new List<Vector3>();
                    for(int e=0;e<polygon.Count;e++)
                    {
                        Vector3 from=polygon[e],to=polygon[(e+1)%polygon.Count];bool fromIn=from.z>=.005f,toIn=to.z>=.005f;
                        if(fromIn)clipped.Add(from);
                        if(fromIn!=toIn)clipped.Add(Vector3.Lerp(from,to,(.005f-from.z)/(to.z-from.z)));
                    }
                    var projected=clipped.Select(v=>(Vector2)eye.WorldToViewportPoint(eye.transform.TransformPoint(v))).ToArray();
                    for(int k=1;k+1<projected.Length;k++)
                    {
                        Vector2 a=projected[0],b=projected[k],c=projected[k+1];
                        foreach(float x in new[]{.42f,.5f,.58f})foreach(float y in new[]{.42f,.5f,.58f})
                        {var p=new Vector2(x,y);float u=Cross(b-a,p-a),v=Cross(c-b,p-b),w=Cross(a-c,p-c);if((u>0&&v>0&&w>0)||(u<0&&v<0&&w<0))centerSamples++;}
                    }
                }
                Destroy(mesh);
            }
            Check(centerSamples==0,"Hand/wand intersects the sampled central aiming area.");
            Check(maxX>1f || minY<0f,"Sleeve ends inside the image rather than continuing beyond its edge.");
            Capture("hand_"+expected+"_real_controller");
            File.AppendAllText(Path.Combine(dir,"hand_projection.csv"),$"{expected},{actual},{minX},{maxX},{minY},{maxY},{centerSamples}\n");
            var row=new ReviewResult{id="hand_"+expected,kind="real_controller_animation",pass=failures.Count==0,failures=failures.ToArray(),end=player.transform.position};report.results.Add(row);report.total++;if(!row.pass)report.failed++;Save();
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        void Capture(string name)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;
            var canvas=FindFirstObjectByType<Canvas>();
            if(canvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=eye;canvas.planeDistance=.12f;Canvas.ForceUpdateCanvases();}
            try
            {
                RenderPipeline.SubmitRenderRequest(eye,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;
                var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                File.WriteAllBytes(Path.Combine(dir,name+".png"),tex.EncodeToPNG());Destroy(tex);shot++;
            }
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(canvas){canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.ForceUpdateCanvases();}}
        }
        void OnDestroy(){InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;trace?.Dispose();}
    }
    public sealed class ReviewContactRecorder:MonoBehaviour
    {
        public ReviewPlayTests owner;
        void OnControllerColliderHit(ControllerColliderHit hit){owner?.Contact(hit);}
    }
}
#endif
