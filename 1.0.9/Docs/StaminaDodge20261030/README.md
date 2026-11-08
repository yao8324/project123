# 体力恢复与全伤害闪避 · 2026-10-30

工程：当前打开的 1.0.6，通过 Unity MCP 修改和运行检查。

## 当前规则

| 项目 | 行为 |
|---|---|
| 普通恢复等待 | 每次实际消耗体力后 0.5 秒；动作或格挡持续时不恢复 |
| 耗尽惩罚 | 体力从正数归零时开始 2 秒，与普通等待同时计时，不累计成 2.5 秒 |
| 惩罚期间 | 禁止新攻击、技能、突进及举盾；已发动动作可完成，移动和跳跃保留 |
| 返还体力 | 可入账，但不能提前解除耗尽惩罚 |
| 惩罚计时 | 失败输入、零消耗不会重新计时；暂停冻结，战斗重置清除 |
| 破防 | 使用同一耗尽计时；到期可恢复，松开防御键才能重新举盾 |
| 提示 | 体力条上方显示“体力耗尽”及剩余秒数，技能在惩罚中不可用 |

恢复速度仍为 14 点/秒。0.5 秒从最后一次真实消耗开始计算；长动作结束时若计时已过，不额外再等 0.5 秒。旧 BossDefinition.guardRegenDelay 仍保留为历史兼容数据，当前共用体力不再读取它。

## 完美闪避覆盖

- 横斩、禁页弹幕、Boss 冲撞、裂地爆发、扩散环波、光束均从实际伤害检查接入。
- 光束每段实际伤害都能触发，不再仅限刚发射的第一段。
- 弹幕沿真实运动路径检查至首次碰撞：先越过起步胶囊再撞墙仍可奖励；先被墙挡住不能奖励。
- 仍需在突进开始后 220 毫秒内，攻击命中起步位置且玩家已离开至少 0.6 米、当前位置安全。同帧受击、格挡或吞印取消候选；每次突进最多奖励一次。
- 普通突进不自动无敌。已有的 0.75 秒减速、奖励前 0.30 秒保护和彩色虚影继续沿用。

## 修改文件

| 文件 | 修改 |
|---|---|
| Assets/Scripts/Core/MeleeLoadout.cs | 普通等待 0.5 秒与耗尽惩罚 2 秒数据 |
| Assets/Art/PlayerSwordShield/OathboundLoadout.asset | 当前剑盾配置保存最终参数 |
| Assets/Scripts/Player/PlayerMelee.cs | 两段等待并行、耗尽状态、动作入口限制、精确恢复边界 |
| Assets/Scripts/Player/PlayerCombat.cs | 破防共享耗尽计时，移除旧恢复延迟叠加 |
| Assets/Scripts/UI/BossCombatHUD.cs | 耗尽倒计时、恢复举盾提示与技能不可用状态 |
| Assets/Scripts/Core/BossEncounter.cs | 持续激光每段判定、弹幕首次碰撞前的有效路径 |
| README.md、CHANGELOG.md | 操作说明、当前参数与更新记录 |
| Docs/PerfectDodge20261030/README.md | 同步当前闪避覆盖和体力规则 |
| Docs/StaminaDodge20261030/ | 修改前备份、专项探针、结果与文件清单 |

## 最小验证

仅运行本次相关的 MCP Play Mode 探针，没有全量测试或构建。

[MinimalChecks.txt](MinimalChecks.txt) 共 19 项通过：0.49/0.51 秒恢复边界、1.99/2.01 秒耗尽边界、失败输入不延长、体力返还不能绕过、最后一管体力的实际突进和奖励、破防及重置、六类真实攻击、持续激光后续段、墙前/墙后弹幕、超时和同帧受击拒绝奖励。Console 0 Error / 0 Warning；新提示所需中文字形均存在。

复查步骤：Unity Play Mode 后执行 FixtureSetup.cs.txt，再依次执行 CoreChecks、RushPrepare、LaserPrepare、PagePrepare、WallPrepare、WallVerify。各文件为文档，不参与项目编译。物理检查准备和验证分开工具调用，以便 CharacterController 的手动位移进入物理查询。一次探针因 C#6 局部变量重名导致准备阶段未执行，已修正并仅复查受影响墙体用例；保留初次结果及说明于 FirstProbeRun.txt、ProbeSetupNote.txt。

已退出 Play Mode，时间倍率和后台运行设置恢复，临时障碍已清理，场景未保存运行时对象。最终状态见 FinalVerification.txt，文件 SHA-256 见 FilesChanged.json。

手动体验：消耗部分体力后停手，约 0.5 秒开始回升；把体力用到零时观察 2 秒倒计时。用 A/D + Shift 闪出禁页、横斩或已在持续发射的光束，极限时机应出现奖励。
