# 1.0.3 · 新版图书馆建模

- **版本号：** `1.0.3`。
- **版本类型：** 美术集成修订版。
- **备注：** 新版图书馆建模。
- **上一版本：** `1.0.2`，基准提交 `743fb68bdeb56dc4b18ce5cfc630fc473472b246`。
- **Unity：** `6000.3.25f1`；渲染管线 URP `17.3.0`。
- **来源：** 远端上一版完整工程，加上本次用户确认的 V6 美术交付白名单。没有复制工作目录中无关的未提交修改。

## 主要改动

- 新增完整的 `Assets/Scenes/MilanLibrary_Magic_V6.unity` 展示场景，配置为本版首个启动场景；原 LibraryMain 和旧资源保留。
- 导入 21,439 个可见源构件，合并为 889 个网格渲染对象，包含哥特式外壳、木作室内、扩大后的两个工作区、书架魔法、悬浮书、天球及无白背板的金边 Maynooth 校标。
- 67 种 URP 材质、97 盏实时灯光、47 组变换动画和 335 个碰撞体（含玩家）。
- 在 `Blender/MilanLibrary_Magic_V6/` 保存完整 `.blend`、重建用 Before 快照、模块导出、贴图、真实 Blender 图和原验证清单。
- 保留远端最新的第一人称控制器及 C 蹲下、Shift 冲刺、滚轮输入绑定，未用过期本地脚本覆盖。

## 打开与操作

1. 使用 Git LFS 拉取本版本资源。GitHub 的普通源码 ZIP 可能只有 LFS 指针；应优先使用 `git clone` 与 `git lfs pull`。
2. 使用 Unity Hub 打开 `1.0.3/` 版本目录，不能打开分支根目录。编辑器版本为 `6000.3.25f1`。
3. 打开 `Assets/Scenes/MilanLibrary_Magic_V6.unity`；Play 后 WASD 移动、鼠标环视、Shift 冲刺、C 蹲下、空格跳跃；Esc 释放鼠标。
4. 在 Blender 中打开 `Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_Assembly.blend`。
5. Unity 实景与原验证见 [Review.html](Docs/MilanV6Unity/Review.html)；独立归档工程的复核见 `Docs/MilanV6Unity/archive_validation.json`。

## 脚本与验证

新增四个 C# 文件，均位于 Visuals 模块：

- `Assets/Scripts/Visuals/MilanMagicMotion.cs`：播放世界变换采样。
- `Assets/Scripts/Visuals/Editor/MilanMeshImporter.cs`：把真实求值网格、法线与 UV 导入为 Unity Mesh。
- `Assets/Scripts/Visuals/Editor/MilanV6SceneBuilder.cs`：场景装配、材质与灯光配置、实景截图。
- `Assets/Scripts/Visuals/Editor/MilanArchiveValidation.cs`：独立工程导入、引用、动画及上一版输入兼容性复核。

复核命令：在本版本工程中，用 Unity `6000.3.25f1` 批处理执行 `ArcaneArchive.Visuals.Editor.MilanArchiveValidation.Run`。它会打开 V6 场景、检查缺失资源、采样动画并生成真实 Unity 图。原工作工程已通过 Play、站立、动画与磁盘重开检查；独立归档工程已通过 Unity `6000.3.25f1` 的首次导入、编译及复核：1,025 项依赖无缺失，缺失脚本、材质、网格均为 0；47 组变换动画、发光材质及新版蹲下输入检查通过。详见 [独立工程验证](Docs/MilanV6Unity/archive_validation.json)。

## 已知问题与未实现范围

- 全精度约 1,577 万三角面，尚未进行 LOD、遮挡烘焙和目标设备性能优化；不是性能达标声明。
- URP 使用实时灯光与环境近似，不等同于 Blender Cycles 多次间接光；灯光强度和材质微闪驱动器暂为静态基准值。
- 这次为美术场景集成。上传、检索、资料生成书籍和阅读业务闭环仍未接入，预览书不代表可交互书籍。
- 延续原工程的历史资源和 White Castle 样板，不把其历史局限当作本次新建 V6 模型的验证结果。
