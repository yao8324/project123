# 虚拟环境设计 协作规则

## 项目边界

- 当前 Unity 项目根目录是包含 `Assets`、`Packages` 和 `ProjectSettings` 的目录。
- 所有代码、场景、Prefab、材质和项目文档必须保存在当前 Unity 项目内。
- 策划案位于项目上级目录，只可读取；不得将业务文件写入上级目录。
- 未经确认，不得删除现有 Package、场景或 `ProjectSettings` 中的文件。

## 代码与数据组织

- C# 文件按以下模块组织：`Core`、`Player`、`Interaction`、`Knowledge`、`UI`、`Visuals`。
- 书籍信息必须由数据驱动。禁止把书名、关键词、文件路径和书架位置硬编码在场景物体上。
- 用户导入的资料必须写入 `Application.persistentDataPath`，禁止写入 `Assets`。

## P0 优先级

- P0 必须优先保证“导入 → 书籍 → 书架 → 检索 → 阅读”闭环。
- 第一人称移动、图书馆大厅、书架、文件导入、文件转书籍、书籍信息面板、标题和关键词检索，以及书籍与书架高亮均属于 P0。

## 修改与验证

- 每次修改 C# 脚本后，必须等待 Unity 编译完成并检查 Unity Console Error。
- 每次脚本修改的交付说明必须列出修改的文件，并给出验证步骤。

## Git 约定

- Git 提交信息使用 Conventional Commits，例如 `feat:`、`fix:`、`docs:`、`chore:`。
