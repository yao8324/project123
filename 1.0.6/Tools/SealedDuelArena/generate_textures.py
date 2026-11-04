"""Deterministic original tiling PBR surfaces, no third-party texture dependency."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
root=Path(__file__).resolve().parents[2]/"Assets/Art/SealedDuelArena20261104/Textures"
root.mkdir(parents=True,exist_ok=True)
n=1024;rng=np.random.default_rng(11042026)
def noise(scale):
 a=rng.random((scale,scale)).astype(np.float32)
 # Tileable fBm: pad wrapped low-resolution samples before bicubic interpolation.
 p=np.pad(a,((1,1),(1,1)),mode="wrap")
 im=Image.fromarray(p,mode="F").resize((n+2*n//scale,n+2*n//scale),Image.Resampling.BICUBIC)
 k=n//scale;return np.asarray(im)[k:k+n,k:k+n].copy()
coarse=noise(8);medium=noise(40);fine=noise(200);grain=rng.random((n,n)).astype(np.float32)
height=coarse*.12+medium*.22+fine*.34+grain*.32
for name,color,amp,rough in [("Limestone",(155,150,140),.26,.82),("Basalt",(85,88,88),.27,.71),("WeatheredStone",(128,128,122),.31,.86),("Bronze",(130,100,61),.22,.48),("VaultStone",(154,151,141),.25,.84)]:
 h=height.copy()
 if name=="Bronze":
  h=.70*noise(32)+.25*fine+.05*grain
  brushing=np.sin(np.arange(n)[:,None]*2.7)*.008;h+=brushing
 h=(h-.5)*amp
 shade=1+h+np.minimum(fine-.48,0)*.16
 if name=="VaultStone":
  yy=np.arange(n)[:,None]/n*2;xx=np.arange(n)[None,:]/n*4+np.floor(yy)*.5
  u=xx%1;v=yy%1;joint=(np.minimum(u,1-u)<.012)|(np.minimum(v,1-v)<.012)
  h-=joint*.10;shade-=joint*.24
 rgb=np.clip(np.stack([shade*c for c in color],axis=-1),0,255).astype("uint8")
 Image.fromarray(rgb).save(root/(name+"_Base.png"))
 # Unity tangent-space normal PNG, explicitly imported as NormalMap by the builder.
 dx=(np.roll(h,-1,axis=1)-np.roll(h,1,axis=1))*.65;dy=(np.roll(h,-1,axis=0)-np.roll(h,1,axis=0))*.65
 normal=np.stack([-dx,-dy,np.ones_like(h)],axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
 Image.fromarray(np.clip((normal*.5+.5)*255,0,255).astype("uint8")).save(root/(name+"_Normal.png"))
 # Standard URP metallic texture: metal in R, smoothness in alpha.
 met=np.full((n,n,4),255,dtype="uint8");met[:,:,0]=190 if name=="Bronze" else 0;met[:,:,1:3]=0
 met[:,:,3]=np.clip((1-rough+(medium-.5)*.12)*255,0,255).astype("uint8")
 Image.fromarray(met).save(root/(name+"_MetalSmooth.png"))
print("Generated",len(list(root.glob("*.png"))),"original PBR textures in",root)
