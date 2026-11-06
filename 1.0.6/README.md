# IM MONSTER · main 当前工程

2026-11-06：**战斗操作与界面已统一到 1.0.8 的剑盾方案**，同时保留 main 的剧情、封闭斗场和三水晶供能。目录继续沿用 `1.0.6/`。

- **编辑器：Unity 6000.3.24f1。** Unity Hub 添加本目录，等待资源与脚本导入。
- **完整启动：** `Assets/Scenes/IMMonster_FullDelivery.unity`，进入 Play Mode 后选择剧情探索或 Boss 斗场。
- **直接战斗：** `Assets/Scenes/SealedDuelArena_Showcase.unity`。
- **操作：** 左键连斩；右键举盾/弹反/吞印；1～4 技能；Shift 突进；I 装备页；Esc 暂停。攻防与突进共用体力。
- **水晶机制：** 先断链，再碎晶；近战按距离、角度与遮挡结算，全碎触发失衡，重试恢复。
- **已验证：** 战斗 84 项、剧情 171 项、Windows 独立包 13 项自动化检查通过；Windows 构建 0 错误，另抽查实际窗口的 HUD 与特效。
- **本机 Windows 包：** `Builds/MainCombat108_20261106/IMMonster.exe`。Builds 不随源码推送，队友可从 main 源码构建；2026-11-04 的旧下载包不含本次剑盾操作。

[完整操作、迁移范围与构建/验证步骤](Docs/CombatUnification20261106/README.md) · [验收摘要](Docs/CombatUnification20261106/delivery-validation.json) · [修改脚本清单](Docs/CombatUnification20261106/changed-scripts.json) · [更新日志](CHANGELOG.md)

## 历史交付说明

<details>
<summary>展开原 1.0.6 说明（旧操作、编辑器版本和测试数字仅对应历史版本）</summary>

> 2026-10-29：本工程现已作为IM MONSTER团队main第一版v0.1.0的运行基线，已接入新的圆形厅美术。下文保留原程序交付说明。

# Arcane Archive 魔法图书馆 · 1.0.6

项目名称：**虚拟环境设计**。产品名称：**Arcane Archive 魔法图书馆**。

这是基于历史 1.0.5 整理的独立 Unity 工程，属于**战斗玩法修订版**。2026-10-27 按仓库最新团队协作规则，在 **`program` 分支的 `1.0.6/`** 交付；`lixiang` 保留历史归档，不在其上覆盖旧版本。当前成果供管理者验收，尚未表示正式采用。用户资料业务闭环和独立运行包仍未完成。

2026-10-27 经用户确认继续调整当前 1.0.6：裂地宽度扩大为一阶段 1.8 米、二阶段 2.2 米，预警同步拓宽；冲撞改为约 0.12～0.18 秒的高速突进，保留世界空间光带和三道消散残影；禁页改成第一次挥手召出头顶 6／8 枚环列，第二次挥手以 22 米/秒快速发射。延续旧金、紫黑、象牙纸页与禁书符印的美术基调。参数、修改清单与验收见 [禁咒特效调整](Docs/BossDesign/ArcaneEffects20261027/README.md)。

2026-10-26 的动作衔接、线状追踪和格挡恢复记录见 [动作衔接与线状裂地](Docs/BossDesign/CombatFlow20261026/README.md)。格挡仍为每秒恢复 12 点，最后一次消耗后等待 2.5 秒。

## 打开项目

- Unity：**6000.3.25f1**（Unity 6.3 LTS）。
- Unity Hub 添加本文件所在的 **`1.0.6/`**，不要添加外层 Git 仓库目录。
- 主场景：`Assets/Scenes/MilanLibrary_Magic_V6.unity`。
- 等待资源导入、Package 解析和脚本编译完成，检查 Console 后进入 Play Mode。
- Build Settings 第一项为 MilanLibrary_Magic_V6；旧 LibraryMain 仍为第二个启用场景，备份场景不作为启动入口。

首次下载应使用 Git LFS。以下命令获取 `program` 分支已提交的工程：

```powershell
git lfs install
git clone --branch program --single-branch https://github.com/yao8324/project123.git
cd project123
git lfs pull
```

不要仅复制脚本或场景；必须同时保留 Assets、所有对应 .meta、Packages 和 ProjectSettings。GitHub 源码 ZIP 是否包含 LFS 实体取决于仓库设置，推荐 Git + LFS。若模型文件内容以 `version https://git-lfs.github.com/spec/v1` 开头，它仍是指针，需要下载实体。首次导入需要可访问包源与 Git 依赖。

## 本版已实现

- **环境漫游：** 城堡图书馆、书架与桌面陈设、第一人称手部；移动、重力、接地跳跃、冲刺、蹲下和碰撞。
- **统一 UI：** Esc 菜单、探索指南、馆藏空状态、功能概述和探索入口。页面采用棕金配色、逐行分隔与悬停提示，中文使用项目内 TMP 字体资产。
- **可用设置：** 默认高画质；均衡/高画质按钮与 F8/F9 实际切换；鼠标灵敏度支持 0.25～3.00 倍调整。
- **按键换绑：** 探索指南中点击按键修改绑定，支持冲突检测、取消、恢复默认与跨运行保存。
- **魔法书交互：** 3 米中心射线检测；书、魔法阵和底座使用统一交互区域，短暂移开时保留提示，遮挡、超距和打开 UI 时清除。
- **视觉修订：** 地毯重叠闪烁修正；魔法书、地球仪展示位置与动画节奏调整；入口墙面、装饰和门洞修订。
- **自动魔法大门：** 宽幅双扇门、深色门板、黄铜与紫晶封印；靠近开启、远离关闭，门口停留保护及关闭中反向开启；关闭时搭接与压条遮住中央缝隙。
- **禁书守卫战斗：** 独立封印大厅、分段感知走位、五种攻击、二阶段、弱追踪飞弹、吞印、格挡条、暂停、胜负、重试和返回。当前主场景使用新蒙皮模型、85 根骨骼和 17 段导入动作，配套金紫符文法环、禁页拖尾、双螺旋激光、裂地金屑与冲撞光带。动作由战斗状态计时驱动，伤害与 AI 仍使用原逻辑。已移除无引用的 138 个旧 Boss/特效资源及 3 个旧生成脚本，保留在用竞技场结构与碰撞。详见 [骨骼动作与特效接入记录](Docs/BossDesign/CombatV2/README.md)。

## 试玩 Boss

1. 打开主场景并进入 Play Mode。按 **Esc → 深入探索 → 深入探索** 开始；也可以靠近魔法书按 E，在功能概述中点击“深入探索”。
2. **左键**发射秘文弹；**右键按下**吞印，**按住右键**格挡正面攻击。格挡会消耗左下方格挡条，松开后恢复；耗尽会短暂破防。每次成功吞印 +25 力量，满值后的下一发强化。原有 WASD、Shift、Space、C 保留。
3. Boss 在观察阶段选择接近、后撤或侧移路线，收招时不会自动跟随玩家转向。看到头顶禁页成环后，留意第二次挥手，提前横移或吞印；飞弹只有弱追踪。激光需持续横移或冲刺甩开，扩散圆环用跳跃躲避。裂地预警覆盖整条危险带，每段出现 0.8 秒后爆发，持续侧移躲开。冲撞预警末段锁定直线，释放后几乎瞬间到达，应在预警时侧闪；随后金紫拖尾和残影消散，保留 1.45 秒收招反击窗口。残影本身没有追加伤害。
4. Esc 暂停后可继续、设置、修改战斗按键或返回图书馆；胜利/失败后可重试或返回。

战斗使用原 Input System；不会另建玩家或 EventSystem。设计、实际差异、资源路径和验证证据见 [Boss 实现与验证](Docs/BossDesign/ImplementationAndVerification.md)。

## 默认操作

| 操作 | 默认输入 |
| --- | --- |
| 移动 / 环视 | WASD / 鼠标 |
| 冲刺 / 跳跃 / 蹲下 | Shift / Space / C |
| 打开菜单、返回上一页或关闭 UI | Esc |
| 重新锁定鼠标 | 鼠标左键（漫游状态） |
| 查看魔法书功能概述 | 瞄准可交互区域，3 米内按 E |
| 均衡 / 高画质 | F8 / F9，也可在设置页点击 |
| 修改按键 | Esc → 操作说明 → 点击右侧按键 |
| 修改灵敏度 | Esc → 设置预览 → 拖动灵敏度滑条 |

打开主 UI 时暂停玩家移动与视角，释放鼠标；关闭后恢复。Esc 始终保留安全返回能力，换绑等待期间 Esc 取消。F8/F9 与方向键备用移动绑定保留，不分配给其他操作。

按键保存到 `Application.persistentDataPath/input-bindings.json`。画质和灵敏度调整目前只在本次运行内生效；下次运行回到默认。音量功能尚未接入。

## 馆藏逻辑与功能边界

**馆藏只代表用户成功导入的文件。** 装饰书、魔法书和 DemoBookRecord 演示资产不是用户馆藏；当前未接入导入服务，馆藏显示为空。界面中的导入、检索、阅读预览不代表真实业务已经完成。

P0 仍为：**导入资料 → 生成书籍 → 归档书架 → 搜索定位 → 打开阅读**。后续通过 LibraryDatabase 管理 KnowledgeBook，用户文件与 JSON 元数据写入 `Application.persistentDataPath`，不得写入 Assets。

计划格式策略：TXT/Markdown 在 Unity 阅读，图片在 Unity 预览；PDF、Word、PPT 保存元数据并由系统默认程序打开，P0 不承诺解析 Office/PDF 正文。

“深入探索”已接入独立 Boss 战首版；它是竞赛主题的扩展玩法，不把装饰书或战斗数据计入用户馆藏。AI、RAG、多人、账号、真实资料导入/检索/阅读仍未实现。详细接入契约见 [馆藏 UI 设计](Docs/CollectionUIDesign.md)。

## 工程结构与依赖

| 路径 | 内容 |
| --- | --- |
| Assets/Scenes | 主场景、历史场景及修改前备份 |
| Assets/Scripts/Core | 统一输入绑定服务及历史 UI 输入协调器 |
| Assets/Scripts/Player | 第一人称移动、支撑识别、手部表现 |
| Assets/Scripts/Interaction | IInteractable、书籍射线交互、自动大门 |
| Assets/Scripts/Knowledge | DemoBookRecord 演示数据；正式资料数据库待实现 |
| Assets/Scripts/UI | RecoveryMenu 统一菜单、交互提示、设置与 UI 装饰 |
| Assets/Scripts/Visuals | 画质、魔法动画；Editor 下为导入、建场及验证工具 |
| Assets/Art、Assets/UI | 网格、材质、角色、界面与字体资源 |
| Packages、ProjectSettings | 包清单、锁定依赖、渲染、输入及构建配置 |
| Blender、Tools | 原生建模资料、导出和历史重建工具 |
| Docs | 设计说明、验证记录、截图及上传准备检查 |

主要依赖：URP **17.3.0**、Input System **1.20.0**、uGUI **2.0.0**、Test Framework **1.6.0**。完整依赖以 [manifest.json](Packages/manifest.json) 与 [packages-lock.json](Packages/packages-lock.json) 为准；现有 Unity MCP Git 包及历史模板包保留。当前使用 URP 3D 与现有 Input System。

大型 FBX、Blender 文件和 .mlmesh 使用本目录 [.gitattributes](.gitattributes) 中的 LFS 规则。打开主场景无需先运行 Blender 或场景重建工具。历史重建脚本不保证保留 1.0.5 后续场景手工修订，不能把它们当作常规启动步骤。

## 验证与已知限制

当前修订的验证与实机截图见 [2026-10-27 禁咒特效验收](Docs/BossDesign/ArcaneEffects20261027/README.md)。2026-10-26 的动作与裂地检查、2026-10-24 的骨骼接入检查属于各自历史基线，不代替本轮验证。未执行 standalone 构建或目标设备性能验收。

以下是**此前功能开发留下的历史验证记录**，不计入本轮 Boss 检查：

- [设置验证](Docs/UIReview/FunctionalSettingsReview.md)：按钮切换实际画质，滑条更新玩家灵敏度。
- [换绑验证](Docs/UIReview/InputRebindingReview.md)：动作绑定、冲突处理、JSON 保存/重载及恢复默认。
- [连续交互验证](Docs/UIReview/ContinuousBookInteraction.md)：31 个上下视角射线命中、0.15 秒容差、遮挡/超距/UI 清除。
- [入口验证](Docs/UIReview/GrandEntranceRedesign.md)：门开关正式 Tick 逻辑、CharacterController 通行、45/45 关闭遮挡射线，记录中 Console Error 为 0。
- [本次上传准备检查](Docs/ReleaseReadiness.md)：文档、Git 忽略、LFS 实体和 .meta 配对。

已知限制：中庭栏杆仍存在冲刺跳越风险；高精度环境与约 14.2 万三角面的 Boss 需要目标设备性能复测，Boss 暂无 LOD；尚无本版独立运行包及完整平台回归。旧版 Docs 中的测试数字、包哈希与 macOS 构建结果属于各自历史版本，不能作为 1.0.6 发布验收证据。Unity 工程的内部产品标识/版本设置也不因 Git 分支名称自动更新。

## 协作、上传与素材

- [更新日志](CHANGELOG.md)
- [协作及首次上传步骤](CONTRIBUTING.md)
- [项目约束](AGENTS.md)
- [历史 1.0.5 基线说明](https://github.com/yao8324/project123/blob/lixiang/1.0.5/README.md)
- [当前团队协作规则](../协作说明.md)
- [程序交付说明](../团队任务/程序/提交/PROG-20261027-1.0.6_提交.md)

素材包含 WhiteCastle、Stylized Library、原项目贴图与校标等第三方内容；其授权仍按原始许可处理。本仓库没有为这些素材授予新的开源许可，公开仓库或二次分发前需核对资源授权。Poly Haven 木材贴图的 CC0 说明及其他出处见 [Texture_Credits.txt](Blender/MilanLibrary_Magic_V6/Texture_Credits.txt)。


## 2026-11-04 完整整合包下载

[GitHub Release 下载页](https://github.com/yao8324/project123/releases/tag/full-2026-11-04)

完整包包含Mac试玩程序、Unity完整工程、制作资源，以及本次剧情交互/环境声和三水晶斗场新增内容。需要同时下载 `.zip.001` 和 `.zip.002`，放在同一目录，用7-Zip或Keka打开第一卷解压。当前可执行程序为Mac版，Windows可从Unity工程构建。

工程入口：`Assets/Scenes/IMMonster_FullDelivery.unity`。玩法说明与验证见 `Docs/FullDelivery20261104/README.md`。

</details>
