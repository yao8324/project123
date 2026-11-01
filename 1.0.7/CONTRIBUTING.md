# 1.0.7 源码归档与上传说明

本次按用户明确指定，从 `project123-submit-program-20261027/1.0.6/` 复制独立工程到 `project123/1.0.7/`，并归档到 **lixiang**。这是本次归档位置的明确选择；团队日常程序交付仍使用 program。

Unity Hub 打开 `1.0.7/`，使用 Unity 6000.3.25f1；场景为 `Assets/Scenes/MilanLibrary_Magic_V6.unity`。保留 Assets 与完整 `.meta`、Packages、ProjectSettings、Docs、Blender、Tools 及本版规则。不要提交 Library、Temp、Logs、Obj、Build、Builds、UserSettings、IDE 缓存、凭据或本地回滚 ZIP。大型资源使用现有 `.gitattributes` 的 LFS 规则。

## GitHub Desktop

1. `File → Add local repository` 添加外层 `project123`，不要添加 `1.0.7/`。
2. 确认 `Current branch` 为 **lixiang**。先 `Fetch origin` 核对远端；如需拉取，先妥善保存所有本地工作再同步，不能丢弃现有修改或强制覆盖。
3. 在 Changes 中取消全选，只选择 `1.0.7/`、根 `README.md` 与根 `ARCHITECTURE.md`；本工作区还有其他原有修改，留待各自任务处理。
4. Summary 填写 `feat: archive 1.0.7 Unity project snapshot`，点击 **Commit to lixiang**。
5. 点击 **Push origin**，等待 Git 和 LFS 上传完成。若远端有新提交，先整合冲突后普通推送，不强推。
6. 在 GitHub 的 lixiang 分支检查 1.0.7 和根版本索引。Commit 只保存到本机；Push 才上传。

本次检查见 [归档检查](Docs/ArchivePreparation_20261101.md)。历史测试不代表本次重跑；本次未创建标签、GitHub Release 或独立运行包。用户指定的源目录与其他版本目录没有被本次复制操作修改。
