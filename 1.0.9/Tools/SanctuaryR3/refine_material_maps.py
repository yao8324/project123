"""R3-only periodic material fields; sparse warped mineral seams, not noise contours."""
from pathlib import Path
import numpy as np
from PIL import Image
P=Path(__file__).resolve().parents[2];out=P/'Assets/Art/Bosses/Knight/SanctuaryR3/Textures';out.mkdir(exist_ok=True)
N=2048;rng=np.random.default_rng(5031)
def noise(k):
 a=rng.random((k,k));im=Image.fromarray(np.uint8(np.tile(a,(3,3))*255)).resize((N*3,N*3),Image.Resampling.BICUBIC)
 return np.asarray(im,dtype=np.float32)[N:2*N,N:2*N]/255
coarse=noise(4);cloud=noise(11);medium=noise(47);fine=noise(280);grain=rng.random((N,N),dtype=np.float32)
y,x=np.mgrid[:N,:N].astype(np.float32)/N
# Integer winding and periodic noise guarantee repeatable boundary behavior.
u=x+y+.20*(coarse-.5)+.065*(cloud-.5)+.009*(medium-.5)
veins=np.zeros_like(x);haze=np.zeros_like(x)
for position,width,strength in [(.12,.0011,.85),(.43,.0022,1),(.49,.00065,.45),(.82,.0015,.65)]:
 distance=np.abs((u-position+.5)%1-.5);gate=np.clip((cloud-.24)*2.2,0,1)
 veins+=np.exp(-(distance/width)**2)*strength*gate
 haze+=np.exp(-(distance/(width*7))**2)*strength*gate
for name in ['Marble','Slate','Gold','GateSteel']:
 if name=='Marble':
  base=np.array([.81,.785,.725]);rgb=base+(.055*(coarse-.5)+.025*(cloud-.5))[...,None]-veins[...,None]*np.array([.12,.115,.10])-haze[...,None]*.028
  gloss=.64+.06*(cloud-.5);metal=0;height=.008*fine+.002*grain
 elif name=='Slate':
  base=np.array([.255,.278,.315]);rgb=base+(.045*(coarse-.5)+.025*(cloud-.5))[...,None]+veins[...,None]*np.array([.065,.062,.056])+haze[...,None]*.016
  gloss=.62+.09*(cloud-.5);metal=0;height=.012*fine+.003*grain
 elif name=='Gold':
  rgb=np.array([.70,.51,.245])+(.035*(cloud-.5)+.008*(fine-.5))[...,None]
  gloss=.64+.13*(cloud-.5)+.035*(fine-.5);metal=1;height=.006*fine+.002*grain
 else:
  rgb=np.array([.17,.195,.23])+(.035*(cloud-.5)+.018*(medium-.5))[...,None]
  gloss=.43+.10*(medium-.5);metal=1;height=.025*medium+.012*fine
 rgb+=.003*(grain-.5)[...,None]
 Image.fromarray(np.uint8(np.clip(rgb,0,1)*255)).save(out/(name+'_Albedo.png'))
 dx=(np.roll(height,-1,1)-np.roll(height,1,1));dy=(np.roll(height,-1,0)-np.roll(height,1,0));normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=2);normal/=np.linalg.norm(normal,axis=2)[...,None]
 Image.fromarray(np.uint8((normal*.5+.5)*255)).save(out/(name+'_Normal.png'))
 mask=np.zeros((N,N,4),np.uint8);mask[...,0]=int(metal*255);mask[...,1]=255;mask[...,3]=np.uint8(np.clip(gloss,0,1)*255)
 Image.fromarray(mask).save(out/(name+'_Mask.png'))
 print(name,'smoothness',float(gloss.min()),float(gloss.max()))
