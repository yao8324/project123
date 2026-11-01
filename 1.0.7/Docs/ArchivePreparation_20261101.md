# 1.0.7 源码归档检查

日期：2026-11-01。类型：战斗玩法修订版的独立源码快照；上一版本：1.0.6。

## 来源与范围

- 指定源目录：`Unity项目/githubclone/project123-submit-program-20261027/1.0.6/`。
- 目标目录：`Unity项目/githubclone/project123/1.0.7/`。
- 目标分支：用户明确选择的 `lixiang`；这是本次历史归档，日常团队程序交付规则仍使用 program。
- 来源选择：源仓库 Git 已跟踪或未被忽略的现有工程文件。完整保留 Assets、Packages、ProjectSettings、Docs、Blender、Tools、.meta、包清单、锁文件和工程规则；不包含软链接、子模块、缓存、运行包或本地回滚 ZIP。
- 本版没有混入另一个 `project123/1.0.6/` 工作区后续的近战、体力或闪避成果，也不声明与团队 main v0.1.0 相同。

## 本次实际检查

| 项目 | 结果 |
| --- | --- |
| 原始复制文件 | 4,228 个，共 2,229,832,536 字节 |
| 复制内容 SHA256 | 文档调整前逐文件对比指定源目录，4,228 个一致，差异 0 |
| 源文件可用性 | 候选文件均存在；未发现软链接 |
| 核心工程文件 | Assets、Packages/manifest.json、packages-lock.json、ProjectSettings/ProjectVersion.txt、EditorBuildSettings.asset 和启动场景均存在 |
| Unity / 场景 | 6000.3.25f1；Assets/Scenes/MilanLibrary_Magic_V6.unity |
| Assets 元数据 | 未发现缺少配套 .meta 或孤立 .meta |
| LFS 实体 | 未发现文件内容仍为 Git LFS 指针 |
| LFS 属性 | 919 个工程文件匹配 filter=lfs；5 个超过 100 MiB 的文件全部匹配 |
| Git 忽略 | 原始复制文件在目标路径下没有被忽略；Unity 缓存与本地备份未复制 |
| 远端基线 | 只读查询 GitHub lixiang 为 `6c4127678e534be70aa78968f672d5e5ac2d94b9`，与本地分支 HEAD 一致；查询时该提交没有 1.0.7 |
| 代码及配置 | 不修改运行脚本、场景、Prefab、资源 GUID、Packages 或 ProjectSettings |

逐文件 SHA256 清单保存在本次整理的外部工作记录中，未加入工程，避免把机器路径和辅助产物提交到仓库。

## 文档调整

复制后更新本版 `README.md`、`CONTRIBUTING.md`，在 `CHANGELOG.md` 前补充本次归档记录，并新增本报告。其余源文件保留原样。根目录 `README.md` 添加 1.0.7 索引和当前入口，根 `ARCHITECTURE.md` 添加本版来源与架构边界，保留原有历史内容。

源工程历史报告保留原日期与版本路径。其中 2026-10-27 的 36 项技能检查、25 项动作回归和资源检查仅是原工程历史证据。本轮没有重新运行 Unity 编译、Play Mode、自动技能检查、独立构建或性能测试，不能据此声明新目录已通过运行验收。

## 上传范围

在 GitHub Desktop 中确认外层仓库为 `project123`、分支为 `lixiang`，取消全选后仅选择：

- `1.0.7/` 全部文件；
- 根目录 `README.md`；
- 根目录 `ARCHITECTURE.md`。

工作区已有其他旧版本与根 Assets 的未提交修改，本次保留，不纳入 1.0.7 提交。推荐提交信息为 `feat: archive 1.0.7 Unity project snapshot`。

点击 Commit 后还需 Push origin 并等待 LFS 上传结束。推送前需确认远端变化，冲突正常整合，不强推。源码整理完成不等于已提交、已上传、已被管理者采用或已发布 GitHub Release。
