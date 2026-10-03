"""Background renderer. Invoked by Blender MCP; does not modify native files."""
import bpy,json,sys,time,traceback
from pathlib import Path
P=Path(__file__).resolve().parents[1]
job=json.loads(Path(sys.argv[sys.argv.index('--')+1]).read_text());s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
progress=P/'Audit'/job.get('progress','render_progress.json')
try:
 pref=bpy.context.preferences.addons['cycles'].preferences
 try:pref.compute_device_type='METAL';pref.get_devices()
 except Exception:pass
 for d in pref.devices:d.use=d.type=='METAL'
 s.cycles.device='GPU';s.render.engine='CYCLES'
 for i,j in enumerate(job['renders']):
  s.camera=bpy.data.objects[j['camera']];s.render.resolution_x=j.get('width',1200);s.render.resolution_y=j.get('height',800);s.render.resolution_percentage=100;s.cycles.samples=j.get('samples',32)
  s.render.use_compositing=j.get('composite',True);s.frame_set(j.get('frame',35));s.render.image_settings.file_format='PNG';s.render.filepath=str(P/j['file'])
  progress.write_text(json.dumps({'complete':False,'index':i,'rendering':j['file']}));bpy.ops.render.render(write_still=True)
 progress.write_text(json.dumps({'complete':True,'count':len(job['renders'])}))
except Exception:progress.write_text(json.dumps({'complete':False,'error':traceback.format_exc()}));raise
