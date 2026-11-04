using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ArcaneArchive.Player;
using IMMonster.SceneArt;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Contest
{
    [DefaultExecutionOrder(200)]
    public sealed class ContestStoryGame : MonoBehaviour
    {
        public enum ScreenMode { MainMenu, Explore, Reader, Dialogue, Cutscene, Pause, Help, Settings, Credits, Confirm, Ending }
        [Serializable] public sealed class Record { public string id,scene_anchor,title,body,summary; }
        [Serializable] public sealed class Line { public string id,speaker,text,stage; }
        [Serializable] public sealed class Incident { public string id,title,body; }
        [Serializable] public sealed class Content { public Record[] records;public Line[] dialogue;public Incident incident_record; }
        public FirstPersonController player;
        public Camera view;
        public Transform cameraPivot;
        public GameObject library, hall;
        public ArchiveWorkstationArt workstation;
        public GameObject body, bodyEvidence, incidentObject, sealedBundle, transferCase, conduit, palm;
        public Renderer[] sealRenderers;
        public Font font;
        public TextAsset contentFile;
        public ContestStoryState State { get; private set; } = new ContestStoryState();
        public ScreenMode Mode { get; private set; } = ScreenMode.MainMenu;
        public ContestHud Hud { get; private set; }
        public ContestPowerChallenge Power { get; private set; } = new ContestPowerChallenge();
        public bool Started { get; private set; }
        public bool Paused => Mode==ScreenMode.Pause || Mode==ScreenMode.Settings || Mode==ScreenMode.Help || Mode==ScreenMode.Credits || Mode==ScreenMode.Confirm;
        public bool CanContinue => checkpoint!=null;
        public bool PowerActive { get; private set; }
        public bool ReduceFlash { get; private set; } = true;
        public float Volume { get; private set; } = .55f;
        public string SavePath { get; private set; }
        public string LastInteraction { get; private set; }
        public int InteractionCount { get; private set; }
        public string CurrentTargetId => target?target.id:null;
        public Content StoryContent => content;
        public ContestStoryState Checkpoint => checkpoint?.Copy();
        public bool Automated => Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-contest-test");
        public bool CanAcceptUi => Time.frameCount>uiFrame+1;
        public float StorySeconds { get; private set; }
        public string LastNotice { get; private set; }
        public string StartupNotice { get; private set; }
        Content content;
        ContestStoryState checkpoint;
        ContestWorldTarget target;
        List<Line> conversation;
        int lineIndex,uiFrame;
        Action conversationDone;
        ScreenMode resumeMode;
        bool cutsceneRunning,waitForRelease=true,initialised;
        float noticeUntil,lastActionAt,fade,previousVolume,previousScale;
        Vector3 restingEye;
        AudioSource effects,ambience;
        readonly Dictionary<int,AudioClip> clips=new Dictionary<int,AudioClip>();
        readonly Dictionary<string,int> completionCounts=new Dictionary<string,int>();
        readonly Dictionary<Light,float> lightDefaults=new Dictionary<Light,float>();
        readonly Vector3 workFeet=new Vector3(5.2f,.08f,7.1f),returnFeet=new Vector3(6,.08f,1.4f);

        void Awake()
        {
            previousScale=Time.timeScale;previousVolume=AudioListener.volume;
            restingEye=cameraPivot.localPosition;
            content=JsonUtility.FromJson<Content>(contentFile.text);
            if(content==null || content.records==null || content.records.Length!=3 || content.dialogue==null || !player || !view || !font)
                throw new InvalidOperationException("Contest scene references or story data missing");
            SavePath=Path.Combine(Application.persistentDataPath,"IMMonsterContest20261107",Automated?"acceptance-checkpoint.json":"checkpoint.json");
            if(!Automated){ContestCheckpointStore.TryRead(SavePath,out checkpoint,out var loadNotice);StartupNotice=loadNotice;}
            if(!Automated){Volume=PlayerPrefs.GetFloat("IMContest.Volume",.55f);ReduceFlash=PlayerPrefs.GetInt("IMContest.ReducedFlash",1)==1;}
            AudioListener.volume=Volume;
            foreach(var light in library.GetComponentsInChildren<Light>(true))if(light.type==LightType.Point)lightDefaults[light]=light.intensity;
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;
            ambience=gameObject.AddComponent<AudioSource>();ambience.playOnAwake=false;ambience.spatialBlend=0;ambience.loop=true;ambience.volume=.22f;
            ambience.clip=MakeClip(4,6);ambience.Play();
            Hud=gameObject.AddComponent<ContestHud>();Hud.Initialize(this,font);
            ApplyPresentation();initialised=true;ShowMainMenu();
        }
        IEnumerator Start()
        {
            yield return null;
            var quality=player.GetComponent<ArcaneArchive.Visuals.MilanQualityOptions>();if(quality)quality.Select("Balanced");
            FreezeControls();
        }
        void Update()
        {
            if(!initialised || player.Bindings==null)return;
            var input=player.Bindings;
            if(!input.IsRebinding && !input.SuppressInputThisFrame &&
                (input.MenuAction.WasPressedThisFrame() || Keyboard.current?.escapeKey.wasPressedThisFrame==true))HandleEscape();
            if(Mode==ScreenMode.Explore)
            {
                StorySeconds+=Time.unscaledDeltaTime;
                target=FindTarget();
                bool held=input.InteractAction.IsPressed();
                if(waitForRelease){if(!held)waitForRelease=false;}
                else if(!input.SuppressInputThisFrame && input.InteractAction.WasPressedThisFrame())
                {
                    if(State.stage==5 && State.incidentRead)ConfirmPalm();
                    else if(target)Activate(target);
                }
                if(PowerActive)
                {
                    Power.Assist=State.assist;
                    int outcome=Power.Step(Time.unscaledDeltaTime,held,target && target.id=="RESCUE_CONDUIT" && !waitForRelease,false);
                    if(outcome<0){Notice("回响过强。先松开，让力量减弱。",4);Sound(2);}
                    if(outcome>0)CompletePulse();
                }
                if(player.transform.position.y<-3)RecoverFall();
            }
            else target=null;
            if(Mode==ScreenMode.Dialogue && CanAcceptUi && Keyboard.current?.spaceKey.wasPressedThisFrame==true)AdvanceDialogue();
            string hint=target?Prompt(target.id):"";
            if(State.stage==5 && State.incidentRead && Mode==ScreenMode.Explore)hint="["+InteractKey+"] 抬起左掌，对照共鸣纹";
            if(Mode==ScreenMode.Explore && string.IsNullOrEmpty(hint) && Time.unscaledTime-lastActionAt>20)hint=DirectionHint();
            Hud.Refresh(Mode,Started,Objective(),hint,Time.unscaledTime<noticeUntil?LastNotice:"",PowerActive,Power,State);
            UpdateConduit();
        }
        void LateUpdate(){if(initialised)FreezeControls();}
        void OnApplicationFocus(bool focused)
        {if(initialised && !Automated && !focused && Started && Mode!=ScreenMode.MainMenu && Mode!=ScreenMode.Ending && !Paused)Pause();}
        void OnDestroy()
        {
            Time.timeScale=previousScale;AudioListener.pause=false;AudioListener.volume=previousVolume;
            if(player){player.SetGameplayInputEnabled(true);player.WaitForReleasedControls();}
            if(ambience && ambience.clip)Destroy(ambience.clip);
            foreach(var clip in clips.Values)if(clip)Destroy(clip);
        }
        public string InteractKey => player.Bindings!=null?player.Bindings.Display("Interact"):"E";
        void SetMode(ScreenMode value)
        {
            Mode=value;uiFrame=Time.frameCount;waitForRelease=true;
            if(value!=ScreenMode.Explore)Power.Suspend();
            FreezeControls();
        }
        void FreezeControls()
        {
            bool explore=Mode==ScreenMode.Explore;
            Time.timeScale=explore?1:0;
            player.SetGameplayInputEnabled(explore);player.SetCursorLocked(explore && !(Automated && !Application.isEditor));
            AudioListener.pause=Paused;
        }
        public void NewGame(bool confirmed=false)
        {
            if(!confirmed && (Started || CanContinue))
            {Confirm("开始新游戏","将覆盖本篇的检查点记录。",()=>NewGame(true));return;}
            StopAllCoroutines();cutsceneRunning=false;State=new ContestStoryState();Power.Reset();PowerActive=false;completionCounts.Clear();StorySeconds=0;
            Started=true;ApplyPresentation();
            if(!Place(workFeet,Quaternion.identity)){ShowMainMenu();return;}
            WriteCheckpoint();Hud.HideModal();SetMode(ScreenMode.Explore);Touch();
            Talk(new[]{"D001"},()=>Explore());
        }
        public void ContinueGame()
        {
            if(checkpoint==null)return;
            Restore(checkpoint.Copy());
        }
        void Restore(ContestStoryState restored)
        {
            StopAllCoroutines();cutsceneRunning=false;PowerActive=false;State=restored;Started=true;Power.Reset();Power.Assist=State.assist;
            completionCounts.Clear();
            if(State.stage>=2){completionCounts["STORY_ARCHIVE_START"]=1;completionCounts["STORY_BREACH"]=1;}
            if(State.stage>=3)completionCounts["STORY_AWAKEN"]=1;
            if(State.nodes==3)completionCounts["STORY_MAINTENANCE_ENTER"]=1;
            if(State.stage>=5)completionCounts["STORY_ARCHIVE_RETURN"]=1;
            if(State.stage>=6)completionCounts["STORY_IDENTITY_CONFIRM"]=1;
            if(State.stage==7){completionCounts["STORY_RESCUE"]=1;completionCounts["STORY_COMPLETE"]=1;}
            // Transient scenes always resume at a stable, interactive stage.
            if(State.stage==1)State.stage=0;
            if(State.stage==2)State.stage=3;
            ApplyPresentation();
            Vector3 feet=State.stage>=4?returnFeet:State.stage>=2?new Vector3(5.05f,.08f,7):workFeet;
            if(!Place(feet,Quaternion.identity)){ShowMainMenu();return;}
            if(State.stage==7){ShowEnding();return;}
            fade=0;Hud.Fade(0);Hud.Memory(false);Explore();Notice("已回到检查点。",3);
            if(State.stage==6 && !State.rescued)Notice("靠近回路，按交互键重新开始引导。已完成的段落保留。",6);
        }
        public void RetryCheckpoint()
        {
            if(checkpoint==null)return;
            Confirm("回到检查点","检查点之后尚未保存的进度将被重置。",ContinueGame);
        }
        public void ShowMainMenu()
        {
            StopAllCoroutines();cutsceneRunning=false;PowerActive=false;
            hall.SetActive(false);library.SetActive(true);fade=0;Hud.Fade(0);Hud.Memory(false);
            SetMode(ScreenMode.MainMenu);Hud.MainMenu(CanContinue);Touch();
        }
        public void RequestMainMenu()=>Confirm("返回主菜单","将保留最近检查点，可从主菜单继续。",ShowMainMenu);
        public void Pause()
        {
            if(Mode==ScreenMode.Pause || Mode==ScreenMode.MainMenu || Mode==ScreenMode.Ending)return;
            resumeMode=Mode;SetMode(ScreenMode.Pause);Hud.PauseMenu();
        }
        public void Resume()
        {
            Hud.HideModal();
            var previous=resumeMode;
            SetMode(previous);
            if(previous==ScreenMode.Dialogue)RenderLine();
            else if(previous==ScreenMode.Reader && readerRecord!=null)OpenRecord(readerRecord);
            else if(previous==ScreenMode.Reader && incidentReader)OpenIncident();
            else if(previous==ScreenMode.Cutscene){Hud.HideModal();Hud.ResumeCinematic();}
            else Explore();
        }
        void HandleEscape()
        {
            if(Mode==ScreenMode.Explore || Mode==ScreenMode.Cutscene || Mode==ScreenMode.Dialogue)Pause();
            else if(Mode==ScreenMode.Reader){readerRecord=null;incidentReader=false;Explore();}
            else if(Mode==ScreenMode.Pause)Resume();
            else if(Mode==ScreenMode.Settings || Mode==ScreenMode.Help || Mode==ScreenMode.Credits)BackFromUtility();
            else if(Mode==ScreenMode.Confirm)BackFromUtility();
        }
        void BackFromUtility()
        {
            player.Bindings?.Cancel();
            if(utilityReturn==ScreenMode.MainMenu)ShowMainMenu();else if(utilityReturn==ScreenMode.Ending)ShowEnding();else{SetMode(ScreenMode.Pause);Hud.PauseMenu();}
        }
        ScreenMode utilityReturn=ScreenMode.MainMenu;
        void OpenUtility(ScreenMode screen)
        {utilityReturn=Mode==ScreenMode.MainMenu?ScreenMode.MainMenu:ScreenMode.Pause;SetMode(screen);}
        public void ShowHelp(){OpenUtility(ScreenMode.Help);Hud.Help(BackFromUtility);}
        public void ShowSettings(){OpenUtility(ScreenMode.Settings);Hud.Settings(BackFromUtility);}
        public void ShowCredits(){OpenUtility(ScreenMode.Credits);Hud.Credits(BackFromUtility);}
        public void Confirm(string title,string message,Action accept)
        {
            utilityReturn=Mode==ScreenMode.MainMenu || Mode==ScreenMode.Ending?Mode:ScreenMode.Pause;
            SetMode(ScreenMode.Confirm);Hud.Confirmation(title,message,accept,BackFromUtility);
        }
        public void RequestExit()=>Confirm("退出游戏","最近检查点已保留。",()=>{
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        });
        public void SetVolume(float value){Volume=Mathf.Clamp01(value);AudioListener.volume=Volume;if(!Automated)PlayerPrefs.SetFloat("IMContest.Volume",Volume);}
        public void SetReducedFlash(bool value){ReduceFlash=value;if(!Automated)PlayerPrefs.SetInt("IMContest.ReducedFlash",value?1:0);}
        public void SetAssist(bool value){State.assist=value;Power.Assist=value;Notice(value?"辅助引导已开启。":"辅助引导已关闭。",3);}
        public void ChangeQuality(string value){var quality=player.GetComponent<ArcaneArchive.Visuals.MilanQualityOptions>();if(quality)quality.Select(value);}
        public void ChangeSensitivity(float value)=>player.SetMouseSensitivityMultiplier(value);
        void Explore(){readerRecord=null;incidentReader=false;Hud.HideModal();SetMode(ScreenMode.Explore);player.WaitForReleasedControls();Touch();}
        void Touch(){lastActionAt=Time.unscaledTime;}
        public void Notice(string message,float seconds=3){LastNotice=message;noticeUntil=Time.unscaledTime+seconds;}
        bool Place(Vector3 feet,Quaternion rotation)
        {
            Physics.SyncTransforms();
            if(player.TryReturnToSpawn(feet,rotation))return true;
            Notice("目的位置被阻挡，已保留原检查点。请重试。",8);return false;
        }
        void RecoverFall()
        {
            Vector3 feet=hall.activeSelf?new Vector3(160,.08f,-13.3f):(State.stage>=4?returnFeet:workFeet);
            Place(feet,Quaternion.identity);Notice("已返回安全位置。",3);
        }
        void WriteCheckpoint()
        {
            var copy=State.Copy();
            if(ContestCheckpointStore.TryWrite(SavePath,copy,out var error))checkpoint=copy;
            else Notice(error,8);
        }
        void Complete(string id)
        {
            if(completionCounts.ContainsKey(id))throw new InvalidOperationException("Story completion repeated: "+id);
            completionCounts[id]=1;Debug.Log("[Contest] Complete "+id);
        }
        public int CompletionCount(string id)=>completionCounts.TryGetValue(id,out int value)?value:0;
        public ContestWorldTarget FindTarget()
        {
            var ray=view.ViewportPointToRay(new Vector3(.5f,.5f,0));
            foreach(var hit in Physics.RaycastAll(ray,3.2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
            {
                if(hit.collider.transform.IsChildOf(player.transform))continue;
                var candidate=hit.collider.GetComponentInParent<ContestWorldTarget>();
                if(candidate && candidate.isActiveAndEnabled && Vector3.Distance(player.transform.position,candidate.Point)<=2.3f)return candidate;
                if(!hit.collider.isTrigger)return null;
            }
            return null;
        }
        public void Activate(ContestWorldTarget item)
        {
            if(Mode!=ScreenMode.Explore || !item || !item.gameObject.activeInHierarchy || FindTarget()!=item)return;
            Touch();LastInteraction=item.id;InteractionCount++;Sound(0);string id=item.id;
            if(id.StartsWith("ARCHIVE_RECORD_SLOT_"))
            {
                if(State.stage!=0){Notice("先完成当前目标，再转移这批遗赠。",3);return;}
                var record=content.records.FirstOrDefault(r=>r.scene_anchor==id);if(record!=null)OpenRecord(record);return;
            }
            if(id=="ARCHIVE_WORK_BADGE")
            {TalkText(State.stage==0?"工作牌":"翻面的工作牌",State.stage==0?"伊莱恩维尔  封忆师\n本批遗赠核验与封存。":"工作牌已经翻面。记忆里的名字仍然没有答案。",Explore);return;}
            if(id=="ARCHIVE_TIE_CORD" && State.stage==0)
            {if(!State.sealedBatch){State.sealedBatch=true;ApplyPresentation();Notice("蓝绳已系好，封档结构稳定。",4);Sound(1);}return;}
            if(id=="ARCHIVE_TO_MAINTENANCE")
            {if(State.stage<3){Notice("先完成本批档案核验。",3);return;}if(State.stage>=4){Notice("维护已完成，先取回这里的记录。",3);return;}StartCoroutine(Transfer(true));return;}
            if(id=="MAINTENANCE_TO_ARCHIVE")
            {if(State.nodes<3){Notice("还有维护节点未稳定。",3);return;}StartCoroutine(Transfer(false));return;}
            if(id.StartsWith("MAINT_NODE_"))
            {
                int n=int.Parse(id.Substring(id.Length-1));
                if(State.nodes>=n){Notice("这处节点已经稳定。",2);return;}
                if(State.nodes!=n-1){Notice("先稳定前一个节点。",3);return;}
                if(State.stage!=3)return;
                State.nodes=n;ApplyNodes();Sound(1);Notice(new[]{"导流恢复  1/3","档案供能恢复  2/3","局部回路稳定  3/3"}[n-1],4);
                if(n==3){Complete("STORY_MAINTENANCE_ENTER");Talk(new[]{"D010","D011","D012"},Explore);}return;
            }
            if(id.StartsWith("REVEAL_"))
            {
                if(State.stage==6 && State.rescued && id=="REVEAL_CORD"){PickupBatch();return;}
                if(State.stage!=4){Notice("先按当前目标行动。",2);return;}
                int bit=id=="REVEAL_CORD"?1:id=="REVEAL_BADGE"?2:id=="REVEAL_WOUND"?4:0;
                if(bit==0)return;
                State.clues|=bit;
                Talk(new[]{bit==1?"D013":bit==2?"D014":"D015"},()=>{
                    if(State.clues==7){Talk(new[]{"D016"},()=>{Complete("STORY_ARCHIVE_RETURN");State.stage=5;ApplyPresentation();Explore();});}
                    else Explore();
                });return;
            }
            if(id=="ARCHIVE_INCIDENT_RECORD" && State.stage==5){OpenIncident();return;}
            if(id=="RESCUE_CONDUIT" && State.stage==6)
            {
                if(State.rescued){Notice("回路已稳定。取走遇难者怀中的本批档案。",4);return;}
                if(!PowerActive){Power.Reset();PowerActive=true;Notice("按住引导，松开收束。先完成一次练习。",6);}return;
            }
            if(id=="ARCHIVE_TRANSFER_CASE")
            {if(State.stage==6 && State.batchLocation==2)FinishTransfer();else Notice("先稳住回路，再把本批档案带来。",4);return;}
        }
        Record readerRecord;bool incidentReader;
        void OpenRecord(Record record)
        {
            readerRecord=record;SetMode(ScreenMode.Reader);int index=Array.IndexOf(content.records,record),bit=1<<index;
            bool verified=(State.verifiedMask&bit)!=0;
            Hud.Reader(record.title,record.body+"\n\n核验摘要\n"+record.summary,verified?"已核验  合上":"核验完成",()=>{
                State.verifiedMask|=bit;Sound(1);Explore();Notice("档案已核验 "+CountBits(State.verifiedMask)+"/3",3);
            },Explore,State.verifiedMask==7 && State.sealedBatch && index==2?"完成本批交接":null,()=>{
                if(State.verifiedMask==7 && State.sealedBatch){readerRecord=null;Complete("STORY_ARCHIVE_START");State.stage=1;Hud.HideModal();Talk(new[]{"D002"},()=>StartCoroutine(Breach()));}
            });
        }
        void OpenIncident()
        {
            incidentReader=true;SetMode(ScreenMode.Reader);
            Hud.Reader(content.incident_record.title,content.incident_record.body,"确认记录",()=>{
                State.incidentRead=true;ApplyPresentation();Explore();Notice("左掌传来同样的共鸣。按交互键对照掌纹。",6);
            },Explore,null,null);
        }
        void ConfirmPalm()
        {
            if(State.stage!=5 || !State.incidentRead)return;
            StartCoroutine(IdentityFlash());
        }
        IEnumerator Breach()
        {
            cutsceneRunning=true;SetMode(ScreenMode.Cutscene);Hud.HideModal();
            Hud.Cinematic(content.dialogue.Single(l=>l.id=="D003").text,true);Sound(2);
            yield return StoryWait(2.5f);
            Hud.Cinematic("你抱紧了刚刚封好的档案。",true);Sound(3);
            cameraPivot.localPosition=restingEye-Vector3.up*.25f;
            yield return StoryWait(2.5f);
            Hud.Cinematic("",true);yield return FadeTo(1,1.1f);Sound(3);
            State.batchLocation=1;State.stage=2;ApplyPresentation();
            yield return StoryWait(3);
            library.SetActive(true);hall.SetActive(false);Place(new Vector3(5.05f,.08f,7),Quaternion.identity);
            cameraPivot.localPosition=restingEye-Vector3.up*.55f;
            yield return FadeTo(0,1.5f);cutsceneRunning=false;
            Talk(new[]{"D004","D005","D006","D007","D008"},()=>StartCoroutine(StandUpAndBegin()));
        }
        IEnumerator StandUpAndBegin()
        {
            cutsceneRunning=true;SetMode(ScreenMode.Cutscene);Hud.HideModal();Vector3 low=cameraPivot.localPosition;float elapsed=0;
            while(elapsed<.75f)
            {if(!Paused)elapsed+=Time.unscaledDeltaTime;cameraPivot.localPosition=Vector3.Lerp(low,restingEye,Mathf.SmoothStep(0,1,elapsed/.75f));yield return null;}
            cameraPivot.localPosition=restingEye;cutsceneRunning=false;
            Complete("STORY_BREACH");Complete("STORY_AWAKEN");State.stage=3;WriteCheckpoint();Explore();
        }
        IEnumerator IdentityFlash()
        {
            cutsceneRunning=true;SetMode(ScreenMode.Cutscene);Hud.HideModal();palm.SetActive(true);Sound(2);
            Hud.Memory(true);Hud.Cinematic("残存的记忆\n封印内侧，同样的三缺环纹。",true);
            yield return StoryWait(4);Hud.Memory(false);cutsceneRunning=false;
            Talk(new[]{"D017","D018"},()=>{
                Complete("STORY_IDENTITY_CONFIRM");State.identityKnown=true;State.stage=6;WriteCheckpoint();ApplyPresentation();
                Talk(new[]{"D019","D020","D021"},Explore);
            });
        }
        IEnumerator Transfer(bool enter)
        {
            cutsceneRunning=true;SetMode(ScreenMode.Cutscene);Hud.HideModal();Hud.Cinematic("",true);yield return FadeTo(1,.45f);
            GameObject destination=enter?hall:library,origin=enter?library:hall;destination.SetActive(true);Physics.SyncTransforms();
            bool success=Place(enter?new Vector3(160,.08f,-13.3f):returnFeet,Quaternion.identity);
            if(success)
            {
                origin.SetActive(false);
                if(!enter && State.stage==3){State.stage=4;ApplyPresentation();WriteCheckpoint();}
            }
            else destination.SetActive(false);
            yield return FadeTo(0,.45f);cutsceneRunning=false;
            if(success && enter && State.nodes==0)Talk(new[]{"D009"},Explore);else Explore();
        }
        IEnumerator StoryWait(float seconds)
        {while(seconds>0){if(!Paused)seconds-=Time.unscaledDeltaTime;yield return null;}}
        IEnumerator FadeTo(float value,float seconds)
        {
            float start=fade,elapsed=0;
            while(elapsed<seconds){if(!Paused)elapsed+=Time.unscaledDeltaTime;fade=Mathf.Lerp(start,value,Mathf.Clamp01(elapsed/seconds));Hud.Fade(fade);yield return null;}
            fade=value;Hud.Fade(fade);
        }
        void CompletePulse()
        {
            Sound(1);
            if(!State.tutorialDone){State.tutorialDone=true;Notice("练习完成。现在完成三段收束，记得松开再按。",5);return;}
            State.pulses++;Notice("收束完成 "+State.pulses+"/3",4);
            if(State.pulses==3){State.rescued=true;PowerActive=false;ApplyPresentation();Notice("回路稳定了。取走遇难者怀中的封档包。",7);}
            WriteCheckpoint();
        }
        void PickupBatch()
        {
            if(State.batchLocation>=2)return;
            State.batchLocation=2;ApplyPresentation();WriteCheckpoint();Sound(1);Notice("已收齐本批遗赠。将它放入安全转移箱。",6);
        }
        void FinishTransfer()
        {
            if(State.transferred)return;
            State.transferred=true;State.batchLocation=3;State.stage=7;PowerActive=false;
            Complete("STORY_RESCUE");ApplyPresentation();WriteCheckpoint();Sound(1);
            Talk(new[]{"D022","D023","D024"},()=>{Complete("STORY_COMPLETE");ShowEnding();});
        }
        void ShowEnding(){Hud.Fade(0);SetMode(ScreenMode.Ending);Hud.Ending();}
        public void Talk(IEnumerable<string> ids,Action done)
        {conversation=ids.Select(id=>content.dialogue.Single(l=>l.id==id)).ToList();lineIndex=0;conversationDone=done;RenderLine();}
        void TalkText(string speaker,string value,Action done)
        {conversation=new List<Line>{new Line{speaker=speaker,text=value}};lineIndex=0;conversationDone=done;RenderLine();}
        void RenderLine()
        {
            if(conversation==null || lineIndex>=conversation.Count)return;
            SetMode(ScreenMode.Dialogue);var line=conversation[lineIndex];
            Hud.Dialogue(line.speaker.Replace("玩家内心","内心"),line.text,AdvanceDialogue);
        }
        public void AdvanceDialogue()
        {
            if(Mode!=ScreenMode.Dialogue || !CanAcceptUi)return;
            lineIndex++;if(lineIndex<conversation.Count)RenderLine();
            else{Hud.HideModal();var done=conversationDone;conversationDone=null;done?.Invoke();}
        }
        void ApplyNodes()
        {
            foreach(var a in hall.GetComponentsInChildren<MaintenanceArtAnchor>(true))
                if(a.anchorId.StartsWith("MAINT_NODE_"))a.SetStable(int.Parse(a.anchorId.Substring(a.anchorId.Length-1))<=State.nodes);
        }
        void ApplyPresentation()
        {
            if(!workstation)return;
            bool after=State.stage>=2;
            workstation.SetDisturbed(after);workstation.SetArchiveSealed(State.sealedBatch);
            foreach(var group in new[]{workstation.intactProps,workstation.disturbedProps})
            {
                var slot=group.transform.Find("ArchiveSlot_03");if(slot)slot.gameObject.SetActive(State.batchLocation==0);
                for(int n=1;n<=2;n++){var s=group.transform.Find("ArchiveSlot_0"+n);if(s)s.gameObject.SetActive(State.batchLocation<2);}
                var badge=group.transform.Find("ElaineBadge");if(badge)badge.gameObject.SetActive(!after);
            }
            body.SetActive(after);bodyEvidence.SetActive(State.stage>=4);
            incidentObject.SetActive(State.stage>=5);
            sealedBundle.SetActive(after && State.batchLocation==1);
            transferCase.SetActive(true);conduit.SetActive(after);
            palm.SetActive(State.incidentRead && State.stage>=5 && State.stage<7);
            foreach(var pair in lightDefaults)if(pair.Key)pair.Key.intensity=pair.Value*(after?.72f:1);
            var lid=transferCase.transform.Find("Lid");if(lid)lid.localRotation=Quaternion.Euler(State.transferred?0:-24,0,0);
            if(State.stage==0 || State.stage>=4){library.SetActive(true);hall.SetActive(false);}
            ApplyNodes();
        }
        void UpdateConduit()
        {
            if(sealRenderers==null)return;
            var block=new MaterialPropertyBlock();
            Color c=State.rescued?new Color(.25f,.72f,.60f):State.identityKnown?new Color(.12f,.23f,.60f):new Color(.55f,.42f,.18f);
            float pulse=State.rescued?.45f:.55f+.15f*Mathf.Sin(Time.time*3);
            foreach(var renderer in sealRenderers)if(renderer){renderer.GetPropertyBlock(block);block.SetColor("_EmissionColor",c*pulse);renderer.SetPropertyBlock(block);}
        }
        public string Objective()
        {
            if(!Started)return "";
            switch(State.stage)
            {
                case 0:return State.verifiedMask==7 && State.sealedBatch?"返回三号档案，完成本批交接":"核验遗赠 "+CountBits(State.verifiedMask)+"/3  ·  "+(State.sealedBatch?"蓝绳已封档":"系好蓝色封档绳");
                case 1:return "警报";
                case 2:return "听见救援的声音";
                case 3:return State.nodes==3?"回到出口，返回档案馆":hall.activeSelf?"稳定节点 "+State.nodes+"/3":"从侧门进入下层维护厅";
                case 4:return "查看桌后，核对三项证据 "+CountBits(State.clues)+"/3";
                case 5:return State.incidentRead?"对照左掌的共鸣纹":"查看遇难者旁的事故记录";
                case 6:return State.transferred?"遗赠已安全转移":State.batchLocation==2?"将本批遗赠放入安全转移箱":State.rescued?"取走遇难者怀中的封档包":"引导力量，保护遗赠 "+State.pulses+"/3";
                default:return "本批遗赠已安全转移";
            }
        }
        string DirectionHint()
        {
            if(State.stage==0)return "三份档案在桌面。蓝绳垂在桌沿左侧。";
            if(State.stage==3)return hall.activeSelf?(State.nodes==3?"出口在来时的石门下方。":"沿外圈寻找 NODE "+(State.nodes+1)+"。中央不需要进入。") : "侧门在工作区前方，门上有 LOWER MAINTENANCE 标识。";
            if(State.stage==4)return "绕过工作台，查看桌后的覆布与三个证据点。";
            if(State.stage==5)return "事故记录在遇难者旁的小册上。";
            return "回路和安全转移箱在侧门与工作区之间。";
        }
        public string Prompt(string id)
        {
            string key="["+InteractKey+"] ";
            if(id.StartsWith("ARCHIVE_RECORD_SLOT_"))return key+"查看档案 "+id[id.Length-1];
            if(id=="ARCHIVE_TIE_CORD")return key+"系好蓝色封档绳";
            if(id=="ARCHIVE_WORK_BADGE")return key+(State.stage==0?"查看工作牌":"查看翻面的工作牌");
            if(id=="ARCHIVE_TO_MAINTENANCE")return key+"前往下层维护厅";
            if(id=="MAINTENANCE_TO_ARCHIVE")return key+"返回档案馆";
            if(id.StartsWith("MAINT_NODE_"))return key+"稳定节点 "+id[id.Length-1];
            if(id=="REVEAL_CORD")return key+(State.rescued?"取走本批封档包":"查看怀中的绳结");
            if(id=="REVEAL_BADGE")return key+"观察工作牌";
            if(id=="REVEAL_WOUND")return key+"观察受伤的衣料";
            if(id=="ARCHIVE_INCIDENT_RECORD")return key+"查看事故抄件";
            if(id=="RESCUE_CONDUIT")return PowerActive?"按住 "+InteractKey+" 引导  ·  松开收束":key+"引导回路";
            if(id=="ARCHIVE_TRANSFER_CASE")return key+"交付本批遗赠";
            return "";
        }
        static int CountBits(int mask){int n=0;while(mask!=0){n+=mask&1;mask>>=1;}return n;}
        public void Sound(int kind){if(effects)effects.PlayOneShot(GetClip(kind),kind==3?.35f:.45f);}
        AudioClip GetClip(int kind){if(!clips.TryGetValue(kind,out var clip)){clip=MakeClip(kind,kind==3?.6f:.32f);clips[kind]=clip;}return clip;}
        AudioClip MakeClip(int kind,float seconds)
        {
            const int rate=22050;int n=Mathf.CeilToInt(seconds*rate);var samples=new float[n];var random=new System.Random(1729+kind);
            for(int i=0;i<n;i++)
            {
                float t=(float)i/rate,envelope=kind==4?1:Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/seconds))*Mathf.Exp(-t*5);
                float v=kind==3?(float)(random.NextDouble()*2-1)*Mathf.Exp(-t*13):
                    kind==4?.10f*(Mathf.Sin(t*2*Mathf.PI*110)+.3f*Mathf.Sin(t*2*Mathf.PI*165)):
                    Mathf.Sin(t*2*Mathf.PI*(kind==2?164:kind==1?660:440))+.25f*Mathf.Sin(t*2*Mathf.PI*(kind==2?175:880));
                samples[i]=v*envelope*.18f;
            }
            var clip=AudioClip.Create("IMMonster_Synth_"+kind,n,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}
