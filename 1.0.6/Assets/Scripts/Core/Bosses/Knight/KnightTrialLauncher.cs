using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneArchive.Core
{
    [RequireComponent(typeof(BossEncounter))]
    public sealed class KnightTrialLauncher : MonoBehaviour
    {
        public KnightDefinition definition;
        public TMP_FontAsset font;
        public bool storyGate;
        BossEncounter encounter;
        GameObject panel;
        IEnumerator Start()
        {
            encounter=GetComponent<BossEncounter>();
            yield return null;yield return null;
            encounter.combatFont=font;
            if(!encounter.BeginKnight(definition,storyGate))Debug.LogError("Knight trial could not start; check its configuration.");
        }
        void Update()
        {
            if(!encounter)return;
            if(encounter.KnightStoryPending && !panel)ShowGate();
            if(!encounter.KnightStoryPending && panel){Destroy(panel);panel=null;}
        }
        void ShowGate()
        {
            panel=new GameObject("Knight ceasefire",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            panel.transform.SetParent(transform,false);var canvas=panel.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=350;
            var scale=panel.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);
            var bg=new GameObject("Backdrop",typeof(Image));bg.transform.SetParent(panel.transform,false);
            var rect=bg.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            bg.GetComponent<Image>().color=new Color(.025f,.025f,.04f,.96f);
            Text(panel.transform,"停战窗口",new Vector2(0,130),new Vector2(800,60),32);
            Text(panel.transform,"门外的人，还没有全部撤离。\n他握住剑，却没有再向前一步。\n\n独立挑战：此处保留剧情接入点，不结算主线结局。",new Vector2(0,15),new Vector2(900,160),23);
            Button("继续挑战",new Vector2(-170,-145),()=>encounter.ResumeKnightTrial());
            Button("返回图书馆",new Vector2(170,-145),()=>encounter.ReturnToLibrary());
        }
        void Button(string label,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(panel.transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(280,52);rect.anchoredPosition=position;
            go.GetComponent<Image>().color=new Color(.25f,.21f,.14f);go.GetComponent<Button>().onClick.AddListener(action);
            Text(go.transform,label,Vector2.zero,new Vector2(270,46),22);
        }
        void Text(Transform parent,string value,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;
            var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;
            text.alignment=TextAlignmentOptions.Center;text.color=new Color(.92f,.86f,.72f);text.raycastTarget=false;
        }
        void OnDestroy(){if(panel)Destroy(panel);}
    }
}
