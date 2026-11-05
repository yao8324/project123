# 资料依据与冲突处理

V2 补充：已读取用户 Downloads 中《【无限机兵】于战场上起舞吧 最帅Boss 洛希德.mp4》，实际提取 132 帧检查造型、连段、位移及雷光。无需通过网页访问该视频；本地来源、时间点和改编说明见 [V2 设计](RefinementV2/Design.md) 与 LoshideReference 各组 manifest.json。以下网络失败记录只描述最初资料核对，不表示本次仍未看到参考。

本轮读取本地完整剑盾 1.0.6、项目父目录的策划/会议文档，以及本地 Git 对象中的 program/main 剧情资料。联网打开仓库未成功，Git 远端核对超时，因此这里的 `origin/*` 是本机已有远端跟踪引用，不能声称已核实 GitHub 当时最新状态。精确提交与关键文件 SHA-256 见 SourceManifest.json。

## 1. 世界观和版本关系

| 资料 | 本轮用途与处理 |
| --- | --- |
| 项目上级 `虚拟环境策划案第一版.docx`、`1.docx` | 阅读早期知识库定位、空间与制作原则；不把这些业务循环当作当前比赛故事的剧情规则 |
| 上级 `魔法图书馆1.0.5试玩反馈_四项问题.docx` | 确认碰撞、穿插、突兀装饰等历史问题；精细场景继续重视通行和构件合理性 |
| 根 README、ARCHITECTURE、AGENTS；1.0.6 AGENTS/CONTRIBUTING | 确认独立工程、历史归档、现有工作区与验证要求；本次不上传、不另定归档号 |
| 1.0.3 Docs/ProjectBrief、LibraryDemoArtPlan | 早期产品/场景与材质灯光背景 |
| program `IM_MONSTER/策划/IM_MONSTER_故事背景与剧情框架设定_20261024_v02.docx` | 源石、地热、供能、终境守藏院、遗赠、亡灵、玩家怪物身份与典狱长动机的原框架 |
| main `IM_MONSTER/策划/比赛版_20261107/IM_MONSTER_比赛版完整制作策划_v03.md` | 伊莱恩、蓝绳/工作牌/腹伤、先“不是伊莱恩”后“是怪物”、最后救援；短篇不强制敌对典狱长 |
| main `Assets/Contest20261107/Data/StoryContent.json` | 核对比赛版事件和身份泄露边界；其中设计状态字段不当作代码已实现的证据 |
| program Docs/StoryCombatPlan20261105/README、StoryAndEndings_v04、DevelopmentWorkflow | 第三关封印圣骑士、职责改编、三 Boss 流程、装置/援军/选择/结局及公共协作建议 |
| 团队任务/剧情/当前任务、场景/提交/策划对照与场景边界_20261102 | 核对现有任务边界、维护厅不等于核心封印室、典狱长先救援的既定关系 |
| main Docs/SealedDuelArena/CrystalCombat/Plan | 识别三水晶、减伤回血来自另一条分支成果；不谎称当前剑盾工作区已集成 |

本次采用 v0.4 的骑士职责和长线结构，继续遵守 v0.2 的底层世界规则。v0.3 短篇维持自身独立范围。v0.4 的三结局和公共契约属于方案，不会因为设计稿引用它们就变成已实现功能。

## 2. 现有 Boss、美术与战斗资料

已核对 1.0.6 的 BossEncounterDesign、WardenDesignReview，CombatV2/CombatFlow/ArcaneEffects 的实际资源与后续修订关系，以及 MeleeCombat、MeleePolish、BladeChoreography、CombatHUD、BossPressure、MeleeReach、VolleyHoming、PerfectDodge、DodgeTuning、StaminaDodge、TemporalStop、WaveMeleePolish 等记录和相关代码。重复历史备份保留为演变证据，不用较早数值覆盖当前资产。

实际风格参考包括 `Docs/BossDesign/References/library_style.png` 与 `Docs/MeleeReach20261030/Captures/BossWideSlash.png`：哥特结构、木作、暖光、旧金、紫黑和象牙色；典狱长为书页翼和长袍剪影。骑士采用同一材质语汇但改为完整、接地的防线形象。

数值基准具体读取：

- `Assets/Art/PlayerSwordShield/OathboundLoadout.asset`、`Core/MeleeLoadout.cs`。
- `Player/PlayerMelee.cs`、`PlayerCombat.cs`、`PlayerDash.cs`、`FirstPersonController.cs`。
- `Core/BossEncounter.cs`、`BossDefinition.cs`、`BossMeleeCombat.cs`、`BossCombatBehaviours.cs`、`BossPerfectDodge.cs`、`BossRitualEffects.cs`、`BossWavePresentation.cs`。
- `Assets/Art/ForbiddenWarden/WardenEncounter.asset`、`Visuals/BossArena.cs`、`UI/BossCombatHUD.cs`、`UI/RecoveryMenu.cs`。

重点冲突处理：

| 旧说明 | 当前依据 |
| --- | --- |
| 左键秘文弹、独立格挡条 | 当前装备为剑盾；攻击/格挡/吞印/突进共用体力 |
| 轻击体力 8、技能 24/18/22/32 | 当前为 12 与 30/24/28/38 |
| 体力等待 0.8/1.6 秒 | 当前普通等待 0.5 秒，耗尽 2 秒并行 |
| 激光持续追踪 10°/秒 | 当前典狱长发射期间锁线，资产 trackingSpeed=0；骑士不直接复制此招 |
| 最新归档 1.0.7 就是完整剑盾版 | 1.0.7 的来源说明明确不含此工作区后续剑盾/闪避，故本次以指定 1.0.6 开发 |
| 骑士一定大剑重甲，或一定为百年前本人 | 旧案只有候选外观与继承职责；V1 曾提案剑盾重甲，V2 按用户视频改为修长银甲与长柄刃器，个人身世仍不擅定 |

## 3. 本次新增提案

终扉守誓者称号、终扉前庭、完整冠环、1400 生命、55%/20% 阈值、局部撤离证据与候选台词，均为本次设计/原型。V1 按最初要求采用简模与六招；用户随后指定洛希德视频并要求不要过于简洁，V2 已改为长柄刃器、临时腕盾、分层银甲与封缄长带，扩展为八招。两版都要求写明后续精细开发思路；这些提案不冒充原案定稿或团队已验收事项。
