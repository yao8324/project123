#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ArcaneArchive.Soulbinder.Editor {
 [InitializeOnLoad] public static class SoulbinderPlayTest {
  static SoulbinderPlayTest(){EditorApplication.playModeStateChanged+=State;}
  public static void Run(){SessionState.SetBool("SoulbinderRuntimeTest",true);EditorSceneManager.OpenScene(SoulbinderSceneBuilder.ScenePath);EditorApplication.EnterPlaymode();}
  static void State(PlayModeStateChange change){if(change==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("SoulbinderRuntimeTest",false)){SessionState.SetBool("SoulbinderRuntimeTest",false);new GameObject("Runtime acceptance").AddComponent<SoulbinderRuntimeProbe>();}}
 }
}
#endif
