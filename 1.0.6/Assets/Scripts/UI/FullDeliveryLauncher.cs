using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ArcaneArchive.UI
{
 public sealed class FullDeliveryLauncher:MonoBehaviour
 {
  public Font font;
  void Start(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(Array.IndexOf(Environment.GetCommandLineArgs(),"-crystal-smoke")>=0)SceneManager.LoadScene("SealedDuelArena_Showcase");}
  void OnGUI(){GUI.skin.font=font;float x=(Screen.width-600)/2f,y=Screen.height/2f-150;GUI.Box(new Rect(x-30,y-35,660,330),"");GUI.skin.label.fontSize=26;GUI.Label(new Rect(x,y,600,45),"IM MONSTER · 完整整合试玩");GUI.skin.label.fontSize=17;GUI.Label(new Rect(x,y+50,600,50),"剧情交互反馈＋分类环境声＋三水晶 Boss 斗场");GUI.skin.button.fontSize=22;if(GUI.Button(new Rect(x,y+115,600,65),"剧情探索 · 遗赠"))SceneManager.LoadScene("IMMonsterContest20261107");if(GUI.Button(new Rect(x,y+195,600,65),"Boss 斗场 · 锁链与水晶"))SceneManager.LoadScene("SealedDuelArena_Showcase");}
 }
}
