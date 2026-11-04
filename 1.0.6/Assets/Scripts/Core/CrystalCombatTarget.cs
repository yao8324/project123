using UnityEngine;
namespace ArcaneArchive.Core
{
    public sealed class CrystalCombatTarget : MonoBehaviour
    {
        public BossPowerNetwork network;
        public int index;
        public bool Available=>network && !network.IsBroken(index);
        public bool Hit(float damage)=>network && network.Hit(index,damage);
        public string Description=>!Available?"":network.ChainRemaining(index)>0?
            "锁链 "+Mathf.CeilToInt(network.ChainRemaining(index))+" / "+network.chainHealth:
            "水晶 "+Mathf.CeilToInt(network.CrystalRemaining(index))+" / "+network.crystalHealth;
    }
}
