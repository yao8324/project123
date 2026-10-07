using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace ArcaneArchive.UI
{
    [DefaultExecutionOrder(-100)]
    public sealed class RecoveryMenu:MonoBehaviour
    {
        [SerializeField] FirstPersonController player;
        [SerializeField] Vector3 spawnPosition=new Vector3(0,.08f,7.8f);
        GameObject panel,viewmodel;
        UnityEngine.UI.Text status;
        Font font;
        bool resumeCursor;
        public bool IsOpen => panel && panel.activeSelf;
        public UnityEngine.UI.Button RespawnButton {get;private set;}
        public UnityEngine.UI.Button ResumeButton {get;private set;}
        public uint RespawnCount {get;private set;}
        void Awake()
        {
            if(!player)player=GetComponent<FirstPersonController>();
            var view=player.GetComponentInChildren<FirstPersonViewModel>(true);if(view)viewmodel=view.gameObject;
            font=Font.CreateDynamicFontFromOSFont(new[]{"PingFang SC","Heiti SC","Arial"},26);
            var canvasObject=new GameObject("RecoveryMenuCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera=Camera.main;canvas.planeDistance=.12f;canvas.sortingOrder=400;
            var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            panel=new GameObject("RecoveryPanel",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            var rect=(RectTransform)panel.transform;rect.SetParent(canvasObject.transform,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(440,290);
            panel.GetComponent<UnityEngine.UI.Image>().color=new Color(.055f,.045f,.035f,.97f);
            Label(panel.transform,"游戏菜单",new Vector2(0,99),30);
            RespawnButton=Button("回到出生点",new Vector2(0,30),ReturnToSpawn);
            ResumeButton=Button("继续游戏",new Vector2(0,-35),Resume);
            status=Label(panel.transform,"卡住时可返回大厅入口",new Vector2(0,-103),18);
            if(!FindFirstObjectByType<EventSystem>())
            {
                var events=new GameObject("RecoveryEventSystem",typeof(EventSystem));
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            panel.SetActive(false);
        }
        UnityEngine.UI.Text Label(Transform parent,string text,Vector2 position,int size)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(UnityEngine.UI.Text));
            var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(410,46);
            var label=go.GetComponent<UnityEngine.UI.Text>();label.font=font;label.text=text;label.fontSize=size;
            label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.96f,.9f,.78f);label.raycastTarget=false;return label;
        }
        UnityEngine.UI.Button Button(string text,Vector2 position,UnityEngine.Events.UnityAction clicked)
        {
            var go=new GameObject(text,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
            var rect=(RectTransform)go.transform;rect.SetParent(panel.transform,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(350,54);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.28f,.20f,.11f,1f);
            var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick.AddListener(clicked);
            Label(go.transform,text,Vector2.zero,24);return button;
        }
        void Update()
        {
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {if(IsOpen)Resume();else Open();}
        }
        void LateUpdate(){if(resumeCursor){resumeCursor=false;player.SetCursorLocked(true);}}
        public void Open()
        {
            panel.SetActive(true);player.SetGameplayInputEnabled(false);player.SetCursorLocked(false);
            if(viewmodel)viewmodel.SetActive(false);
            status.text="卡住时可返回大厅入口";
        }
        void ReturnToSpawn()
        {
            if(!IsOpen)return;
            if(player.TryReturnToSpawn(spawnPosition,Quaternion.identity))
            {RespawnCount++;status.text="已回到出生点，点击继续游戏";}
            else status.text="出生点暂不可用，请稍后重试";
        }
        public void Resume()
        {
            panel.SetActive(false);if(viewmodel)viewmodel.SetActive(true);
            player.WaitForReleasedControls();player.SetGameplayInputEnabled(true);resumeCursor=true;
        }
    }
}
