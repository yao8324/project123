using UnityEngine;

namespace ArcaneArchive.Contest
{
    public sealed class ContestWorldTarget : MonoBehaviour
    {
        public string id;
        public Transform aimPoint;
        public Vector3 Point => aimPoint?aimPoint.position:transform.position;
    }
}
