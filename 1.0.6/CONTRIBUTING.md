# 1.0.6 程序交付与协作说明

以仓库 main 的 [README](https://github.com/yao8324/project123/blob/main/README.md) 和 [协作说明](https://github.com/yao8324/project123/blob/main/协作说明.md) 为当前流程。程序、战斗与技术集成在 program 开发并交付；管理者将指定任务收取到 codex/team-submit，决定是否进入 main。lixiang 是历史归档，本轮不修改其旧版本。

## 工程与提交范围

- 外层目录是 Git 仓库，Unity Hub 打开其中的 1.0.6/，Unity 版本为 6000.3.25f1。
- Assets/、.meta、Packages/、ProjectSettings/、Docs/、Blender/、Tools/ 一并保留；大资源沿用本版 .gitattributes 的 LFS 规则。
- 不提交 Library、Temp、Logs、Obj、Build、Builds、UserSettings、IDE 缓存、凭据或本地回滚 ZIP。
- 先阅读 [项目约束](AGENTS.md)。新增脚本须等待 Unity 编译，并记录实际运行验证；没有运行的项目要明确注明。
- 源码放工程路径；提交说明和证据链接放 团队任务/程序/提交/，不在那里复制整套工程。

## 交付步骤

1. 保存本地工作，先同步最新 main，解决冲突。历史归档迁移使用隔离工作区，保留原工作区。
2. 完成当前用户或管理者指定工作，检查资源引用、.meta 配对、依赖、忽略项及 LFS。
3. 复制程序提交模板，说明完成内容、验证步骤和实际结果、已知问题；更新程序当前任务为“待验收”。管理者尚未派发正式任务编号时，明确注明交付标识的来源，不虚构已通过的验收。
4. 仅暂存本任务文件、对应文档及程序任务目录；检查 git diff --cached --name-only、git diff --cached --check、git lfs status。使用 Conventional Commits。
5. 再次核对远端 program，正常快进推送。冲突先整合，禁止强推，不直接推送 main、codex/team-submit、lixiang 或其他成员分支。
6. 确认远端提交与本地一致，检查 program 分支下的 1.0.6/ 和交付说明可见。提交完成不等于管理者验收通过，也不代表已发布运行包。

GitHub Desktop 添加外层仓库，选择 program，检查 Changes 后 Commit，再 Push origin。Unity Hub 添加其中的 1.0.6/。首次克隆后运行 git lfs pull；不能用未下载实体的 LFS 指针替代模型。

## 当前复查入口

打开 Assets/Scenes/MilanLibrary_Magic_V6.unity，检查 Console 后进入 Play Mode；验证移动、菜单、返回输入及 Boss 战。最新三项技能的参数、自动检查与截图见 [禁咒特效验收](Docs/BossDesign/ArcaneEffects20261027/README.md)。独立运行包和目标设备性能需要另行验收，历史记录不能代替本次结果。
