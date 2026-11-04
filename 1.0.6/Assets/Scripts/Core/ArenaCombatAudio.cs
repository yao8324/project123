using UnityEngine;
namespace ArcaneArchive.Core
{
 public sealed class ArenaCombatAudio:MonoBehaviour
 {
  public AudioClip guardBlock,guardRaise,armorHit,heavyArmorHit,playerHurt,meleeSwingReserved;
  public AudioClip crystalBreak,crystalDebris,chainHit,chainBreak,chainLoop,crystalLoop;
  public AudioClip volleyRelease,volleyCharge,groundFracture,shockwave,laserCharge,rush;
  BossEncounter owner;AudioSource voice;AudioSource[] chainSources,crystalSources;
  bool paused;public int GuardRaiseCount{get;private set;}public int GuardBlockCount{get;private set;}
  public int ActiveCrystalLoops{get{int n=0;if(crystalSources!=null)foreach(var s in crystalSources)if(s.isPlaying)n++;return n;}}
  public int ActiveChainLoops{get{int n=0;if(chainSources!=null)foreach(var s in chainSources)if(s.isPlaying)n++;return n;}}
  AudioSource Source(Transform parent,bool spatial){var g=new GameObject("Combat_Audio_Voice");g.transform.SetParent(parent,false);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=spatial?1:0;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=2;s.maxDistance=24;return s;}
  public void Initialize(BossEncounter e)
  {
   owner=e;if(!voice)voice=Source(transform,false);
   if(chainSources==null && e.PowerNetwork){chainSources=new AudioSource[3];crystalSources=new AudioSource[3];for(int i=0;i<3;i++){chainSources[i]=Source(e.PowerNetwork.visuals.crystals[i],true);crystalSources[i]=Source(e.PowerNetwork.visuals.crystals[i],true);}}
   if(chainSources!=null)for(int i=0;i<3;i++){StartLoop(chainSources[i],chainLoop,i*.7f);StartLoop(crystalSources[i],crystalLoop,i*1.3f);}
   paused=false;GuardRaiseCount=GuardBlockCount=0;StopLaser();
  }
  void StartLoop(AudioSource s,AudioClip clip,float offset){s.Stop();s.clip=clip;s.loop=true;s.volume=.65f;if(clip){s.time=Mathf.Repeat(offset,clip.length);s.Play();}}
  void Update()
  {
   if(!owner)return;if(owner.IsFinished){StopAll();return;}
   if(owner.IsPaused!=paused){paused=owner.IsPaused;if(voice){if(paused)voice.Pause();else voice.UnPause();}
    if(chainSources!=null)for(int i=0;i<3;i++){if(paused){chainSources[i].Pause();crystalSources[i].Pause();}else{chainSources[i].UnPause();crystalSources[i].UnPause();}}}
  }
  public void Play(AudioClip clip,float volume=.35f){if(voice&&clip)voice.PlayOneShot(clip,volume);}
  public void RaiseGuard(){GuardRaiseCount++;Play(guardRaise,.28f);}
  public void Block(){GuardBlockCount++;Play(guardBlock,.4f);}
  public void HitArmor(bool heavy){Play(heavy?heavyArmorHit:armorHit,heavy?.42f:.34f);}
  public void HitChain(){Play(chainHit,.3f);}
  public void BreakChain(int i){if(chainSources!=null)chainSources[i].Stop();Play(chainBreak,.36f);}
  public void BreakCrystal(int i){if(crystalSources!=null)crystalSources[i].Stop();Play(crystalBreak,.4f);Play(crystalDebris,.12f);}
  public bool Warn(int attack){if(attack==0&&volleyCharge){Play(volleyCharge,.2f);return true;}if(attack==3&&laserCharge){Play(laserCharge,.28f);return true;}return false;}
  public void Attack(int attack){if(attack==4)Play(rush,.36f);}
  public void StopLaser(){}
  public void StopAll(){if(voice)voice.Stop();StopLaser();if(chainSources!=null)for(int i=0;i<3;i++){chainSources[i].Stop();crystalSources[i].Stop();}}
  void OnDisable(){StopAll();}
 }
}
