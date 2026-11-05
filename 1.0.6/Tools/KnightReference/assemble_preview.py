"""Package captured native game frames into a compact review GIF and contact sheet."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse

parser=argparse.ArgumentParser()
parser.add_argument('clip',choices=['Orbit','Heavenfall','OathCombo'])
parser.add_argument('--version',choices=['RefinementV2','PressureV3','SelectionV4'],default='RefinementV2')
args=parser.parse_args()
project=Path(__file__).resolve().parents[2]
root=project/'Docs/KnightDesign20261105'/args.version
paths=sorted((root/(args.clip+'Frames')).glob('frame_*.png'))
if not paths:raise RuntimeError('No completed native frames')
frames=[]
for p in paths:
    with Image.open(p) as image:
        frame=image.convert('RGB').resize((960,540),Image.Resampling.LANCZOS)
        frames.append(frame)
frames[0].save(root/(args.clip+'.gif'),save_all=True,append_images=frames[1:],duration=83,loop=0,optimize=False)
font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',17)
sheet=Image.new('RGB',(1920,3*298),'#11131a')
draw=ImageDraw.Draw(sheet)
for j in range(12):
    i=round(j*(len(frames)-1)/11);x=(j%4)*480;y=(j//4)*298
    sheet.paste(frames[i].resize((480,270),Image.Resampling.LANCZOS),(x,y+28))
    draw.text((x+8,y+4),f'{args.clip}  {i/12:.2f}s (controlled)',font=font,fill='#ead7a6')
sheet.save(root/(args.clip+'_Contact.jpg'),quality=94)
frames[round(len(frames)*.48)].save(root/(args.clip+'_Release.png'))
print('Saved',args.clip,'frames',len(frames),'gif_bytes',(root/(args.clip+'.gif')).stat().st_size)
