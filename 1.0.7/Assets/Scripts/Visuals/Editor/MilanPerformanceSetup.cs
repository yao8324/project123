using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ArcaneArchive.Player;

namespace ArcaneArchive.Visuals.Editor
{
    public static class MilanPerformanceSetup
    {
        const string Folder = "Assets/Art/MilanLibraryV6/Settings/";
        public static void ConfigureScene()
        {
            var ultra = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder + "MilanV6_URP.asset");
            if (!ultra) throw new InvalidOperationException("Preserved Ultra pipeline missing.");
            var balanced = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder + "MilanV6_Balanced.asset");
            if (!balanced) { balanced = UnityEngine.Object.Instantiate(ultra); AssetDatabase.CreateAsset(balanced, Folder + "MilanV6_Balanced.asset"); }
            else EditorUtility.CopySerialized(ultra, balanced);
            balanced.name = "MilanV6_Balanced";
            // Selected from the measured, bounded runtime comparisons; leave Ultra intact.
            balanced.renderScale = .85f;
            var pipeline = new SerializedObject(balanced);
            pipeline.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            pipeline.FindProperty("m_AnyShadowsSupported").boolValue = false;
            pipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(balanced);

            var quality = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = quality.FindProperty("m_QualitySettings");
            int ultraIndex = -1, balancedIndex = -1;
            for(int i=0;i<levels.arraySize;i++)
            {
                string name = levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                if(name == "Ultra") ultraIndex=i;
                if(name == "Balanced") balancedIndex=i;
            }
            if(ultraIndex < 0) throw new InvalidOperationException("Ultra quality level missing.");
            if(balancedIndex < 0)
            {
                // Inserting after Ultra duplicates that complete serialized level.
                levels.InsertArrayElementAtIndex(ultraIndex);
                balancedIndex = ultraIndex; ultraIndex++;
                levels.GetArrayElementAtIndex(balancedIndex).FindPropertyRelative("name").stringValue="Balanced";
            }
            var ultraLevel = levels.GetArrayElementAtIndex(ultraIndex);
            var balancedLevel = levels.GetArrayElementAtIndex(balancedIndex);
            ultraLevel.FindPropertyRelative("customRenderPipeline").objectReferenceValue=ultra;
            balancedLevel.FindPropertyRelative("customRenderPipeline").objectReferenceValue=balanced;
            balancedLevel.FindPropertyRelative("vSyncCount").intValue=1;
            quality.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline=ultra;
            QualitySettings.SetQualityLevel(ultraIndex,true);
            PlayerSettings.defaultScreenWidth=1280;
            PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            if(!player) throw new InvalidOperationException("Original scene controller missing.");
            if(!player.GetComponent<MilanQualityOptions>()) player.gameObject.AddComponent<MilanQualityOptions>();
            // Opening the existing scene in the Inspector clamps this authored value to .01.
            // Preserve the existing viewmodel camera framing when saving only quality settings.
            var overlay=player.GetComponentsInChildren<Camera>(true).Single(c=>c.name=="FirstPersonPresentationCamera");
            overlay.nearClipPlane=.005f;
            AssetDatabase.SaveAssets();
        }
        public static void ApplyToExistingScene()
        {
            for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save unsaved scenes before applying the performance profile.");
            string scene="Assets/Scenes/MilanLibrary_Magic_V6.unity";
            var opened=EditorSceneManager.OpenScene(scene);
            ConfigureScene();
            if(!EditorSceneManager.SaveScene(opened,scene)) throw new IOException("Failed to save performance profile binding.");
            MilanArchiveValidation.Run();
            Debug.Log("MILAN_BALANCED_PROFILE_SAVED");
        }
    }
}
