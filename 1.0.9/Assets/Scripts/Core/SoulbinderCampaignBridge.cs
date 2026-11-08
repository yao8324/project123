using System;
using System.Collections;
using System.Linq;
using ArcaneArchive.Soulbinder;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ArcaneArchive.Core {
 // Keep the original player, equipment and chapter state alive while the first arena owns input.
 public sealed class SoulbinderCampaignBridge:MonoBehaviour {
  static SoulbinderCampaignBridge current;
  public static bool Active=>current!=null;
  public static bool Transitioning=>current!=null&&current.transitioning;
  BookCampaign owner;Scene original,arena;GameObject[] roots;bool[] wasActive;bool transitioning;
  SoulbinderLevel level;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){current=null;}
  public static bool Enter(BookCampaign campaign,string sceneName){
   if(Active||!campaign||!Application.CanStreamedLevelBeLoaded(sceneName))return false;
   var go=new GameObject("Chapter scene transition");var b=go.AddComponent<SoulbinderCampaignBridge>();current=b;b.owner=campaign;b.original=campaign.gameObject.scene;b.roots=b.original.GetRootGameObjects().Where(r=>r!=go).ToArray();b.wasActive=b.roots.Select(r=>r.activeSelf).ToArray();DontDestroyOnLoad(go);
   b.StartCoroutine(b.Load(sceneName));return true;
  }
  IEnumerator Load(string sceneName){
   transitioning=true;Time.timeScale=1;for(int i=0;i<roots.Length;i++)roots[i].SetActive(false);
   AsyncOperation operation=null;Exception failure=null;
   try{operation=SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Additive);}catch(Exception e){failure=e;}
   if(failure!=null){Restore();owner.ExternalSceneFailed(failure.Message);Dispose();yield break;}
   while(!operation.isDone)yield return null;
   arena=SceneManager.GetSceneByName(sceneName);if(!arena.IsValid()){Restore();owner.ExternalSceneFailed("关卡加载失败");Dispose();yield break;}
   SceneManager.SetActiveScene(arena);level=arena.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SoulbinderLevel>(true)).FirstOrDefault();
   if(!level){var unload=SceneManager.UnloadSceneAsync(arena);while(unload!=null&&!unload.isDone)yield return null;Restore();owner.ExternalSceneFailed("关卡控制器缺失");Dispose();yield break;}
   transitioning=false;
  }
  public static bool Return(bool victory){
   var b=current;if(!b||b.transitioning||!b.arena.IsValid())return false;
   if(victory&&(!b.level||!b.level.Completed||b.level.Model.Phase!=EncounterPhase.Won))return false;
   b.StartCoroutine(b.Unload(victory));return true;
  }
  IEnumerator Unload(bool victory){
   transitioning=true;Time.timeScale=1;SceneManager.SetActiveScene(original);var op=SceneManager.UnloadSceneAsync(arena);while(op!=null&&!op.isDone)yield return null;
   Restore();var campaign=owner;Dispose();if(campaign)campaign.FinishExternalChapter(victory);
  }
  void Restore(){if(original.IsValid())SceneManager.SetActiveScene(original);for(int i=0;i<roots.Length;i++)if(roots[i])roots[i].SetActive(wasActive[i]);}
  void Dispose(){if(current==this)current=null;Destroy(gameObject);}
  void OnGUI(){if(transitioning){GUI.color=new Color(0,0,0,.9f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(Screen.width/2-50,Screen.height/2-15,200,40),"Loading chapter...");}}
 }
}
