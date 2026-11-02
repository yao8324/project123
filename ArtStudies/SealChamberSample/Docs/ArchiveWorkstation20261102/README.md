# 档案工作区 · 用户追加美术候选（2026-11-02）

本轮停在**第一阶段：可独立检查的工作台摆放与灾前/受袭后状态**。用户在维护厅交付后明确授权继续。此候选不冒充Client新派发任务，不改变ART-W20261028-01已提交待验收状态，也不表示正式采用或完整剧情已接入。

使用现有Unity **6000.3.24f1**、URP **17.3.0**；没有安装新Unity。未修改 `1.0.6/` 的共享场景、桌子、展示装置或程序逻辑。

## 已完成

- 选择现有入口附近右侧阅读桌：`IV4_W04_Table` 的mesh_0685/0686/0687，桌面中心约 **(5.8,0.836,8.4)**。魔法书/地球仪展示台位于Z约13.3，未挪动或覆盖。
- 一个工作区附加Prefab，复用上一轮档案夹、蓝绳、工作牌，只新增已封档绳结及少量散页。三份档案按I/II/III保留数据槽位，不擅自编写剧情正文。
- 灾前摆放：三份档案有序排列，工作牌正面、松开的蓝绳。
- 已封档表现：第一份档案上增加交叉蓝绳及绳结。表现切换不自动完成任务。
- 受袭后摆放：档案轻微错位，少量纸页落在桌后，保持玩家正面接近方向；工作牌朝下并关闭背透姓名印字。已封档状态跨灾前/受袭后切换保留。
- 预留工作视点、醒来观察点、返回取记录目标、后续绳结/工作牌近景点。醒来位置只是镜头候选，不是已验证的玩家出生点。
- Inspector提供中文按钮，可查看灾前/受袭后摆放并切换蓝绳状态。预览场景附冷暖照明建议，交付附加Prefab中不含灯光或实体桌子/地板。

## 打开与查看

1. 用现有24版打开 `ArtStudies/SealChamberSample`。
2. 打开 `Assets/Art/SealChamberStudy/ArchiveWorkstation20261102/Scenes/ArchiveWorkstationReview.unity`。
3. 选中 `ArchiveWorkstation_ArtCandidate`。Inspector顶部的中文按钮切换灾前、受袭后、封档状态；切换仅用于摆放预演。
4. `REVIEW_ONLY_ExistingDeskReference` 是从现有桌子提取的局部比例参照，参考色材质不代表正式桌子的纹理调整。`REVIEW_ONLY_Lighting` 是预览照明。二者均不在附加Prefab内。

## 程序接入

主要资产：`Assets/Art/SealChamberStudy/ArchiveWorkstation20261102/Prefabs/ArchiveWorkstation.prefab`。

根放到 **(5.8,0.836,8.4)**、Y旋转0°、缩放1，叠在**现有阅读桌上**。保留运行场景原桌子及碰撞，不导入第二张参考桌子/地板/预览灯。资源依赖上一轮 `MaintenanceW20261028` 道具与同根Scripts，必须保持路径和meta。

交互使用沿用的 `MaintenanceArtAnchor`：

| 锚点 | 用途 |
| --- | --- |
| ARCHIVE_RECORD_SLOT_01 / 02 / 03 | 绑定剧情成员提供的三份档案数据 |
| ARCHIVE_TIE_CORD | 程序接收系蓝绳请求，完成条件满足后切换封档表现 |
| ARCHIVE_WORK_BADGE | 开场工作牌观察点；不是醒来后的玩家身份确认 |
| ARCHIVE_SEAL_COMPLETE_MARKER | 已封档视觉位置，无独立交互碰撞，不计作额外任务 |

灾前和受袭后各有同ID的锚点，但只有一组激活；不要同时打开两个状态根。道具使用Trigger作为射线目标，没有新增实体挡路碰撞。程序应使用原有统一交互入口适配，不能同时激活两套输入器。

`ArchiveWorkstationArt.SetArchiveSealed(bool)`改变蓝绳表现；`SetDisturbed(bool)`改变摆放并保留封档状态；`ResetPresentation()`仅重置美术表现。它们均不传送、不推进事件、不改字幕/姓名、不改程序任务计数、不增删运行灯光。正式灾变灯光由程序绑定已有灯具，预览的冷暖色只是美术建议。

建议玩家工作位置约(5.8,0.08,7.3)，对应基线已有桌前接近方向；侧门返回点仍为(6,0.08,1.4)。下一阶段需在程序候选副本中做二者之间的实际通行和完整环境观察，本轮没有据此宣称路线已通过。

## 验证与限制

见[validation.json](validation.json)和[逐条检查](validation-checks.txt)：24版编译、制作成功；99条美术检查通过，覆盖四种灾变/封档组合、活动槽位唯一性、三份档案中心及四角支撑、可交互距离、纸页不进入桌前接近方向、牌背朝上且无活动姓名印字、状态保留、无额外实体碰撞/灯光、无空网格。检查采用独立展示环境，不等同于生产控制器、完整故事或目标设备性能验收。

待完成：正式大厅环境中的摆放复核；真实角色绕桌和工作台—侧门往返；完整开场镜头节奏与剧情/程序事件接线。遗体与伤口模型未制作，后续反转只预留观察点。三份正文仍待剧情成员交付。

## 图片

[灾前第一人称](Captures/01_intact_first_person.png) · [完成封档](Captures/02_seal_complete.png) · [受袭后同视角](Captures/03_disturbed_same_view.png) · [散页位置](Captures/04_disturbed_overview.png) · [工作牌和蓝绳近景](Captures/05_badge_and_cord.png)

## 制作与续做入口

- 运行组件：`Assets/Art/SealChamberStudy/Scripts/ArchiveWorkstationArt.cs`。
- 制作源：`Assets/Scripts/Visuals/Editor/ArchiveWorkstationBuilder.cs`，以及同目录 `ArchiveWorkstationArtInspector.cs`。
- 重建菜单：`Art Studies / Archive / Build workstation candidate`。读取仓库现有 `1.0.6/Assets/Art/MilanLibraryV6/Meshes/mesh_0685～0687.mlmesh` 提取局部参考；运行工程源文件只读。
- 出图菜单：`Art Studies / Archive / Capture workstation candidate`，重建后建议新开一次编辑器出图。
- 下一步先导入**附加Prefab**到程序独立预览副本，核对实际材质/环境、操作视线与绕桌路线，再扩大到灾后引导。不要把整套参考场景覆盖到正式大厅。
