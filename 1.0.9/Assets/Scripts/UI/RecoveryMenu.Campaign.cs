using UnityEngine;
using ArcaneArchive.Core;
namespace ArcaneArchive.UI
{
    public sealed partial class RecoveryMenu
    {
        BookCampaign Campaign => GetComponent<BookCampaign>();
        void BuildCampaignBook(Transform p)
        {
            var campaign=Campaign;
            Label(p,campaign.definition.book.title,new Vector2(0,155),new Vector2(840,50),32,brass);
            Label(p,"灾后的守藏院，还有人和遗赠没有离开。",new Vector2(0,100),new Vector2(840,40),22,paper);
            Button(p,"翻开故事 · 连续挑战三关",new Vector2(0,34),new Vector2(620,54),()=>campaign.StartChapter(0,true));
            for(int i=0;i<campaign.definition.chapters.Length;i++)
            {int index=i;Button(p,campaign.definition.chapters[i].title,new Vector2((i-1)*285,-52),new Vector2(270,52),()=>campaign.StartChapter(index,false));}
            Label(p,"第一章：秘文弹 / 吞印 · 后两章：剑盾 / 1–4 技能\nWASD 移动 · 空格跳跃 · Shift 冲刺 · Esc 暂停",new Vector2(0,-132),new Vector2(850,65),20,paper);
            footer.text="1.0.9 · 连续模式：缚魂者 → 典狱长 → 圣骑士 → 结尾；也可单独选关。";
        }
        void BuildCampaignStory(Transform p)
        {
            if(!Campaign)return;
            Label(p,Campaign.StoryTitle,new Vector2(0,153),new Vector2(850,52),30,brass);
            Label(p,Campaign.StoryText,new Vector2(0,6),new Vector2(830,224),23,paper);
            Button(p,Campaign.Complete?"回到书页":"继续",new Vector2(180,-172),new Vector2(270,46),()=>Campaign.ContinueStory());
            Button(p,"返回书页",new Vector2(-180,-172),new Vector2(270,46),Campaign.ReturnToBook);
            footer.text=Campaign.Complete?"I AM A MONSTER · 遗赠": "遗赠 · "+(Campaign.ChapterIndex+1)+" / 3";
        }
        void BuildCampaignResult(Transform p)
        {
            bool won=Encounter.CurrentState==BossEncounter.State.Won;
            Label(p,won?"本章战斗完成":"本次挑战失败",new Vector2(0,120),new Vector2(820,60),34,brass);
            Label(p,Campaign.Chapter.title+"\n"+(won?"撤离还在继续。翻到下一页。":"观察预警，格挡和侧移；重试会恢复生命与场地。"),new Vector2(0,30),new Vector2(840,110),23,paper);
            if(won)Button(p,Campaign.Sequential?(Campaign.ChapterIndex==2?"完成故事":"进入下一关"):"查看战后剧情",new Vector2(0,-68),new Vector2(440,48),()=>Campaign.AdvanceAfterVictory());
            Button(p,"重试本关",new Vector2(-180,-140),new Vector2(270,46),()=>Encounter.Retry());
            Button(p,"返回书页",new Vector2(180,-140),new Vector2(270,46),Campaign.ReturnToBook);
            footer.text="第 "+(Campaign.ChapterIndex+1)+" 章 / 3";
        }
    }
}
