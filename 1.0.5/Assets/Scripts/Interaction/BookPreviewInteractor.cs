using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEngine;
namespace ArcaneArchive.Interaction
{
    [DisallowMultipleComponent]
    public sealed class BookPreviewInteractor : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float distance = 3f;
        [SerializeField, Range(0f,.3f)] private float exitGraceSeconds = .15f;
        private FirstPersonController player;
        private RecoveryMenu menu;
        private Camera view;
        private Collider targetCollider;
        private float lastSeenAt;
        public DemoBookTarget CurrentTarget { get; private set; }
        private void Awake()
        {
            player=GetComponent<FirstPersonController>();
            menu=GetComponent<RecoveryMenu>();
            view=GetComponentInChildren<Camera>();
        }
        private void Update()
        {
            if(!menu || !player || !view) {ClearTarget();return;}
            var bindings=player.Bindings;
            if(!player.GameplayInputEnabled || !player.CursorIsLocked || menu.IsOpen ||
                !bindings || bindings.SuppressInputThisFrame) {ClearTarget();return;}
            var ray=view.ViewportPointToRay(new Vector3(.5f,.5f,0));
            RaycastHit hit;
            bool hitSomething=Physics.Raycast(ray,out hit,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide);
            var candidate=hitSomething?hit.collider.GetComponentInParent<DemoBookTarget>():null;
            if(candidate && candidate.Record)
            {
                CurrentTarget=candidate;targetCollider=hit.collider;lastSeenAt=Time.unscaledTime;
            }
            else
            {
                // Real geometry blocks immediately. Only an empty-ray miss receives grace.
                if(hitSomething || !CanRetainTarget(Time.unscaledTime)) {ClearTarget();return;}
            }
            menu.ShowPrompt(CurrentTarget.InteractionPrompt);
            // A grace-period label never permits interaction without a current ray hit.
            if(candidate && bindings.InteractAction.WasPressedThisFrame())
            {
                candidate.Interact(null);
                ClearTarget();
            }
        }
        private bool CanRetainTarget(float now)
        {
            if(!CurrentTarget || !CurrentTarget.isActiveAndEnabled || !CurrentTarget.Record ||
                !targetCollider || !targetCollider.enabled || !targetCollider.gameObject.activeInHierarchy ||
                now-lastSeenAt>exitGraceSeconds) return false;
            Vector3 origin=view.transform.position;
            if((targetCollider.ClosestPoint(origin)-origin).sqrMagnitude>distance*distance)return false;
            // Ignore interaction triggers while checking a wall between camera and display.
            return !Physics.Linecast(origin,targetCollider.bounds.center,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        }
        private void ClearTarget()
        {
            CurrentTarget=null;targetCollider=null;
            if(menu)menu.HidePrompt();
        }
        private void OnDisable() {ClearTarget();}
    }
}
