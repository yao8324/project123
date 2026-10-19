# 2026-10-07 实际试玩修订验证说明

本目录对应独立 CandidateProject。本轮最终运行结果：107/107 通过，completed=true，failed/errors/exceptions 均为 0；原始证据位于 Reports/WindowReview01/FinalController09/summary.json，21,577 行轨迹位于 trajectory.csv。

## 修改及理由

- 22 组侧墙/后殿花窗：先还原 GF08_Side_frame.002/.008 和对应 GF19 原始网格，随后将窗框、连续窗洞内衬、玻璃按同一宿主坐标系重新安装。普通窗保留原雕刻厚度；楼梯旁两处采用浅浮雕深度以保留通道。玻璃内表面贴合宿主墙内缘，消除可进入的深窗腔。5 组前高窗经单独检查保持其分段前立面原位置。
- 6 张 IV4 阅读桌、2 处 V5 书桌：导出白名单曾使这些物件的 collision_role 落入 none。现从独立木构部件生成 66 个简化盒体，涵盖桌面、腿和支撑，仍保留桌下空隙；不以整桌大盒覆盖通道。
- 正面 4 处小门、8 扇门叶恢复关闭角度，门面与碰撞一致。中央主门仍敞开。未加入 E 交互。
- 用 38.8m 宽连续石质前平台替换 5 块门前小坡。正面坡长 4.2m，左右 1.8m 缓接，使用原台阶 MAT_V2_Ivory_Carving 及同方向 0.5 UV 单位/米。保留室内木楼梯。
- Esc 菜单使用 ScreenSpaceOverlay uGUI，真实按钮经 EventSystem 处理鼠标事件；修复相机空间 UI 在瞬移后的指针位置失配。安全回出生点检查地面和站立胶囊空间，临时禁用 CC 传送后恢复，并清除垂直速度、姿态及输入锁存。
- 蹲伏过程以实际插值高度同步胶囊中心，固定脚底，避免落地状态误触发。
- 手部使用同一 5,632 三角面资源：基于参考图重新定向、定位和拉长原杖网格，袖口从右下画外接入；向前上方约 44°，不是竖直指天。六段动画及近墙回缩保留。

## C# 修改文件

- Assets/Scripts/Player/FirstPersonController.cs：安全恢复、释放后输入门控、批处理虚拟锁鼠、蹲伏固定脚底。
- Assets/Scripts/Player/FirstPersonViewModel.cs：恢复时重置手部姿态与动画。
- Assets/Scripts/UI/RecoveryMenu.cs：新建可点击 Esc 菜单。
- Assets/Scripts/Visuals/Editor/MilanFirstPersonSetup.cs：参考手部朝向与画面位置。
- Assets/Scripts/Visuals/Editor/MilanV6SceneBuilder.cs：接入恢复 UI。
- Assets/Scripts/Visuals/AcceptanceTestOnly/ReviewPlayTests.cs、HandReviewFrames.cs：仅 Editor 的控制器/鼠标事件测试与离屏图像采集。
- Assets/Scripts/Visuals/AcceptanceTestOnly/Editor/ReviewPlayLauncher.cs、MilanHandReviewCapture.cs、ReviewFinalization.cs：无 GameView 聚焦的批处理入口。

## 验证步骤与范围

1. 用 Unity 6000.3.25f1 静音 batchmode 编译，每轮检查 C# 编译错误及运行 Console Error/Exception。运行 ReviewFinalization.Prepare 验证场景、材质、网格、碰撞合同和六动画绑定。
2. 以真实 FirstPersonController + InputSystem 虚拟键盘/鼠标运行 final_controller_suite.json；步长为固定 1/60 模拟秒，不是实测帧率，也不是真人手动试玩。
3. 最终 95 条世界用例：42 条可达花窗地层/二层路线，28 条桌台方向接触及退路，8 条桌面站立/跳落承托，17 条主门/四小门/全宽前坡及侧沿路线。每条记录轨迹、碰撞接触与检查结果。
4. 5 类恢复测试：窗隙、楼梯、跳跃、蹲伏、按住跳跃。由 Esc 输入打开菜单，指针实际命中 Button，连续三次按下/抬起，再点击继续游戏；检查安全位置、胶囊启用、站姿、输入恢复和请求锁鼠。
5. 7 条手部检查：真实状态机选择六段动画和近墙姿态。逐三角形近裁剪后检查中央 3×3 瞄准采样点，保存实际 Unity 图和 12fps 模拟时间静音预览。手部投影检查的 BakeMesh 缩放补偿依据 Unity API： https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/skinnedmeshrenderer/bakemesh 。
6. 全部 27 组玻璃逐实例几何扫描：同一基线区域每组 4,545 条正向射线，包含实际可见 GF、A（含 A04 宿主墙）、E 结构。27 组外侧近景已逐图审阅；部分内侧近景被既有书架/墙体遮挡，实例 CSV 单列，不将遮挡机位当成完整内侧视觉通过。窗口正侧视 Unity 截图另行检查；射线有限采样不能证明任意亚采样细缝绝不存在。

## 明确不计为通过的旧/无效测试

- PlayReview01、PlaySmoke02 及旧 121 组/2,653 步不计为本轮通过证据。
- 首跨东西两窗上方没有 6m 可站立楼面，因此不冒充二层控制器路线；已覆盖地面运动、几何扫描和高处视觉。
- V5 两处书桌的四个 X 方向旧起点位于原柜体或邻接家具内，初始被推出/踏上柜体的结果不能证明桌面穿透。最终用两处书桌各自可达正背面接触绕行与桌面承托覆盖；不删除家具以强行打通该路线。
- 高处五前窗、盲窗装饰和钟塔雕饰没有可达站立位置，仅进行几何/视觉审查，不声称角色在空中贴窗行走。
- 自动测试使用虚拟锁鼠，未夺取用户 OS 鼠标；真实桌面锁鼠手感及真人试玩仍由用户在新 App 中验收。
- Balanced 720p 默认、F8 Balanced/F9 Ultra 保留。清单三角形从 16,086,876 变为 16,085,492（减少 1,384）；由于窗组件碰撞角色分批，渲染批对象从 891 变为 905。历史 M3 53–60fps 不是本轮重测数值；离屏固定步长不能代替硬件帧率测量。

## 文件安全

仅修改 CandidateProject 和授权独立报告目录。不改日常脏工作树，不改只读归档，不 commit/push，不恢复用户删除的旧 App，不清理用户文件，不关闭或聚焦其现有 Player，不播放音频。只生成一个新 Review App。

## 最终构建

macOS Universal（arm64 / x86_64）构建成功，0 错误；代码签名严格校验通过。唯一警告是未登录 Unity Cloud，因此未上传原生调试符号，不影响本地 App 构建。应用标识 com.arcanearchive.milan.review20261007；项目原 ProductName/BundleVersion/ApplicationIdentifier 已在构建后恢复。未自动启动新 App，未进行本轮 standalone 帧率测量。
