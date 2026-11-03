using UnityEngine;

namespace ArcaneArchive.Contest
{
    // Frame-rate independent integration. UI, storage and input stay with the story owner.
    public sealed class ContestPowerChallenge
    {
        public float Strength { get; private set; }
        public float Progress { get; private set; }
        public float OverloadSeconds { get; private set; }
        public int Failures { get; private set; }
        public bool NeedsRelease { get; private set; }
        public bool Assist { get; set; }
        public void Reset(bool requireRelease=true)
        {Strength=Progress=OverloadSeconds=0;NeedsRelease=requireRelease;}
        public void Suspend(){NeedsRelease=true;OverloadSeconds=0;}
        // Result 1 = a pulse completed, -1 = overloaded, 0 = still active.
        public int Step(float seconds, bool held, bool eligible, bool paused)
        {
            if(paused || !eligible){Suspend();return 0;}
            if(NeedsRelease){if(!held)NeedsRelease=false;return 0;}
            float remaining=Mathf.Max(0,seconds);
            while(remaining>.00001f)
            {
                float dt=Mathf.Min(remaining,1f/120f);remaining-=dt;
                Strength=Mathf.Clamp(Strength+(held?(Assist?20:30):-(Assist?20:25))*dt,0,100);
                float low=Assist?20:35,high=Assist?85:70;
                if(Strength>=low && Strength<=high)Progress+=dt;
                OverloadSeconds=Strength>85?OverloadSeconds+dt:0;
                if(OverloadSeconds>=.5f)
                {Failures++;Strength=20;Progress=OverloadSeconds=0;NeedsRelease=true;return -1;}
                if(Progress>=1.2f){Strength=Progress=OverloadSeconds=0;NeedsRelease=true;return 1;}
            }
            return 0;
        }
    }
}
