# 禁书守卫：实现与验证

工程：`1.0.5`；主场景：`Assets/Scenes/MilanLibrary_Magic_V6.unity`。更新：2026-10-19。最新动作、冲撞、龟裂、弱追踪及本轮修改文件见 [战斗细化记录](CombatPolish20261019.md)。下方标明日期的旧验收为历史记录。

**本轮最终验收：** 新增战斗专项 39 项、激光/格挡 19 项、基础闭环 19 项全部通过。退出 Play Mode 后 Console 0 Error/Warning，10 个战斗 Prefab 的脚本、材质及 143 个战斗中文字形检查通过。当前源工程准备作为 1.0.6 归档到 lixiang；上传步骤见 [1.0.6 上传指南](../Upload106.md)。

## 试玩入口

进入 Play Mode，按 **Esc → 深入探索 → 深入探索**。也可瞄准魔法书按 E，在功能概述中直接点击“深入探索”。战斗复用当前玩家、Input System、菜单与准星；不会另建玩家或 EventSystem。

| 输入 | 行为 |
| --- | --- |
| WASD / 鼠标 | 移动 / 环视 |
| Shift / Space / C | 冲刺 / 跳跃 / 蹲下 |
| 左键 | 秘文弹，持盾时不能射击 |
| 右键按下 | 0.65 秒吞印窗口，吞噬正面飞页；每次 +25 怪物力量，满值后下一发强化 |
| 右键按住 | 格挡正面飞页和激光，消耗格挡条 |
| 松开右键 | 延迟恢复格挡条；破防后必须松开才能重新举盾 |
| Esc | 暂停、继续、战斗按键、设置或放弃返回 |

右键沿用 `Absorb` 输入绑定，在战斗菜单“战斗按键”中可换绑。不要同时接入旧 Input Manager。环波、裂地不能用盾硬挡，仍需跳跃或离开落点；冲撞正面可挡但消耗很大。

## 已实现内容

- 可编辑 Boss 网格：褶皱长袍、无面面具、七段冠环、书页翼、长指甲手部、可打开的胸前禁书。材质沿用旧金、深紫、皮革和羊皮纸。
- 独立圆形封印大厅：外圈实体墙、柱子、尖拱、穹顶、封印门与灯光；平整地面使用真实 MeshCollider。进入时停用图书馆环境，返回时恢复原激活状态和玩家位置。
- 两阶段与五种攻击：弱追踪禁页齐射、蓄力激光、缄默环波、裂地禁咒与封缄冲撞；50% 生命转阶段并清理在途攻击。
- 状态机管理预警、锁定、发射、收招、死亡和重试。暂停时战斗计时、位移、攻击与格挡状态停止推进。
- 玩家生命、秘文弹、吞印、强化、格挡条、破防、血条与结果页。重试清空弹体并重置生命、Boss 阶段、冷却和格挡。

## 激光、移动和格挡规则

**激光：** 蓄力 2.2 秒，最后 0.65 秒锁定方向；发射持续 1.3 秒。预警用细紫线和逐渐扩大的封印环，发射用半透明紫色外层与金色光芯。发射前 0.18 秒维持方向，此后用 0.25 秒平滑提升追踪转速，最大 10°/秒，持续横移或冲刺可避开；不预测玩家落点、不瞬间转向。光束半径 0.30 米，最远 32 米，每 0.22 秒尝试造成 14 点伤害。SphereCast 按距离选择最近实体，渲染与伤害同时受墙体遮挡；命中护盾时视觉终点提前到盾前，避免遮住整块屏幕。

**移动：** Boss 先通过视野与实体遮挡记录玩家位置，在 2.8 秒观察阶段选择接近、后撤或侧移的固定落点，不持续追逐实时坐标。目标距离 6.5–9.5 米，正常速度 2.4 米/秒，二阶段提高 20%。观察末段和施法前段有限速转向，收招时完全停止位移与跟随转向，留下反击窗口。胶囊扫掠与场内半径限制约束普通移动和冲撞；冲撞最多 11 米，碰墙或玩家即停。

**格挡：** 容量 100；持盾每秒消耗 18，正面命中额外消耗伤害值的 2.8 倍；吞印启动消耗 14。盾完整且格挡值足够时，正面约 120° 范围内的飞页、激光和冲撞不扣血。格挡值不足时触发破防并承受剩余伤害，破防持续 1.8 秒；移动和闪避仍可用。松开防御后延迟 1.5 秒、以每秒 26 的速度恢复；破防期间不恢复。不能同时持盾射击，也不能用盾挡住背后攻击或地面伤害。

参数资产：`Assets/Art/ForbiddenWarden/WardenEncounter.asset`。修改数值应在该资产中完成，避免把测试数值写死到场景对象。

## 实际文件

| 路径 | 用途 |
| --- | --- |
| `Assets/Scripts/Core/BossDefinition.cs` | 可配置战斗、激光、位移和格挡参数 |
| `Assets/Scripts/Core/BossEncounter.cs` | 会话、五招、目标锁定、激光遮挡、胜负与清理 |
| `Assets/Scripts/Core/BossCombatBehaviours.cs` | 感知、固定走位目标、冲撞、裂地与飞弹弱追踪 |
| `Assets/Scripts/Player/PlayerCombat.cs` | 射击、吞印、生命、格挡消耗/恢复/破防 |
| `Assets/Scripts/UI/BossCombatHUD.cs` | Boss 血条、生命与格挡条、资源/冷却、进入淡入 |
| `Assets/Scripts/UI/RecoveryMenu.cs` | 深入探索入口、战斗暂停、按键说明、胜负和重试 |
| `Assets/Scripts/Core/LibraryInputBindings.cs` | 在原输入系统中加入 Attack 与 Absorb，可换绑 |
| `Assets/Scripts/Player/FirstPersonViewModel.cs` | UI 隐藏手部时避免对停用 Animator 调用 Update |
| `Assets/Scripts/Visuals/BossArena.cs` | 场地、出生点、Boss 与效果 Prefab 引用 |
| `Assets/Scripts/Visuals/WardenVisuals.cs` | 冠环、书翼、手臂、悬浮与胸书动作 |
| `Assets/Scripts/Visuals/LaserBeamVisuals.cs` | 蓄力封印、双色光束、命中端点；不负责伤害 |
| `Assets/Scripts/Visuals/Editor/BossEncounterBuilder.cs` | 可编辑程序化建模源、Prefab 创建、字体烘焙；仅 Editor |
| `Assets/Scripts/Visuals/Editor/WardenModelRefinement.cs` | 曲面面具、兜帽、肩甲、手部、誓约链、层叠书页和胸书细化；仅 Editor |
| `Assets/Scripts/Visuals/Editor/BossAcceptance.cs` | 基础闭环与环波/封印躲避自动输入验收；仅 Editor |
| `Assets/Scripts/Visuals/Editor/BossLaserGuardAcceptance.cs` | 激光/格挡/移动专项自动输入验收；仅 Editor |
| `Assets/Scripts/Visuals/Editor/BossTestInputScope.cs` | 自动验收期间临时将输入路由到 Game View，结束后恢复；仅 Editor |
| `Assets/Art/ForbiddenWarden/Source/ForbiddenWarden.obj` | 可导入建模软件的网格导出 |
| `Assets/Art/ForbiddenWarden/Meshes/` | Unity 网格资产 |
| `Assets/Art/ForbiddenWarden/Materials/` | URP 材质，含 LaserVioletGlow / LaserGoldCore |
| `Assets/UI/Fonts/BossChineseSDF.asset` | 含新增文案的静态中文 SDF；运行时无需系统字体 |
| `Assets/Audio/Boss/` | 四个本地合成的施法/命中提示音 |

实际 Prefab 均位于 `Assets/Art/ForbiddenWarden/Prefabs/`：

- `ForbiddenWarden.prefab`：Boss 模型与分件。
- `SealChamber.prefab`：独立战斗场地。
- `ForbiddenPage.prefab`、`RunicBolt.prefab`：敌方禁页与玩家秘文弹。
- `RunicRing.prefab`：环波、地面预警与玩家护盾。
- `SealPillars.prefab`、`SealImpact.prefab`：保留的旧封印柱和命中碎片；当前裂地不再使用封印柱。
- `FracturedGround.prefab`、`RushTelegraph.prefab`：不规则龟裂与冲撞路线预警。
- `ChargedLaser.prefab`：蓄力激光。

本次升级更新战斗脚本、战斗数值、Boss 模型及效果 Prefab、字体与文档，不重建或移动图书馆。原始场景及脚本备份在 `Backup/`，激光升级前的战斗文件在 `BeforeLaserGuard/`，追踪和模型细化前的文件在 `BeforeTrackingAndDetail/`。之前四项试玩反馈的修复保留。

## 验证记录与边界

测试在 Unity 6000.3.25f1 编辑器 Play Mode 中执行，自动输入通过原 Input System；按钮使用 Unity 的 PointerClick 事件。瞄准辅助只存在于 Editor 测试工具。验收期间临时调整内存中的默认 InputSettings（若项目使用持久化设置资产则创建副本），允许后台接收排队输入；停止、退出 Play Mode 或编译前恢复原配置。默认设置保持同一实例，避免 Input System 在退出 Play Mode 时引用已销毁对象。测试不会修改项目输入配置资产或用户已保存的按键配置。

- [基础闭环](PlayModeAcceptance.json)：移动、冲刺、跳跃、Esc 暂停、点击不误射、实际攻击造成失败、重试、实际秘文弹触发二阶段与胜利、返回与对象清理。
- [防御与清理](DefenseAcceptance.json)：实际环波的跳跃躲避、地面封印锁定后的冲刺躲避、失焦处理器暂停、连续进出五次无额外 HUD / EventSystem。
- [激光与格挡](LaserGuardAcceptance.json)：自然攻击循环中的移动、实际右键格挡扣条、持续激光破防、暂停冻结、松开恢复、有限转速追踪与横移躲避、临时实体遮挡光束与伤害、返回清理。后两轮仅设定下一招为激光，使用正常预警时长、伤害与输入逻辑。
- 截图位于 `Captures/`：`laser_block.png`、`guard_break.png`、`laser_charge.png`、`victory.png`、`defeat.png`、`wave_jump.png`、`seal_warning.png`。部分早期模型截图为过程记录，以最新截图和工程为准。

JSON 中的 `failure` 为空且列出的检查为 PASS 才代表对应运行通过。测试证据不等同于真人长时间试玩或独立构建验收。尚未打包、提交或推送。

2026-10-18 追踪升级前基线：基础闭环 19 项、躲避/清理 17 项、激光/格挡专项 18 项通过。追踪升级后的专项改为 19 项，新增实际方向改变和角速度上限验证；最新运行结果以 JSON 为准。模型设计与修改意见区见 [设计评审稿](WardenDesignReview.md)，资源检查见 [资源检查](AssetAudit.json)。测试工具结束后恢复原输入设置。

**追踪与模型细化最终验收（2026-10-18）：** 激光/格挡 19 项和基础闭环 19 项均通过。本轮光束实际转向约 10.26°，采样最大角速度约 10.0003°/秒（浮点误差范围），侧向冲刺场景保持满血。最终退出 Play Mode 后 Console 为 0 Error、0 Warning，无 Input System 断言；输入焦点设置恢复正常，主场景没有未保存改动。8 个战斗 Prefab 的资源检查无问题，354 个脚本中文字符字形齐全。运行时加载细化模型为 34,416 三角面，仍只有原有 2 个碰撞组件。

本次直接修改的代码为 `Core/BossDefinition.cs`、`Core/BossEncounter.cs`、`Visuals/Editor/BossEncounterBuilder.cs`、`Visuals/Editor/BossLaserGuardAcceptance.cs`、`Visuals/Editor/BossTestInputScope.cs`，新增 `Visuals/Editor/WardenModelRefinement.cs`（均相对 `Assets/Scripts/`）。更新模型 Prefab、26 个合并细节网格、2 个独立细节材质、OBJ 导出、激光参数资产、中文字体、说明文档与截图。材质和场地检查以资产内容为准；未安装 Package，未修改 ProjectSettings。

截图说明：`laser_block.png` 是最终激光格挡实机画面；`guard_break.png` 捕捉的是破防后立即打开的暂停菜单，用于记录暂停状态，破防扣条与扣血以专项 JSON 为证。`laser_charge.png` 为锁定后开始横移的画面。

这是一版完整可玩的原型：当前采用 Transform 分件动画与轻量网格，没有完成设计文档中的 LOD、完整蒙皮、复杂布料、差异化音乐和所有高精度美术细节。原设计 2–4 分钟为后续平衡目标；自动精确瞄准可以更快结束战斗，不能把该时长当作真人体验结论。

## 手动复核

1. 点击深入探索，观察 Boss 追近与绕侧；站远、贴近分别查看距离反应。
2. 等激光蓄力，向侧方持续移动/冲刺；光束应缓慢追来，但可甩开。停在光路上应受伤。
3. 面向 Boss 按住右键硬接激光；前段只掉格挡值，耗尽后出现破防与生命损失。松开后等待恢复。
4. 持盾同时按左键应不能射击；放开右键后恢复射击。背对来袭攻击不应挡住。
5. 在激光途中按 Esc，确认光束、生命、格挡值停止变化；继续、重试和返回后没有残余光束。
6. 检查胜负页、中文字形及 Console；普通运行不需要手动执行建模或测试工具。
