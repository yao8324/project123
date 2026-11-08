using UnityEngine;
using ArcaneArchive.UI;

namespace ArcaneArchive.Core
{
    [DisallowMultipleComponent]
    public sealed class BookCampaign : MonoBehaviour
    {
        public CampaignDefinition definition;
        public int ChapterIndex { get; private set; } = -1;
        public int Victories { get; private set; }
        public bool Complete { get; private set; }
        public bool ShowingStory { get; private set; }
        public string StoryTitle { get; private set; }
        public string StoryText { get; private set; }
        public string Error { get; private set; }
        public bool Sequential { get; private set; }
        public bool Active => ChapterIndex>=0 && !Complete;
        public CampaignDefinition.Chapter Chapter => definition && ChapterIndex>=0 && ChapterIndex<definition.chapters.Length ? definition.chapters[ChapterIndex] : null;
        BossEncounter encounter;
        RecoveryMenu menu;
        bool aftermath;
        int lastActionFrame=-1;
        void Awake(){encounter=GetComponent<BossEncounter>();menu=GetComponent<RecoveryMenu>();}
        void Start(){if(definition && definition.book)menu.ShowBook(definition.book);}
        public bool StartChapter(int index,bool sequential)
        {
            if(!definition || definition.chapters==null || index<0 || index>=definition.chapters.Length || ShowingStory || encounter.IsActive)return false;
            ChapterIndex=index;Sequential=sequential;Victories=0;Complete=false;aftermath=false;Error=null;
            Show(Chapter.title,Chapter.introduction);return true;
        }
        void Show(string title,string text)
        {StoryTitle=title;StoryText=text;ShowingStory=true;menu.ShowPage(RecoveryMenu.Page.CampaignStory);}
        public bool ContinueStory()
        {
            if(!ShowingStory || lastActionFrame==Time.frameCount)return false;
            lastActionFrame=Time.frameCount;
            if(Complete){ReturnToBook();return true;}
            if(aftermath)
            {
                aftermath=false;
                if(!Sequential){ReturnToBook();return true;}
                if(ChapterIndex==definition.chapters.Length-1)
                {Complete=true;Show(definition.endingTitle,definition.ending);return true;}
                ChapterIndex++;Show(Chapter.title,Chapter.introduction);return true;
            }
            ShowingStory=false;
            bool began;
            if(!string.IsNullOrEmpty(Chapter.externalScene))began=SoulbinderCampaignBridge.Enter(this,Chapter.externalScene);
            else if(Chapter.knight)began=encounter.BeginKnight(Chapter.knight);
            else{encounter.definition=Chapter.combat;encounter.arenaPrefab=Chapter.arena;began=encounter.Begin();}
            if(!began){Error="本关资源暂不可用，请返回书页重试。";Show(Chapter.title,Error);}
            return began;
        }
        internal void FinishExternalChapter(bool won)
        {
            if(!Active||ShowingStory)return;
            if(!won){ReturnToBook();return;}
            Victories++;aftermath=true;Show(Chapter.title+" · 战后",Chapter.aftermath);
        }
        internal void ExternalSceneFailed(string message){Error="关卡未能载入，请返回书页重试。";Show(Chapter.title,Error);Debug.LogError("Chapter load failed: "+message);}
        public bool AdvanceAfterVictory()
        {
            if(!Active || ShowingStory || encounter.CurrentState!=BossEncounter.State.Won)return false;
            // ReturnToLibrary cleans up combat, HUD and scoped lighting before another arena is loaded.
            if(!encounter.ReturnToLibrary())return false;
            Victories++;aftermath=true;Show(Chapter.title+" · 战后",Chapter.aftermath);return true;
        }
        public void ReturnToBook()
        {
            if(encounter.IsActive && !encounter.ReturnToLibrary())return;
            ChapterIndex=-1;ShowingStory=false;aftermath=false;Error=null;
            menu.ShowBook(definition.book);
        }
    }
}
