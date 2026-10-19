# 版本归档与 Unity 项目架构

## 1.0.5 开发中：环境交互与 UI

基于 1.0.4 的独立工程，位于 dev/1.0.5 分支的 1.0.5/；旧版本只读。主场景为 MilanLibrary_Magic_V6，LibraryMain 仍在 Build Settings 第二项启用。

- **Core / Player：** LibraryInputBindings 管理运行时克隆的 Input Actions、按键换绑、冲突检查和 persistentDataPath/input-bindings.json；FirstPersonController 读取同一套输入，并提供灵敏度和输入禁用接口。
- **UI：** RecoveryMenu 统一处理 Esc、主面板、世界提示及玩家输入状态；设置页实际调用 MilanQualityOptions 和玩家灵敏度接口。默认高画质，画质和灵敏度仅本次运行生效；按键覆盖跨运行保存。旧 UIInputStateManager 不承担当前主场景第二套菜单输入。
- **Interaction / Knowledge：** BookPreviewInteractor 执行 3 米中心射线、0.15 秒空射线容差和遮挡/超距清除；DemoBookTarget 通过 IInteractable 呈现 DemoBookRecord。统一交互体覆盖魔法书、底座和法阵。DemoBookRecord 是演示数据，不是用户馆藏数据库。
- **入口环境：** AutomaticEntranceDoor 管理双扇自动门、接近/离开阈值及通行保护；GrandEntranceBuilder 是编辑器生成工具，最终高度适配、封口与灯光另有场景修订。主场景及 Assets/Art/Models/GrandEntrance 保存成品；不能通过重跑旧建场流程假定还原全部最新调整。
- **Visuals：** 保留 URP 场景、魔法变换动画与第一人称手部；本版修订地毯重叠、展示位置、动画节奏和入口结构。

**未实现：** LibraryDatabase、KnowledgeBook 正式存储、UploadStation、SearchStation 与真实阅读服务；用户馆藏保持空状态。深入探索/Boss 战只有 UI 入口，AI/RAG、多人同样尚未实现。P0 仍优先资料闭环。

当前说明见 [1.0.5 README](1.0.5/README.md)、[馆藏契约](1.0.5/Docs/CollectionUIDesign.md)、[更新日志](1.0.5/CHANGELOG.md)。UIReview 内为各次功能验证记录，较早原型说明不覆盖后续修订；本次上传准备未重跑 Unity 功能回归。

## 分支结构

```text
repository/           # 当前开发分支 dev/1.0.5；归档分支 lixiang
├── README.md          # 版本索引与改动摘要
├── AGENTS.md          # 版本上传规则
├── ARCHITECTURE.md    # 归档结构与项目逻辑
├── 1.0.1/             # 初始完整 Unity 工程
├── 1.0.2/             # 完整工程与 White Castle 挖空样板
├── 1.0.3/             # 新版图书馆建模（保留原样）
├── 1.0.4/             # GlassFloor 历史基线，保留原样
└── 1.0.5/             # 当前独立 Unity 6000.3.25f1 开发工程
    ├── Assets/        # V6 场景、原始网格、材质、Visuals 脚本及旧版资源
    ├── Packages/
    ├── ProjectSettings/
    ├── Docs/          # 项目设计、Unity 实景和验证报告
    ├── Blender/       # V6 原生模型、重建快照、模块、贴图及实景
    ├── Tools/         # 模型导出与历史重建工具
    ├── README.md
    ├── CHANGELOG.md
    ├── CONTRIBUTING.md
    └── AGENTS.md
```

每个版本的 `Assets`、`Packages` 和 `ProjectSettings` 只服务于同一目录下的项目。Unity 的资源引用依靠 `.meta` GUID，复制版本时必须一并保留对应 `.meta`。版本之间不使用共享资源目录；在 Unity Hub 中选择具体版本目录作为工程根目录。`Library`、`Temp` 和其他生成目录由 Unity 在本机重新创建。

## 1.0.1 已实现的逻辑

- **场景与渲染：** `Assets/Scenes/LibraryMain.unity` 是启用的启动场景；项目使用 URP 3D 配置，包含魔法图书馆环境资源及演示场景。
- **玩家与输入：** `FirstPersonController` 读取 `InputSystem_Actions.inputactions` 的 `Player` 动作，控制第一人称移动、视角、跳跃、Shift 冲刺和 C 蹲下；落地判定基于碰撞体射线检测。
- **交互：** `PlayerInteractor` 从相机中心寻找实现 `IInteractable` 的对象，按 E 触发；`InteractionPromptUI` 显示交互提示。`InteractableTestObject` 目前用于验证交互链路。
- **界面输入：** `UIInputStateManager` 在打开界面时暂停玩家操作并释放鼠标，关闭时恢复。`Previous`/`Next` 已绑定鼠标滚轮，但尚未连接业务功能。

## 规划中的资料闭环

项目设计目标是“导入资料 → 生成书籍 → 归档书架 → 检索定位 → 打开阅读”。计划由 `UploadStation` 将文件写入 `Application.persistentDataPath`，`LibraryDatabase` 管理 `KnowledgeBook` 元数据，`BookShelf` 与 `BookView` 呈现书籍，`SearchStation` 定位并高亮，`ReadingPanel` 显示或打开内容。这些模块在 `1.0.1` 中尚未实现，不能把设计文档当作已有功能。字段、交互和验收标准见 [1.0.1 技术设计](1.0.1/Docs/TechnicalDesign.md) 与 [实施路线图](1.0.1/Docs/ImplementationRoadmap.md)。

后续版本更新此文件时，应以该版实际代码和场景为依据调整“已实现”部分，并在对应版本 README 记录架构变化。

## 1.0.2 已实现与待接入内容

- **已实现：** 独立复制 `1.0.1` Unity 工程；新增 White Castle 中央大厅挖空的 Blender 工作文件、结构预览和重建脚本。原始 FBX 保留在同版 `Assets/_WhiteCastle/Meshes/`。
- **规划中：** 清理伸入大厅的装饰网格，完成室内材质、门框、楼梯和碰撞，并将最终模型导出后接入 Unity 场景。`1.0.2` 的 Unity 场景和运行逻辑仍与 `1.0.1` 相同。


## 1.0.3 已实现：新版图书馆建模

- **独立归档：** 基于远端 `1.0.2`，保留其第一人称控制器和输入绑定；新增用户确认的 V6 美术资源及依赖，不覆盖旧版本目录。
- **场景与资产：** `Assets/Scenes/MilanLibrary_Magic_V6.unity` 成为首个启动场景，原 LibraryMain 保留。V6 包括哥特式完整外观、木作室内、上传与检索工作区、书架和金边 Maynooth 校标；889 个网格渲染对象、67 种材质、97 盏灯光、47 组变换动画。原生 Blender 模型与重建资料位于 `Blender/MilanLibrary_Magic_V6/`。
- **导入链路：** `MilanMeshImporter` 将 `.mlmesh` 数据导入 Unity Mesh；`MilanV6SceneBuilder` 配置场景、URP 材质、灯光和截图；`MilanMagicMotion` 在运行时播放采样的变换动画。网格和原生 Blender 文件通过版本内 Git LFS 规则管理。
- **验证：** `MilanArchiveValidation` 在独立工程内实际导入、编译、打开场景和生成截图；1,025 项依赖无缺失，缺失脚本、材质、网格均为 0，动画和上一版蹲下输入检查通过。详见 [1.0.3 实景与验证](1.0.3/Docs/MilanV6Unity/Review.html)。

## 1.0.3 仍需实现与验证

- 全精度约 1,577 万三角面，仍需 LOD、遮挡和目标设备性能测试；已通过的场景验证不表示设备性能达标。
- Unity URP 实时灯光与 Blender Cycles 的间接光不同；本次迁移了变换动画，灯光强度和材质微闪驱动仍为静态基准值。
- 上传、检索、书籍归档和阅读仍是规划中的业务模块。本版工作区、魔法书与书架属于美术展示，没有实现资料业务闭环。

## 1.0.4 已实现：GlassFloor 候选修订

使用上传者提供并已本机验证的独立完整工程；旧版本目录不改写。当前场景仍为 MilanLibrary_Magic_V6，新增入口二楼144㎡连续木楼板并保留中央挑空，修复彩窗发光纹样链路。此前窗洞与复合桌台碰撞、中央门通行/四门关闭、连续石质前坡、Esc 点击恢复、右下手部与六动画均纳入本版。默认 Balanced 720p，F8/F9 切换，E 业务交互暂停。

### 内部结构与职责

- `Core`：UIInputStateManager 协调玩家与菜单输入。
- `Player`：FirstPersonController 控制移动/支撑/重力/蹲跳；FirstPersonViewModel 管理手部状态，安全传送与复位由恢复流程调用。
- `Interaction`：保留交互接口与历史检测实现；V6 当前不启用 E 门或书籍业务。
- `Knowledge`：预留数据模块，未实现资料闭环。
- `UI`：RecoveryMenu 提供 Esc 释放鼠标、按钮回出生点及继续。
- `Visuals`：变换动画和画质；Editor 下为网格导入、建场、刷新、验证；AcceptanceTestOnly 保存自动虚拟输入/截图/诊断，正常构建不启用诊断。
- `Assets/Art/MilanLibraryV6` 与 `Assets/Art/Characters/FirstPersonMage` 分别保存建筑导出资产与第一人称手部。
- `Packages` 与 `ProjectSettings` 完整保留；依赖 URP17.3.0、InputSystem1.20.0、uGUI2.0.0，以及原有 Git 包和锁定解析。
- `Blender/MilanLibrary_Magic_V6` 保留 Assembly → CloudRepair → PlaytestReview → GlassFloorReview 原生依赖链；`Tools/MilanV6` 提供修复、碰撞角色约定、导出和手部重建。Texture_Credits 明确外部贴图与原项目素材来源。

完整目录表、操作和命令见 [本版 README](1.0.4/README.md)。每个版本的运行资源不依赖仓库其他版本目录。

### 验证与未完成项

本輪真实 Controller/InputSystem 虚拟输入回归107/107，新增楼面60/61，14,400支撑射线无缺失；最终66张离屏截图、64次材质耗时采样，最终轮次Console零错误。macOS Universal构建0错误/1个Cloud符号上传警告，签名与ZIP CRC已核验。发布整理3272个代码/资源/设置/工具文件与验收工程逐一SHA256相同，未再做游戏修改。详细证据见 [本轮验证](1.0.4/Docs/GlassFloorReview_20261007/VERIFICATION.md)。

**未完成：**约1.23m中庭护栏可冲刺跳越，因此不是全部防跌落通过。部分窗内侧视角被书架遮挡；玻璃为实时近似，不含物理折射或光谱透射。未完成本轮真人输入与standalone FPS验收。资料导入、书籍生成、检索和阅读仍是规划项，不将环境展示当业务功能。
