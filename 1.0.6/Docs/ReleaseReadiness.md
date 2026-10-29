# 1.0.5 上传准备检查

检查日期：2026-10-15。范围为本地文件、文档及 Git 配置；没有提交、推送、创建标签或发布运行包，没有重新运行 Unity 验收。

## 检查结果

| 项目 | 结果 |
| --- | --- |
| 分支 | dev/1.0.5；检查时尚未设置上游 |
| 来源 | 1.0.4，提交 c3a80b9 |
| Unity 工程 | 1.0.5/，包含 Assets、Packages、ProjectSettings |
| Unity 版本 | 6000.3.25f1 |
| 启动场景 | MilanLibrary_Magic_V6 为 Build Settings 首项；LibraryMain 仍为第二个启用项 |
| 包依赖 | manifest 与 packages-lock 存在；保留原有包配置 |
| 文档 | 更新本版 README、根 README 和 ARCHITECTURE；新增 CHANGELOG、CONTRIBUTING 和本报告 |
| Git 忽略 | Library、Temp、Logs、Obj、Build、Builds、UserSettings、IDE 缓存规则生效；候选文件未发现这些缓存目录 |
| Unity 元数据 | Assets 文件及目录检查未发现缺少 .meta 或孤立 .meta |
| 大文件 | 发现 5 个超过 100 MiB 的文件，均匹配 Git LFS |
| LFS 实体 | 913 个候选文件匹配 LFS，总计 992,887,045 字节；检查文件头没有残留指针，也没有空 LFS 文件 |
| 旧版本 | 当前 Git 已跟踪修改仅为根 README 和 ARCHITECTURE；未修改 1.0.1～1.0.4 |
| 本地状态 | 1.0.5 整个目录仍未跟踪，需要 git add、commit 后才能上传 |

## 本次文档修正

移除本版 README 中大段混入的 1.0.4 操作和构建说明，改用历史链接；修正“E 交互暂停”“设置仅预览”“示例书即馆藏”等过时表述。补充真实设置、换绑、连续交互、自动大门，以及尚未接入的资料业务和 Boss 战边界。

.gitignore、.gitattributes、AGENTS.md 已存在，本次保留；没有为第三方素材添加未经授权的开源许可证。

## 验证边界

LFS 文件头检查不等于全部文件的重新 SHA256 校验，也不证明远端额度充足或推送成功。初始化时的实体哈希校验记录见 VersionInitialization.md；本次只确认当前候选资源是非空实体并具有 LFS 属性。

各功能此前的 Unity 验证记录位于 UIReview。记录中的 Console 零错误属于当时验证结果；本次仅修改文档，不据此声明今天已重新跑过所有运行测试。旧版构建包、性能数字和历史自动测试结果不能作为 1.0.5 独立构建验收。

## 上传前后还要执行

按照 [协作说明](../CONTRIBUTING.md) 暂存并检查文件，再提交和推送 dev/1.0.5。推送后核对远端 HEAD，并用干净克隆验证 LFS 下载和 Unity 导入。发布可执行包之前另做构建与目标设备回归。
