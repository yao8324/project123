using ArcaneArchive.Player;
using UnityEngine;

namespace ArcaneArchive.Interaction
{
    [DisallowMultipleComponent]
    public sealed class AutomaticEntranceDoor : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private float openDistance = 5.5f;
        [SerializeField] private float closeDistance = 7f;
        [SerializeField] private float approachHalfWidth = 3.5f;
        [SerializeField] private float openingSeconds = 1.25f;
        [SerializeField] private float closingSeconds = 1.7f;
        [SerializeField] private float openingAngle = 90f;
        [SerializeField] private float safetyHalfWidth = 3.9f;
        [SerializeField] private float safetyDepth = 3.9f;
        private float swingDirection = 1f;
        public float OpenAmount { get; private set; }
        public bool OpenRequested { get; private set; }

        private void Awake()
        {
            if(!player)
            {
                var controller=FindFirstObjectByType<FirstPersonController>();
                if(controller)player=controller.transform;
            }
            if(!player || !leftHinge || !rightHinge)
            {
                Debug.LogError("Automatic entrance door requires player and both hinges.",this);
                enabled=false;
            }
        }
        private void Update() { Tick(Time.deltaTime); }
        private void Tick(float deltaTime)
        {
            if(!player || !leftHinge || !rightHinge)return;
            Vector3 p=transform.InverseTransformPoint(player.position);
            bool sameFloor=p.y>-1f && p.y<6.5f;
            bool approach=sameFloor && Mathf.Abs(p.x)<approachHalfWidth && Mathf.Abs(p.z)<openDistance;
            // Keep the whole doorway and both possible leaf sweeps occupied until clear.
            bool occupied=sameFloor && Mathf.Abs(p.x)<safetyHalfWidth && Mathf.Abs(p.z)<safetyDepth;
            bool departed=!sameFloor || Mathf.Abs(p.z)>closeDistance || Mathf.Abs(p.x)>approachHalfWidth+1.5f;
            if(approach || occupied)
            {
                if(OpenAmount<=.001f)swingDirection=p.z>=0f?1f:-1f;
                OpenRequested=true;
            }
            else if(departed)OpenRequested=false;
            float previous=OpenAmount;
            OpenAmount=Mathf.MoveTowards(OpenAmount,OpenRequested?1f:0f,deltaTime/Mathf.Max(.1f,OpenRequested?openingSeconds:closingSeconds));
            if(Mathf.Approximately(previous,OpenAmount))return;
            float eased=OpenAmount*OpenAmount*(3f-2f*OpenAmount);
            float angle=openingAngle*eased*swingDirection;
            leftHinge.localRotation=Quaternion.Euler(0,angle,0);
            rightHinge.localRotation=Quaternion.Euler(0,-angle,0);
            // CharacterController queries must see the moving solid leaf colliders this frame.
            Physics.SyncTransforms();
        }
    }
}
