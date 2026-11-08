"""Original deterministic, periodic stone and antique-gold PBR textures for the sanctuary."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
N=2048
rng=np.random.default_rng(1107)
out=Path(__file__).resolve().parents[2]/'Assets/Art/Bosses/Knight/Sanctuary20261107/Textures'
out.mkdir(parents=True,exist_ok=True)
def noise(k):
 a=rng.random((k,k));tiled=np.tile(a,(3,3))
 im=Image.fromarray(np.uint8(tiled*255)).resize((N*3,N*3),Image.Resampling.BICUBIC)
 return np.asarray(im,dtype=np.float32)[N:2*N,N:2*N]/255
coarse=noise(5);cloud=noise(13);medium=noise(45);fine=noise(320)
grain=rng.random((N,N),dtype=np.float32)
y,x=np.mgrid[0:N,0:N].astype(np.float32)/N
scratch_im=Image.new('L',(N,N));draw=ImageDraw.Draw(scratch_im)
for _ in range(1100):
 sx=int(rng.integers(0,N));sy=int(rng.integers(0,N));length=int(rng.integers(5,100));tilt=int(rng.integers(-5,6))
 for ox in (-N,0,N):
  for oy in (-N,0,N):draw.line((sx+ox,sy+oy,sx+length+ox,sy+tilt+oy),fill=int(rng.integers(55,180)),width=1)
scratches=np.asarray(scratch_im,dtype=np.float32)/255
for name,color,metal,smooth in [
 ('Limestone',(.72,.69,.63),0,.27),
 ('Marble',(.79,.775,.735),0,.42),
 ('Slate',(.275,.29,.315),0,.46),
 ('Brass',(.76,.59,.32),.92,.46),
 ('Steel',(.24,.27,.31),.65,.34),
 ('Wood',(.28,.18,.105),0,.28),
 ('Leather',(.19,.105,.065),0,.22)]:
 variation=.032*(cloud-.5)+.025*(medium-.5)+.018*(fine-.5)+.009*(grain-.5)
 height=.08*medium+.1*fine+.025*grain
 rgb=np.broadcast_to(np.array(color,dtype=np.float32),(N,N,3)).copy()
 metallic=np.full((N,N),metal,dtype=np.float32);gloss=np.full((N,N),smooth,dtype=np.float32)
 occlusion=np.ones((N,N),dtype=np.float32)
 if name=='Marble':
  phase=2*np.pi*(2*x+y)+coarse*8+cloud*2.4+medium*.55
  vein=np.exp(-np.square(np.abs(np.sin(phase))/.085))
  hair=np.exp(-np.square(np.abs(np.sin(phase*2.7+cloud*2))/.035))
  broad=np.exp(-np.square(np.abs(np.sin(phase))/.30))
  rgb+=variation[:,:,None]
  rgb-=broad[:,:,None]*np.array([.035,.031,.022])
  rgb-=vein[:,:,None]*np.array([.075,.068,.050])
  rgb-=hair[:,:,None]*np.array([.035,.031,.023])
  # Mineral veins are inside the stone, not embossed cables.
  height=.026*fine+.008*grain
  gloss+=.035*(cloud-.5)-.025*vein
 elif name=='Limestone':
  pits=np.maximum(0,.055-grain)*1.8
  rgb+=variation[:,:,None]-pits[:,:,None]
  height-=pits*2;occlusion-=pits*.6
 elif name=='Slate':
  phase=2*np.pi*(3*x+2*y)+coarse*8+cloud*2+medium*.4
  vein=np.exp(-np.square(np.abs(np.sin(phase))/.13))
  hair=np.exp(-np.square(np.abs(np.sin(phase*2.2+cloud*3))/.045))
  rgb+=variation[:,:,None]+.055*(coarse-.5)[:,:,None]
  rgb+=vein[:,:,None]*np.array([.055,.051,.044])+hair[:,:,None]*np.array([.032,.030,.025])
  height=.045*fine+.018*grain;gloss+=.08*(cloud-.5)-.05*vein
 elif name=='Brass':
  patina=np.clip((cloud-.48)*3,0,1)*np.clip((medium-.3)*2,0,1)
  pores=np.maximum(0,.06-grain)*2
  rgb+=variation[:,:,None]*.6
  rgb-=patina[:,:,None]*np.array([.065,.055,.03])
  rgb+=scratches[:,:,None]*np.array([.04,.035,.02])
  rgb-=pores[:,:,None]*.4
  height=.06*fine-.11*scratches-.4*pores
  metallic-=patina*.08;gloss-=patina*.10+scratches*.10;gloss+=.025*(fine-.5)
  occlusion-=patina*.10+pores*.6
 elif name=='Wood':
  stripes=np.sin(2*np.pi*22*x+coarse*4+medium*.9)
  rgb+=(.022*stripes+.045*(medium-.5)+.02*(fine-.5))[:,:,None]
  height=.04*stripes+.04*fine
 elif name=='Steel':
  rgb+=variation[:,:,None]*.5-scratches[:,:,None]*.015
  height=.04*fine-.04*scratches;gloss-=scratches*.08
 else:
  rgb+=variation[:,:,None]*.6;height=.17*fine+.05*grain
 Image.fromarray(np.uint8(np.clip(rgb,0,1)*255),'RGB').save(out/(name+'_Albedo.png'))
 dx=(np.roll(height,-1,1)-np.roll(height,1,1))*1.3;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*1.3
 normals=np.stack((-dx,-dy,np.ones_like(dx)),axis=2);normals/=np.linalg.norm(normals,axis=2)[:,:,None]
 Image.fromarray(np.uint8((normals*.5+.5)*255),'RGB').save(out/(name+'_Normal.png'))
 mask=np.zeros((N,N,4),dtype=np.uint8);mask[:,:,0]=np.uint8(np.clip(metallic,0,1)*255);mask[:,:,1]=np.uint8(np.clip(occlusion,0,1)*255);mask[:,:,3]=np.uint8(np.clip(gloss+(fine-.5)*.06,0,1)*255)
 Image.fromarray(mask,'RGBA').save(out/(name+'_Mask.png'))
 print(name)
