# IM MONSTER：遗赠 · 三 Boss 剧情整合完整版

本目录是 2026-11-08 的完整 Windows x64 游戏交付。这里的“完整版”指三关可以连续打通的整合试玩版；不表示原长篇策划的所有玩法与分支均已实现。

## 下载与启动

下载 `IMMonster_ThreeBoss_Windows_20261108.zip`，完整解压，然后打开 `IMMonster_ThreeBoss_Windows/IMMonster.exe`。不能只复制 exe；必须保留同目录的 Data、MonoBleedingEdge 与 DLL。无需 Unity，无需联网。

ZIP 使用 Git LFS 保存；网页请使用文件的 Download 下载，或克隆后执行 `git lfs pull`。如果下载得到几百字节的文本，那是 LFS 指针，不是游戏。SHA256SUMS.txt 提供校验。

## 游戏流程

书中选择“翻开故事 · 连续挑战三关” → 短对白 → 缚魂者 → 典狱长 → 黄金翼圣骑士 → 撤离结尾。胜利后进入下一关；失败可重试本关。也可以书中单独选关。

操作：WASD 移动，鼠标观察，空格跳跃，Shift 冲刺，左键连斩，右键举盾/弹反，1–4 技能，I 装备，Esc 暂停。

## 验证与范围

- Unity 实际 Play Mode：304 项断言通过，包含真实虚拟键盘输入与 Boss 自主 AI、受控近战命中、失败重试和三关通关。
- Mac 编辑器游戏窗口：实际点击检查书本、对白、进入战斗、暂停及返回书页。
- Windows x64 Mono 构建：0 错误、0 警告；ZIP CRC、依赖与 PE x64 文件检查见 PackageVerification.json。
- 尚未在 Windows 实机复验原生运行与性能。
- 缚魂者复用现有战斗底层；水晶为场景装饰；救援以战后叙述呈现。未实现独立召唤小怪/锚点机制、长篇探索及三个分支结局。

## 工程与重建

本提交的完整 Unity 工程位于仓库 [`1.0.8/`](../../1.0.8/README.md)，使用 Unity 6000.3.25f1。入口为 `Assets/Scenes/IMMonster_ThreeBoss_Book.unity`；运行后默认打开三 Boss 书页。构建与验收步骤见 [整合说明](../../1.0.8/Docs/ThreeBoss/README.md)。

本轮提交在独立 `codex/three-boss-book-20261107` 分支，未合并 main。
