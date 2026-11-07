# 终扉圣堂 · Unity 场景交付（V2 上部重构）

**当前是按用户反馈重做的 V2**：主厅抬高至约 23.3 米；五道厚主尖拱、分瓣交叉穹顶、十座束柱、雕花柱头、上层回廊和八扇高侧窗。V1 的简化圆顶已替换。

基于 `lixiang` 的 1.0.8（`c87b41c`），Unity **6000.3.25f1**。这是独立本地开发工程，团队远端没有推送或合并。

## 打开与试玩

1. 在 Unity Hub 添加本工程目录：`圣骑士场地_20261107/1.0.8`。
2. 查看和编辑场景：`Assets/Scenes/BossTests/Knight_Sanctuary_Art.unity`。Scene 视图可直接选建筑、道具与灯光；点击 Play，Game 视图显示独立场景的最终专用渲染管线。
3. 实际战斗：`Assets/Scenes/BossTests/Knight_Sanctuary_Test.unity`，点击 Play 自动进入圣骑士挑战。使用原有游戏控制；Esc 打开菜单，结束后可重试或返回图书馆。
4. 菜单 `Arcane Archive > Knight > Open sanctuary art scene / Open sanctuary battle scene` 也可切换场景。

## 场景内容

- 保留 28 米直径战斗场地与原有出生点。象牙石材束柱、分块尖拱、五跨主券与分瓣交叉拱面、上下回廊、下层高窗、八扇高侧窗与玫瑰窗饰；主厅高度约 23.3 米。
- 北侧双扇金属封印门、黄铜残环、横栓、铆钉、冷色观察缝，以及旧裂缝上的维修夹具。
- 平坦深灰石板地面、细黄铜圈与罗盘纹样；主战区不堆杂物。
- 周边档案架、管线、维修灯；撤离角落含蓝绳箱、空托位、清单板、维修工具与垂落布料。
- 七组原创 2048×2048 可平铺 PBR 贴图：石灰岩、象牙大理石、蓝灰纹石、旧金、钢、木材、皮革，含法线、金属度、光滑度及遮蔽。大理石为石内纹脉，柱头是连续铸金底座与双层立体叶饰，带主叶脉和分支细纹；门环为有厚度和倒角的扁平金属镶件。使用 URP Lit。
- 8 盏灯，主光有软阴影，独立 SSAO 接触阴影、反射探针、ACES 色调与轻量 Bloom。第二阶段门前冷光缓慢增强，跟随战斗时钟；离场恢复原有环境与渲染管线。

## 编辑资源

主要预制体：`Assets/Art/Bosses/Knight/Sanctuary20261107/SanctuaryArena.prefab`。

修改此预制体可同时更新美术场景与战斗实例。建筑位于 `Sanctuary architecture`，照明位于 `Sanctuary lighting`，模块、材质、网格、贴图在相邻文件夹。窗廊为可重复摆放模块；束柱交接处使用省去重复细柱的变体，主要组按材质合并网格；单块石材等细部可通过生成源重建。

`SanctuaryKnight.asset` 复制原始圣骑士配置，仅替换场地。原版 `ProductionV5` 资源及原版测试场景保留。

生成源：
- `Assets/Scripts/Visuals/Editor/KnightSanctuaryBuilder.cs`：布局、建筑、道具、灯光与两个场景。
- `KnightSanctuaryGrandArchitecture.cs`：V2 主拱、束柱、叶饰柱头、交叉拱面、高侧窗和上层回廊。
- `KnightSanctuaryGeometry.cs`：网格生成与模块合并，小花饰按实际弧长控制细分。
- `Assets/Scripts/Visuals/KnightSanctuaryLighting.cs`：运行时场景环境切换与恢复。
- `Tools/KnightSanctuary/materials.py`：可重复生成的原创纹理，依赖 Python、NumPy、Pillow。

**重新生成会覆盖本次生成的资源与两个场景中的手工调整。** 先保留手工改版或把修改写入生成源。菜单 `Build concept sanctuary` 生成后，请关闭并重新打开工程再做最终视觉验收，避免同一进程的旧网格缓存。

## 验证与实景

`AssetValidation.json`、`RuntimeValidation.json`、`ArtValidation.json` 保存最后一次实际 Unity 验证结果。`Captures/` 是 V2 Unity 渲染出的 1920×1080 实景，不是概念图。`Captures_V1/` 仅用于旧版对照；`12_Comparison_LockedCamera.png` 与 V1 入口使用相同相机位置和视角。

验证覆盖原有碰撞保留、额外道具碰撞、48 个近墙通行位置、角色控制器 192 段完整绕场移动、两阶段全部启用招式与第二阶段玩家相机实景、暂停时钟、重试、返回/再进入、环境与渲染管线恢复。自动战斗检查使用高血量和受控时间推进，不代表真人难度验收或目标 Windows 机器的性能验收。

`ProtectedBaseline.json` 记录受保护的原有源码和配置哈希。验证器排除 Unity Editor Search 自身的缓存异常；游戏运行错误另行记录，不能把该报告理解为整个编辑器从未出现外部服务或缓存警告。

本次交付是概念方向的 Unity 模块化实现；概念图用于比例、材质和氛围参考。主线剧情与结局接入、Windows 构建和团队合并不在本次场景搭建范围内。

## 独立审查与交付边界

前两轮独立代码/资源审查覆盖基础场景与 V2 主体结构；发现的撤离箱与书架穿插已改为空壁龛。后续大理石、黄金浮雕及比例调整由作者进行冷启动近景复核，额外修复细柱与主束柱穿插，并增加间距断言。五个家具碰撞体只占用外围道具区；原有地面与边界碰撞逐项保留。

两个非阻塞维护项已记录：克隆渲染器继续引用原版的时间减缓/残影 Renderer Features（修改这两项会影响两处）；反复生成时可能留有未引用的旧 SSAO 子资源，当前仅有一份启用。时间减缓特效在返回后的专项视觉验证尚未做。

本地使用独立的 1.0.8 稀疏检出，避免复制仓库内其它七版工程；工作资源完整。最终视觉验收采用重启 Unity 后的结果。本次已针对柱子、主拱、穹顶的体量与复杂层次进行结构性重做；实际结果请看 V2 实景。最新材质细节见 `15_Marble_Gold_Capital.png`、`16_Marble_Pier_Base.png`，整体比例见 `17_Reference_Proportions.png`。
