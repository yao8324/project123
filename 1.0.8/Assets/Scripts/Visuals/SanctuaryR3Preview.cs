using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace ArcaneArchive.Visuals
{
    // Isolated art/animation preview. This does not replace the team's boss state machine.
    public sealed class SanctuaryR3Preview : MonoBehaviour
    {
        public Animator actor;
        public AnimationClip[] clips;
        public Camera view;
        public float combatRadius = 28f;
        PlayableGraph graph;
        Vector3 origin;
        Quaternion rotation;
        double elapsed;
        int selected;
        bool playing;
        public double Elapsed => elapsed;
        public bool Playing => playing;
        void Start(){ origin=actor.transform.position; rotation=actor.transform.rotation; Play(0); }
        public void Play(int index)
        {
            if(clips==null || index<0 || index>=clips.Length || !clips[index])return;
            if(graph.IsValid())graph.Destroy();
            selected=index; elapsed=0; playing=true;
            actor.transform.SetPositionAndRotation(origin,rotation); actor.Rebind(); actor.applyRootMotion=true;
            graph=PlayableGraph.Create("R3 animation preview");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output=AnimationPlayableOutput.Create(graph,"Actor",actor);
            var playable=AnimationClipPlayable.Create(graph,clips[index]);
            output.SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
        }
        void Update()
        {
            if(!playing || !graph.IsValid())return;
            var remaining=clips[selected].length-elapsed;
            float step=Mathf.Min(Time.deltaTime,(float)remaining);
            if(step>0){graph.Evaluate(step);elapsed+=step;}else playing=false;
        }
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(18,18,380,180),GUI.skin.box);
            GUILayout.Label("R3 — first-pass animation preview / not final combat");
            GUILayout.Label("Combat diameter: 56 m");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Walk"))Play(0);
            if(GUILayout.Button("Dash"))Play(1);
            if(GUILayout.Button("Jump attack"))Play(2);
            GUILayout.EndHorizontal();
            if(GUILayout.Button(playing?"Pause":"Resume"))playing=!playing;
            GUILayout.Label("Model and animation refinement remains pending.");
            GUILayout.EndArea();
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
        void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Vector3 prev=new Vector3(combatRadius,0,0);for(int i=1;i<=128;i++){float a=i*Mathf.PI*2/128;var next=new Vector3(Mathf.Cos(a)*combatRadius,0,Mathf.Sin(a)*combatRadius);Gizmos.DrawLine(prev,next);prev=next;}}
    }
}
