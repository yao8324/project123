# 剑盾近战与行者档案 · 2026-10-29

用户授权在当前打开的 1.0.6 工程继续修改。此次按“仅做最小测试”执行，未提交、推送或制作独立包。

## 已接入

- 使用原 `BossModeling/PlayerSwordShield_v1` 的两份 FBX 和 Blender 源文件；原作未修改。工程内原生副本在 `Blender/PlayerSwordShield/`，运行资源在 `Assets/Art/PlayerSwordShield/`。
- 剑盾重新映射为 URP 材质；保留黑钢、旧金、象牙封印、暗红皮革和紫晶。材质为 Unity 内重建，未烘焙原 Blender 程序纹理。
- 右手持剑、左手持盾，复用原有手部/袖口网格，移除烘焙网格中的魔杖三角面。握柄与手部随同一挂点运动。修正 FBX 烘焙后重复单位缩放及更新已有 Mesh 时 GPU 缓冲未刷新的问题。
- 三段轻击包含侧斩、反向斩及下劈，具有预备、加速、跟随和收招；采用平滑姿态插值、呼吸/步行起伏、持盾过渡和受击回弹。剑尖和剑根共同生成短时半透明拖尾。
- Boss 保留原模型与骨架，新增 `AA_MeleeWindup`、`AA_MeleeStrike`，共 19 段动作。胸肩转体、头部反向稳定、两段右臂求解与持杖手共同驱动近战，握持关系保持；衣摆和书翼有滞后反应，刃杖带紫色拖尾。
- 入场与转阶段按完整状态时长播放；收招混合缩短至不超过 0.42 秒，保持离场动作继续采样。非循环释放动作避免末帧直接跳回开头。受击增加轻微躯干响应，失衡使用原 Stagger 动作。
- Boss 近距离会选择刃杖横斩：0.9 秒预备、地面危险弧线、一次命中、收招。判定检查距离、朝向、高度和遮挡。盾击与失衡累计可打断，右键举盾起始 0.22 秒可弹反近战。
- I 或 Esc → 属性与装备：真实生命、体力、格挡、剑伤、斩距和技能数值；独立相机渲染剑盾模型，可拖动查看正背面。菜单与武器使用同一暂停状态。

## 操作和默认参数

| 输入 | 动作 | 伤害 | 体力 | 冷却 | 作用 |
| --- | --- | ---: | ---: | ---: | --- |
| 左键 | 三段连斩 | 30 / 30 / 43.5 | 每击 8 | 动作间隔 | 连续输入接续，第三击失衡更高 |
| 右键 | 举盾 / 弹反 / 吞印 | — | — | 弹反重启 0.65s | 持续消耗格挡；及时举盾弹反近战 |
| 1 | 破誓重斩 | 82 | 24 | 4s | 正面重击，失衡 48 |
| 2 | 缄眼盾击 | 26 | 18 | 6s | 近身打断，命中恢复 16 格挡 |
| 3 | 踏影突刺 | 58 | 22 | 5s | 前进最多 2.35 米，CharacterController 碰撞阻挡 |
| 4 | 回旋封印 | 68 | 32 | 9s | 半径 3.3 米的周身近战斩击 |
| I | 属性与装备 | — | — | — | 再按 I 关闭或回到原菜单 |
| Esc | 暂停菜单 | — | — | — | 可打开属性与装备 |

体力上限 100，最后消耗后等待 0.8 秒；空闲且未持盾时每秒恢复 22。格挡沿用原容量、消耗与恢复。成功弹反恢复 18 体力、20 格挡，增加 20 封印力量。原吞印仍可吸收飞弹，每枚增加 25 力量；满值时下一次命中的剑击为 1.65 倍伤害，命中才消耗。Boss 失衡阈值 100，超过 2.5 秒未受击逐步回落。

表内为基础伤害，Boss 收招时的近战反击沿用 1.5 倍奖励，可与满力量伤害叠加。伤害统一由战斗逻辑在命中时刻处理；每个动作仅结算一次，轨迹本身无额外伤害。远程攻击代码保留供既有资源兼容，装备剑盾时左键为近战。

参数入口：`Assets/Art/PlayerSwordShield/OathboundLoadout.asset`；Boss 近战参数在现有 `Assets/Art/ForbiddenWarden/WardenEncounter.asset` 对应的 BossDefinition 字段。第一人称姿态在 SwordShieldView，Boss 新动作生成入口为 `Arcane Archive/Combat/Connect sword shield and melee`。已有成品资产可直接运行，不需执行重建菜单。

## 最小验证（已执行）

Unity 6000.3.25f1、MilanLibrary_Magic_V6 场景，Play Mode：

1. `MinimalSmoke.json`：通过真实 Input System 虚拟键盘输入 1～4，四个动作均触发，伤害分别为 82/26/58/68，体力扣除与冷却匹配。检查中暂停 AI 自动更新，并调用正式 Advance 推进动作，避免不相关战斗干扰。
2. 同一记录：虚拟右键使角色举盾，在弹反窗口调用正式受击逻辑，生命不变且 Boss 进入失衡；虚拟 I 打开属性页并暂停。
3. `PauseSmoke.json`：跨编辑器工具调用观察暂停，位置与战斗时钟不变；Esc 菜单包含可进入的属性装备入口。
4. `CloseCombatSmoke.json`：三段轻击伤害 30/30/43.5；近距离 AI 选择编号 5 近战，连续推进有效段仅伤害一次；返回图书馆后清理动作/持盾并恢复体力。
5. 实际 Game View 检查了持握、Boss 预备/横斩、玩家剑光和属性页。`Captures/02_Equipment.png`、`04_BossMeleeWindup.png`、`05_BossMeleeStrike.png`、`07_PlayerSwordTrail.png` 为最终截图。动作截图使用受控时刻；属性截图也在暂停 AI 更新的夹具中取得，背景 HUD 未自动刷新隐藏。截图不能代替整段逐帧回归。
6. 脚本编译通过；本轮最终 Console 读取无 Error/Warning。未运行旧的大范围自动验收脚本。

没有进行完整流程回归、不同帧率/分辨率矩阵、性能与目标平台测试、独立构建或全量动画穿插检查。Boss 原网格拓扑与面数未重做；本次建模相关优化集中在持杖骨架姿态、握持、动作轮廓和实时表现。旧验收工具内“左键发射秘文弹”的历史预期不适用于这套近战装备。

## 修改文件

修改：

- `Assets/Scripts/Core/BossEncounter.cs`：接入近战/失衡状态及返回清理。
- `Assets/Scripts/Core/BossCombatBehaviours.cs`：近距离选招和对峙走位。
- `Assets/Scripts/Core/BossDefinition.cs`：Boss 近战、失衡参数。
- `Assets/Scripts/Core/LibraryInputBindings.cs`：固定 1～4、I，接入原绑定与冲突迁移。
- `Assets/Scripts/Player/PlayerCombat.cs`：剑术输入、近战弹反、力量与格挡恢复。
- `Assets/Scripts/UI/RecoveryMenu.cs`：I 与 Esc 入口及暂停恢复。
- `Assets/Scripts/UI/BossCombatHUD.cs`：体力和四技能冷却。
- `Assets/Scripts/Visuals/WardenAnimationDriver.cs`：动作衔接、近战播放、受击及刃光。
- 主场景 `Assets/Scenes/MilanLibrary_Magic_V6.unity`、`ForbiddenWardenRigged.prefab`、`BossChineseSDF.asset`、README、CHANGELOG。

新增：

- `Assets/Scripts/Core/MeleeLoadout.cs`、`BossMeleeCombat.cs`。
- `Assets/Scripts/Player/PlayerMelee.cs`、`SwordShieldView.cs`。
- `Assets/Scripts/Visuals/BladeRibbon.cs`、`MeleeEffects.cs`。
- `Assets/Scripts/UI/RecoveryEquipment.cs`、`EquipmentPreview.cs`。
- `Assets/Scripts/Visuals/Editor/SwordShieldSetup.cs`。
- `Assets/Art/PlayerSwordShield/` 下的 FBX、握持网格、材质、两个动作、Prefab、配置和 BladeLight.shader；配套 .meta。
- `Assets/UI/Fonts/MeleeChineseSupplement.asset`：48 个静态中文字形与 atlas/material 子资产，运行时无需系统字体。
- `Blender/PlayerSwordShield/` 原生源文件副本及本交付目录。

另清理上一轮未完成清理的临时 `InteractionPolicyAcceptance.cs`，保留其历史检查记录。没有更改原始 Blender 资源目录、其他 Unity 版本或远端分支。

修改前指定文件快照及哈希在 `Before/` 和 `BeforeHashes.json`；本轮实际文件清单与哈希在 `ChangedFiles.json`。
