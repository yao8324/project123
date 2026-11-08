using System;
using System.IO;
using UnityEngine;
namespace ArcaneArchive.Soulbinder {
 [Serializable] public sealed class SoulbinderRecord {public int version=1;public bool bossDefeated;public string decision="Pending";public int assistTokens;}
 public static class SoulbinderCheckpoint {
  public static bool Valid(SoulbinderRecord r)=>r!=null&&r.version==1&&r.bossDefeated&&(r.decision=="Pending"||r.decision=="Contain"||r.decision=="Contract")&&r.assistTokens==(r.decision=="Contract"?1:0);
  public static bool Read(string path,out SoulbinderRecord r,out string error){r=null;error=null;try{if(!File.Exists(path))return false;r=JsonUtility.FromJson<SoulbinderRecord>(File.ReadAllText(path));if(!Valid(r))throw new InvalidDataException("存档格式不匹配");return true;}catch(Exception e){r=null;error=e.Message;return false;}}
  public static bool Write(string path,SoulbinderRecord r,out string error){error=null;try{if(!Valid(r))throw new InvalidDataException("存档状态无效");Directory.CreateDirectory(Path.GetDirectoryName(path));var tmp=path+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(r,true));if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);return true;}catch(Exception e){error=e.Message;return false;}}
 }
}
