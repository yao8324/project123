# 魔法图书馆 1.0.4 · GlassFloor 候选修订版

本目录是完整、可独立打开的 Unity 工程。归档版本 **1.0.4**；类型为候选修订版，上一归档版本为 **1.0.3**（`1976c775fba34fa9ee295647ce40dd058a4f50cf`）。本版来源于已实际编译、测试和构建的独立 CandidateProject，发布整理只补充说明和依赖，没有再修改游戏代码或场景。

## 本版变化与边界

- 修复彩窗纹样发光贴图导出遗漏造成的泛白，保留原有不透明深度写入、正常高光和默认性能设置。
- 入口侧二楼新增 24×6 米、144㎡连续楼板；二楼已有和新增楼面统一错缝木板。中央挑空、楼梯洞口与边界护栏保留。
- 纳入此前窗洞对齐、六张阅读桌和两处书桌台复合碰撞、四小门关闭、仅中央门通行、连续前石坡、Esc 点击回出生点、右下手部六状态修订。
- **已知风险：约1.23米的中庭栏杆仍可被冲刺跳越。** 防跌落项未全通过，用户已接受当前栏杆现状并要求直接发布；没有提高栏杆或改变跳跃。该版本不声明防跌落全部通过。
- “导入资料 → 生成书籍 → 归档书架 → 检索 → 阅读”仍未接入；E 交互暂停。上传台、书架与书桌当前是环境展示。

## 下载、打开和控制

先安装 Git LFS，再克隆原私有仓库的 lixiang 分支（需要仓库读权限）：

```sh
git lfs install
git clone --branch lixiang --single-branch https://github.com/yao8324/project123.git
cd project123
git lfs pull
```

在 Unity Hub 中选择**本版本目录**，不要选择仓库根目录。使用 **Unity 6000.3.25f1**，打开 `Assets/Scenes/MilanLibrary_Magic_V6.unity`，等待依赖导入、编译完成且 Console 无 Error 后 Play。GitHub 自动源码 ZIP 可能仅含 LFS 指针，推荐 Git + LFS；若 `.blend` 或 `.mlmesh` 内容以 `version https://git-lfs.github.com/spec/v1` 开头，说明尚未下载资源实体。

| 操作 | 输入 |
| --- | --- |
| 移动 / 环视 | WASD / 鼠标 |
| 冲刺 / 跳跃 / 蹲下 | Shift / Space / C |
| 释放鼠标并打开恢复菜单 | Esc；随后可真实点击“回到出生点”或继续游戏 |
| 默认性能 / 高画质 | F8 Balanced（720p）/ F9 Ultra |
| E 交互 | 已暂停，不会打开四扇关闭的小门 |

已构建的 macOS Universal（arm64/x86_64）应用名为 `MilanLibrary_GlassFloor.app`，显示名 `Milan Library Glass Floor 20261007`，应用标识 `com.arcanearchive.milan.glassfloor20261007`，内部版本 **2026.10.7.2**。运行包不在 Git 中，本次分支源码推送不代表 GitHub Release 已发布。现有包 `MilanLibrary_GlassFloor_20261007.zip` 的 SHA256 为 `5666505939a31b6047995f4c9f7d3b1876f2164efe9dafcf1fbcabb9c094650f`。拿到包后以此核对；不要凭旧同名 App 判断版本。未交付已验证的 Windows 可执行包。

## 工程内部结构

| 目录 / 模块 | 职责与当前状态 |
| --- | --- |
| `Assets/Scenes/` | 当前入口 MilanLibrary_Magic_V6；保留旧场景及其依赖 |
| `Assets/Art/MilanLibraryV6/` | 导出的 manifest、Meshes、Materials、Textures 与资源导入设置 |
| `Assets/Art/Characters/FirstPersonMage/` | 第一人称袖口、手、魔杖及六动画 FBX/Prefab |
| `Assets/Scripts/Core/` | UIInputStateManager：界面与玩家输入状态协调 |
| `Assets/Scripts/Player/` | FirstPersonController：移动/重力/蹲跳；GroundSurface：支撑识别；FirstPersonViewModel：手部动画 |
| `Assets/Scripts/Interaction/` | 交互接口、射线检测和历史测试物；当前 V6 暂停 E 业务交互 |
| `Assets/Scripts/Knowledge/` | 预留资料数据模块，尚未实现业务闭环 |
| `Assets/Scripts/UI/` | RecoveryMenu：Esc 菜单、点击恢复与继续 |
| `Assets/Scripts/Visuals/` | MilanMagicMotion 变换动画、MilanQualityOptions 画质切换 |
| `Assets/Scripts/Visuals/Editor/` | 网格导入、建场、角色接入、性能配置、资源验证 |
| `Assets/Scripts/Visuals/AcceptanceTestOnly/` | Editor 自动输入/截图/回归与诊断构建支持；正式构建不启用诊断钩子 |
| `Assets/Settings/` | Input System 动作和渲染配置 |
| `Packages/` | manifest 与 packages-lock，完整保留依赖解析结果 |
| `ProjectSettings/` | Unity 项目、输入、画质和构建设置 |
| `Blender/MilanLibrary_Magic_V6/` | 当前原生模型及 Assembly → CloudRepair → PlaytestReview → GlassFloorReview 四阶段输入 |
| `Tools/MilanV6/` | 修复、导出、碰撞角色约定、手部生成和可复现测试辅助脚本 |
| `Docs/GlassFloorReview_20261007/` | 本轮报告、截图、静音预览、测试输入、包哈希 |
| `Docs/PlaytestReview_20261007/` | 上一轮窗/桌/门/坡/恢复/手部证据 |

书籍元数据将由数据驱动；用户资料以后必须保存到 `Application.persistentDataPath`，不能写入 `Assets`。项目规则见 [AGENTS.md](AGENTS.md)。

## 依赖与素材来源

Unity 6000.3.25f1；URP 17.3.0；Input System 1.20.0；uGUI 2.0.0；Test Framework 1.6.0；原生建模使用 Blender 5.2.1 LTS。完整包列表以 `Packages/manifest.json` 和 `Packages/packages-lock.json` 为准，包括现有 `com.coplaydev.unity-mcp` Git 依赖；首次解析需要网络及可用 Unity 许可证，不依赖作者本机 Library。保留锁文件，未擅自删除 Package。

大型原生模型、旧 Test2.fbx 与 V6 网格沿用本版 `.gitattributes` 的 Git LFS 规则。四个 Blender 文件属于重建依赖链，不是重复运行包。当前编辑入口为 `MilanLibrary_Magic_V6_GlassFloorReview.blend`。

素材声明见 [Texture_Credits.txt](Blender/MilanLibrary_Magic_V6/Texture_Credits.txt)：Poly Haven `wood_table_001` 为 CC0；手稿图为项目原创程序图；原项目 V4 贴图和 Maynooth 校标保留原来源，不能据此宣称全部资源均为 CC0。

## 重建和打包

下面命令在本版本目录运行。保存修改并关闭**本工程**编辑器，避免锁冲突；不要关闭其他工程或正在试玩的 Player。打开已导出的场景不需要先重建 Blender。

```sh
BLENDER=/Applications/Blender.app/Contents/MacOS/Blender
UNITY=/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
# 全链重建：Assembly → CloudRepair → PlaytestReview → GlassFloorReview → 导出，并重建手部
python3 Tools/MilanV6/rebuild_repaired_scene.py --blender "$BLENDER"
# 保留玩家、UI、灯光、画质设置，刷新生成的建筑与彩窗
"$UNITY" -batchmode -noaudio -quit -projectPath "$PWD" -executeMethod MilanAcceptance.GlassFloorFinalization.Refresh -logFile "$PWD/Docs/rebuild.log"
# macOS 构建；输出目录必须是未使用的新路径
MILAN_PLAYER_OUTPUT="$PWD/Builds/GlassFloor_Rebuild/MilanLibrary_GlassFloor.app" MILAN_BUILD_REPORT="$PWD/Docs/rebuild_build_report.json" "$UNITY" -batchmode -noaudio -quit -projectPath "$PWD" -executeMethod MilanAcceptance.GlassFloorFinalization.Build -logFile "$PWD/Docs/build.log"
```

只迭代玻璃/楼面时，也可执行 `apply_glass_floor_review.py`（依赖已收录的 PlaytestReview），再以生成的 GlassFloorReview.blend 执行 `export_scene.py`，最后执行 Refresh。脚本读写均以本工程为根。`walkability_geometry_validation.json` 与 `walkability_test_routes.json` 已收录，完整重建会更新它们。构建 helper 恢复临时产品名、标识和版本设置；同路径已有产物时不会覆盖。

## 验证结果与复测

[本轮详细报告](Docs/GlassFloorReview_20261007/VERIFICATION.md) 与 [证据目录](Docs/GlassFloorReview_20261007/Evidence/) 保留实际结果：本轮回归 **107/107**；新增楼面 **60/61**（失败为栏杆冲刺跳越）；新增楼板 **14,400** 条支撑射线无遗漏；最终 **66** 张离屏截图和 **64** 次材质耗时样本，最终轮次 Console Error/Exception 为 0。27 外窗逐图核对；部分内侧视角有书架遮挡，不声称全部内侧图像无遮挡。失败的早期 A/B 和一次 Burst 原生崩溃未计入通过。

macOS 构建 0 错误、1 个未上传 Unity Cloud 原生调试符号的警告；ZIP CRC 和严格代码签名校验通过，不代表 Apple 公证。本次出版整理以哈希核对已验证工程的代码、资源和设置，没有重新执行功能测试。测试通过真实 Controller + Input System 虚拟设备自动输入完成，不是真人试玩；离屏采样含 GPU 读回，不能当成本轮 standalone FPS。

每次 C# 修改后必须等待 Unity 编译并检查 Console。复测入口为 `MilanAcceptance.ReviewPlayLauncher.Run`，传入证据目录中的 `regression_suite.json` 或 `floor_suite.json`，环境变量详见报告。当前包的真实鼠标手感和目标机器性能仍需使用者验收；栏杆现状已获用户接受，不阻塞本次发布。

## 归档与发布说明

本版按原私有仓库 `yao8324/project123` 的 lixiang 三段版本目录架构归档，既有 1.0.1/1.0.2/1.0.3 保持原样。源码、验证证据与原生依赖在本目录内；运行 App、Library、缓存及本机凭据不纳入。发布范围见 [PUBLISH_PLAN.md](Docs/GlassFloorReview_20261007/PUBLISH_PLAN.md)。`DELIVERY.json` 和 `source_package.json` 记录发布整理前的本机验收包，里面的相对 Builds 路径不是仓库下载链接；其旧 published 字段不用于判断当前 Git 分支状态。
