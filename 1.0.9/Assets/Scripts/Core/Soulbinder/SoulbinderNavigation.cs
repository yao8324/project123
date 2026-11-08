using UnityEngine;
using UnityEngine.AI;
namespace ArcaneArchive.Soulbinder {
 [DefaultExecutionOrder(-100)] public sealed class SoulbinderNavigation:MonoBehaviour {
  public NavMeshData data;NavMeshDataInstance instance;
  void OnEnable(){if(data)instance=NavMesh.AddNavMeshData(data);}
  void OnDisable(){if(instance.valid)instance.Remove();}
 }
}
