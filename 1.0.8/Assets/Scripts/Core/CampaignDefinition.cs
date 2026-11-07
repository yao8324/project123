using System;
using ArcaneArchive.Knowledge;
using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Core
{
    [CreateAssetMenu(menuName="Arcane Archive/Book Campaign")]
    public sealed class CampaignDefinition : ScriptableObject
    {
        [Serializable] public sealed class Chapter
        {
            public string title;
            [TextArea(3,12)] public string introduction, aftermath;
            public BossDefinition combat;
            public BossArena arena;
            public KnightDefinition knight;
        }
        public DemoBookRecord book;
        public Chapter[] chapters;
        public string endingTitle;
        [TextArea(5,20)] public string ending;
    }
}
