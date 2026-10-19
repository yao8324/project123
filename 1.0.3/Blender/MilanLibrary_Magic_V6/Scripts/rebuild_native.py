"""Rebuild V6 from its preserved Before snapshot, in an independent Blender process.
Usage: blender --background --python /absolute/path/Scripts/rebuild_native.py
This overwrites the V6 Assembly output in this package folder. Copy the folder first
if you have subsequently edited the assembly.
"""
import bpy,runpy
from pathlib import Path
P=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(P/'MilanLibrary_Magic_V6_Before.blend'))
for step in ['02_stations.py','03_enchanted_shelves.py','04_cameras.py','04b_final_cleanup.py','09_emblem_direct_mount.py','05_validate.py','06_inventory.py']:
    runpy.run_path(str(P/'Scripts'/step))
