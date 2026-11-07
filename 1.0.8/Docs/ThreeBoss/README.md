# IM MONSTER：遗赠 · 三 Boss 书本整合试玩

本版本按用户最新要求，用本地模型和战斗代码，形成书本 → 缚魂者 → 典狱长 → 黄金翼圣骑士 → 撤离结尾的线性完整试玩流程。

## 运行

Windows x64：解压整个发布 ZIP，双击 IMMonster.exe。必须保留同目录下 IMMonster_Data、UnityPlayer.dll、MonoBleedingEdge 等所有文件；不能只复制 exe。离线运行，不需要安装 Unity。

启动自动翻开书本。选择“翻开故事 · 连续挑战三关”，阅读短对白后点击“继续”。也能从书本独立挑战任意 Boss。胜利选择“进入下一关”，第三关选择“完成故事”。失败选择“重试本关”，恢复当前场地与双方生命值。Esc 暂停，可返回书页重选。

操作：WASD 移动，鼠标观察，空格跳跃，Shift 冲刺，左键剑术连斩，右键举盾/弹反，数字 1–4 技能，I 装备属性，Esc 暂停。具体键位以暂停菜单的战斗按键为准。

## 本地资源与剧情

- 缚魂者：本地 Soulbinder_TargetRebuild_R18.blend、大厅 SoulbinderHall_Detailed_R10.blend 导出。合并网格与材质槽，保留骨架和既有待机/施法动作。新增第一关战斗适配复用典狱长底层，使用远程禁页、追踪裂地与环波，降低伤害和生命；不是原策划完整的召唤小怪/锚点管理战。
- 典狱长：已有战斗和骨架，使用本地 SealedDuelArena20261104 最新场景美术；本整合版的外围水晶作为场景装饰，未接入另一工作副本的独立水晶供能玩法。
- 圣骑士：R17 黄金翼骑士，56 米直径场地，现有剑盾/冲刺/跳跃攻击。
- 剧情依据 StorySource_20261104.txt。先揭示“我不是伊莱恩”，再确认主角是逃出封印的怪物。采取救出典狱长、维持撤离供能的线性路线；修复与救援以战后文字叙述呈现，不包含原长篇策划的完整探索、三个分支结局或配音。

## 源码入口

Unity 6000.3.25f1，打开 Assets/Scenes/IMMonster_ThreeBoss_Book.unity。
Campaign.asset 保存三关配置和全部关前/关后对白；CampaignBook.asset 保存书籍标题与资料。BookCampaign.cs 控制剧情与推进；RecoveryMenu.Campaign.cs 绘制书本、对白和结果。

主要新增/修改：Core/BookCampaign.cs、CampaignDefinition.cs、BossDefinition.cs、BossCombatBehaviours.cs；UI/RecoveryMenu.cs、RecoveryMenu.Campaign.cs；Visuals/SoulbinderCampaignVisuals.cs；Visuals/Editor/ThreeBossBuilder.cs、ThreeBossCampaignAcceptance.cs、ThreeBossFlowCheck.cs；Art/ThreeBoss/ 及独立场景。

## 重建和验证

在工程根目录用已安装的 Unity 编辑器执行：

- 重新生成整合场景：-batchmode -quit -projectPath <工程目录> -executeMethod ArcaneArchive.Visuals.Editor.ThreeBossBuilder.Prepare -logFile <日志>
- 实际 Play Mode 验收（不能附加 -quit，检查结束会自行退出）：-batchmode -projectPath <工程目录> -executeMethod ArcaneArchive.Visuals.Editor.ThreeBossCampaignAcceptance.Run -logFile <日志>
- Windows x64 Mono 构建：-batchmode -quit -buildTarget win64 -projectPath <工程目录> -executeMethod ArcaneArchive.Visuals.Editor.ThreeBossBuilder.BuildWindows -logFile <日志>

Acceptance.txt 保存断言和测试边界；WindowsBuild.txt 保存构建结果。受控命中测试与实际自动战斗烟雾检查不能等同首次真人全程试玩。当前宿主是 Mac，Windows 原生运行和目标显卡性能需要 Windows 设备复验。

原始两个工作副本保留。本分支独立整合，没有合入 main。
