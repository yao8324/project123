using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ArcaneArchive.Contest
{
    public sealed class ContestHud : MonoBehaviour
    {
        ContestStoryGame game;
        Font font;
        RectTransform root,modal,world,cinematic,memory,powerRoot;
        Image fadeImage,strengthFill,safeFill,progressFill;
        Text objective,hint,notice,cinematicText,powerLabel,powerHint;
        ContestReticle reticle;
        readonly Dictionary<string,Button> buttons=new Dictionary<string,Button>();
        readonly Dictionary<string,Text> bindingLabels=new Dictionary<string,Text>();
        readonly Color paper=new Color(.94f,.89f,.78f),gold=new Color(.71f,.57f,.33f),ink=new Color(.055f,.065f,.065f,.96f),muted=new Color(.64f,.66f,.61f);
        public Button FindButton(string id)=>buttons.TryGetValue(id,out var b)?b:null;
        public void Initialize(ContestStoryGame owner,Font face)
        {
            game=owner;font=face;
            root=Rect("ContestInterface",transform,Vector2.zero,Vector2.zero);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            if(!FindFirstObjectByType<EventSystem>())
            {var e=new GameObject("ContestEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));e.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            world=Rect("StoryHud",root,Vector2.zero,Vector2.zero);Stretch(world);
            var plaque=Panel("ObjectivePanel",world,new Vector2(-270,285),new Vector2(660,82),new Color(.03f,.045f,.04f,.84f));
            Label(plaque,"终境守藏院  /  遗赠",new Vector2(0,22),new Vector2(620,22),13,gold,TextAnchor.MiddleLeft);
            objective=Label(plaque,"",new Vector2(0,-10),new Vector2(620,38),21,paper,TextAnchor.MiddleLeft);
            reticle=Rect("InteractionReticle",world,Vector2.zero,new Vector2(40,40)).gameObject.AddComponent<ContestReticle>();
            hint=Label(world,"",new Vector2(0,-45),new Vector2(900,58),18,paper);
            notice=Label(world,"",new Vector2(0,-295),new Vector2(1040,75),21,paper);
            var escapePanel=Panel("PauseHint",world,new Vector2(510,312),new Vector2(230,31),new Color(.02f,.03f,.03f,.75f));
            Label(escapePanel,"Esc  暂停 / 操作",Vector2.zero,new Vector2(210,28),14,paper,TextAnchor.MiddleRight);
            powerRoot=Panel("PowerFeedback",world,new Vector2(0,-100),new Vector2(650,130),new Color(.025f,.04f,.055f,.94f));
            powerLabel=Label(powerRoot,"引导强度",new Vector2(0,42),new Vector2(600,28),20,paper);
            var rail=Panel("Rail",powerRoot,new Vector2(0,9),new Vector2(580,18),new Color(.14f,.17f,.20f));
            safeFill=Panel("SafeRange",rail,new Vector2(14.5f,0),new Vector2(203,18),new Color(.13f,.45f,.40f)).GetComponent<Image>();
            strengthFill=Panel("Strength",rail,Vector2.zero,new Vector2(580,5),gold).GetComponent<Image>();
            strengthFill.rectTransform.anchorMin=new Vector2(0,.5f);strengthFill.rectTransform.anchorMax=new Vector2(0,.5f);strengthFill.rectTransform.pivot=new Vector2(0,.5f);strengthFill.rectTransform.anchoredPosition=Vector2.zero;
            var progressRail=Panel("PulseProgressRail",powerRoot,new Vector2(0,-15),new Vector2(580,4),new Color(.18f,.22f,.21f));
            progressFill=Panel("PulseProgress",progressRail,Vector2.zero,new Vector2(580,4),new Color(.35f,.70f,.60f)).GetComponent<Image>();
            progressFill.rectTransform.anchorMin=progressFill.rectTransform.anchorMax=progressFill.rectTransform.pivot=new Vector2(0,.5f);progressFill.rectTransform.anchoredPosition=Vector2.zero;
            powerHint=Label(powerRoot,"",new Vector2(0,-44),new Vector2(600,30),16,muted);
            powerRoot.gameObject.SetActive(false);
            cinematic=Panel("CinematicSubtitle",root,new Vector2(0,-255),new Vector2(1060,128),new Color(.02f,.025f,.027f,.9f));
            cinematicText=Label(cinematic,"",Vector2.zero,new Vector2(990,112),25,paper);cinematic.gameObject.SetActive(false);
            memory=Panel("MemoryImage",root,Vector2.zero,new Vector2(1280,720),new Color(.015f,.025f,.06f,.96f));Stretch(memory);
            for(int ring=0;ring<3;ring++)
            {
                float radius=125+ring*50;
                for(int i=0;i<30;i++)
                {if(i%10<2)continue;float a=i*12*Mathf.Deg2Rad;var s=Panel("SealEcho",memory,new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius+35),new Vector2(13,2),new Color(.2f,.3f,.55f,.65f));s.localRotation=Quaternion.Euler(0,0,i*12+90);}
            }
            Label(memory,"封  印  深  处",new Vector2(0,33),new Vector2(500,70),29,gold);
            Label(memory,"这一次，记忆来自我自己。",new Vector2(0,-155),new Vector2(820,45),22,paper);
            memory.gameObject.SetActive(false);
            modal=Rect("ModalRoot",root,Vector2.zero,Vector2.zero);Stretch(modal);
            fadeImage=Panel("Fade",root,Vector2.zero,Vector2.zero,Color.black).GetComponent<Image>();Stretch(fadeImage.rectTransform);fadeImage.raycastTarget=false;Fade(0);
        }
        void Update()
        {
            if(game && game.player && game.player.Bindings!=null)
                foreach(var pair in bindingLabels)if(pair.Value)pair.Value.text=BindingName(pair.Key)+"  "+game.player.Bindings.Display(pair.Key);
        }
        public void Refresh(ContestStoryGame.ScreenMode mode,bool started,string currentObjective,string currentHint,string currentNotice,bool powerActive,ContestPowerChallenge challenge,ContestStoryState state)
        {
            world.gameObject.SetActive(started && mode==ContestStoryGame.ScreenMode.Explore);
            var feedback=game.CurrentInteractionFeedback;
            reticle.Present(feedback.Available,feedback.HasTarget && !feedback.Available);
            objective.text=currentObjective;hint.text=currentHint;notice.text=currentNotice;
            powerRoot.gameObject.SetActive(powerActive);
            if(powerActive)
            {
                float low=state.assist?20:35,high=state.assist?85:70;
                safeFill.rectTransform.sizeDelta=new Vector2((high-low)*5.8f,18);safeFill.rectTransform.anchoredPosition=new Vector2((low+high)*2.9f-290,0);
                strengthFill.rectTransform.sizeDelta=new Vector2(challenge.Strength*5.8f,5);
                progressFill.rectTransform.sizeDelta=new Vector2(Mathf.Clamp01(challenge.Progress/1.2f)*580,4);
                powerLabel.text=(state.tutorialDone?"收束 "+state.pulses+" / 3":"先练习一次")+"    强度 "+Mathf.RoundToInt(challenge.Strength);
                powerHint.text=challenge.NeedsRelease?"先松开交互键，再开始下一段":challenge.Failures>=3&&!state.assist?"Esc → 设置，可开启辅助引导":"让强度停留在绿色安全区  ·  按住上升，松开下降";
            }
        }
        void Clear()
        {
            buttons.Clear();bindingLabels.Clear();
            for(int i=modal.childCount-1;i>=0;i--){var child=modal.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            modal.gameObject.SetActive(true);cinematic.gameObject.SetActive(false);
        }
        public void HideModal(){modal.gameObject.SetActive(false);cinematic.gameObject.SetActive(false);}
        RectTransform Backdrop(string title,string subtitle=null,float width=750,float height=530)
        {
            Clear();Panel("Shade",modal,Vector2.zero,new Vector2(1280,720),new Color(0,0,0,.53f));
            var p=Panel("ReadingPanel",modal,Vector2.zero,new Vector2(width,height),ink);
            Panel("TopRule",p,new Vector2(0,height*.5f-2),new Vector2(width,3),gold);
            Label(p,title,new Vector2(0,height*.5f-46),new Vector2(width-70,58),29,paper,TextAnchor.MiddleLeft);
            if(subtitle!=null)Label(p,subtitle,new Vector2(0,height*.5f-91),new Vector2(width-70,35),16,gold,TextAnchor.MiddleLeft);
            return p;
        }
        public void MainMenu(bool resume)
        {
            Clear();var p=Panel("MainPanel",modal,new Vector2(-315,0),new Vector2(540,720),new Color(.02f,.035f,.04f,.95f));
            Label(p,"IM MONSTER",new Vector2(0,239),new Vector2(460,86),40,gold,TextAnchor.MiddleLeft);
            Label(p,"我是怪物",new Vector2(0,170),new Vector2(460,70),44,paper,TextAnchor.MiddleLeft);
            Label(p,"遗赠",new Vector2(0,111),new Vector2(460,35),22,muted,TextAnchor.MiddleLeft);
            Label(p,"一段记忆，让你相信自己是另一个人。",new Vector2(0,58),new Vector2(460,50),18,paper,TextAnchor.MiddleLeft);
            float y=0;Button(p,"NewGame","开始游戏",new Vector2(0,y),()=>game.NewGame());
            if(resume){y-=52;Button(p,"Continue","继续游戏",new Vector2(0,y),game.ContinueGame);}
            y-=52;Button(p,"Help","操作说明",new Vector2(0,y),game.ShowHelp);
            y-=52;Button(p,"Settings","设置",new Vector2(0,y),game.ShowSettings);
            y-=52;Button(p,"Credits","制作与素材鸣谢",new Vector2(0,y),game.ShowCredits);
            y-=52;Button(p,"Quit","退出游戏",new Vector2(0,y),game.RequestExit);
            Label(modal,"单人叙事探索  ·  鼠标与键盘\n所有关键线索都在游戏中。",new Vector2(320,-280),new Vector2(520,70),18,paper,TextAnchor.MiddleRight);
            if(!string.IsNullOrEmpty(game.StartupNotice))Label(modal,game.StartupNotice,new Vector2(320,-193),new Vector2(520,70),18,paper,TextAnchor.MiddleRight);
        }
        public void PauseMenu()
        {
            var p=Backdrop("暂停","检查点会保留已完成的关键进度。",640,540);
            Button(p,"Resume","继续",new Vector2(0,95),game.Resume);
            Button(p,"Retry","回到检查点",new Vector2(0,40),game.RetryCheckpoint);
            Button(p,"Help","操作说明",new Vector2(0,-15),game.ShowHelp);
            Button(p,"Settings","设置",new Vector2(0,-70),game.ShowSettings);
            Button(p,"MainMenu","返回主菜单",new Vector2(0,-125),game.RequestMainMenu);
            Button(p,"Quit","退出游戏",new Vector2(0,-180),game.RequestExit);
        }
        public void Reader(string title,string body,string primary,Action accept,Action close,string extra,Action extraAction)
        {
            var p=Backdrop(title,"记忆档案馆  /  遗赠核验",900,590);
            ScrollText(p,body,new Vector2(0,15),new Vector2(810,345),23);
            Button(p,"ReaderConfirm",primary,new Vector2(165,-222),accept,new Vector2(300,47));
            Button(p,"ReaderClose","暂时合上",new Vector2(-165,-222),close,new Vector2(300,47));
            if(extra!=null)Button(p,"Handover",extra,new Vector2(0,-276),extraAction,new Vector2(630,42));
        }
        public void Dialogue(string speaker,string body,Action next)
        {
            Clear();var p=Panel("DialoguePanel",modal,new Vector2(0,-220),new Vector2(1110,235),new Color(.025f,.033f,.035f,.95f));
            Label(p,speaker,new Vector2(-80,77),new Vector2(890,30),18,gold,TextAnchor.MiddleLeft);
            Label(p,body,new Vector2(-40,8),new Vector2(970,105),27,paper,TextAnchor.MiddleLeft);
            Button(p,"DialogueNext","继续  /  Space",new Vector2(390,-78),next,new Vector2(250,42));
        }
        public void Cinematic(string text,bool active){cinematicText.text=text;cinematic.gameObject.SetActive(active);cinematic.SetAsLastSibling();fadeImage.rectTransform.SetAsLastSibling();}
        public void ResumeCinematic(){cinematic.gameObject.SetActive(!string.IsNullOrEmpty(cinematicText.text));}
        public void Memory(bool visible){memory.gameObject.SetActive(visible);if(visible){memory.SetAsLastSibling();cinematic.SetAsLastSibling();modal.SetAsLastSibling();fadeImage.rectTransform.SetAsLastSibling();}}
        public void Fade(float opacity){var c=fadeImage.color;c.a=opacity;fadeImage.color=c;}
        public void Confirmation(string title,string message,Action yes,Action no)
        {
            var p=Backdrop(title,null,680,320);Label(p,message,new Vector2(0,10),new Vector2(600,100),23,paper);
            Button(p,"ConfirmYes","确认",new Vector2(155,-100),yes,new Vector2(270,45));Button(p,"ConfirmNo","取消",new Vector2(-155,-100),no,new Vector2(270,45));
        }
        public void Help(Action back)
        {
            var p=Backdrop("操作说明","可以随时暂停，关闭强闪烁，或开启辅助引导。",920,600);
            var b=game.player.Bindings;
            string body="移动  "+b.Display("Forward")+" / "+b.Display("Left")+" / "+b.Display("Backward")+" / "+b.Display("Right")+"　鼠标环顾\n冲刺  "+b.Display("Sprint")+"　跳跃  "+b.Display("Jump")+"　蹲下  "+b.Display("Crouch")+"\n交互  "+b.Display("Interact")+"　暂停  "+b.Display("Menu")+"\n\n靠近物件并用准星对准。查阅档案后点击“核验完成”。\n三份记录可任意顺序查阅，蓝绳垂在桌面左侧。\n观察环境变化，按左上角目标继续。\n\n力量引导：按住交互键提高强度，松开让它下降。\n在绿色安全区累积进度，过强只重试当前段。\n每段结束后先松开，再开始下一段。\n\n四个关键检查点自动保存。暂停菜单可回到检查点。\n再次启动可继续。故事路线没有必须击败的敌人。";
            ScrollText(p,body,new Vector2(0,-5),new Vector2(830,370),21);Button(p,"Back","返回",new Vector2(0,-250),back);
        }
        public void Settings(Action back)
        {
            var p=Backdrop("设置","按键提示读取当前绑定，Esc可以取消换绑。",960,650);
            Label(p,"主音量",new Vector2(-325,190),new Vector2(180,35),20,paper,TextAnchor.MiddleLeft);
            Slider(p,new Vector2(30,190),new Vector2(500,25),game.Volume,game.SetVolume,0,1);
            Label(p,"音效音量",new Vector2(-325,149),new Vector2(180,35),20,paper,TextAnchor.MiddleLeft);
            Slider(p,new Vector2(30,149),new Vector2(500,25),game.EffectsVolume,game.SetEffectsVolume,0,1);
            Label(p,"环境音量",new Vector2(-325,108),new Vector2(180,35),20,paper,TextAnchor.MiddleLeft);
            Slider(p,new Vector2(30,108),new Vector2(500,25),game.EnvironmentVolume,game.SetEnvironmentVolume,0,1);
            Label(p,"鼠标灵敏度",new Vector2(-325,67),new Vector2(180,35),20,paper,TextAnchor.MiddleLeft);
            Slider(p,new Vector2(30,67),new Vector2(500,25),game.player.MouseSensitivityMultiplier,game.ChangeSensitivity,.25f,3);
            Toggle(p,"减少强闪烁与镜头晃动",new Vector2(-190,20),game.ReduceFlash,game.SetReducedFlash);
            Toggle(p,"辅助引导  扩大力量安全区",new Vector2(200,20),game.State.assist,game.SetAssist);
            Button(p,"Balanced","均衡画质",new Vector2(-185,-27),()=>game.ChangeQuality("Balanced"),new Vector2(300,38));
            Button(p,"Ultra","高画质",new Vector2(185,-27),()=>game.ChangeQuality("Ultra"),new Vector2(300,38));
            var ids=new[]{"Forward","Backward","Left","Right","Sprint","Jump","Crouch","Interact","Menu","Relock"};
            for(int i=0;i<ids.Length;i++)
            {
                string id=ids[i];var button=Button(p,"Bind"+id,BindingName(id)+"  "+game.player.Bindings.Display(id),new Vector2(i%2==0?-210:210,-78-i/2*39),()=>game.player.Bindings.Begin(id),new Vector2(390,36));
                bindingLabels[id]=button.GetComponentInChildren<Text>();
            }
            Button(p,"Back","返回",new Vector2(0,-290),back);
        }
        public void Credits(Action back)
        {
            var p=Backdrop("制作与素材鸣谢","IM MONSTER  我是怪物  /  比赛候选",920,590);
            string body="制作  IM MONSTER项目组\n\n本作沿用团队图书馆与维护厅资源。\n开发过程中使用AI辅助策划、代码与部分美术制作。\n完整参与范围及素材来源见随包说明。\n\n声音  CC0素材录音与声音设计\nStarNinjas / laleksic / rubberduck / LEGIT Audio / JaggedStone\n字体  Noto Sans CJK SC\n字体许可  SIL Open Font License 1.1\n\n感谢游玩。";
            ScrollText(p,body,new Vector2(0,-4),new Vector2(830,360),21);Button(p,"Back","返回",new Vector2(0,-245),back);
        }
        public void Ending()
        {
            var p=Backdrop("我是怪物","本批遗赠已安全转移",820,550);
            Label(p,"你已经知道自己是谁。\n\n这一次，你约束了力量，\n让别人的记忆留了下来。\n\n远处，撤离的脚步仍在继续。",new Vector2(0,25),new Vector2(720,240),26,paper);
            Button(p,"Restart","重新开始",new Vector2(-210,-185),()=>game.NewGame(),new Vector2(200,46));
            Button(p,"MainMenu","返回主菜单",new Vector2(0,-185),game.ShowMainMenu,new Vector2(200,46));
            Button(p,"Quit","退出游戏",new Vector2(210,-185),game.RequestExit,new Vector2(200,46));
        }
        Button Button(Transform parent,string id,string title,Vector2 position,Action action,Vector2? size=null)
        {
            var r=Panel("Button_"+id,parent,position,size??new Vector2(430,44),new Color(.11f,.145f,.14f));
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.3f,1.22f,.94f);colors.pressedColor=new Color(.65f,.8f,.75f);colors.selectedColor=colors.highlightedColor;button.colors=colors;
            var label=Label(r,title,Vector2.zero,(size??new Vector2(430,44))-new Vector2(18,4),20,paper);label.raycastTarget=false;
            button.onClick.AddListener(()=>{if(game.CanAcceptUi){game.PlayUiFeedback(id);action?.Invoke();}});buttons[id]=button;return button;
        }
        void ScrollText(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var viewport=Panel("TextViewport",parent,position,size,new Color(0,0,0,0));viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;scroll.viewport=viewport;
            var label=Label(viewport,text,Vector2.zero,new Vector2(size.x-22,size.y),fontSize,paper,TextAnchor.UpperLeft);
            var content=(RectTransform)label.transform;content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(-18,size.y);
            var fitter=label.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;
        }
        void Slider(Transform parent,Vector2 position,Vector2 size,float value,Action<float> onChange,float min,float max)
        {
            var r=Panel("Slider",parent,position,size,new Color(.15f,.21f,.2f));var slider=r.gameObject.AddComponent<Slider>();
            var fill=Panel("Fill",r,Vector2.zero,size,new Color(.43f,.62f,.51f));Stretch(fill);slider.fillRect=fill;
            var handle=Panel("Handle",r,Vector2.zero,new Vector2(18,size.y+9),gold);slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();
            slider.minValue=min;slider.maxValue=max;slider.value=value;slider.onValueChanged.AddListener(v=>onChange(v));
        }
        void Toggle(Transform parent,string caption,Vector2 position,bool value,Action<bool> change)
        {
            var r=Rect("Toggle",parent,position,new Vector2(380,36));var t=r.gameObject.AddComponent<Toggle>();
            var box=Panel("Box",r,new Vector2(-175,0),new Vector2(27,27),new Color(.18f,.24f,.22f));
            var check=Panel("Check",box,Vector2.zero,new Vector2(17,17),gold).GetComponent<Image>();t.targetGraphic=box.GetComponent<Image>();t.graphic=check;t.isOn=value;
            Label(r,caption,new Vector2(20,0),new Vector2(340,36),17,paper,TextAnchor.MiddleLeft);t.onValueChanged.AddListener(v=>change(v));
        }
        RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<Image>();image.color=color;return r;}
        Text Label(Transform parent,string text,Vector2 position,Vector2 size,int pixels,Color color,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var r=Rect("Text",parent,position,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=pixels;t.color=color;t.text=text;t.alignment=align;t.lineSpacing=1.12f;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static string BindingName(string id)
        {
            switch(id){case "Forward":return "前进";case "Backward":return "后退";case "Left":return "向左";case "Right":return "向右";case "Sprint":return "冲刺";case "Jump":return "跳跃";case "Crouch":return "蹲下";case "Interact":return "交互";case "Menu":return "暂停";default:return "锁定视角";}
        }
    }
}
