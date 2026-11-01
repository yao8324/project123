# 持握修正与技能冷却显示 · 2026-10-29

本轮在用户指定的当前 1.0.6 工程实现。维持左剑右盾、原技能伤害、体力与冷却参数，只调整持握姿态和技能栏表现。

## 持握

- 默认左手剑向前内侧倾斜。剑枢轴局部位置为 (-0.29, -0.31, 0.49)，局部欧拉角为 (20, 6, -19)。
- 举盾收回胸前，格挡枢轴由 (0.13, -0.16, 0.65) 调为 (0.24, -0.25, 0.40)，局部欧拉角为 (12, -8, 8)。沿用平滑过渡，盾击仍有独立前推。
- 延续用户提供的 2077 近战录屏参考；剑盾动作的公开参考为 [Obsidian 的 Avowed 战斗介绍](https://news.xbox.com/en-us/2024/01/22/avowed-developer-direct-side-quest-deep-dive/)。采用清晰待机、举盾、攻击分工的制作思路；这里的具体姿态参数与图标均为本项目实现。

## 四技能图标

| 按键 | 技能 | 图案与主色 | 原有冷却 |
| --- | --- | --- | --- |
| 1 | 破誓重斩 | 琥珀色下劈剑与裂地 | 4 秒 |
| 2 | 缄眼盾击 | 青绿色盾牌、闭眼符号与冲击线 | 6 秒 |
| 3 | 踏影突刺 | 冰蓝色前刺剑与速度线 | 5 秒 |
| 4 | 回旋封印 | 紫色双刃环绕中心封印 | 9 秒 |

![四款技能图标](SkillIcons.png)

施放后立即显示灰度图标，依据实际剩余冷却从 12 点方向顺时针恢复彩色；外圈和指针同步旋转。中心显示向上取整的剩余秒数，最后一秒显示一位小数，冷却结束时消失并闪亮一次。体力不足单独显示，不产生虚假冷却。所有进度由现有战斗时钟与技能冷却驱动，暂停时同步冻结。

图标为本地原创矢量形状生成，256×256 PNG 用于游戏，SVG 位于 `IconSources/`。可通过 Unity 菜单 `Arcane Archive/UI/Build melee skill icons` 重新生成并绑定资源。

## 修改文件

- `Assets/Scripts/Player/SwordShieldView.cs`：待机持剑与格挡姿态。
- `Assets/Scripts/Core/MeleeLoadout.cs`：技能彩色/灰度图标、主色及计时环引用。
- `Assets/Scripts/UI/BossCombatHUD.cs`：接入四个图标技能位与真实冷却。
- 新增 `Assets/Scripts/UI/SkillCooldownBadge.cs` 及 `.meta`：径向恢复、指针、数字倒计时、完成与体力状态。
- 新增 `Assets/Scripts/Visuals/Editor/MeleeSkillIconBuilder.cs` 及 `.meta`：原创图标及灰度版生成工具。
- 新增 `Assets/UI/MeleeSkills/`：四张彩色、四张灰度、一张计时环 PNG 及对应 `.meta`。
- `Assets/Art/PlayerSwordShield/OathboundLoadout.asset`：绑定图标和配色。
- `Assets/Scenes/MilanLibrary_Magic_V6.unity`：保存修正后的默认姿态。
- `README.md`、`CHANGELOG.md` 与本目录：交付说明、修改前快照、检查记录及截图。

完整文件与哈希见 `ChangedFiles.json`；修改前七个已有文件的快照位于 `Before/`，哈希见 `BeforeHashes.json`。

## 最小验证与复核步骤

按用户“仅做最小测试”的要求，仅检查本次相关路径，未构建独立运行包或进行完整回归。

1. Unity 编译完成；本轮最后 Play Mode 检查 Console 为 0 Error / 0 Warning。
2. 临时隔离 Boss AI 与玩家位移，使用 Input System 虚拟 1～4 键触发现有技能输入路径。推进实际技能冷却，分别核对恢复 0%、25%、50%、75%、100% 时的图标恢复值、指针方向和数字；四项全部通过。打开属性菜单验证冷却冻结，通过。结果见 `MinimalChecks.json`。
3. 默认姿态确认左手剑尖朝前、朝内；虚拟右键触发现有格挡路径，确认盾牌局部前伸降至 0.40。结果见 `PoseCheck.json`。
4. 剩余 0.08 秒显示 0.1；刚完成后再次冷却不会残留完成闪光。结果见 `DisplayCheck.json`。
5. 已退出 Play Mode，主场景无未保存变更，缺失脚本为 0，见 `FinalState.json`。隔离与计时样本只存在于临时运行状态。

手动复核：进入试炼，观察默认剑尖方向，按住右键观察盾牌回收；分别施放 1～4，查看变灰、顺时针恢复和数字；按 I 暂停后等待，关闭界面后确认进度继续。

截图使用游戏直接捕获。冷却对照图特意设置四个不同进度用于比较，并非连续实战施法记录。

- [待机与就绪图标](Captures/02_ReadyHUD.png)
- [胸前格挡](Captures/03_CloseGuard.png)
- [四个冷却阶段](Captures/05_FinalCooldown.png)
