# V2 交付与验证记录

当前成果是可以运行、继续替换资产的细化战斗原型。参考来源为用户提供的本地洛希德视频；设计和后续制作分别见 [当前方案](Design.md) 与 [角色/场景制作说明](ArtProduction.md)。

## 打开与试玩

用 Unity 6000.3.25f1 打开当前 1.0.6 工程，打开 [Knight_Test.unity](../../../Assets/Scenes/BossTests/Knight_Test.unity)，点击 Play。测试入口会自动加载 `KnightDefinitionRefined`，无需重新运行生成器。仍沿用当前完整玩家输入：左键连斩、右键举盾/瞬间弹反、数字1–4装备技能、Shift突进，其他按键以项目绑定界面为准。Esc 暂停；结果页支持重试与返回图书馆。

当前默认不启用20%剧情门槛，便于测完整胜负；需要测试停战时在 `KnightTrialLauncher` 勾选 `storyGate`。正式主线、三结局与存档尚未接入。

## 已实际完成的核对

| 项目 | 实际结果与证据 |
| --- | --- |
| 项目编译 | 脚本修改后已等待重编译完成；最终项目 Console 为0 Error、0 Warning |
| 原有战斗回归 | [31项检查](Regression31.json)，31通过、0失败；包含剑盾接线、旧六招、阶段、停战、重试和返回 |
| 新版行为检查 | [30项检查](RefinedChecks.json)，30通过、0失败；包含旋锋移动/碰墙/弹反、后撤三刃、落雷锁点/伤害/升降/暂停/打断与返回 |
| 运行资源 | [AssetAudit.json](AssetAudit.json)：角色105个网格实例、7904三角面，丢网格/丢脚本/缺失或不支持材质均0 |
| Unity元数据 | [DeliveryAudit.json](DeliveryAudit.json)：本轮资源无缺失meta、无重复Knight GUID；文档本地链接无缺失 |
| 最终编辑器 | [FinalVerificationV2.json](FinalVerificationV2.json)：已退出Play，测试场景仍指向新版，记录实际编辑器和构建入口状态 |
| 中文菜单 | 已实际渲染并查看 [结果页](ResultMenu.png)，无缺字警告 |
| 文件检查 | `git diff --check -- 1.0.6` 通过；[394个源文件校验记录](SourceManifestV2.json) 可核对当前实现 |

31+30是受控 Play Mode 集成检查，不是61次真人完整战斗。测试暂停自动AI和控制器推进，调用正式移动、受击、技能、时钟与表现方法；没有据此声称真实输入端到端、完整获胜、低帧率所有边界或目标机性能已经验收。未制作独立构建。

角色目前使用105个独立网格Renderer，便于关节和部件编辑，尚未做发行级合批、LOD与蒙皮。7904三角面只是当前原型统计，不能用来证明绘制开销合格。

## 动态与静态预览

- [回身旋锋](Orbit.gif)：44个原生Game View帧，按12帧/战斗秒推进，展示转身、切线移动和两次释放。
- [悬誓落雷](Heavenfall.gif)：64帧；锁点后移动玩家以观察雷柱，圈和命中仍由正式逻辑计算。
- [模型近景](Model_Close.png)：暂时隐藏HUD与玩家手部的原生Game View，用于查看造型。
- [旋锋分镜](Orbit_Contact.jpg) / [落雷分镜](Heavenfall_Contact.jpg)：标注受控战斗时间的截图索引。

动态预览由 [CapturePreview.cs.txt](CapturePreview.cs.txt) 记录的方法体生成，截取的是游戏实际渲染；没有把视频参考帧当作游戏画面。这些预览不是实时人类操作录像。原视频只读，未导入运行资产。

## 修改和新增的脚本

以下路径相对当前工程；共享脚本的改动保留在少量骑士入口、姿态/受击分流与文案处。其余为独立骑士模块或编辑器生成器。

| 文件 | 用途 |
| --- | --- |
| [BossEncounter.cs](../../../Assets/Scripts/Core/BossEncounter.cs) | 骑士会话、状态、暂停、重试与伤害入口 |
| [BossCombatBehaviours.cs](../../../Assets/Scripts/Core/BossCombatBehaviours.cs) | 骑士表现与转向分流 |
| [BossMeleeCombat.cs](../../../Assets/Scripts/Core/BossMeleeCombat.cs) | 避免骑士受击套用典狱长骨架 |
| [BossCombatHUD.cs](../../../Assets/Scripts/UI/BossCombatHUD.cs) | 骑士阶段名 |
| [RecoveryMenu.cs](../../../Assets/Scripts/UI/RecoveryMenu.cs) | 会话结果文案及骑士静态中文字体 |
| [KnightDefinition.cs](../../../Assets/Scripts/Core/Bosses/Knight/KnightDefinition.cs) | 八招数据、位移/升空参数 |
| [KnightEncounter.cs](../../../Assets/Scripts/Core/Bosses/Knight/KnightEncounter.cs) | 锁向、选择、真实移动、预警、伤害与清理 |
| [KnightTrialLauncher.cs](../../../Assets/Scripts/Core/Bosses/Knight/KnightTrialLauncher.cs) | 独立测试入口和可选停战窗口 |
| [KnightVisuals.cs](../../../Assets/Scripts/Visuals/Bosses/Knight/KnightVisuals.cs) | 关节、握持、武器、长带与姿态混合 |
| [KnightRefinedEffects.cs](../../../Assets/Scripts/Visuals/Bosses/Knight/KnightRefinedEffects.cs) | 刃迹、待发刃、金弧、雷纹与生命周期 |
| [KnightTrialBuilder.cs](../../../Assets/Scripts/Visuals/Editor/KnightTrialBuilder.cs) | 保留的V1生成器；不会在试玩时自动重建 |
| [KnightRefinementBuilder.cs](../../../Assets/Scripts/Visuals/Editor/KnightRefinementBuilder.cs) | 原生细化网格、材质、Prefab、字体与独立V2配置生成 |

新资源集中在 [Knight](../../../Assets/Art/Bosses/Knight) 和 [BossTests](../../../Assets/Scenes/BossTests)。原V1资源和推导保留为历史资料，当前入口使用 `Refined` 下的新模型、场景Prefab、定义与字体。

本轮未提交或推送Git，未新建归档版本号，未更改构建入口。1.0.7已有的ProjectSettings修改保留。下一阶段优先做正式握持、脚步和动作过渡，再进入贴图、灯光、音效、性能和完整实战调平。
