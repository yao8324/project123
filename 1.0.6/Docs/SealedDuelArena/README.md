# 封印斗场 · 本地开发版

## 打开与操作

Mac 试玩包：`Builds/SealedDuelArena/IM MONSTER - Sealed Arena.app`。双击打开后直接进入 Boss 战。
WASD 移动，鼠标转向，Shift 疾跑，空格跳跃；左键秘文弹，右键吞印／按住格挡；Esc 暂停、重试或返回档案馆。

当前试玩包已包含星空、深色材质和建筑精修，重新构建成功（0 错误）。NativeNight 的 12 项原生检查全部通过，含真实 UI 鼠标点击“继续战斗”和“返回图书馆”，以及退出后恢复原图书馆天空光照。使用虚拟鼠标驱动实际 EventSystem，不是直接调用按钮回调。

Unity 6000.3.25f1 打开本项目，直接试玩场景为 `Assets/Scenes/SealedDuelArena_Showcase.unity`。
常规场景 `Assets/Scenes/MilanLibrary_Magic_V6.unity` 的原 Boss 入口已连接新场地。比赛剧情场景保持原流程。

## 已建设

齐平封印地坪、外围双层尖拱与环廊、柱头卷叶、巨型封印门、结构穹顶与破口、外围石灯和碎石。中心半径 12m 保持平整无环境遮挡，战斗半径沿用 14m。

资源：`Assets/Art/SealedDuelArena20261104`；生成器：`Assets/Scripts/Visuals/Editor/SealedDuelArenaBuilder.cs`。
原 Boss 模型、17 段动画和 10 个技能效果引用保留。

## 当前验证证据

- `StaticChecks.json`：89 项通过，2,453 个地面胶囊采样、72 条完整直径胶囊穿越路线、72 个周界方向，以及网格外向面检查。
- `NormalsRed.log` / `NormalsGreen.log`：石块、拱线、圆管朝向错误的复现与修复。
- `NativeNight/NativeSmoke.json`：Mac 实际程序 12 项通过，含移动、攻击、暂停、重试、返回及再次进入；使用虚拟按键驱动真实控制器，结束流程使用显式伤害注入。不是管理者人工验收。
- `Regression`：此前本次工程运行的战斗与动画回归证据。
- `Evidence/08_outward_surfaces.png`、`09_gallery.png`、`10_gate.png`、`11_breach.png`：最新 Unity Play Mode 摄像机截图。为观察环境临时隐藏 Boss；没有用概念图替代实机建筑。

## 材质来源

Poly Haven CC0 扫描材质 `large_sandstone_blocks` 与 `stone_tiles`；原始来源、文件校验和许可证在 `ArtSource/SealedDuelArena20261104/PolyHaven`。其余表面由 `Tools/SealedDuelArena/generate_textures.py` 生成。

## 交付状态与后续选择

本地可试玩构建及验证已完成。星空、降低亮度和深色材质已落地。完整验收证据见 verification.json 与 CompletionAudit.md。
独立检查指出的未保存场景保护、完整战斗半径及连续胶囊扫掠已修复。实机装饰密度较 AI 概念图简化，管理者美术验收仍由用户决定。

下一轮水晶、锁链、魔法阵、障碍物、新音效及受击反馈是审阅方案，未擅自植入。音效试听与概念图见 NextIteration/试听与方案.html。

运行重建：Unity Editor 菜单 IM Monster / Sealed Arena / Build approved Boss arena；命令行 executeMethod 为 ArcaneArchive.Visuals.Editor.SealedDuelArenaBuilder.BuildMacAndCheck。
本地分支 codex/sealed-boss-arena-20261104；不自动推送或合并 GitHub。
