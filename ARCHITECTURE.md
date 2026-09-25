# 版本归档与 Unity 项目架构

## 分支结构

```text
lixiang/
├── README.md          # 所有版本的索引与改动摘要
├── AGENTS.md          # 成员和 AI 的版本上传规则
├── ARCHITECTURE.md    # 归档结构与项目逻辑
└── 1.0.1/             # 可由 Unity Hub 独立打开的完整工程
    ├── Assets/
    ├── Packages/
    ├── ProjectSettings/
    ├── Docs/
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
