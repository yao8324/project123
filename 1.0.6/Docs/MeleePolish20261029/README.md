# 左剑右盾与近战手感重制 · 2026-10-29

本轮按用户要求继续修改当前打开的 1.0.6，仅读取 E:\录屏\Cyberpunk 2077 的参考视频。只做最小功能检查及关键姿态检查，未打包、提交或上传。

## 参考与落地

已抽帧查看三份视频，主要参考第三份 19.03.29.05.mp4 的 06:20–15:40，尤其 09:41.8–09:44.7 连续横斩。参考观察与时间点见 [Reference/README.md](Reference/README.md)。录屏含慢时覆盖效果，项目参数是本次设计值，并非测得的 2077 原始参数。

- 左手逆誓剑、右手缄眼盾。鼠标仍为左键攻击、右键防御/弹反/吞印，1～4 技能及 I / Esc 属性页保持原入口。
- 完全新建左右手网格，不再烘焙旧 MageHand。四根卷握手指、对握拇指、掌腕、连续前臂、分节指甲片与护臂；细粒皮革色图/法线、黑钢和旧金镶边。左右顶点镜像并反绕序，无负缩放。
- 两骨蒙皮分开驱动握柄与前臂：手指始终握住武器；腕部 8 cm 过渡，前臂保持刚性并连入镜头下侧。避免跨身斩击时露出悬空截断的前臂。手指为闭合握持造型，没有逐指张合动画。
- 重写普攻和四技能的动作曲线：短预备、快速有效段、惯性收势，关键姿态与伤害时刻共用动作时钟。连击从当前姿态接续，轻击后段支持输入缓冲，收招可转入防御。
- 真实命中才触发 45 ms 的武器停顿，重斩/第三段为约 74 ms；保持瞄准和世界时钟运行。短促镜头冲量与回弹，突刺最多增加 3.2° FOV，退出菜单/战斗时还原。加入本地产生的挥空、盾推、接触和弹反短音效。
- 刀光改为世界空间重采样的短尾，带细亮刃边与透明渐隐；命中火花集中接触点。技能分别为竖向下劈月牙、盾面断环、前向刺痕、紫金错层环斩。特效在暂停时停止推进。

## 操作与节奏

| 输入 | 动作 | 总长 | 命中时刻 | 基础伤害 / 体力 / 冷却 |
| --- | --- | --- | --- | --- |
| 左键 | 三段连斩 | 0.52 / 0.52 / 0.64 s | 0.20 / 0.20 / 0.26 s | 30 / 30 / 43.5 伤害，每击 8 体力 |
| 右键 | 举盾、弹反、吞印 | 原防御规则 | 举盾起始弹反窗口 0.22 s | 沿用格挡资源 |
| 1 | 破誓重斩 | 0.88 s | 0.32 s | 82 / 24 / 4 s |
| 2 | 缄眼盾击 | 0.60 s | 0.20 s | 26 / 18 / 6 s |
| 3 | 踏影突刺 | 0.68 s | 0.29 s | 58 / 22 / 5 s |
| 4 | 回旋封印 | 0.94 s | 0.36 s | 68 / 32 / 9 s |

上表总长不含命中停顿；接续输入可在约 78% 后段转入下一击。输入缓冲为 0.24 s，约 58% 后段可转入防御。突刺仍由 CharacterController 推进，最远 2.35 m，受场景和 Boss 碰撞阻挡。原恢复期反击倍率、满力量倍率、技能伤害与体力/冷却平衡保持。

参数入口为 `Assets/Art/PlayerSwordShield/OathboundLoadout.asset`。手模型可由 `OathboundHandsBuilder.BuildPrefabs()` 重建，完整接入菜单仍为 `Arcane Archive/Combat/Connect sword shield and melee`。无需重建即可运行。可供 Blender 编辑的静态 OBJ/MTL 副本位于 `Blender/PlayerSwordShield/HandsV2/`；Unity Mesh/Prefab 保留蒙皮数据。

## 最小验证

Unity 6000.3.25f1 / MilanLibrary_Magic_V6：

- [MinimalChecks.json](MinimalChecks.json)：8 项通过。使用真实 Input System 的虚拟输入事件检查 1～4、三段连斩、收招转防御、左右装备/蒙皮、I 暂停与禁止攻击。四技能各结算一次，伤害/体力正确；命中停顿生效，动作时钟单调。突刺遇 Boss 实际移动约 1.60 m 后停止，未越过目标。
- [PresentationReset.json](PresentationReset.json)：镜头冲量可见（该样本约 0.812°），清理后姿态/FOV恢复，返回大厅后体力100、无残留防御或攻击。
- 脚本编译通过。随后追加一次不调用截图工具的正常 Play Mode 重斩，检查动作与短特效自然结束；结果记录于 `FinalState.json`。
- 检查了大厅待机、跨身斩击、重斩、盾推、突刺、环斩关键姿态。截图使用受控动作时刻和暂停 AI 的临时夹具，部分特效年龄为手动固定的展示值，不能代替完整逐帧回归，也不用于核对 HUD。最终待机图为 `Captures/08_FinalReady.png`。
- 截图期间 MCP 包的 `ScreenshotUtility.cs:197` 出现5条 PlayerLoop 递归报错，堆栈保存在 [ScreenshotToolIssue.txt](ScreenshotToolIssue.txt)。该错误来自截图工具调用；已另外进行不截图的正常运行检查。未改第三方 Package。

未进行全流程回归、多帧率性能矩阵、独立包验证或全部姿态穿插检查。此轮实现参考录屏的节奏、持握、反馈和刀光，保留本项目魔法图书馆美术。

## 文件清单

修改：

- `Assets/Scripts/Core/MeleeLoadout.cs`：动作时间、接触时间与停顿参数。
- `Assets/Scripts/Player/PlayerMelee.cs`：连击缓冲、收招转防御、命中停顿、技能效果、冲刺时钟。
- `Assets/Scripts/Player/PlayerCombat.cs`：收招防御衔接。
- `Assets/Scripts/Player/SwordShieldView.cs`：左右调换、动作曲线、蒙皮前臂连接、刀光时钟。
- `Assets/Scripts/Visuals/BladeRibbon.cs`、`MeleeEffects.cs`、`Assets/Art/PlayerSwordShield/BladeLight.shader`：刀光、接触与技能效果。
- `Assets/Scripts/Visuals/Editor/SwordShieldSetup.cs`：新手安装、左剑右盾挂点。
- 主场景、OathboundLoadout.asset、README、CHANGELOG；重建入口同步保存原武器/Boss动作资产。

新增：

- `Assets/Scripts/Player/MeleeFeedback.cs`：可还原镜头反馈与本地产生的短音效。
- `Assets/Scripts/Visuals/Editor/OathboundHandsBuilder.cs`：新手网格、蒙皮权重、材质与导出源。
- `Assets/Art/PlayerSwordShield/Models/Oathbound_LeftHand.asset`、`Oathbound_RightHand.asset`，同名 Prefab、Gauntlet 材质及两张皮革贴图；对应 .meta。
- `Blender/PlayerSwordShield/HandsV2/` 和本交付记录目录。

指定原文件修改前快照在 `Before/`，哈希在 `BeforeHashes.json`；交付文件清单见 `ChangedFiles.json`。原 Blender 剑盾源文件与参考录屏均未改动。
