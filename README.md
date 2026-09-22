# Arcane Archive 魔法图书馆

“虚拟环境设计”课程项目：一个可第一人称漫游的魔法图书馆个人知识库。用户将在图书馆中导入资料、将资料映射为书籍、归档至书架，并通过检索台找到和阅读内容。

## 当前 Demo

- Unity 版本：`6000.3.24f1`（Unity 6.3 LTS）
- 渲染管线：URP 17.3.0
- 启动场景：`Assets/Scenes/LibraryMain.unity`
- 已完成：第一人称移动、视角、冲刺、跳跃、基础世界交互、UI 输入状态管理，以及 WhiteCastle + Stylized Library 的魔法图书馆环境。
- 环境包括：南侧入口、四周石质城堡墙、高窗、火盆、中央阅读区、两侧书架、上传区、检索台和北侧壁炉。

## 打开项目

1. 使用 Unity Hub 添加本仓库根目录。
2. 使用 Unity `6000.3.24f1` 打开项目。
3. 打开 `Assets/Scenes/LibraryMain.unity`。
4. 点击 Play 运行。

## 操作方式

| 操作 | 按键 |
| --- | --- |
| 移动 | WASD |
| 视角 | 鼠标 |
| 冲刺 | Shift |
| 跳跃 | Space |
| 世界交互 | E |
| 打开/关闭最小测试面板 | Esc |
| 解锁鼠标 | Esc |
| 重新锁定鼠标 | 鼠标左键 |

## 当前场景布局

- 南侧入口与出生点：`(0, 0, -10)`，面朝北。
- 上传区：左前方，约 `(-7, 0, -4)`。
- 课程资料与个人资料书架：西侧，约 `X = -11.5`。
- 代码与参考资料书架：东侧，约 `X = 11.5`。
- 中央长桌和地毯：约 `(0, 0, -0.5)`。
- 检索台：北侧中央，约 `(0, 0, 7)`。
- 北侧壁炉、高窗和石墙位于检索台后方。

## 目录说明

```text
Assets/
├── Scenes/             # Unity 场景
├── Scripts/            # Core、Player、Interaction、Knowledge、UI、Visuals 模块
├── Prefabs/            # 项目自建 Prefab
├── ScriptableObjects/  # 数据资产
├── Art/                # 项目美术资源
├── _WhiteCastle/       # 城堡建筑素材
└── Stylized_Library/   # 图书馆室内素材（场景统一使用 Prefabs/URP）

Docs/                   # 策划、技术设计、阶段记录与截图
AGENTS.md               # 协作约束
```

## 开发约定

- 所有新场景、Prefab、材质、脚本和文档均保存在本 Unity 项目内。
- 书籍信息须数据驱动，不能把书名、关键词、文件路径或书架位置硬编码到场景物体。
- 用户导入文件写入 `Application.persistentDataPath`，不能写入 `Assets`。
- 新的 Stylized Library 实例使用 `Assets/Stylized_Library/Prefabs/URP/`。
- 不删除现有 Package、场景或 ProjectSettings 文件，除非先确认。
- 修改 C# 后须等待 Unity 编译完成并检查 Console Error。

## GitHub 协作

每次开始前先同步远端：

```bash
git pull --rebase origin main
```

完成修改后，检查、提交并推送：

```bash
git status
git add Assets Docs README.md AGENTS.md .gitignore
git commit -m "feat: describe your change"
git push origin main
```

提交信息使用 Conventional Commits，例如：

```text
feat: add upload station layout
fix: prevent player from jumping in midair
docs: update environment setup notes
chore: update gitignore
```

Unity 项目不提交 `Library`、`Temp`、`Logs`、`Obj`、`Build`、`Builds` 或 `UserSettings`；这些目录已由 `.gitignore` 忽略。
