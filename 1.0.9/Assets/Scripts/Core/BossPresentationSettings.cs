using UnityEngine;

namespace ArcaneArchive.Core
{
    public static class BossPresentationSettings
    {
        public const string HintsKey="ArcaneArchive.BossCombatHints";
        public static bool ShowHints
        {
            get=>PlayerPrefs.GetInt(HintsKey,1)!=0;
            set{PlayerPrefs.SetInt(HintsKey,value?1:0);PlayerPrefs.Save();}
        }
    }
}
