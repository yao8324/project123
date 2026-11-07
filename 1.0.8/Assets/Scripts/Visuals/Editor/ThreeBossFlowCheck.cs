using System; using System.IO; using System.Linq; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
namespace ArcaneArchive.Visuals.Editor {
 public static class ThreeBossFlowCheck {
 public static void Baseline() {
 EditorSceneManager.OpenScene("Assets/Scenes/BossTests/Knight_Sanctuary_R3_Combat.unity");
 bool present=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(x=>x && x.GetType().Name=="BookCampaign");
 Directory.CreateDirectory("Docs/ThreeBoss"); File.WriteAllText("Docs/ThreeBoss/Baseline.txt",present?"Unexpected existing campaign":"FAIL expected: three-boss book campaign and next-level flow are missing");
 if(!present)throw new Exception("Expected missing feature: no three-boss book campaign / next-level flow");
 }
 }
}
