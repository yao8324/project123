# 黄金羽翼圣骑士与圣堂 · R15 试玩版

日期：2026-11-07。项目版本仍为 1.0.8；本次开发分支为 `codex/knight-sanctuary-20261107`。

本包只交付自己的骑士 Boss 和圣堂战斗场景，三位 Boss 的剧情串联由整合同学负责。属于开发中预发布版本，尚未达到概念图至少 90% 相似度的最终美术要求。

## 下载内容

- `KnightSanctuary-R15-macOS.zip`：独立 Mac 试玩程序及快速说明。包含 arm64 和 x86_64 可执行程序；实际启动与输入验证在 Apple M3 上完成。没有 Windows 运行包。
- `KnightSanctuary-R15-UnitySource.zip`：完整 `1.0.8` Unity 源码、实际模型文件、可编辑 Blender、制作脚本、设计与检查记录。排除 Library、Temp、Logs、Builds 等缓存和构建产物。无需从 Git LFS 另下载模型即可打开此源码 ZIP。
- `SHA256SUMS.txt` 和 `ReleaseManifest.json`：下载校验和与来源提交。

## Mac 试玩

完整解压后打开 `KnightSanctuary.app`，等待首次加载进入骑士战斗。请保留整个 app 包。若系统阻止未经公证的开发版，可在系统设置「隐私与安全性」中对本次下载选择「仍要打开」；无需关闭系统安全功能。

WASD 移动、鼠标控制视角、左键剑击、右键盾防/弹反、Shift 突进、Space 跳跃、Esc 暂停。其余快捷键与共用战斗系统一致。此次实际原生输入检查覆盖鼠标攻击、暂停/恢复和失败结算，没有完成整场人工通关。

## Unity 接入

1. Unity Hub 添加解压后的 `1.0.8`，使用 Unity **6000.3.25f1**，等待导入完成。
2. 打开 `Assets/Scenes/BossTests/Knight_Sanctuary_R3_Combat.unity`，点击 Play。`Knight_Sanctuary_R3_Preview.unity` 是单独的外观预览。
3. 迁移请按同目录 `骑士场景_接入说明.md` 保留配置、Prefab、材质、动画、共用战斗代码及 `.meta`；不要仅复制 FBX。新增招式表现依赖 `SanctuaryR3CombatVisuals.cs`，FBX 本身只有走动、冲刺、跳劈三段动作。
4. 如从 Git 克隆，切换上述分支后执行 `git lfs install --local` 和 `git lfs pull`。

场地战斗直径 56 米，整体设计框架 64×72×36 米。黄金羽翼在慢行时收拢、冲刺和跳劈时展开。角色身形、盔甲与材质仍在多轮精修中。

## 本次验证

- Unity Mac 开发版构建成功：0 Error、1 Warning。该 Warning 是未登录 Unity Cloud，原生调试符号未上传；不影响本地程序生成。见 `StandaloneBuildR15.json`。
- R14 受控运行检查通过：实际剑击命中/扣血、二阶段、胜负结算、重试与返回，以及动作/特效挂点。固定对手与控制时钟的检查不等同于人工实战通关。见 `CombatRuntimeCheck_R14.txt`。
- Mac 原生程序实际进入本场景；已操作暂停/恢复和鼠标攻击，观察到玩家受伤、Boss 血条减少以及失败结算。原生移动、重试按钮和完整胜利过程尚未逐项人工验证。
- M3、1280×720、预热 3 秒后 120 帧短样本：均值 22.78 ms（约 43.9 FPS），P95 33.40 ms。只代表短时启动渲染样本，不代表持续战斗性能。见 `NativeR15/NativePreview.json`。

## 已知待办

最终概念图相似度、肩甲和盾牌遮挡、动作润色、局部布料穿插、模型合批/LOD 与持续性能仍待改进。编辑器存在 SearchDatabase 索引异常；原生退出日志有 ComputeBuffer 释放警告。Windows 构建、公证签名、完整人工通关和三 Boss 整合未完成。

本版供团队下载试玩与接入，不代表正式发布或最终验收。完整迭代证据见 `Progress.md`。
