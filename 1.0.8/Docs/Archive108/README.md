# 1.0.8 归档与验证 · 2026-11-06

目标：`yao8324/project123` → `lixiang` → `1.0.8/`。类型：开发中战斗玩法修订版；上一归档编号：1.0.7。

本次按用户“上传到 lixiang 分支，作为 1.0.8”的明确指令，使用刚完成试玩的骑士 V5 完整工程。内容来源为远端 `19d8d46bdb0d277e094441030344b2f15f1ba253` 的 `1.0.6/` 加本轮 V5 成果；不借用原工作区 1.0.7 未提交内容。上传准备从最新远端建立独立工作区，仅新增 1.0.8 并更新根 README、ARCHITECTURE，旧版本保持原样。V5 是内部制作迭代名，不是归档编号。

## 复制与资源检查

- 源工程共 **6,868 个文件，2,778,604,636 字节**；复制后按 SHA256 核对。三份说明文档补充归档上下文，其他 **6,865 个源文件逐字节一致**，运行资源、脚本、包依赖、设置、Blender 与工具未改动。
- 完整保留 Assets、Packages、ProjectSettings、Docs、Blender、Tools、规则文件及 .meta，不使用软链接、子模块或共享资源。
- 资源 `.meta` 配对无缺失/孤立，2,898 个 GUID 无重复。九个 `.gitkeep` 是 Unity 忽略的隐藏占位文件，无需 .meta。
- 没有未还原的 LFS 指针；既有大型 FBX、mlmesh、Blender 与新增 V5 Blender 源沿用版本内 LFS 规则。
- Library、Temp、Logs、Obj、Build/Builds、UserSettings、IDE 缓存和 Blender 自动备份均不进入 Git。运行包留在本地，源码归档不包含 EXE。

机器记录见 [源文件哈希清单](SourceManifest.json)、[复制核对](CopyVerification.json)、[工程资源核对](ProjectAudit.json)。全目录首次加入 Git 的空白检查会显示继承的 Unity 序列化及第三方源文件尾空白；核对其均与批准源逐字节一致，保留原文件，新增/修改说明文档的空白检查通过。最终暂存范围与 LFS 检查见 [提交前核对](StagingVerification.json)。

## 本次独立 Unity 验证

在新的 1.0.8 工程目录使用 Unity **6000.3.25f1**，执行首次资源导入、Package 解析和脚本编译。批处理正常结束，Unity 日志明确报告退出码 0；无 C# 编译错误或异常记录。启动日志有许可访问令牌不可用提示，随后正常解析授权，不影响本次成功退出。详情及原始本地日志 SHA256 见 [编译记录](CompileVerification.json)。本轮不宣称整份环境日志从无错误字样。

归档没有修改 C#；本次仅重新验证新位置导入/编译，没有重新运行 Play Mode、Windows 构建或真人完整战斗。源工程的 **117 项受控检查、0 错误/0 警告 Windows 构建、原生启动及 120 帧渲染样本** 保留在 [V5 说明](../KnightDesign20261105/ProductionV5/README.md)，属于归档前对同一份运行内容的实测。源报告中的一次 SearchDatabase 编辑器索引异常和退出 ComputeBuffer 提示保留原说明。

## 本轮 V5 脚本清单与复验

下列文件是 V5 相对于远端基线的代码变更，本次复制归档不再修改：

- `Assets/Scripts/Visuals/Bosses/Knight/KnightVisuals.cs`
- `Assets/Scripts/Visuals/Bosses/Knight/KnightRigConstraints.cs`
- `Assets/Scripts/Visuals/Bosses/Knight/KnightArenaPresentation.cs`
- `Assets/Scripts/Visuals/Editor/KnightProductionBuilder.cs`
- `Assets/Scripts/Visuals/Editor/KnightProductionAcceptance.cs`
- `Assets/Scripts/Visuals/AcceptanceTestOnly/KnightProductionNativePreview.cs`

复验先下载 LFS 实体，用 Unity 6000.3.25f1 打开 1.0.8，等待编译并检查 Console。运行 `Assets/Scenes/BossTests/Knight_Production_Test.unity` 可进入骑士；大厅的地球仪旁魔法书也可选择两位 Boss。受控自动检查入口为 `ArcaneArchive.Visuals.Editor.KnightProductionAcceptance.Run`，会运行八招两阶段、外环路线、握柄/落脚、暂停、阈值剧情、重试与返回检查，并在结束后退出编辑器。制作与构建入口见 V5 README；一般试玩无需重跑生成器。

## 剩余工作

模型、动作和前庭是可继续制作的阶段成果，仍需手工拓扑、UV/烘焙、穿插修正、动作精修、最终音效、完整人工实战及其他设备验证。正式三 Boss 衔接、亡灵援军、跨关奖励、剧情选择与三结局存档尚未接通；资料导入、检索和阅读闭环仍是规划项。
