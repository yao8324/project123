using ArcaneArchive.Player;
using ArcaneArchive.Visuals.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MilanAcceptance
{
    public static class ReviewFinalization
    {
        // Distinct bundle identity prevents Finder from routing the new review to an older running app.
        public static void BuildReview()
        {
            string name=PlayerSettings.productName,version=PlayerSettings.bundleVersion;
            var target=UnityEditor.Build.NamedBuildTarget.Standalone;
            string identifier=PlayerSettings.GetApplicationIdentifier(target);
            try
            {
                PlayerSettings.productName="Milan Library Review 20261007";
                PlayerSettings.bundleVersion="2026.10.7";
                PlayerSettings.SetApplicationIdentifier(target,"com.arcanearchive.milan.review20261007");
                MilanAcceptanceBuild.BuildMacPlayer();
            }
            finally
            {
                PlayerSettings.productName=name;PlayerSettings.bundleVersion=version;
                PlayerSettings.SetApplicationIdentifier(target,identifier);AssetDatabase.SaveAssets();
            }
        }
        public static void Prepare()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MilanLibrary_Magic_V6.unity");
            var player=Object.FindFirstObjectByType<FirstPersonController>();
            MilanFirstPersonSetup.Attach(player.gameObject,Camera.main);
            MilanPerformanceSetup.ConfigureScene();
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            MilanArchiveValidation.Run();
            Debug.Log("MILAN_REVIEW_FINAL_SCENE_PREPARED");
        }
    }
}
