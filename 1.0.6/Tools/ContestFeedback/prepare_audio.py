"""Prepare compact in-game audio from the CC0 sources listed below.

Usage: python3 prepare_audio.py --sources /path/to/downloaded/source/archives
Requires ffmpeg on PATH. No audio is generated at runtime.
"""
import argparse
from array import array
from pathlib import Path
import hashlib
import json
import math
import subprocess
import tempfile
import wave
import zipfile

RATE = 22050
PROJECT = Path(__file__).resolve().parents[2]
DEST = PROJECT / "Assets/Contest20261107/Resources/ContestAudio"
SOURCES = {
    "book_flips.zip": {"author":"StarNinjas", "page":"https://opengameart.org/content/10-book-page-flips", "download":"https://opengameart.org/sites/default/files/book_flips_-_starninjas.zip"},
    "various.zip": {"author":"laleksic", "page":"https://opengameart.org/content/various-sound-effects", "download":"https://opengameart.org/sites/default/files/sounds_8.zip"},
    "rpg.zip": {"author":"rubberduck", "page":"https://opengameart.org/content/80-cc0-rpg-sfx", "download":"https://opengameart.org/sites/default/files/80-CC0-RPG-SFX_0.zip"},
    "room.zip": {"author":"LEGIT Audio", "page":"https://opengameart.org/content/the-shop", "download":"https://opengameart.org/sites/default/files/legit_audio_-_the_shop_free_sfx_mp3.zip"},
    "dungeon.ogg": {"author":"JaggedStone", "page":"https://opengameart.org/content/loopable-dungeon-ambience", "download":"https://opengameart.org/sites/default/files/dungeon_ambient_1_0.ogg"},
}

def decode(path, filters="anull", seconds=26):
    result = subprocess.run(["ffmpeg","-v","error","-i",str(path),"-t",str(seconds),"-af",filters,"-ar",str(RATE),"-ac","1","-f","f32le","pipe:1"],check=True,capture_output=True)
    samples=array("f");samples.frombytes(result.stdout);return samples

def normalize(samples, peak=.32):
    maximum=max((abs(x) for x in samples),default=0)
    if maximum<.00001:raise ValueError("Silent input")
    gain=peak/maximum
    return array("f",(x*gain for x in samples))

def event(samples):
    samples=normalize(samples)
    significant=[i for i,x in enumerate(samples) if abs(x)>.001]
    samples=samples[max(0,significant[0]-220):min(len(samples),significant[-1]+441)]
    fade=min(220,len(samples)//4)
    for i in range(fade):
        samples[i]*=i/fade;samples[-1-i]*=i/fade
    return samples

def loop(samples, seconds=24):
    length=RATE*seconds
    samples=array("f",(samples[i%len(samples)] for i in range(length)))
    # Wrap the last .6 seconds into the first .6 seconds without a silent gap.
    overlap=int(RATE*.6)
    blend=array("f",(samples[length-overlap+i]*(1-i/overlap)+samples[i]*i/overlap for i in range(overlap)))
    return samples[overlap:length-overlap]+blend

def add_layer(bed, sound, when, gain):
    start=int(when*RATE)
    for i,x in enumerate(sound):
        if start+i<len(bed):bed[start+i]+=x*gain

def save(name, samples):
    path=DEST/(name+".wav")
    pcm=array("h",(round(max(-.98,min(.98,x))*32767) for x in samples))
    with wave.open(str(path),"wb") as output:
        output.setnchannels(1);output.setsampwidth(2);output.setframerate(RATE);output.writeframes(pcm.tobytes())
    return {"file":str(path.relative_to(PROJECT)),"seconds":round(len(samples)/RATE,3),"sha256":hashlib.sha256(path.read_bytes()).hexdigest()}

def main():
    parser=argparse.ArgumentParser();parser.add_argument("--sources",type=Path,required=True);args=parser.parse_args()
    DEST.mkdir(parents=True,exist_ok=True)
    manifest={"license":"CC0-1.0","license_url":"https://creativecommons.org/publicdomain/zero/1.0/", "sources":SOURCES,"outputs":{}}
    with tempfile.TemporaryDirectory(prefix="immonster-audio-") as temp:
        root=Path(temp)
        for archive, info in SOURCES.items():
            original=args.sources/archive;info["sha256"]=hashlib.sha256(original.read_bytes()).hexdigest()
            if original.suffix==".zip":
                target=root/original.stem;target.mkdir()
                with zipfile.ZipFile(original) as z:
                    for name in z.namelist():
                        if Path(name).name==name and not name.endswith("/"):(target/name).write_bytes(z.read(name))
        book=root/"book_flips";foley=root/"various";rpg=root/"rpg";room=next((root/"room").glob("01*.mp3"))
        recipes={
            "PaperOpen":(book/"book_flip.1.ogg","anull"),
            "PaperClose":(book/"book_flip.3.ogg","anull"),
            "Cloth":(book/"book_flip.8.ogg","lowpass=f=1600"),
            "Hardware":(foley/"pickup_keys.wav","anull"),
            "Node":(foley/"switch_on.wav","anull"),
            "Door":(foley/"door_creak_open.wav","anull"),
            "Ui":(foley/"switch_off.wav","atrim=duration=0.11"),
            "PowerStart":(rpg/"spell_01.ogg","lowpass=f=3500"),
            "PowerSuccess":(rpg/"item_gem_01.ogg","lowpass=f=4000"),
            "Overload":(foley/"grind_stone.wav","atrim=duration=0.65,afade=t=out:st=0.4:d=0.25"),
            "Bundle":(rpg/"book_04.ogg","lowpass=f=2400"),
            "Transfer":(foley/"drawer_close.wav","anull"),
            "Impact":(foley/"door_close.wav","anull"),
            "Alarm":(foley/"bell.wav","aecho=0.8:0.6:350:0.3"),
        }
        clips={}
        for name,(path,filters) in recipes.items():
            clips[name]=event(decode(path,filters,4));manifest["outputs"][name]=save(name,clips[name])
            manifest["outputs"][name]["recipe"]=str(path.relative_to(root))+"; "+filters
        room_bed=normalize(decode(room,"highpass=f=80,lowpass=f=1800"),.13)
        cave_bed=normalize(decode(args.sources/"dungeon.ogg","highpass=f=60,lowpass=f=2600"),.22)
        for name,base,cave_gain,room_gain in [("Calm","room",0,1),("Tension","cave",1,.15),("Maintenance","cave",.8,.2),("Restored","room",.18,1),("Investigation","room",.48,.65),("Rescue","cave",1,.3),("Ending","room",.06,1)]:
            rb=loop(room_bed);cb=loop(cave_bed)
            bed=array("f",(r*room_gain+c*cave_gain for r,c in zip(rb,cb)))
            creak=event(decode(foley/"floor_creak1.wav","lowpass=f=1400",3))
            for when in [5,16]:add_layer(bed,creak,when,.09 if name in ["Calm","Ending","Restored"] else .18)
            if name=="Calm":add_layer(bed,clips["PaperClose"],11,.08)
            if name in ["Tension","Rescue"]:
                for when in [3,10,18]:add_layer(bed,clips["Alarm"],when,.08)
            if name=="Maintenance":
                grind=event(decode(foley/"grind_metal.wav","lowpass=f=900",3))
                for when in [2,13]:add_layer(bed,grind,when,.22)
            manifest["outputs"][name]=save(name,bed)
            manifest["outputs"][name]["recipe"]="Recorded room/cave beds, seamless wrap and distant recorded foley layers; "+name
        power=normalize(decode(foley/"grind_glass.wav" if (foley/"grind_glass.wav").exists() else foley/"grind_metal.wav","lowpass=f=1600",6),.16)
        manifest["outputs"]["PowerLoop"]=save("PowerLoop",loop(power,6))
        manifest["outputs"]["PowerLoop"]["recipe"]="Recorded metal grind, low-pass and seamless loop; playback pitch follows controlled strength"
    output=PROJECT/"Docs/Feedback20261104/audio-sources.json";output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n")
    print("Prepared",len(manifest["outputs"]),"audio clips")

if __name__=="__main__":main()
