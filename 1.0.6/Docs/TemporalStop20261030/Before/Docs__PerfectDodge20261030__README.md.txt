# Shift 突进与极限闪避 · 2026-10-30

应用工程：当前打开的 `1.0.6`。通过 Unity MCP 完成修改与运行验证。

## 操作与参数

- 点按 Shift 立即沿 WASD 输入方向突进；未输入方向时向前。继续按住仍为原有奔跑。沿用 Sprint 的换绑设置。
- 突进 4.8 米 / 0.24 秒，起步快、末段缓出；消耗 20 点共用体力，冷却 0.85 秒。可在空中使用一次，落地恢复。
- CharacterController 每段最多移动 0.18 米，保留墙体、地形、角色碰撞。突进没有额外无敌时间。突刺技能与突进不会叠加推动角色。
- 普通突进只有收手姿态、短促音效、少量视角反馈。时间减缓和彩色残影只属于极限闪避奖励。

## 奖励判定

从突进按下起 **220 毫秒内**，Boss 的真实攻击必须到达突进前的位置，同时玩家已经横向/纵向离开至少 0.6 米并躲过当前攻击范围，才能获得奖励。不是见到预警就给奖励，也没有扩大的擦边判定区。

- 横斩：使用原有距离、扇形角度、高度和遮挡条件。
- 弹幕：实际飞行线段与起步时的角色胶囊体相交，弹体没有命中玩家或被吞印/障碍物消耗。
- 冲撞：Boss 实际扫掠路径与原位置相交，受障碍物截断。
- 裂地：仅在该段真正爆发时检查实际多边形。
- 环波：波前真正扫到原位置时检查，玩家当前位置必须安全。
- 光束：只在开始造成伤害的第一跳检查，已持续生效的光束不能反复刷奖励。
- 同帧任何受击、格挡接触或吞印都会取消候选奖励；每次突进最多奖励一次。持续举盾、破防和吞印状态也不能领取。
- 判定按当前帧严格截止；低帧率不会自动延长 220 毫秒窗口。

奖励持续 **0.75 秒真实时间**：前 0.45 秒世界速度为 25%，后 0.30 秒平滑恢复。玩家移动、视角和近战动作保留响应。Esc / I、失焦、死亡、返回图书馆或组件停用立即清理，并恢复时间倍率及物理步长。

## 表现

实际剑、盾、双手的青绿、青蓝、紫红、金色轮廓残影；蒙皮手部逐帧烘焙当前姿态。共六组复用残影、边缘扫描纹、外围速度光丝和成功音效，中心保持清楚。材质随 Resources 纳入构建，中文提示使用项目内静态补字资产。

![极限闪避实际相机画面](Captures/PerfectDodge.png)

## 最小验证

当前参数已按试玩反馈调整，9 项针对性检查见 [本轮手感调校](../DodgeTuning20261030/README.md)。下述 21 条记录与本目录复查脚本对应首版参数，保留作为历史证据。

仅运行本次改动相关的 MCP Play Mode 检查，没有全量测试或构建。

[检查记录](MinimalChecks.txt) 中 21 条均通过（含两条测试场景准备确认）。验证了实际 Shift 输入、4.2 米位移、体力/冷却、薄墙阻挡、101 毫秒不奖励、80 毫秒奖励、命中仍受伤、同帧接触取消、六类 Boss 攻击接入、暂停与到期恢复。实际相机图检查了残影材质和形态，最终 Console 0 Error / 0 Warning。

手动体验：进入封印试炼，以 A/D/S + Shift 闪避；过早离开应只有普通突进。贴近攻击落点时闪出，才出现“极限闪避 · 时间减缓”和彩色残影。按 Esc 应立即退出减速。耗体力、停手恢复仍沿用共用体力机制。

复查脚本是 `.cs.txt` 文档，不参与编译。手动驱动 CharacterController 的 MCP 探针须先跨一帧刷新物理查询状态，再检查弹体/光束/冲撞；运行中的输入测试临时开放后台输入，结束已恢复。清理记录见 [RuntimeCleanup.txt](RuntimeCleanup.txt)。

## 修改文件

| 文件（工程内路径） | 用途 |
|---|---|
| Assets/Scripts/Player/PlayerDash.cs | 新增突进、严格奖励窗口、时间倍率生命周期 |
| Assets/Scripts/Core/BossPerfectDodge.cs | 新增原位置胶囊、横斩及冲撞判定共用逻辑 |
| Assets/Scripts/Visuals/DashAfterimageVfx.cs | 新增剑盾/双手彩色残影与外围光丝 |
| Assets/Scripts/Player/FirstPersonController.cs | Shift 接入、扫掠移动、玩家时间及暂停清理 |
| Assets/Scripts/Player/PlayerMelee.cs | 奖励时玩家动作时间、突刺位移互斥 |
| Assets/Scripts/Player/PlayerCombat.cs | 真实受击/格挡接触取消奖励 |
| Assets/Scripts/Player/SwordShieldView.cs | 突进收手姿态、玩家动作时间 |
| Assets/Scripts/Player/MeleeFeedback.cs | 突进/奖励音效和视野反馈 |
| Assets/Scripts/Core/BossEncounter.cs | 弹幕、环波、光束的实际攻击检查与帧末确认 |
| Assets/Scripts/Core/BossMeleeCombat.cs | 横斩命中与闪避共用范围检查 |
| Assets/Scripts/Core/BossCombatBehaviours.cs | 冲撞和裂地实际攻击检查 |
| Assets/Scripts/UI/BossCombatHUD.cs | 显示突进键及共用体力提示 |
| Assets/Scripts/UI/RecoveryMenu.cs | 指南中显示突进 / 奔跑 |
| Assets/Art/PlayerSwordShield/TemporalEcho.shader | 新增轮廓、扫描纹与透明消散着色器 |
| Assets/Resources/PlayerDash/TemporalEcho.mat | 新增构建内引用的残影材质 |
| Assets/UI/Fonts/DodgeChineseSupplement.asset | 新增静态中文补字 |
| Assets/UI/Fonts/BossChineseSDF.asset | 引用中文补字 |
| README.md、CHANGELOG.md | 操作与更新记录 |
| Docs/PerfectDodge20261030/ | 本轮备份、截图、检查和文件清单 |

新增 Unity 资产及脚本均带对应 `.meta`；详细 SHA-256 清单见 [FilesChanged.json](FilesChanged.json)。`Before/` 仅保存本轮修改前的原件。
