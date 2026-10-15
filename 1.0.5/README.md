# Arcane Archive 魔法图书馆 · 1.0.5

项目名称：**虚拟环境设计**。产品名称：**Arcane Archive 魔法图书馆**。

这是基于 1.0.4（`c3a80b9`）开发的独立 Unity 工程，分支为 `dev/1.0.5`。当前版本属于**开发中修订版**：可第一人称漫游的魔法图书馆环境、可操作的设置与交互 UI 原型。尚未实现用户资料业务闭环，也未发布本版独立运行包。

## 打开项目

- Unity：**6000.3.25f1**（Unity 6.3 LTS）。
- Unity Hub 添加本文件所在的 **`1.0.5/`**，不要添加外层 Git 仓库目录。
- 主场景：`Assets/Scenes/MilanLibrary_Magic_V6.unity`。
- 等待资源导入、Package 解析和脚本编译完成，检查 Console 后进入 Play Mode。
- Build Settings 第一项为 MilanLibrary_Magic_V6；旧 LibraryMain 仍为第二个启用场景，备份场景不作为启动入口。

首次下载应使用 Git LFS。以下命令适用于 `dev/1.0.5` 已推送到远端之后：

```powershell
git lfs install
git clone --branch dev/1.0.5 --single-branch https://github.com/yao8324/project123.git
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

“深入探索”是未来魔法世界与 Boss 战的界面入口，当前没有战斗逻辑。AI、RAG、多人、账号、真实资料导入/检索/阅读均未实现。详细接入契约见 [馆藏 UI 设计](Docs/CollectionUIDesign.md)。

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

以下是**此前功能开发留下的验证记录**，本次文档整理未重新执行 Unity 编译、真人输入测试或独立构建：

- [设置验证](Docs/UIReview/FunctionalSettingsReview.md)：按钮切换实际画质，滑条更新玩家灵敏度。
- [换绑验证](Docs/UIReview/InputRebindingReview.md)：动作绑定、冲突处理、JSON 保存/重载及恢复默认。
- [连续交互验证](Docs/UIReview/ContinuousBookInteraction.md)：31 个上下视角射线命中、0.15 秒容差、遮挡/超距/UI 清除。
- [入口验证](Docs/UIReview/GrandEntranceRedesign.md)：门开关正式 Tick 逻辑、CharacterController 通行、45/45 关闭遮挡射线，记录中 Console Error 为 0。
- [本次上传准备检查](Docs/ReleaseReadiness.md)：文档、Git 忽略、LFS 实体和 .meta 配对。

已知限制：中庭栏杆仍存在冲刺跳越风险；高精度环境需要目标设备性能复测；尚无本版独立运行包及完整平台回归。旧版 Docs 中的测试数字、包哈希与 macOS 构建结果属于各自历史版本，不能作为 1.0.5 发布验收证据。Unity 工程的内部产品标识/版本设置也不因 Git 分支名称自动更新。

## 协作、上传与素材

- [更新日志](CHANGELOG.md)
- [协作及首次上传步骤](CONTRIBUTING.md)
- [项目约束](AGENTS.md)
- [上一版基线说明](../1.0.4/README.md)

素材包含 WhiteCastle、Stylized Library、原项目贴图与校标等第三方内容；其授权仍按原始许可处理。本仓库没有为这些素材授予新的开源许可，公开仓库或二次分发前需核对资源授权。Poly Haven 木材贴图的 CC0 说明及其他出处见 [Texture_Credits.txt](Blender/MilanLibrary_Magic_V6/Texture_Credits.txt)。
