"""Package the verified Windows player, including all runtime dependencies."""
import hashlib,json,shutil,struct,zipfile
from pathlib import Path
project=Path(__file__).resolve().parents[2]
build=project/'Builds/IMMonster_ThreeBoss_Windows'
report=(project/'Docs/ThreeBoss/WindowsBuild.txt').read_text()
assert 'Result: Succeeded' in report and 'Errors: 0' in report,report
for relative in ['IMMonster.exe','UnityPlayer.dll','MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll','IMMonster_Data/Managed/Assembly-CSharp.dll','IMMonster_Data/boot.config']:
 assert (build/relative).is_file(),f'Missing runtime dependency: {relative}'
# LZ4 builds store serialized player data in data.unity3d.
packed_data=build/'IMMonster_Data/data.unity3d'
assert packed_data.is_file() and packed_data.stat().st_size > 0
with packed_data.open('rb') as stream:
 assert stream.read(8)==b'UnityFS\0', 'Invalid compressed Unity data header'
for name in ['IMMonster.exe','UnityPlayer.dll']:
 with (build/name).open('rb') as stream:
  assert stream.read(2)==b'MZ'
  stream.seek(0x3c);offset=struct.unpack('<I',stream.read(4))[0];stream.seek(offset)
  assert stream.read(4)==b'PE\0\0' and struct.unpack('<H',stream.read(2))[0]==0x8664
instructions='''IM MONSTER：遗赠 — 三 Boss 书本整合试玩

使用 Windows 64 位电脑。完整解压后双击 IMMonster.exe。
必须保留 IMMonster_Data、UnityPlayer.dll、MonoBleedingEdge 等同目录文件，不能只发送 exe。

启动后在书里选择“翻开故事 · 连续挑战三关”。
缚魂者 → 典狱长 → 黄金翼圣骑士 → 撤离结尾。
阅读对白后点“继续”；胜利点“进入下一关”；第三关胜利点“完成故事”。
失败点“重试本关”。也可从书页单独选关，Esc 暂停后返回书页。

WASD 移动；鼠标观察；空格跳跃；Shift 突进；左键连斩；右键举盾/弹反；1–4 技能；I 装备；Esc 暂停。

验证：Unity Play Mode 304 项通过，含真实输入和自主 AI 烟雾检查、受控实际近战命中、失败重试和三关通关。
已在 Mac 的 Unity 游戏窗口点击检查菜单、剧情、进入战斗、暂停和返回书页。
Windows x64 Mono 构建成功；尚未在 Windows 实机复验性能与原生运行。

当前为线性试玩：缚魂者复用现有战斗底层；外围水晶是装饰；救援通过战后文字呈现。
未包含长篇完整探索、独立召唤小怪/锚点机制或三分支结局。无需网络和 Unity。
'''
(build/'先读我_操作说明.txt').write_text(instructions,encoding='utf-8-sig')
shutil.copytree(project/'Docs/ThreeBoss/Licenses',build/'Licenses',dirs_exist_ok=True)
shutil.copy2(project/'Assets/Art/ThreeBoss/Fonts/OFL.txt',build/'Licenses/NotoSansCJK_OFL.txt')
def digest(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for block in iter(lambda:f.read(4*1024*1024),b''):h.update(block)
 return h.hexdigest()
files=sorted(p for p in build.rglob('*') if p.is_file() and p.name != 'BuildManifest.json' and not any('DoNotShip' in part for part in p.parts))
manifest={'format':'Windows x86_64 PE, Unity Mono','entry':'IMMonster.exe','unity':'6000.3.25f1','chapters':['缚魂者','缄默典狱长','封印圣骑士'],'play_mode_assertions':304,'windows_native_run_verified':False,'files':[{'path':str(p.relative_to(build)),'bytes':p.stat().st_size,'sha256':digest(p)} for p in files]}
(build/'BuildManifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
release=project.parent/'完整版/三Boss剧情整合_20261108';release.mkdir(parents=True,exist_ok=True)
archive=release/'IMMonster_ThreeBoss_Windows_20261108.zip'
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=5) as z:
 for p in files+[build/'BuildManifest.json']:z.write(p,'IMMonster_ThreeBoss_Windows/'+str(p.relative_to(build)))
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 assert 'IMMonster_ThreeBoss_Windows/IMMonster.exe' in z.namelist()
sha=digest(archive)
(release/'SHA256SUMS.txt').write_text(sha+'  '+archive.name+'\n')
shutil.copy2(build/'先读我_操作说明.txt',release/'先读我_操作说明.txt')
result={'archive':str(archive),'bytes':archive.stat().st_size,'sha256':sha,'files':len(files)+1,'zip_crc':'passed','pe_machine':'0x8664','dependencies':'present'}
(project/'Docs/ThreeBoss/PackageVerification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
print(json.dumps(result,ensure_ascii=False,indent=2))
