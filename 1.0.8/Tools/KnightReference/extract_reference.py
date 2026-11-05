"""Extract local reference video frames and timestamped contact sheets; never changes the video."""
import argparse
import hashlib
import json
import math
import subprocess
from pathlib import Path
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--start', type=float, default=3)
parser.add_argument('--end', type=float, default=530)
parser.add_argument('--step', type=float, default=20)
parser.add_argument('--name', default='overview')
args = parser.parse_args()
project = Path(__file__).resolve().parents[2]
video = next((Path.home() / 'Downloads').glob('*无限机兵*洛希德*.mp4'))
output = project / 'Docs/KnightDesign20261105/LoshideReference' / args.name
output.mkdir(parents=True, exist_ok=True)
reader = imageio_ffmpeg.read_frames(str(video))
meta = next(reader)
reader.close()
times = []
value = args.start
while value <= min(args.end, meta['duration'] - .2):
    times.append(round(value, 3))
    value += args.step
font = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 19)
cols = 4
tile_w, tile_h, label = 480, 270, 28
for batch in range(math.ceil(len(times)/28)):
    selection = times[batch*28:(batch+1)*28]
    sheet = Image.new('RGB', (cols*tile_w, math.ceil(len(selection)/cols)*(tile_h+label)), '#14151c')
    draw = ImageDraw.Draw(sheet)
    for i, time in enumerate(selection):
        path = output / ('frame_%07.3fs.jpg' % time)
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-ss', str(time), '-i', str(video), '-frames:v', '1', '-vf', 'scale=960:540', '-q:v', '2', '-y', str(path)], check=True, capture_output=True)
        with Image.open(path) as frame:
            frame = frame.resize((tile_w, tile_h), Image.Resampling.LANCZOS)
            x, y = (i%cols)*tile_w, (i//cols)*(tile_h+label)
            sheet.paste(frame, (x, y+label))
            draw.text((x+8, y+3), '%02d:%05.2f  [%0.3fs]' % (int(time//60), time%60, time), fill='#f0d7a5', font=font)
        print('frame', time, flush=True)
    sheet.save(output/('contact_%02d.jpg' % batch), quality=92)
(output/'manifest.json').write_text(json.dumps({'source':str(video),'source_bytes':video.stat().st_size,'source_sha256':hashlib.file_digest(video.open('rb'),'sha256').hexdigest(),'metadata':meta,'seconds':times,'purpose':'Internal visual and motion analysis; no source video or extracted audio is used as runtime content.'}, ensure_ascii=False, indent=2), encoding='utf-8')
print('OUTPUT',output,flush=True)
