using System;
using UnityEngine;
using UnityEngine.Events;

namespace IMMonster.SceneArt
{
    // Asset-level interaction contract. Story progression and input remain owned by program.
    public sealed class MaintenanceArtAnchor : MonoBehaviour
    {
        [Serializable] public sealed class RequestEvent : UnityEvent<string> { }
        public string anchorId;
        public string displayLabel;
        public float interactionDistance = 2.3f;
        public Transform interactionPoint;
        public Renderer indicator;
        public Color idleColor = new Color(.10f,.52f,.62f);
        public Color stableColor = new Color(.30f,.72f,.70f);
        public RequestEvent interactionRequested = new RequestEvent();
        [SerializeField] bool stable;
        public bool IsStable => stable;
        public bool RequestInteraction(Vector3 playerPosition)
        {
            Vector3 point = interactionPoint ? interactionPoint.position : transform.position;
            if (stable || Vector3.Distance(playerPosition, point) > interactionDistance) return false;
            interactionRequested.Invoke(anchorId);
            return true;
        }
        public void SetStable(bool value)
        {
            stable = value;
            if (!indicator) return;
            var block = new MaterialPropertyBlock(); indicator.GetPropertyBlock(block);
            block.SetColor("_BaseColor", stable ? stableColor : idleColor);
            block.SetColor("_EmissionColor", (stable ? stableColor : idleColor)*.65f);
            indicator.SetPropertyBlock(block);
        }
        void OnEnable() { SetStable(stable); }
    }
}
