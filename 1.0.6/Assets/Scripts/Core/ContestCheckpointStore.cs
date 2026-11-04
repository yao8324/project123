using System;
using System.IO;
using UnityEngine;

namespace ArcaneArchive.Contest
{
    [Serializable]
    public sealed class ContestStoryState
    {
        public int version = 1;
        public int stage, verifiedMask, nodes, clues, pulses, batchLocation;
        public bool sealedBatch, incidentRead, identityKnown, rescued, transferred, tutorialDone, assist;
        public ContestStoryState Copy() => JsonUtility.FromJson<ContestStoryState>(JsonUtility.ToJson(this));
        public bool IsValid()
        {
            if(version!=1 || stage<0 || stage>7 || verifiedMask<0 || verifiedMask>7 || nodes<0 || nodes>3 ||
                clues<0 || clues>7 || pulses<0 || pulses>3 || batchLocation<0 || batchLocation>3)return false;
            if(stage>=2 && (verifiedMask!=7 || !sealedBatch))return false;
            if(stage>=4 && nodes!=3)return false;
            if(stage>=5 && clues!=7)return false;
            if(stage>=6 && !identityKnown)return false;
            if(rescued && pulses!=3)return false;
            if(pulses==3 && !rescued)return false;
            if(stage<2 && batchLocation!=0)return false;
            if(stage>=2 && stage<6 && batchLocation!=1)return false;
            if(stage==6 && (batchLocation<1 || batchLocation>2 || (batchLocation==2 && !rescued)))return false;
            if(identityKnown && stage<6)return false;
            if(transferred && stage!=7)return false;
            if(stage==7 && (!transferred || batchLocation!=3 || !rescued))return false;
            return true;
        }
    }

    public static class ContestCheckpointStore
    {
        public static bool TryRead(string path, out ContestStoryState state, out string message)
        {
            state=null;message=null;
            if(!File.Exists(path))return false;
            try
            {
                var info=new FileInfo(path);
                if(info.Length>32768)throw new InvalidDataException("checkpoint too large");
                state=JsonUtility.FromJson<ContestStoryState>(File.ReadAllText(path));
                if(state==null || !state.IsValid())throw new InvalidDataException("invalid checkpoint");
                return true;
            }
            catch(Exception ex)
            {
                state=null;message="此记录无法继续，请开始新游戏。";
                Debug.LogWarning("[Contest] Checkpoint rejected: "+ex.GetType().Name);return false;
            }
        }
        public static bool TryWrite(string path, ContestStoryState state, out string message)
        {
            message=null;
            if(state==null || !state.IsValid()){message="检查点状态不完整，未覆盖原记录。";return false;}
            string temporary=path+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary,JsonUtility.ToJson(state,true));
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
                return true;
            }
            catch(Exception ex){message="检查点保存失败，可继续本次游戏。";Debug.LogWarning("[Contest] Checkpoint write: "+ex.GetType().Name);return false;}
            finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }
    }
}
