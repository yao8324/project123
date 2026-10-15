# 文字清晰度修正与 Codex 快照检查

2026-10-13，Unity 6000.3.25f1，1.0.5。

## 文字修正

- RecoveryMenu.cs 中全部 uGUI Text 改为 TextMeshProUGUI；最小字号由 15 提升至 18，Canvas pixelPerfect 开启。
- 从现有 uGUI Package 导入自带 TMP Essential Resources，不安装或升级 Package。
- 新建 Assets/UI/Fonts/ArchiveChineseSDF.asset：微软雅黑 90 点采样、370 个当前文案字形、4 张 SDF 图集，Static 模式。图集与材质作为字体子资产保存，当前文案运行时不依赖系统字体文件。后续新增汉字文案时需扩充此字体资产，不能把当前字形覆盖当作完整中文字体库。
- 主场景 RecoveryMenu 引用该字体资产；不替换场景模型或建筑材质。
- 阅读页同步迁移 TMP 的对齐、换行与 GetPreferredValues 高度计算，保留 ScrollRect。
- MilanQualityOptions.cs 原 OnGUI 位图提示改为 RecoveryMenu 的 SDF 提示，F8/F9 的画质功能保持原样。
- Game View 从 Free Aspect 调整为已有 Full HD 1920×1080，并尝试将 ZoomArea scale 恢复 1×。这只影响编辑器预览，不提高 Balanced 场景渲染比例，不改游戏全屏/窗口默认配置。画面放大超过原始像素时仍会发生图像插值，不能保证任意预览缩放都同样锐利。

## 验证

- Unity 完成编译，最终 Console Error=0、Warning=0。
- 菜单活跃的 13 个文字组件全部为 SDF，活跃 legacy Text=0。
- Menu/Help/Settings/Gallery/Book/Reading/Upload/Search/Exit 九个页面逐字检查，无缺失字形；画质提示字形检查也通过。
- 迁移后的 TMP 阅读页滚动测试通过。
- 08-SDFMenuVerified-1080p.png 为原生 1920×1080 菜单图。
- 09-SDFReadingVerified-1080p.png 捕获了同帧重建大量页面时的遮罩/图形过渡，不计为视觉通过；推进一个布局渲染帧后，10-SDFReadingAfterLayout.png 已核对正常文字与裁剪。Console 无相关错误。
- 已退出 Play Mode。旧版本 1.0.1–1.0.4 无修改。

## Codex “应用快照／应用更改”检查

### 用户提供准确提示后的更正

用户随后确认原文为“无法附加应用快照”。因此应归入应用快照附件/捕获流程排查，而不是 Git 应用补丁或 worktree handoff。下方目录检查为早先误分类时的记录，不作为该错误的原因或修复依据。尤其不能据此要求初始化父目录 Git、提交工程或移动版本文件。

按最新三个 Desktop 日志文件重新定向搜索后，已找到准确失败记录。北京时间 2026-10-13 18:48:47 与 19:01:36，windows-capture-native-bridge 报 Windows Appshot completion failed，errorMessage 为 `computer-use request timed out: load_appshot_capture_source`，failureReason=native_completion_failed，hadAxText=false、hadScreenshot=false；热键触发后约 2.1 秒失败。

确认的故障层：Codex Windows 应用快照捕获请求在加载 capture source 阶段超时，未取得截图或应用文本。不能继续归因于 Git/worktree；日志也未证明具体是权限拒绝、GPU、源窗口或捕获辅助进程内部问题。尚未在本任务中修复该客户端捕获超时。

用户先前手工截图已成功附加到聊天，可临时通过 Windows 截图并粘贴/上传图片绕过自动应用快照流程。通用恢复步骤为结束当前任务后完全退出并重新打开 Codex，再测试普通窗口与 Unity；若持续失败，可将上述错误提交到 Codex 反馈。不会在运行当前任务时强制终止宿主应用。Unity 项目文件、UI 修正与 MCP 连接不因该附件提示失效。

用户确认问题来自 Codex，未提供具体错误文本，且只能描述截图短暂出现后提示失败。不能把此描述直接当作 git apply 错误。

查到的事实：

1. 当前任务绑定目录 E:/课程/大三上/虚拟环境设计/小组作业 不是 Git 仓库。
2. 实际 Git worktree 是 Unity项目/githubclone/project123-dev-1.0.5，分支 dev/1.0.5；原 checkout project123 在 main。
3. 1.0.5 目录仍为未跟踪文件，git ls-files 计数为 0；已有游戏修改直接落盘，并被 Unity 从同一目录读取。
4. 今日 Codex Desktop 日志未找到与 apply/snapshot 对应的明确失败记录。找到的 heartbeat snapshot 日志不能当作快照应用错误。

因此目录绑定或 worktree 目标不一致属于可疑因素，尚未证实为该提示的根因。没有擅自初始化父目录 Git、提交版本、应用补丁到 main 或搬移任务。

可行的使用方式：在 Codex 中将实际 worktree 根 project123-dev-1.0.5 添加为项目，Unity 继续打开该目录内 1.0.5；本任务已落盘的修改无需再次应用。若仍需使用快照，复现时保留完整错误文字，并确认目标 checkout、分支和文件路径一致。

官方资料： https://developers.openai.com/codex/app/worktrees 。其说明 worktree/handoff 依赖 Git 与正确 checkout；它并未证明本次快照提示的具体原因。
