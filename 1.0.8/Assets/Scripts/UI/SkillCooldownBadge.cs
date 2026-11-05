using ArcaneArchive.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcaneArchive.UI
{
    // All progress comes from the real skill cooldown; the HUD has no independent timer.
    public sealed class SkillCooldownBadge : MonoBehaviour
    {
        Image colorIcon,grayIcon,clockFill,clockTrack,flashRing,numberShade;
        RectTransform clockHand;
        TMP_Text remainingLabel,statusLabel,titleLabel;
        Color accent;
        bool initialized,wasCooling;
        float readyAt=-10;
        public float RecoveryFraction { get; private set; }=1;
        public float ClockAngle => clockHand ? clockHand.localEulerAngles.z : 0;
        public string Countdown => remainingLabel ? remainingLabel.text : "";
        public bool Cooling { get; private set; }
        public void Initialize(MeleeLoadout.Skill skill,int key,TMP_FontAsset font,Sprite timerRing)
        {
            if(initialized)return;initialized=true;accent=skill.iconAccent;
            grayIcon=Picture("Desaturated skill",Vector2.zero,new Vector2(72,72),skill.cooldownIcon,Color.white);
            colorIcon=Picture("Clockwise colour reveal",Vector2.zero,new Vector2(72,72),skill.icon,Color.white);
            Radial(colorIcon);
            clockTrack=Picture("Timer track",Vector2.zero,new Vector2(84,84),timerRing,new Color(.24f,.20f,.18f,.95f));
            clockFill=Picture("Clockwise recovery ring",Vector2.zero,new Vector2(84,84),timerRing,accent);Radial(clockFill);
            flashRing=Picture("Ready pulse",Vector2.zero,new Vector2(89,89),timerRing,Color.clear);
            var pivot=new GameObject("Clock hand",typeof(RectTransform));pivot.transform.SetParent(transform,false);clockHand=(RectTransform)pivot.transform;
            clockHand.sizeDelta=new Vector2(84,84);
            var pointer=Picture("Clock tip",new Vector2(0,40),new Vector2(4,8),null,new Color(1,.94f,.76f));
            pointer.transform.SetParent(clockHand,false);pointer.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            numberShade=Picture("Countdown contrast",Vector2.zero,new Vector2(44,29),null,new Color(.035f,.025f,.045f,.78f));
            remainingLabel=Label("",Vector2.zero,new Vector2(62,32),24,font,new Color(1,.96f,.86f));remainingLabel.fontStyle=FontStyles.Bold;
            Picture("Key plate",new Vector2(-30,-30),new Vector2(22,22),null,new Color(.11f,.08f,.10f,.97f));
            var keyText=Label(key.ToString(),new Vector2(-30,-30),new Vector2(22,22),16,font,new Color(.96f,.84f,.60f));keyText.fontStyle=FontStyles.Bold;
            titleLabel=Label(skill.title,new Vector2(0,-53),new Vector2(108,22),15,font,new Color(.95f,.88f,.73f));
            statusLabel=Label("",new Vector2(0,53),new Vector2(104,18),12,font,accent);
            Refresh(0,skill.cooldown,true,0);
        }
        static void Radial(Image image)
        {
            image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Radial360;
            image.fillOrigin=(int)Image.Origin360.Top;image.fillClockwise=true;
        }
        public void Refresh(float remaining,float duration,bool affordable,float clock)
        {
            if(!initialized)return;
            Cooling=remaining>0;
            RecoveryFraction=Cooling?1-Mathf.Clamp01(remaining/Mathf.Max(.001f,duration)):1;
            colorIcon.fillAmount=RecoveryFraction;clockFill.fillAmount=RecoveryFraction;
            grayIcon.enabled=Cooling;
            // Ready-but-unaffordable remains distinct from cooldown: no false countdown.
            colorIcon.color=affordable || Cooling?Color.white:new Color(.58f,.58f,.58f,1);
            clockFill.color=Cooling?accent:affordable?accent:new Color(.44f,.34f,.25f);
            clockHand.gameObject.SetActive(Cooling);clockHand.localRotation=Quaternion.Euler(0,0,-360*RecoveryFraction);
            remainingLabel.gameObject.SetActive(Cooling);numberShade.gameObject.SetActive(Cooling);
            remainingLabel.text=Cooling?(remaining>=1?Mathf.CeilToInt(remaining).ToString():(Mathf.Ceil(remaining*10)*.1f).ToString("0.0")):"";
            statusLabel.text=Cooling?"冷却":affordable?"就绪":"体力不足";
            statusLabel.color=Cooling?new Color(.76f,.72f,.66f):affordable?accent:new Color(.94f,.59f,.36f);
            if(wasCooling && !Cooling)readyAt=clock;
            float pulse=Cooling?0:Mathf.Clamp01(1-(clock-readyAt)/.35f);
            flashRing.color=new Color(accent.r,accent.g,accent.b,pulse*.8f);flashRing.rectTransform.localScale=Vector3.one*(1+(1-pulse)*.10f);
            wasCooling=Cooling;
        }
        Image Picture(string name,Vector2 position,Vector2 size,Sprite sprite,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;
            var image=go.GetComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;return image;
        }
        TMP_Text Label(string text,Vector2 position,Vector2 size,int fontSize,TMP_FontAsset font,Color color)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;
            var label=go.GetComponent<TextMeshProUGUI>();label.text=text;label.font=font;label.fontSize=fontSize;
            label.color=color;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.extraPadding=true;return label;
        }
    }
}
