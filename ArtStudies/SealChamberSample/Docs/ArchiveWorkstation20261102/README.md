# 档案工作区 · 用户追加美术候选（2026-11-02）

当前完成第二阶段的**正式大厅独立副本摆放、真实角色交互及三次往返验证**。使用本机已有Unity6000.3.24f1、URP17.3.0；25版未复验。没有修改原运行工程 `1.0.6/` 的场景、程序、版本或包设置。此候选由用户追加授权，待Client检查，不改变原周任务的采用结论。

策划依据已经重新通读，见[策划对照与场景边界](../../../../团队任务/场景/提交/策划对照与场景边界_20261102.md)。本周流程止于返回取记录；维护厅不是源石本体所在的最深封印室。

## 已完成与风格

- 附加Prefab放在现有右侧阅读桌mesh_0685/0686/0687上，根世界坐标 **(5.8,0.836,8.4)**，Y旋转0°、缩放1。保留原桌、椅、台灯、书堆、地毯及其碰撞。
- 三个档案槽位缩放至0.72，避开原台灯和书堆；蓝绳挂在左前桌沿。实景发现原建议站位被Chair_11占用，工作脚点改为 **(5.2,0.08,7.1)**，并验证没有安全落点偏移。
- 灾前/受袭后互斥摆放；散页位于桌后且高于原地毯；受袭后工作牌翻面并隐藏姓名印字。封档状态跨灾变切换和往返保留。
- 复用原大厅的木作、红褐皮革、旧黄铜和暖纸材质/贴图。原库材质在独立制作工程按原路径、GUID镜像，见[逐文件来源与校验](material-reuse.json)；原游戏文件没有被改写。工作牌铜壳、档案封皮和纸张与既有环境同源。
- 完成封档后出现小尺度暖金封印纹样，取自原有V5_Magic_Gold配色并降低发光强度；最大直径约4.2cm，无新灯光、粒子、计时或输入逻辑。它只作为封档结构候选，不解释为灵魂或任意能力。
- 侧门及维护厅出口新增非阻挡射线目标，交互观察点Y=1.55，便于平视靠近；实体墙、门槛与中央通行区保留。
- 中文Inspector按钮预演灾前、受袭后和封档状态。附加Prefab不包含参考桌、地板、运行灯光或第二套输入器。

## 打开与接入

用24版打开 `ArtStudies/SealChamberSample`，打开 `Assets/Art/SealChamberStudy/ArchiveWorkstation20261102/Scenes/ArchiveWorkstationReview.unity`，选择ArchiveWorkstation_ArtCandidate。

交付资产：`Assets/Art/SealChamberStudy/ArchiveWorkstation20261102/Prefabs/ArchiveWorkstation.prefab`。导入整个 `Assets/Art/SealChamberStudy/`，保持路径和meta；运行工程已有 `Assets/Art/MilanLibraryV6/` 依赖。制作工程的该材质镜像只用于独立制作可复现，不在运行工程再导入第二套GUID。

`REVIEW_ONLY_ExistingDeskReference` 包含从原网格提取的局部桌椅/台灯/书堆/地毯参照；`REVIEW_ONLY_Lighting` 是预览照明。二者均不进入附加Prefab，也不覆盖正式大厅。

| 锚点 | 程序接入用途 |
| --- | --- |
| ARCHIVE_RECORD_SLOT_01 / 02 / 03 | 绑定剧情成员的三份档案数据 |
| ARCHIVE_TIE_CORD | 接收系绳请求，条件满足后切换封档表现 |
| ARCHIVE_WORK_BADGE | 开场工作牌观察；醒来后不确认玩家身份 |
| ARCHIVE_SEAL_COMPLETE_MARKER | 已封档视觉位置，无额外交互碰撞或任务计数 |

灾前和受袭后各有同ID锚点，但仅一组激活。道具Trigger用于射线，没有实体挡路碰撞。程序应适配统一交互入口，不能同时激活多套输入器。

`ArchiveWorkstationArt.SetArchiveSealed(bool)`切换封档表现；`SetDisturbed(bool)`切换摆放并保留封档状态；`ResetPresentation()`只重置美术表现。它们不传送、不推进剧情、不改字幕/姓名、不计任务、不增删灯光。正式灾变照明由程序绑定既有灯具。

工作脚点(5.2,0.08,7.1)，醒来候选脚点(5.05,0.08,7.0)，侧门安全返回(6,0.08,1.4)。摆放接口见[placement.json](placement.json)。

## 第二阶段验证

- [静态106项](validation.json)：四种状态、活动槽位唯一、档案中心/四角支撑、距离、原台灯/书堆不重叠、无新增实体阻挡/灯光、散页高于地毯、工作位净空、姓名关闭及封档保留。
- [正式大厅独立副本124项](Stage2/runtime-validation.json)：生产角色控制器、原Input System和PlayerInteractor类，真实E回调、绕桌六段、蹲起、醒来落点、侧门、非敌对维护厅三个节点及三次往返，最后返回取记录。一次E恰好一次回调，已稳定节点拒绝重复交互，只有一个玩家且无遗留厅实例。最低脚点Y约0.075。
- [约76秒实景录像](Stage2/档案工作台_三次往返_实机验证.mp4)：1280×720，最多8fps采样，按真实时间戳保留路线时长，无音轨。479帧；详见[视频信息](Stage2/video-info.json)、[原始轨迹](Stage2/trajectory.csv)和[成功射线记录](Stage2/ray-observations.txt)。
- 制作工程整个Assets的177份meta检查：缺失0、孤立0、重复GUID0；本轮拥有的材质/网格无未引用项，见[资产审计](asset-audit.json)。原有无关设置差异和WPS锁文件不纳入提交。
- [最终资源既有战斗36/36通过](Stage2/combat-regression.json)，退出Play Mode且编译/场景脏状态正常。初次未隔离输入的运行停在右键吸收；默认D/右键路径核对正确，测试限定到虚拟设备并显式采用测试默认绑定后通过。初次失败原因未独立定性，不归因于美术，也不隐去失败报告，见[初次记录](Stage2/combat-attempt1.json)及[输入范围](Stage2/combat-input-scope.txt)。游戏及原回归类未修改，用户绑定文件未保存。

验证使用原大厅与控制器，额外适配器仅负责把美术锚点接到原交互类，并在测试中执行安全传送/非敌对厅实例。测试输入限制到虚拟设备，避免电脑上其他输入干扰。没有实现或替代五事件剧情、正式淡出加载、菜单过渡防重复、6–8分钟成片或人工试玩。RTX4060 Laptop/i7-14650HX、Ultra编辑器观察不代表独立包/目标设备性能达标。

原PlayerInteractor在部分低头角度可能射中玩家自己的CharacterController；本轮侧门观察点提高到平视后通过，程序仍应针对统一交互排除自身碰撞做独立修复验收，不把当前三次通过认定为所有观察角都已修复。

## 图片与续做

[实景灾前](Stage2/Captures/01_production_intact.png) · [封档后](Stage2/Captures/03_production_sealed.png) · [受袭后](Stage2/Captures/04_production_disturbed.png) · [侧门](Stage2/Captures/05_production_side_door.png) · [维护厅出口](Stage2/Captures/06_production_hall_exit.png) · [第三次返回](Stage2/Captures/07_return_to_workstation_3.png)

制作源 `Assets/Scripts/Visuals/Editor/ArchiveWorkstationBuilder.cs`；重建菜单 `Art Studies / Archive / Build workstation candidate`；出图菜单 `Capture workstation candidate`。独立验证工具保存为[ArchiveRuntimeStage2.cs.txt](ArchiveRuntimeStage2.cs.txt)，仅放入测试副本Assets/Editor运行，不导入正式工程。

下一步是程序/剧情绑定正文、任务完成条件、灾变、睁眼、淡出与返回事件，Client验收风格。场景成员按冻结要求修已有阻塞、补镜头与交接说明。遗体、伤口、复杂手部动画和最终战本轮不制作。
