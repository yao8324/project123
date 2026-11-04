"""Fetch the two reviewed CC0 Poly Haven materials, verify hashes, and pack URP maps."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import subprocess,json,hashlib
from PIL import Image
import numpy as np
root=Path(__file__).resolve().parents[2];source=root/'ArtSource/SealedDuelArena20261104/PolyHaven';source.mkdir(parents=True,exist_ok=True)
out=root/'Assets/Art/SealedDuelArena20261104/Textures';records=[];jobs=[]
for asset,prefix in [('large_sandstone_blocks','PH_Wall'),('stone_tiles','PH_Floor')]:
 response=subprocess.run(['curl','--fail','--silent','--show-error','--max-time','30','https://api.polyhaven.com/files/'+asset],capture_output=True,text=True,check=True)
 data=json.loads(response.stdout)
 for channel in ['Diffuse','nor_gl','Rough','AO']:
  spec=data[channel]['2k']['jpg'];path=source/(asset+'_'+channel+'.jpg');jobs.append((path,spec))
 records.append({'asset':asset,'prefix':prefix,'license':'CC0','page':'https://polyhaven.com/a/'+asset,'channels':{k:data[k]['2k']['jpg'] for k in ['Diffuse','nor_gl','Rough','AO']}})
def fetch(job):
 path,spec=job
 if not path.exists() or hashlib.md5(path.read_bytes()).hexdigest()!=spec['md5']:
  subprocess.run(['curl','--fail','--silent','--show-error','--retry','2','--max-time','120','-o',str(path),spec['url']],check=True)
 assert hashlib.md5(path.read_bytes()).hexdigest()==spec['md5'],str(path)
 with Image.open(path) as im:im.verify()
 print('verified',path.name,flush=True)
with ThreadPoolExecutor(max_workers=4) as executor:list(executor.map(fetch,jobs))
for rec in records:
 asset=rec['asset'];prefix=rec['prefix']
 Image.open(source/(asset+'_Diffuse.jpg')).convert('RGB').save(out/(prefix+'_Base.png'))
 Image.open(source/(asset+'_nor_gl.jpg')).convert('RGB').save(out/(prefix+'_Normal.png'))
 Image.open(source/(asset+'_AO.jpg')).convert('RGB').save(out/(prefix+'_Occlusion.png'))
 rough=np.asarray(Image.open(source/(asset+'_Rough.jpg')).convert('L'));metal=np.zeros((*rough.shape,4),dtype='uint8');metal[:,:,3]=255-rough
 Image.fromarray(metal).save(out/(prefix+'_MetalSmooth.png'))
(source/'sources.json').write_text(json.dumps(records,ensure_ascii=False,indent=2))
(source/'LICENSE-CC0.txt').write_text('Poly Haven assets are supplied under CC0. Reviewed asset pages and exact file hashes are recorded in sources.json. Large Sandstone Blocks by Rob Tuytel; Stone Tiles by Christopher Melani. Original maps retained here; Unity maps are channel-packed copies. No paid assets used.\nhttps://polyhaven.com/license\n')
print('Packed two CC0 2K PBR materials for URP.',flush=True)
