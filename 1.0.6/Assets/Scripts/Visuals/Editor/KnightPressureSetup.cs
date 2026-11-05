using ArcaneArchive.Core;
using ArcaneArchive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace ArcaneArchive.Visuals.Editor
{
    public static class KnightPressureSetup
    {
        const string Root="Assets/Art/Bosses/Knight/Refined/";
        public static void Tune(KnightDefinition config)
        {
            config.observeTime=.55f;config.phaseTwoObserve=.38f;config.walkSpeed=3.6f;
            config.thrustTravel=6;config.orbitTravel=3.2f;
            var move=config.Get(KnightMove.OathCombo);move.reach=5.4f;move.arc=150;
            move=config.Get(KnightMove.SealThrust);move.reach=9;move.width=1.8f;
            move=config.Get(KnightMove.GateCleave);move.reach=15;move.width=2.1f;
            move=config.Get(KnightMove.ShieldBash);move.reach=3.5f;move.arc=130;
            move=config.Get(KnightMove.OrbitSweep);move.reach=6.5f;move.arc=300;
            move=config.Get(KnightMove.Heavenfall);move.reach=2.2f;
            EditorUtility.SetDirty(config);
        }
        [MenuItem("Arcane Archive/Knight/Connect book and tune pressure")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save the scene and exit Play before applying book setup.");
            var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(Root+"KnightDefinitionRefined.asset");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"KnightRefinedChinese.asset");
            if(!config || !font)throw new System.InvalidOperationException("Refined assets are required.");
            Tune(config);string current=SceneManager.GetActiveScene().path;int connected=0;
            try
            {
                foreach(var build in EditorBuildSettings.scenes)
                {
                    if(!build.enabled)continue;
                    var scene=EditorSceneManager.OpenScene(build.path);
                    bool changed=false;
                    foreach(var root in scene.GetRootGameObjects())foreach(var menu in root.GetComponentsInChildren<RecoveryMenu>(true))
                    {menu.bookKnight=config;menu.bookKnightFont=font;EditorUtility.SetDirty(menu);connected++;changed=true;}
                    if(changed)EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
            }
            finally{if(!string.IsNullOrEmpty(current))EditorSceneManager.OpenScene(current);}
            return "Connected book menus: "+connected;
        }
    }
}
