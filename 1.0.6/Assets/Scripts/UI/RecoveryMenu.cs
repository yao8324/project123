using ArcaneArchive.Knowledge;
using ArcaneArchive.Player;
using ArcaneArchive.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace ArcaneArchive.UI
{
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed partial class RecoveryMenu : MonoBehaviour
    {
        public enum Page { Explore, Menu, Help, Settings, Gallery, Book, Reading, Upload, Search, Exit, DeepExplore, BattleResult, CombatControls, Equipment }
        public ArcaneArchive.Core.KnightDefinition bookKnight;
        public TMP_FontAsset bookKnightFont;
        private ArcaneArchive.Core.BossEncounter Encounter => GetComponent<ArcaneArchive.Core.BossEncounter>();
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0,.08f,7.8f);
        [SerializeField] private DemoBookRecord[] demoBooks;
        private GameObject canvasObject, overlay, body, viewmodel, promptObject, crosshair;
        [SerializeField] private TMP_FontAsset uiFont;
        private TMP_FontAsset font;
        private TMP_FontAsset originalCombatFont;
        private TMP_Text prompt, footer, qualityHint;
        private GameObject qualityHintObject;
        private float qualityHintUntil;
        private DemoBookRecord selectedBook;
        private MilanQualityOptions qualityOptions;
        private Button balancedQualityButton, ultraQualityButton;
        private int displayedQuality = -1;
        private ArcaneArchive.Core.LibraryInputBindings bindings;
        private readonly System.Collections.Generic.List<BindingVisual> bindingVisuals = new System.Collections.Generic.List<BindingVisual>();
        private sealed class BindingVisual { public string id; public Button button; public TMP_Text label; }
        private bool viewWasActive, syncCursor;
        private readonly Color ink = new Color(.13f,.105f,.094f,.98f);
        private readonly Color paper = new Color(.93f,.89f,.81f);
        private readonly Color brass = new Color(.72f,.60f,.35f);
        public Page CurrentPage { get; private set; }
        public bool IsOpen => CurrentPage != Page.Explore;
        public Button RespawnButton { get; private set; }
        public Button ResumeButton { get; private set; }
        public uint RespawnCount { get; private set; }

        private void Awake()
        {
            if(!player) player=GetComponent<FirstPersonController>();
            if(!player) {Debug.LogError("Arcane Archive menu requires a player.",this);enabled=false;return;}
            var view=player.GetComponentInChildren<FirstPersonViewModel>(true);
            if(view)viewmodel=view.gameObject;
            qualityOptions=player.GetComponent<MilanQualityOptions>();
            font=uiFont;
            if(Encounter)originalCombatFont=Encounter.combatFont;
            if(!font) { Debug.LogError("Arcane Archive requires its baked Chinese SDF font asset.",this);enabled=false;return; }
            canvasObject=new GameObject("ArcaneArchiveUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=400;canvas.pixelPerfect=true;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            overlay=Panel(canvasObject.transform,"ModalOverlay",Vector2.zero,Vector2.zero,new Color(0,0,0,.62f));
            var rect=(RectTransform)overlay.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            crosshair=Label(canvasObject.transform,"·",Vector2.zero,new Vector2(20,30),28,paper).gameObject;
            promptObject=Panel(canvasObject.transform,"BookPrompt",new Vector2(0,-260),new Vector2(500,74),ink);
            prompt=Label(promptObject.transform,"",Vector2.zero,new Vector2(480,65),20,paper);
            qualityHintObject=Panel(canvasObject.transform,"QualityHint",Vector2.zero,new Vector2(350,44),ink);
            var hintRect=(RectTransform)qualityHintObject.transform;
            hintRect.anchorMin=hintRect.anchorMax=hintRect.pivot=new Vector2(0,1);
            hintRect.anchoredPosition=new Vector2(18,-18);
            qualityHint=Label(qualityHintObject.transform,"",Vector2.zero,new Vector2(330,40),18,paper);
            qualityHintObject.SetActive(false);
            promptObject.SetActive(false);overlay.SetActive(false);
            if(!FindFirstObjectByType<EventSystem>())
            {
                var go=new GameObject("ArchiveEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                go.transform.SetParent(canvasObject.transform,false);
                go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            CurrentPage=Page.Explore;
        }
        private void Update()
        {
            var controls=player.Bindings;
            if(controls && !controls.SuppressInputThisFrame &&
                (controls.MenuAction.WasPressedThisFrame() || Keyboard.current?.escapeKey.wasPressedThisFrame==true))HandleEscape();
            if(controls && !controls.SuppressInputThisFrame && controls.EquipmentAction.WasPressedThisFrame())ToggleEquipment();
            if(qualityHintObject)qualityHintObject.SetActive(!IsOpen && Time.unscaledTime<qualityHintUntil);
            if(CurrentPage==Page.Settings && displayedQuality!=QualitySettings.GetQualityLevel())RefreshQualitySelection();
        }
        public void ShowQualityHint(string mode)
        {
            if(!qualityHint)return;
            qualityHint.text="画质："+(mode=="Ultra"?"高画质":"均衡")+"  ·  F8 / F9";
            qualityHintUntil=Time.unscaledTime+5f;
        }
        private void LateUpdate()
        {
            // Runs after the player's legacy Esc cursor handler.
            if(IsOpen)player.SetCursorLocked(false);
            else if(syncCursor){syncCursor=false;player.SetCursorLocked(true);}
        }
        public void HandleEscape()
        {
            if(Encounter && Encounter.IsFinished){ShowPage(Page.BattleResult);return;}
            switch(CurrentPage)
            {
                case Page.Explore:Open();break;
                case Page.Menu:case Page.Book:case Page.DeepExplore:Resume();break;
                case Page.Reading:if(selectedBook)ShowBook(selectedBook);else Open();break;
                case Page.Equipment:if(equipmentFromMenu)Open();else Resume();break;
                case Page.Upload:case Page.Search:ShowPage(Page.Gallery);break;
                default:Open();break;
            }
        }
        public void Open()=>ShowPage(Page.Menu);
        public void Resume()
        {
            if(Encounter && Encounter.IsFinished){ShowPage(Page.BattleResult);return;}
            if(!IsOpen)return;
            if(player.Bindings)player.Bindings.Cancel();
            selectedBook=null;
            var melee=GetComponent<PlayerMelee>();if(melee)melee.SuppressUntilReleased();
            overlay.SetActive(false);CurrentPage=Page.Explore;HidePrompt();crosshair.SetActive(true);
            if(viewmodel)viewmodel.SetActive(viewWasActive);
            player.WaitForReleasedControls();player.SetGameplayInputEnabled(true);syncCursor=true;
            if(Encounter && Encounter.IsActive && Encounter.Combat)Encounter.Combat.SuppressUntilReleased();
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        }
        public void ShowPrompt(string message)
        {
            if(IsOpen||!prompt)return;
            if(prompt.text!=message)prompt.text=message;
            if(!promptObject.activeSelf)promptObject.SetActive(true);
        }
        public void HidePrompt(){if(promptObject && promptObject.activeSelf)promptObject.SetActive(false);}
        public void ShowBook(DemoBookRecord book)
        {
            if(!book)return;selectedBook=book;ShowPage(Page.Book);
        }
        public void ShowPage(Page page)
        {
            if(page==Page.Explore){Resume();return;}
            if(player.Bindings)player.Bindings.Cancel();
            if(page==Page.Menu)selectedBook=null;
            if((page==Page.Book||page==Page.Reading)&&!selectedBook)page=Page.Gallery;
            if(!IsOpen&&viewmodel)viewWasActive=viewmodel.activeSelf;
            player.SetGameplayInputEnabled(false);player.SetCursorLocked(false);syncCursor=false;
            if(viewmodel)viewmodel.SetActive(false);
            CurrentPage=page;HidePrompt();crosshair.SetActive(false);overlay.SetActive(true);
            if(body){body.SetActive(false);Destroy(body);}
            body=Panel(overlay.transform,"Page_"+page,Vector2.zero,new Vector2(980,580),brass);
            var inner=Panel(body.transform,"Inner",Vector2.zero,new Vector2(976,576),ink);
            Label(inner.transform,"ARCANE ARCHIVE",new Vector2(-270,246),new Vector2(350,42),25,brass);
            Label(inner.transform,page==Page.Equipment ? "行者档案  /  剑盾装备" : Encounter && Encounter.IsActive ? "禁书档案  /  封印试炼" : "魔法图书馆  /  UI 展示版",new Vector2(245,246),new Vector2(350,36),17,paper);
            Panel(inner.transform,"Rule",new Vector2(0,211),new Vector2(880,1),brass);
            footer=Label(inner.transform,"界面演示 · 资料功能待接入",new Vector2(0,-256),new Vector2(800,28),15,new Color(.7f,.66f,.57f));
            if(page!=Page.Menu && page!=Page.BattleResult && page!=Page.Equipment)Button(inner.transform,"返回",new Vector2(-378,-205),new Vector2(130,40),HandleEscape);
            if(page!=Page.BattleResult)Button(inner.transform,"×",new Vector2(450,246),new Vector2(40,36),Resume);
            switch(page)
            {
                case Page.Menu:BuildMenu(inner.transform);break;
                case Page.Help:BuildHelp(inner.transform);break;
                case Page.Settings:BuildSettings(inner.transform);break;
                case Page.Gallery:BuildGallery(inner.transform);break;
                case Page.Book:BuildBook(inner.transform);break;
                case Page.DeepExplore:BuildDeepExplore(inner.transform);break;
                case Page.Reading:BuildReading(inner.transform);break;
                case Page.Upload:BuildUpload(inner.transform);break;
                case Page.Search:BuildSearch(inner.transform);break;
                case Page.Exit:BuildExit(inner.transform);break;
                case Page.BattleResult:BuildBattleResult(inner.transform);break;
                case Page.CombatControls:BuildCombatControls(inner.transform);break;
                case Page.Equipment:BuildEquipment(inner.transform);break;
            }
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        }
        private void BuildMenu(Transform p)
        {
            if(Encounter && Encounter.IsActive)
            {
                Label(p,"封印厅 · 战斗暂停",new Vector2(0,153),new Vector2(760,50),32,brass);
                Button(p,"继续战斗",new Vector2(0,90),new Vector2(330,44),Resume);
                Button(p,"属性与装备  [I]",new Vector2(0,36),new Vector2(330,44),()=>{equipmentFromMenu=true;ShowPage(Page.Equipment);});
                Button(p,"战斗按键",new Vector2(0,-18),new Vector2(330,44),()=>ShowPage(Page.CombatControls));
                Button(p,"设置",new Vector2(0,-72),new Vector2(330,44),()=>ShowPage(Page.Settings));
                Button(p,"放弃并返回图书馆",new Vector2(0,-126),new Vector2(330,44),()=>Encounter.ReturnToLibrary());
                footer.text="暂停期间冻结角色与战斗 · Esc 继续";
                return;
            }
            Label(p,"知识成书，\n归藏于此。",new Vector2(-245,70),new Vector2(350,160),37,paper);
            Label(p,"漫游古老书廊，探索你的知识馆藏。\n\n当前为界面原型，资料服务尚未接入。",new Vector2(-245,-85),new Vector2(350,130),18,new Color(.73f,.68f,.58f));
            ResumeButton=Button(p,"继续探索",new Vector2(235,154),new Vector2(330,44),Resume);
            Button(p,"馆藏界面预览",new Vector2(235,100),new Vector2(330,44),()=>ShowPage(Page.Gallery));
            Button(p,"操作说明",new Vector2(235,46),new Vector2(330,44),()=>ShowPage(Page.Help));
            Button(p,"设置预览",new Vector2(235,-8),new Vector2(330,44),()=>ShowPage(Page.Settings));
            RespawnButton=Button(p,"返回大厅入口",new Vector2(235,-62),new Vector2(330,44),ReturnToSpawn);
            Button(p,"退出应用",new Vector2(235,-116),new Vector2(330,44),()=>ShowPage(Page.Exit));
            GildedExploreButton(p,new Vector2(-245,-185),()=>ShowPage(Page.DeepExplore));
            Button(p,"属性与装备  [I]",new Vector2(235,-170),new Vector2(330,44),()=>{equipmentFromMenu=true;ShowPage(Page.Equipment);});
            footer.text="Esc 继续探索  ·  返回入口沿用原有恢复功能";
        }
        private void ReturnToSpawn()
        {
            if(!IsOpen)return;
            if(player.TryReturnToSpawn(spawnPosition,Quaternion.identity)){RespawnCount++;footer.text="已回到入口，点击继续探索";}
            else footer.text="入口暂不可用，请稍后重试";
        }
        private void BuildHelp(Transform p)
        {
            if(bindings!=player.Bindings)
            {
                if(bindings)bindings.Changed-=RefreshBindingLabels;
                bindings=player.Bindings;
                if(bindings)bindings.Changed+=RefreshBindingLabels;
            }
            bindingVisuals.Clear();
            Label(p,"探索指南",new Vector2(0,164),new Vector2(700,48),30,brass);
            var move=InfoRow(p,"HelpRow_0","移动","",112,38);
            BindingButton(move.transform,"Forward",40,92);
            BindingButton(move.transform,"Backward",148,92);
            BindingButton(move.transform,"Left",256,92);
            BindingButton(move.transform,"Right",364,92);
            InfoRow(p,"HelpRow_1","环视","鼠标",74,38);
            string[] actions={"突进 / 奔跑","跳跃","蹲下","查看演示书籍","返回 / 菜单","重新锁定鼠标"};
            string[] ids={"Sprint","Jump","Crouch","Interact","Menu","Relock"};
            for(int i=0;i<ids.Length;i++)
            {
                var row=InfoRow(p,"HelpRow_"+(i+2),actions[i],"",36-i*38,38);
                BindingButton(row.transform,ids[i],300,220);
            }
            var reset=Button(p,"恢复默认按键",new Vector2(290,-205),new Vector2(240,40),()=>{if(bindings)bindings.RestoreDefaults();});
            reset.targetGraphic.color=Color.white;
            var resetColors=reset.colors;
            resetColors.normalColor=brass;
            resetColors.highlightedColor=resetColors.selectedColor=new Color(.88f,.76f,.48f);
            resetColors.pressedColor=new Color(.58f,.46f,.25f);
            reset.colors=resetColors;
            reset.GetComponentInChildren<TMP_Text>().color=ink;
            RefreshBindingLabels();
        }
        private void BindingButton(Transform parent,string id,float x,float width)
        {
            var button=Button(parent,"",new Vector2(x,0),new Vector2(width,34),null);
            if(bindings && !bindings.IsReadOnly(id))button.onClick.AddListener(()=>bindings.Begin(id));
            button.name="Bind_"+id;
            var label=button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=18;
            label.textWrappingMode=TextWrappingModes.NoWrap;
            bindingVisuals.Add(new BindingVisual{id=id,button=button,label=label});
        }
        private void RefreshBindingLabels()
        {
            if((CurrentPage!=Page.Help && CurrentPage!=Page.CombatControls)||!bindings)return;
            foreach(var visual in bindingVisuals)
            {
                if(!visual.button||!visual.label)continue;
                string text=bindings.Display(visual.id);
                if(visual.id=="Menu" && text!="Esc")text+=" / Esc";
                if(visual.id=="Forward")text="前 · "+text;
                if(visual.id=="Backward")text="后 · "+text;
                if(visual.id=="Left")text="左 · "+text;
                if(visual.id=="Right")text="右 · "+text;
                bool readOnly=bindings.IsReadOnly(visual.id);
                visual.label.text=text+(readOnly?"（固定）":"");
                visual.button.interactable=!readOnly&&!bindings.IsRebinding;
            }
            footer.text=bindings.Status??"点击可用按键换绑 · 鼠标左右键固定 · Esc 取消";
        }
        private void BuildSettings(Transform p)
        {
            Label(p,"设置",new Vector2(0,164),new Vector2(700,48),30,brass);
            var qualityRow=InfoRow(p,"SettingsRow_0","画质模式","",87,70);
            balancedQualityButton=Button(qualityRow.transform,"均衡（F8）",new Vector2(72,0),new Vector2(190,42),()=>SelectQuality("Balanced"));
            ultraQualityButton=Button(qualityRow.transform,"高画质（F9）",new Vector2(280,0),new Vector2(190,42),()=>SelectQuality("Ultra"));
            balancedQualityButton.interactable=ultraQualityButton.interactable=qualityOptions!=null;
            RefreshQualitySelection();
            var sensitivityRow=InfoRow(p,"SettingsRow_1","鼠标灵敏度","",17,70);
            var value=Label(sensitivityRow.transform,player.MouseSensitivityMultiplier.ToString("F2")+"x",new Vector2(340,0),new Vector2(94,40),20,paper);
            var sliderObject=Panel(sensitivityRow.transform,"MouseSensitivity",new Vector2(105,0),new Vector2(330,42),Color.clear);
            var track=Panel(sliderObject.transform,"Track",Vector2.zero,new Vector2(310,4),new Color(.35f,.30f,.22f));
            track.GetComponent<Image>().raycastTarget=false;
            var fillArea=Panel(sliderObject.transform,"FillArea",Vector2.zero,new Vector2(310,4),Color.clear);
            fillArea.GetComponent<Image>().raycastTarget=false;
            var fill=Panel(fillArea.transform,"Fill",Vector2.zero,Vector2.zero,brass);
            var fillRect=(RectTransform)fill.transform;
            fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=fillRect.offsetMax=Vector2.zero;
            fill.GetComponent<Image>().raycastTarget=false;
            var handleArea=Panel(sliderObject.transform,"HandleArea",Vector2.zero,new Vector2(310,28),Color.clear);
            handleArea.GetComponent<Image>().raycastTarget=false;
            var handle=Panel(handleArea.transform,"Handle",Vector2.zero,new Vector2(12,0),paper);
            var slider=sliderObject.AddComponent<Slider>();
            slider.fillRect=fillRect;slider.handleRect=(RectTransform)handle.transform;slider.targetGraphic=handle.GetComponent<Image>();
            slider.minValue=.25f;slider.maxValue=3f;slider.wholeNumbers=false;slider.SetValueWithoutNotify(player.MouseSensitivityMultiplier);
            slider.onValueChanged.AddListener(v=>{player.SetMouseSensitivityMultiplier(v);value.text=player.MouseSensitivityMultiplier.ToString("F2")+"x";});
            InfoRow(p,"SettingsRow_2","音量","待音频系统接入",-53,70);
            footer.text="画质与灵敏度即时生效 · 1.00x 为默认灵敏度";
        }
        private void SelectQuality(string name)
        {
            if(!qualityOptions)return;
            qualityOptions.Select(name);
            RefreshQualitySelection();
        }
        private void RefreshQualitySelection()
        {
            displayedQuality=QualitySettings.GetQualityLevel();
            string current=QualitySettings.names[displayedQuality];
            SetQualityButtonState(balancedQualityButton,current=="Balanced");
            SetQualityButtonState(ultraQualityButton,current=="Ultra");
        }
        private void SetQualityButtonState(Button button,bool selected)
        {
            if(!button)return;
            button.targetGraphic.color=selected?new Color(.43f,.33f,.17f):new Color(.24f,.20f,.14f);
            var text=button.GetComponentInChildren<TMP_Text>();if(text)text.color=selected?new Color(1f,.94f,.76f):paper;
        }
        private GameObject InfoRow(Transform parent,string name,string key,string value,float y,float height,UnityEngine.Events.UnityAction action=null)
        {
            var normal = new Color(.18f,.15f,.12f,.32f);
            var row = Panel(parent,name,new Vector2(0,y),new Vector2(820,height),normal);
            var background = row.GetComponent<Image>();
            var accent = Panel(row.transform,"HoverAccent",new Vector2(-409,0),new Vector2(3,height-12),brass);
            accent.GetComponent<Image>().raycastTarget=false;
            accent.SetActive(false);
            var rule = Panel(row.transform,"Divider",new Vector2(0,-height*.5f),new Vector2(820,1),new Color(brass.r,brass.g,brass.b,.3f));
            rule.GetComponent<Image>().raycastTarget=false;
            var left = Label(row.transform,key,new Vector2(-270,0),new Vector2(224,height-8),20,paper);
            left.alignment=TextAlignmentOptions.MidlineLeft;
            var right = Label(row.transform,value,new Vector2(166,0),new Vector2(472,height-8),20,new Color(.78f,.74f,.65f));
            right.alignment=TextAlignmentOptions.MidlineRight;
            var events = row.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID=EventTriggerType.PointerEnter };
            enter.callback.AddListener(_=>{background.color=new Color(.30f,.24f,.15f,.9f);accent.SetActive(true);left.color=brass;right.color=paper;});
            var exit = new EventTrigger.Entry { eventID=EventTriggerType.PointerExit };
            exit.callback.AddListener(_=>{background.color=normal;accent.SetActive(false);left.color=paper;right.color=new Color(.78f,.74f,.65f);});
            events.triggers.Add(enter);events.triggers.Add(exit);
            if(action!=null)
            {
                var click = new EventTrigger.Entry { eventID=EventTriggerType.PointerClick };
                click.callback.AddListener(data=>{
                    var pointer=data as PointerEventData;
                    if(pointer!=null && pointer.button==PointerEventData.InputButton.Left)action();
                });
                events.triggers.Add(click);
            }
            return row;
        }

        private void BuildGallery(Transform p)
        {
            Label(p,"我的馆藏",new Vector2(0,166),new Vector2(760,44),30,brass);
            Label(p,"用户导入的资料  ·  当前 0 份",new Vector2(0,153),new Vector2(760,28),18,new Color(.78f,.74f,.65f));
            InfoRow(p,"CollectionImport","导入资料","查看导入流程  >",90,44,()=>ShowPage(Page.Upload));
            InfoRow(p,"CollectionSearch","检索馆藏","标题 / 文件名 / 关键词  >",46,44,()=>ShowPage(Page.Search));
            var header=Panel(p,"CollectionHeader",new Vector2(0,2),new Vector2(820,36),new Color(.18f,.15f,.12f,.32f));
            CollectionColumn(header.transform,"标题 / 原文件名",-231,300);
            CollectionColumn(header.transform,"格式",-20,100);
            CollectionColumn(header.transform,"类别",122,140);
            CollectionColumn(header.transform,"书架位置",292,170);
            var rule=Panel(p,"CollectionHeaderRule",new Vector2(0,-16),new Vector2(820,1),new Color(brass.r,brass.g,brass.b,.3f));
            rule.GetComponent<Image>().raycastTarget=false;
            Label(p,"馆藏为空",new Vector2(0,-60),new Vector2(760,40),25,paper);
            Label(p,"成功导入的文件才会出现在这里。",new Vector2(0,-98),new Vector2(760,32),18,new Color(.78f,.74f,.65f));
            Label(p,"每份资料对应一本馆藏书籍；场景装饰书不计入馆藏。",new Vector2(0,-142),new Vector2(820,32),18,new Color(.78f,.74f,.65f));
            footer.text="界面预览 · 导入服务待接入 · 当前没有用户资料";
        }
        private void CollectionColumn(Transform parent,string text,float x,float width)
        {
            var label=Label(parent,text,new Vector2(x,0),new Vector2(width,30),18,brass);
            label.alignment=TextAlignmentOptions.MidlineLeft;
        }
        private void BuildBook(Transform p)
        {
            Label(p,selectedBook.title,new Vector2(0,166),new Vector2(800,48),30,brass);
            var cover=Panel(p,"ArchiveTome",new Vector2(-310,-10),new Vector2(190,270),selectedBook.coverColor);
            Panel(cover.transform,"GiltTop",new Vector2(0,123),new Vector2(164,1),brass);
            Panel(cover.transform,"GiltBottom",new Vector2(0,-123),new Vector2(164,1),brass);
            Label(cover.transform,"ARCANE\nARCHIVE",new Vector2(0,67),new Vector2(160,85),22,brass);
            Label(cover.transform,selectedBook.title,new Vector2(0,-43),new Vector2(160,130),26,paper);
            Label(p,selectedBook.category+" · "+selectedBook.fileType,new Vector2(113,95),new Vector2(570,44),25,brass);
            var description=Label(p,selectedBook.summary+"\n\n关键词："+selectedBook.keywords,new Vector2(113,-5),new Vector2(570,154),21,paper);
            description.alignment=TextAlignmentOptions.Center;
            Button(p,"阅读预览",new Vector2(-35,-151),new Vector2(230,44),()=>ShowPage(Page.Reading));
            Button(p,"返回书廊",new Vector2(261,-151),new Vector2(230,44),Resume);
            footer.text="演示书籍 · 当前内容为阅读预览";
        }
        private Button GildedExploreButton(Transform parent,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var go=Panel(parent,"DeepExploreButton",position,new Vector2(210,60),Color.white);
            var button=go.AddComponent<Button>();button.onClick.AddListener(action);
            var colors=button.colors;
            colors.normalColor=new Color(.065f,.022f,.11f);
            colors.highlightedColor=colors.selectedColor=new Color(.24f,.065f,.36f);
            colors.pressedColor=new Color(.035f,.012f,.055f);
            colors.fadeDuration=.16f;button.colors=colors;
            button.targetGraphic.CrossFadeColor(colors.normalColor,0f,true,true);
            var ornament=new GameObject("GildedFiligree",typeof(RectTransform),typeof(CanvasRenderer),typeof(GildedFiligree));
            ornament.transform.SetParent(go.transform,false);
            var rect=(RectTransform)ornament.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var graphic=ornament.GetComponent<GildedFiligree>();graphic.color=new Color(.66f,.30f,1f);graphic.raycastTarget=false;
            Label(go.transform,"深入探索",new Vector2(0,1),new Vector2(118,30),21,new Color(.97f,.88f,1f));
            return button;
        }
        private void BuildDeepExplore(Transform p)
        {
            Label(p,"封印之下",new Vector2(0,153),new Vector2(800,55),33,brass);
            Label(p,"书廊尽头，有些知识从未等待被发现。",new Vector2(0,85),new Vector2(820,45),23,paper);
            InfoRow(p,"UnknownWorld","未知领域","探索被封存的魔法回廊",14,54);
            InfoRow(p,"BossEncounter","禁忌试炼","缄默典狱长 · 维纶",-40,54);
            Label(p,"当封印开启，你面对的将不只是书中的故事。",new Vector2(0,-106),new Vector2(820,42),20,new Color(.78f,.66f,.57f));
            GildedExploreButton(p,new Vector2(0,-177),StartEncounter);
            footer.text="进入后可随时按 Esc 暂停或返回图书馆";
        }
        public void StartEncounter()
        {
            if(!Encounter || !Encounter.Begin())footer.text="封印厅暂不可用，请检查战斗资源";
        }
        private void BuildBattleResult(Transform p)
        {
            bool won=Encounter && Encounter.CurrentState==ArcaneArchive.Core.BossEncounter.State.Won;
            Label(p,Encounter?Encounter.ResultTitle:"试炼结束",new Vector2(0,110),new Vector2(800,65),36,brass);
            Label(p,Encounter?Encounter.ResultDescription:"",new Vector2(0,15),new Vector2(820,100),23,paper);
            Button(p,"再次挑战",new Vector2(-175,-110),new Vector2(270,48),()=>Encounter.Retry());
            Button(p,"返回图书馆",new Vector2(175,-110),new Vector2(270,48),()=>Encounter.ReturnToLibrary());
            footer.text="I AM A MONSTER";
        }
        private void BuildCombatControls(Transform p)
        {
            if(bindings!=player.Bindings)
            {
                if(bindings)bindings.Changed-=RefreshBindingLabels;
                bindings=player.Bindings;if(bindings)bindings.Changed+=RefreshBindingLabels;
            }
            bindingVisuals.Clear();
            Label(p,"战斗按键",new Vector2(0,155),new Vector2(800,50),30,brass);
            var fire=InfoRow(p,"AttackBinding","剑术连斩","",90,52);BindingButton(fire.transform,"Attack",280,220);
            var absorb=InfoRow(p,"AbsorbBinding","举盾 / 弹反 / 吞印","",38,52);BindingButton(absorb.transform,"Absorb",280,220);
            Label(p,"1 破誓重斩   2 缄眼盾击   3 踏影突刺   4 回旋封印\n左键三段连斩；右键举盾瞬间可弹反近战，按住格挡。\n攻击、格挡、吞印共用体力；停手后恢复。I 查看属性。\n激光横移，环波跳跃，裂地侧移；冲撞锁线后侧闪。",new Vector2(0,-90),new Vector2(850,150),18,paper);
            RefreshBindingLabels();
        }
        private void BuildReading(Transform p)
        {
            Label(p,selectedBook.title+" · 阅读预览",new Vector2(0,170),new Vector2(800,44),27,brass);
            var surface=Panel(p,"ReadingPaper",new Vector2(0,-10),new Vector2(850,292),paper);
            var scroll=surface.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.scrollSensitivity=24f;scroll.movementType=ScrollRect.MovementType.Clamped;
            surface.AddComponent<RectMask2D>();scroll.viewport=(RectTransform)surface.transform;
            var text=Label(surface.transform,selectedBook.previewText,Vector2.zero,new Vector2(780,700),23,new Color(.17f,.14f,.11f));
            text.alignment=TextAlignmentOptions.TopLeft;text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Overflow;text.raycastTarget=true;
            var tr=(RectTransform)text.transform;tr.anchorMin=tr.anchorMax=new Vector2(.5f,1);tr.pivot=new Vector2(.5f,1);tr.anchoredPosition=new Vector2(0,-18);
            tr.sizeDelta=new Vector2(780,Mathf.Max(292,text.GetPreferredValues(selectedBook.previewText,780,Mathf.Infinity).y+36));
            scroll.content=tr;scroll.verticalNormalizedPosition=1;
            footer.text="示例排版 · 滚轮阅读 · Esc 返回馆藏卡片";
        }
        private void BuildUpload(Transform p)
        {
            Label(p,"导入资料 · 流程预览",new Vector2(0,166),new Vector2(760,44),30,brass);
            InfoRow(p,"ImportStep_0","01  选择文件","选择本地资料",104,48);
            InfoRow(p,"ImportStep_1","02  整理信息","确认标题、类别与关键词",56,48);
            InfoRow(p,"ImportStep_2","03  确认归档","保存文件与元数据，分配书架位置",8,48);
            Label(p,"TXT / Markdown：馆内阅读  ·  图片：馆内预览",new Vector2(0,-43),new Vector2(820,30),18,paper);
            Label(p,"PDF / Word / PPT：保存文件信息，使用系统程序打开",new Vector2(0,-77),new Vector2(820,30),18,paper);
            Button(p,"选择文件 · 待接入",new Vector2(0,-136),new Vector2(300,44),null).interactable=false;
            footer.text="尚未接入文件选择、复制或元数据保存";
        }
        private void BuildSearch(Transform p)
        {
            Label(p,"检索馆藏 · 界面预览",new Vector2(0,166),new Vector2(760,44),30,brass);
            InfoRow(p,"SearchScope","检索范围","已成功导入的用户资料",108,48);
            InfoRow(p,"SearchFields","检索字段","标题 / 原文件名 / 关键词",60,48);
            InfoRow(p,"SearchRule","匹配方式","不区分大小写的模糊匹配",12,48);
            Label(p,"当前馆藏为空",new Vector2(0,-55),new Vector2(760,40),25,paper);
            Label(p,"先导入资料，再搜索并定位对应的书籍与书架。",new Vector2(0,-101),new Vector2(820,40),18,new Color(.78f,.74f,.65f));
            footer.text="界面预览 · 检索与场景高亮待接入";
        }
        private void BuildExit(Transform p)
        {
            Label(p,"离开魔法图书馆？",new Vector2(0,90),new Vector2(760,60),32,paper);
            Label(p,"确认后关闭运行中的应用。",new Vector2(0,22),new Vector2(760,42),20,paper);
            Button(p,"继续探索",new Vector2(-155,-85),new Vector2(250,48),Resume);
            Button(p,"确认退出",new Vector2(155,-85),new Vector2(250,48),()=>{
#if UNITY_EDITOR
                footer.text="编辑器中不退出；请使用顶部 Play 按钮停止运行";
#else
                Application.Quit();
#endif
            });
        }
        private GameObject Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;
            go.GetComponent<Image>().color=color;return go;
        }
        private TMP_Text Label(Transform parent,string message,Vector2 pos,Vector2 size,int fontSize,Color color)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;
            var text=go.GetComponent<TextMeshProUGUI>();text.font=bookKnight && bookKnightFont?bookKnightFont:Encounter && Encounter.KnightMode && Encounter.combatFont?Encounter.combatFont:font;text.text=message;text.fontSize=Mathf.Max(fontSize,18);text.color=color;
            text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.richText=false;
            text.textWrappingMode=TextWrappingModes.Normal;text.extraPadding=true;return text;
        }
        private Button Button(Transform parent,string title,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var go=Panel(parent,title,pos,size,new Color(.24f,.20f,.14f));var button=go.AddComponent<Button>();
            var colors=button.colors;colors.highlightedColor=new Color(1f,.88f,.60f);colors.pressedColor=new Color(.6f,.65f,.58f);colors.disabledColor=new Color(.5f,.5f,.5f);button.colors=colors;
            if(action!=null)button.onClick.AddListener(action);
            Label(go.transform,title,Vector2.zero,size-new Vector2(12,6),20,paper);return button;
        }
        private void OnDisable()
        {
            if(player && player.Bindings)player.Bindings.Cancel();
            if(overlay)overlay.SetActive(false);
            if(canvasObject)canvasObject.SetActive(false);
            if(player&&IsOpen){player.SetGameplayInputEnabled(true);player.SetCursorLocked(false);}
            if(viewmodel&&IsOpen)viewmodel.SetActive(viewWasActive);
            CurrentPage=Page.Explore;
        }
        private void OnEnable()
        {
            if(canvasObject)canvasObject.SetActive(true);
            HidePrompt();
        }
        private void OnDestroy()
        {
            if(bindings)bindings.Changed-=RefreshBindingLabels;
            if(canvasObject)Destroy(canvasObject);
        }
    }
}

