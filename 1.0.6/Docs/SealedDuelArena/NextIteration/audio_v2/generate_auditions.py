from pathlib import Path
import numpy as np, wave, json, shutil, html
P=Path('/Users/jack/Desktop/虚拟环境团队/最新游戏_art_20261104/1.0.6/Docs/SealedDuelArena/NextIteration'); O=P/'audio_v2'; O.mkdir(exist_ok=True)
sr=48000; rng=np.random.default_rng(44); entries=[]; signals={}
def noise(n,lo=100,hi=4000):
 x=rng.normal(size=n); f=np.fft.rfftfreq(n,1/sr); z=np.fft.rfft(x); z*=((f>lo)&(f<hi)); y=np.fft.irfft(z,n); return y/(np.std(y)+1e-9)
def hit(d,freqs):
 t=np.arange(int(d*sr))/sr; x=np.zeros(len(t))
 for i,f in enumerate(freqs): x+=np.sin(2*np.pi*f*t+rng.uniform(0,6.28))*np.exp(-t*(5+i*2))/(i+1)
 return x+noise(len(t),150,5500)*np.exp(-t*45)*.35
def save(id,title,x,note,loop=False,peak=-9):
 x=x/(max(abs(x))+1e-9)*10**(peak/20)
 if not loop:
  k=min(240,len(x)//4); x[:k]*=np.linspace(0,1,k); x[-k:]*=np.linspace(1,0,k)
 with wave.open(str(O/(id+'.wav')),'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr); w.writeframes((x*32767).astype('<i2').tobytes())
 signals[id]=x; entries.append(dict(id=id,title=title,note=note,loop=loop,duration=round(len(x)/sr,2),peak_db=round(20*np.log10(max(abs(x))+1e-12),2),rms_db=round(20*np.log10(np.sqrt(np.mean(x*x))+1e-12),2),seam_delta=round(float(abs(x[0]-x[-1])),7) if loop else None))
def burst(d,lo,hi):
 t=np.arange(int(sr*d))/sr; return noise(len(t),lo,hi)*np.sin(np.pi*t/d)**2
save('A01','挥砍 · 暗刃破风',burst(.55,350,4500)+.15*hit(.55,[180,340]),'挥动有效帧；挥空也播放。')
save('A02','命中盔甲 · 厚重金属',hit(1.1,[210,537,891,1340,2311]),'实际命中盔甲时叠加，区别于链节。')
save('A04','主角受伤 · 钝击护印震裂',hit(.8,[67,113,199])+.3*burst(.8,70,700),'无真人气声的合成受创稿；成功扣血才播放。')
save('A05','主角防御 · 格挡',hit(.9,[390,780,1153,1981]),'成功格挡触发。')
save('A05b','主角破防 · 护印崩裂',hit(1.3,[123,417,823])+burst(1.3,900,4800)*.25,'破防独立事件。')
save('A07','锁链受击 · 链节震响',hit(.85,[670,1021,1657,2890]),'命中保护链时触发，限频。')
x=hit(1.8,[170,490,1140]);
for k in range(9):
 a=int((.16+k*.12)*sr); h=hit(.35,[750+k*97,1700+k*43]); x[a:a+len(h)]+=h*.3
save('A08','锁链断裂 · 崩断散落',x,'每组锁链破除仅一次；停止该组摩擦循环。')
x=hit(2.2,[62,125,257])*.5
for k in range(18):
 a=int(rng.uniform(.04,1.4)*sr); h=hit(.65,[rng.uniform(1700,3500),rng.uniform(3500,6800)]); x[a:a+len(h)]+=h*.18
save('A06','水晶爆裂 · 晶片与断供',x,'晶体破碎瞬间触发；停止对应供能共鸣。')
for id,title,fr in [('B01a','飞页齐射 · 聚页预警',[90,180,361]),('B02a','冲击环 · 蓄压预警',[55,110,165]),('B03a','地裂 · 地底预警',[48,77,120]),('B05a','冲刺 · 危险预警',[110,165,330])]:
 t=np.arange(int(1.1*sr))/sr; x=sum(np.sin(2*np.pi*f*t) for f in fr)*np.sin(np.pi*t/1.1)**2
 save(id,title,x,'招式预警状态进入时一次播放。',peak=-12)
save('B01b','飞页齐射 · 释放',burst(1.2,500,5500)*( .45+.55*np.sin(np.arange(int(1.2*sr))/sr*24)**2),'整轮一次；单页不叠播完整主音。')
save('B02b','冲击环 · 能量脉冲',hit(1.6,[48,96,192])+burst(1.6,80,900)*.4,'波环释放一次。')
x=np.zeros(int(2.2*sr))
for k in range(4):
 h=hit(.65,[43+k*7,92,177])+burst(.65,100,1800)*.5; a=int(k*.45*sr); x[a:a+len(h)]+=h
save('B03b','连续地裂 · 四段裂地',x,'试听为四段示例；接入时每段由实际地裂事件驱动。')
t=np.arange(int(1.6*sr))/sr
save('B04a','激光 · 蓄能',np.sin(2*np.pi*(85*t+90*t*t))*np.sin(np.pi*t/1.6)**2+.15*burst(1.6,300,1600),'与可见预警同步。')
t=np.arange(sr*8)/sr
save('B04b','激光 · 持续束流',sum(np.sin(2*np.pi*f*t)/(i+1) for i,f in enumerate([80,160,320,480]))*(.8+.2*np.cos(2*np.pi*.5*t)),'可停止循环；激光结束或中断立即淡出。',True,-17)
save('B04c','激光 · 收束',hit(.8,[160,320,641]),'停束后一次短尾音。',peak=-14)
save('B05b','冲刺 · 掠过与急停',burst(1.,90,2300)+.2*hit(1.,[85,170,510]),'冲刺发动；命中另外触发主角受伤。')
save('C03','全部断供 · Boss失能',hit(2.,[43,86,129,215]),'最后一枚水晶破碎时一次，与硬直同步。',peak=-12)
t=np.arange(sr*8)/sr
x=noise(len(t),90,650)*(.7+.3*np.cos(2*np.pi*.25*t))+.18*np.sin(2*np.pi*213*t)
save('C01','背景循环 · 锁链盘旋摩擦',x,'每枚独立声源；锁链断裂停止，低沉轻微摩擦。',True,-29)
x=sum(np.sin(2*np.pi*f*t)/(i+1) for i,f in enumerate([64,96,128,192]))*(.85+.15*np.cos(2*np.pi*.25*t))
save('C02','背景循环 · 水晶供能共鸣',x,'每枚独立声源；断链继续，破晶停止，三路分别控制。',True,-31)
# Three independently phased instances, reduced total level; sequential shutdown at 6,12,18 sec.
d=24; mix=np.zeros(sr*d)
for i,stop in enumerate([6,12,18]):
 for id in ['C01','C02']:
  y=np.tile(np.roll(signals[id],i*sr//3),3)[:sr*d]*.45
  end=stop if id=='C02' else stop-1.5; env=np.clip((end-np.arange(sr*d)/sr)/.15,0,1); mix+=y*env
for sec,id in [(2,'A01'),(2.35,'A02'),(4.5,'A08'),(6,'A06'),(8,'B02a'),(9.1,'B02b'),(10.5,'A08'),(12,'A06'),(14,'A05'),(15,'A04'),(16.5,'A08'),(18,'A06'),(19,'C03')]:
 a=int(sec*sr); y=signals[id]; mix[a:a+len(y)]+=y*.75
# Keep baked ambience/foreground relationship. save only applies one global scale.
save('D01','混音演示 · 三路供能逐一关闭',mix,'24秒：4.5/10.5/16.5秒断链，6/12/18秒破晶；19秒全破失能。',peak=-6)
(O/'manifest.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2))
shutil.copy('/tmp/make_arena_audio.py',O/'generate_auditions.py')
shutil.copy('/Users/jack/.codex/generated_images/01a15e6f-7502-7902-89ec-02be607c7c61/exec-0dedbc80-68c4-499b-88ee-af7ff8570944.png',P/'锁链护晶_总体改进概念_v2.png')
print(json.dumps(entries,ensure_ascii=False,indent=2))
