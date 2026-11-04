using System.Collections;
using ArcaneArchive.Core;
using UnityEngine;
namespace ArcaneArchive.Visuals
{
    // Direct playable entry scene; the ordinary library scene continues using its existing Boss menu entry.
    public sealed class SealedArenaShowcaseEntry : MonoBehaviour
    {
        IEnumerator Start(){yield return null;yield return null;var encounter=GetComponent<BossEncounter>();if(encounter && !encounter.IsActive && !encounter.Begin())Debug.LogError("Sealed arena direct entry failed.");}
    }
}
