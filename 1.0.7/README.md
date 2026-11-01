# Arcane Archive 魔法图书馆 1.0.7

本版是上传者指定工程的**独立源码归档快照**，类型为**战斗玩法修订版**，上一版本为 **1.0.6**。整理日期：**2026-11-01**。

来源为 `project123-submit-program-20261027/1.0.6/`。按用户本次明确选择，归档位置为仓库 `yao8324/project123` 的 **`lixiang` 分支 → `1.0.7/`**。本次只复制工程与整理文档，没有新增运行功能、改动脚本或更改 Unity 内部产品版本设置；版本目录名不代表 GitHub Release 已发布。

此来源与相邻 `project123/1.0.6/` 是不同工作区。本版没有收录后者后续的剑盾近战、共用体力、极限闪避等修改，也不表示集成团队 main v0.1.0 或完成 IM MONSTER 剧情改造。

## 打开工程

1. 使用 GitHub Desktop 克隆 `https://github.com/yao8324/project123.git`，选择 **lixiang** 分支。上传完成后才能从远端获取本版。
2. 下载 Git LFS 资源实体。在 Desktop 的 `Repository → Open in Terminal` 中执行 `git lfs pull`。首次使用 LFS 时先执行 `git lfs install --local`。
3. Unity Hub 添加仓库内 **`1.0.7/`**，不要添加外层仓库根目录。Unity 版本为 **6000.3.25f1**。
4. 等待 Packages 解析、资源导入和脚本编译，打开 **`Assets/Scenes/MilanLibrary_Magic_V6.unity`**，检查 Console，再进入 Play Mode。
5. 本源工程通过 **Esc → 深入探索 → 深入探索** 进入 Boss 技术演示，也可通过魔法书功能概述进入。战斗支持暂停、胜负、重试和返回。

首次导入需要访问 Unity 包源及现有 Git 包依赖。GitHub 自动生成的源码 ZIP 不保证包含 LFS 实体；建议使用 GitHub Desktop 克隆。

## 源工程包含的功能

- 图书馆环境、第一人称移动、冲刺、跳跃、蹲下、交互与碰撞。
- Esc 菜单、画质和灵敏度设置、按键换绑、书籍预览及自动魔法大门。
- 缄默典狱长骨骼 Boss：85 根骨骼、17 段导入动作，五种攻击、二阶段、有限追踪飞弹、吞印、格挡、胜负与重试。
- 2026-10-27 特效基线：一/二阶段 1.8/2.2 米裂地危险带、约 0.12～0.18 秒高速冲撞及路径残影、两次挥手召唤并发射头顶 6/8 枚禁页环列。

战斗中左键发射秘文弹，右键按下吞印、按住格挡；WASD 移动，Shift 冲刺，Space 跳跃，C 蹲下，Esc 暂停。具体参数、历史验证与截图见 [禁咒特效说明](Docs/BossDesign/ArcaneEffects20261027/README.md)。

用户馆藏仅代表成功导入的资料；装饰书和演示数据不属于用户馆藏。本源工程尚未接入真实资料导入、检索、归档与阅读闭环，AI/RAG、多人及账号仍待实现。

## 目录与依赖

| 路径 | 用途 |
| --- | --- |
| `Assets/` | 脚本、场景、Prefab、网格、材质、贴图、字体及配套 `.meta` |
| `Packages/` | 包清单与锁定依赖 |
| `ProjectSettings/` | Unity 版本、输入、渲染和构建配置 |
| `Docs/` | 设计、历史验收、截图及本次归档检查 |
| `Blender/`、`Tools/` | 原生模型、导出及历史制作工具 |
| `.gitattributes`、`.gitignore`、`AGENTS.md` | LFS、忽略规则与项目约束 |

主要依赖为 URP **17.3.0**、Input System **1.20.0**、uGUI **2.0.0** 和 Test Framework **1.6.0**，以 `Packages/manifest.json`、`packages-lock.json` 为准。启动场景沿用 MilanLibrary_Magic_V6，旧 LibraryMain 仍为第二个启用场景。

工程为独立文件副本，不借用旧版本或外部工作区资源。大型 FBX、Blender 与 `.mlmesh` 文件沿用本目录 `.gitattributes` 中的 Git LFS 规则。正常试玩无需重跑 Blender 或建场工具。

## 本次检查与验证边界

复制了 **4,228 个源文件，合计 2,229,832,536 字节**。文档调整前逐文件 SHA256 与指定源工程比较，差异 **0**；Assets 的 `.meta` 配对检查无缺失或孤立项，没有未下载实体的 LFS 指针。详细结果及最终文件核对见 [归档检查](Docs/ArchivePreparation_20261101.md)。

本轮只执行复制和静态归档检查，**没有重新运行 Unity 编译、Play Mode、自动战斗测试或独立构建**。源工程保留的 36 项技能检查、25 项动作回归及资源检查属于 2026-10-27 历史证据，不作为本次重新验收结果。

已知限制包括中庭栏杆可被冲刺跳越、高精度环境及约 14.2 万三角面 Boss 的性能待测、Boss 无 LOD，以及尚无本版独立运行包和完整平台回归。历史问题和记录以各自日期与版本为准。

## GitHub Desktop 上传

GitHub Desktop 添加外层 `project123` 仓库，确认 **Current branch 为 lixiang**。本次仅选择 **`1.0.7/`、根 `README.md`、根 `ARCHITECTURE.md`**，不要勾选工作区原有的旧版本、根 Assets 或其他修改。

提交信息建议为 `feat: archive 1.0.7 Unity project snapshot`。点击 **Commit to lixiang** 后再点击 **Push origin**，等待 Git 与 LFS 上传完成。远端有新提交时先正常同步并处理冲突，禁止强推。上传后在 [lixiang 分支](https://github.com/yao8324/project123/tree/lixiang) 检查 `1.0.7/` 与版本索引。

本次 lixiang 归档由用户明确指定；团队日常程序交付仍按 main 的协作说明使用 program，管理者决定是否采用到 main。

素材许可沿用源工程：WhiteCastle、Stylized Library、原项目贴图与校标等第三方内容未获得新的开源授权；贴图出处见 [Texture_Credits.txt](Blender/MilanLibrary_Magic_V6/Texture_Credits.txt)。
