# 版本归档与 Unity 项目架构

## 分支结构

```text
lixiang/
├── README.md          # 版本索引与改动摘要
├── AGENTS.md          # 版本上传规则
├── ARCHITECTURE.md    # 归档结构与项目逻辑
├── 1.0.1/             # 初始完整 Unity 工程
├── 1.0.2/             # 完整工程与 White Castle 挖空样板
└── 1.0.3/             # 新版图书馆建模，独立 Unity 6000.3.25f1 工程
    ├── Assets/        # V6 场景、原始网格、材质、Visuals 脚本及旧版资源
    ├── Packages/
    ├── ProjectSettings/
    ├── Docs/          # 项目设计、Unity 实景和验证报告
    ├── Blender/       # V6 原生模型、重建快照、模块、贴图及实景
    ├── Tools/         # 模型导出与历史重建工具
    ├── README.md
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
