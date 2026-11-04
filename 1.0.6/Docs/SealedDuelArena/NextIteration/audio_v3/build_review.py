from pathlib import Path
import subprocess,json,numpy as np,wave,html
p=Path('/Users/jack/Desktop/虚拟环境团队/最新游戏_art_20261104/1.0.6/Docs/SealedDuelArena/NextIteration');a=p/'audio_v3'; sources=json.loads((a/'sources.json').read_text());sr=48000
extra=[dict(id='chain_drag',file='chain_drag.mp3',title='Chain Drag Floor — Hitrison',page='https://freesound.org/people/Hitrison/sounds/191513/',license='CC0；当前为网站HQ MP3试听版',license_url='https://creativecommons.org/publicdomain/zero/1.0/'),dict(id='grunt01',file='grunt01.wav',title='Damage Grunt Male 01 — VoiceBosch',page='https://opengameart.org/content/damage-sounds-male-audio-pack',license='CC BY-SA 4.0；署名VoiceBosch，改编音频遵循相同许可',license_url='https://creativecommons.org/licenses/by-sa/4.0/'),dict(id='chain_drop',file='chain_drop.wav',title='Chain Dropped — BlastwaveFx.com',page='https://soundbible.com/559-Chain-Dropped.html',license='CC BY 3.0；署名BlastwaveFx.com',license_url='https://creativecommons.org/licenses/by/3.0/')]
sources+= [x for x in extra if (a/x['file']).exists()]
def read(file,filters='anull'):
 b=subprocess.check_output(['ffmpeg','-v','error','-i',str(a/file),'-af',filters,'-ar',str(sr),'-ac','1','-f','f32le','-']);return np.frombuffer(b,dtype='<f4').copy()
def save(id,x,peak):
 x=x/(max(abs(x))+1e-9)*10**(peak/20)
 with wave.open(str(a/(id+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((x*32767).astype('<i2').tobytes())
 return x
def loop(x):
 # overlap tail into head; the new boundary follows adjacent source samples.
 n=min(sr,len(x)//5); w=np.linspace(0,1,n); return np.r_[x[-n:]*(1-w)+x[:n]*w,x[n:-n]]
chain=read('chain_drag.mp3','highpass=f=90,lowpass=f=1800'); chain=chain[8*sr:17*sr];save('chain_loop',loop(chain),-29)
hum=read('2132.wav','highpass=f=35,lowpass=f=650');hum=np.tile(hum, max(1,int(10*sr/len(hum))+1))[:9*sr];save('crystal_loop',loop(hum),-31)
# Derived chain break: recorded blade-on-chainmail attack + actual loose chain drop.
if (a/'chain_drop.wav').exists():
 x=read('2790.wav');y=read('chain_drop.wav');z=np.zeros(max(len(x),len(y)+int(.08*sr)));z[:len(x)]+=x*.7;z[int(.08*sr):int(.08*sr)+len(y)]+=y*.65;save('chain_break_edit',z,-8)
for id,title,parent,note in [('chain_loop','重链摩擦 · 低音量循环','chain_drag','实录拖链截取、滤去刺耳高频、交叉淡化成约8秒循环；并非悬浮锁链现场录音。'),('crystal_loop','水晶供能 · 低沉循环','2132','以网站低电流嗡鸣为底材，滤波、接缝处理、降低电平；魔法氛围替代素材。'),('chain_break_edit','锁链崩断 · 素材组合','chain_drop','剑切链甲＋重链落地组合；不是实际拉断铁链录音。')]:
 if (a/(id+'.wav')).exists():
  base=next(x for x in sources if x['id']==parent);sources.append(dict(id=id,file=id+'.wav',title=title,page=base['page'],license=base['license']+(' + Mixkit Sound Effects Free License' if id=='chain_break_edit' else ''),license_url=base['license_url'],edit=note))
checks=[]
for s in sources:
 x=read(s['file']);s['duration']=round(len(x)/sr,2);s['peak_dbfs']=round(float(20*np.log10(max(abs(x))+1e-10)),2);checks.append(dict(id=s['id'],duration=s['duration'],decoded=True))
(a/'sources.json').write_text(json.dumps(sources,ensure_ascii=False,indent=2));(a/'verification.json').write_text(json.dumps(checks,indent=2));(a/'build_review.py').write_text(Path('/tmp/build_real_page.py').read_text())
groups=[('01 / 挥砍与盔甲',[('2792','快速挥砍','普通攻击，短促破风。'),('1506','剑刃掠过','与上条比较，选择更合适的重量感。'),('2765','剑击盔甲','命中硬质护甲。'),('2776','剑刃击链甲','柔性甲片与链节的接触质感。'),('2795','重剑撞金属','重击或格挡候选。'),('2797','剑撞厚金属','防御碰撞备选。')]),('02 / 真人受伤',[('grunt01','受伤短哼','作者明确为录制人声；建议轻伤优先试听。'),('2197','疼痛人声备选','比较角色年龄、力度与演出是否合适。')]),('03 / 水晶、锁链与两种背景',[('759','水晶破碎底材','玻璃被重物击碎；先保留物理碎裂，不加闪亮提示音。'),('172','碎片尾音','玻璃碎屑摩擦，可少量叠在破碎尾部。'),('chain_drag','重链拖动原始试听','93秒长录音，保留原始质感；循环加工版见下。'),('chain_drop','重链落地','用于链节断开后的散落尾音。'),('2790','锁链受击候选','原素材是剑切链甲，并非重链断裂。'),('chain_break_edit','锁链破碎组合','以现成金属接触声与重链落地组合。'),('chain_loop','低音量锁链摩擦循环','每枚独立；断链时停止。'),('crystal_loop','低音量水晶共鸣循环','每枚独立；断链继续，破晶才停止。')]),('04 / Boss五种招式的素材方向',[('2380','飞页：快速纸张运动','齐射释放的物理层。'),('2653','飞页：风中纸张','蓄势与纸页集聚底材。'),('2599','冲击环：重型电冲击','能量爆发候选；属于设计音，不是实录魔法。'),('403','地裂：碎石坠落','地裂尾部碎石与重量感。'),('1703','地裂：爆裂与碎石','较强烈备选，需裁短并压低。'),('3201','激光：充能嗡鸣','蓄能、持续束流底材，需按招式剪辑。'),('1489','冲刺：空气掠过','冲刺破风，搭配实际命中反馈。'),('2799','冲刺急停：金属刮擦','甲胄运动或急停擦碰的备选。'),('2746','供能备用：暗色管风琴持续音','明显有音乐性，仅供比较，默认不用它做每枚水晶背景。')])]
def card(i,title,note):
 s=next((s for s in sources if s['id']==i),None)
 if not s:return ''
 return f'''<article><small>{html.escape(s['title'])} · {s['duration']}s</small><h3>{title}</h3><audio controls preload="metadata" {'loop' if i.endswith('_loop') else ''} src="audio_v3/{s['file']}"></audio><p>{note}</p><p class="muted">{s.get('edit','网上现成原素材，未改音色；Mixkit未逐项说明录制方法，因此不一概标为实录。')}</p><a href="{s['page']}" target="_blank">来源页面</a> · <a href="{s['license_url']}" target="_blank">{s['license']}</a></article>'''
body=''.join('<h2>'+t+'</h2><section>'+''.join(card(*v) for v in vs)+'</section>' for t,vs in groups)
page='''<!doctype html><html lang="zh"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>真实质感素材 · 新版音效试听</title><style>body{background:#11151b;color:#e8e5dd;font:16px/1.65 system-ui;margin:0}main{max-width:1160px;margin:auto;padding:40px 24px}h1{font-size:40px}h2{margin-top:48px;color:#d8b881}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:18px}article{padding:22px;border:1px solid #3c3c39;background:#1b2027;border-radius:12px}small,.muted{color:#a9aeb6;font-size:13px}audio{width:100%}a{color:#d8b881}button{padding:12px 20px;background:#d8b881;border:0;border-radius:6px;cursor:pointer}.intro{max-width:860px}img{width:100%;border-radius:14px}</style><main><p>IM MONSTER / SOUND MATERIAL REVIEW · V3</p><h1>真实的材质，克制的魔法</h1><div class="intro"><p>这一版改为网上现成素材。先比较挥砍、盔甲命中和真人受伤，再听锁链与水晶。背景循环保持低电平，破坏反馈用物理撞击与碎片；不再使用上一版自制电子音。</p><p>重链拖动与VoiceBosch人声有录制说明。其他条目按网站原描述标注；能量、激光属于设计音。全部仍是待你选择的候选，尚未替换游戏资源。</p><p>循环规则不变：断链只停该组摩擦，破晶再停该路共鸣；每枚水晶独立控制。素材原声音量不同，试听可先把设备音量调低。</p></div><button onclick="document.querySelectorAll('audio').forEach(a=>a.pause())">停止所有声音</button> · <a href="改进总体汇总.md">玩法与改动位置</a> · <a href="旧版合成试听.html">上一版归档</a>'''+body+'''<h2>场景方向保持不变</h2><p>中央开阔、外围三座护晶、悬浮紫符文锁链、三路供能汇聚Boss；本次只更新声音选材。</p><img src="锁链护晶_总体改进概念_v2.png" alt="斗场概念图"><h2>来源记录</h2><p>Mixkit条目依据 Sound Effects Free License 标注；Freesound重链为CC0；VoiceBosch为CC BY-SA 4.0；BlastwaveFx链落地为CC BY 3.0。加工内容与署名见 <a href="audio_v3/sources.json">完整记录</a>。本页面仅用于本地项目选材，未把第三方素材包公开发布。</p></main><script>document.querySelectorAll('audio').forEach(a=>a.addEventListener('play',()=>document.querySelectorAll('audio').forEach(b=>{if(b!==a)b.pause()})))</script></html>'''
if not (p/'旧版合成试听.html').exists():(p/'旧版合成试听.html').write_text((p/'试听与方案.html').read_text())
(p/'试听与方案.html').write_text(page)
(p/'真实素材试听.html').write_text(page)
print('Decoded',len(sources),'source/edited files; page created.')
