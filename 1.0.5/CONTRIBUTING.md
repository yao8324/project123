# 1.0.5 协作与上传说明

## 目录与分支

开发分支为 **dev/1.0.5**。Git 仓库根目录包含版本索引及多个版本；Unity 根目录是其中的 **1.0.5/**。业务代码、场景和资源只改本版本，旧版本保持原样。先阅读 [项目规则](AGENTS.md) 与 [仓库规则](../AGENTS.md)。

仓库规则中的 lixiang 流程用于版本归档；本次是推送开发分支，不切换到 lixiang 或 main。后续归档应另行确认，不能把开发分支上传当作稳定版发布。

## 提交约定

- 使用 Conventional Commits：feat、fix、docs、chore；例如 `feat: prepare 1.0.5 library interaction and UI demo`。
- 资源移动/新增时必须一起提交 .meta，保留 Packages/manifest.json、packages-lock.json 与 ProjectSettings。
- Library、Temp、Logs、Obj、Build、Builds、UserSettings 和 IDE 缓存不提交；生成包单独交付。
- 修改 C# 后等待 Unity 编译，检查 Console Error，记录实际验证结果。
- 不覆盖既有工作，不使用强制推送；推送冲突时先检查远端差异。

## 首次上传当前工作区

先在 Unity 保存场景和资源。**以下命令在 Git 仓库根目录执行，不是在 1.0.5 内执行。** 先核对分支及远端：

```powershell
git branch --show-current
git remote -v
git status --short
git lfs install
```

应为 dev/1.0.5，origin 应指向 yao8324/project123。然后仅暂存本版及版本说明：

```powershell
git add -- README.md ARCHITECTURE.md 1.0.5
git diff --cached --stat
git diff --cached --check
git diff --cached --name-only
git lfs status
```

确认暂存区没有旧版本修改、缓存或本机凭据。检查 LFS 列表包含大资源；不要跳过 LFS 上传钩子。首次提交会包含完整独立工程，上传时间和远端 LFS 配额取决于资源是否已存在于服务器。

```powershell
git commit -m "feat: prepare 1.0.5 library interaction and UI demo"
git push -u origin dev/1.0.5
```

推送成功后：

```powershell
git status --short --branch
git rev-parse HEAD
git ls-remote --heads origin dev/1.0.5
```

本地 HEAD 应与远端分支提交一致。若远端已存在不同提交导致拒绝推送，停止并核对差异，不使用 --force。推送源码不自动生成可执行包，也不自动发布 GitHub Release。

## 之后更新

在同一分支保存修改、按路径暂存、查看差异、提交，再执行 `git push`。需要整合队友修改时先确保本地工作已保存，查看远端差异，再采用团队约定的合并方式。

使用 GitHub Desktop 时，添加**外层 Git 仓库**，切换 dev/1.0.5，查看 Changes → 填写 Conventional Commit 信息 → Commit → Publish branch（首次）或 Push origin（后续）。若客户端不能添加此 worktree，使用上面的终端步骤；不要误添加另一份工程或直接上传文件夹到网页。

## 交接验证

1. 用 Git + LFS 在另一位置克隆 dev/1.0.5。
2. Unity Hub 打开 1.0.5，确认主场景、模型和材质正常，无缺失脚本。
3. 测试移动、接地跳跃、Esc 开关与恢复输入。
4. 测试设置实际生效、换绑保存/恢复默认、书籍提示与超距隐藏。
5. 测试大门从两侧靠近、离开关闭和门口停留；检查 Console。
6. 若要交付可执行包，另行完成本版构建、目标设备性能和真人输入验证，填写对应记录。
