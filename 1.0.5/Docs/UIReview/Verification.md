# 1.0.5 UI 原型验证记录

日期：2026-10-13。使用 Unity MCP，工程 1.0.5，Unity 6000.3.25f1，场景 MilanLibrary_Magic_V6。

文字清晰度后续修正：全部当前 UI 改为 TMP SDF，含静态中文字体资产与统一画质提示；九页字形检查通过，Console Error/Warning 均为 0。最新字体与快照调查见 [FontAndSnapshotReview.md](FontAndSnapshotReview.md)，以下保留此前原型验证记录。

## 修改范围

- 修改 Assets/Scripts/UI/RecoveryMenu.cs：统一主面板、Esc、光标、手部显示与旧恢复功能。
- 新增 Assets/Scripts/Knowledge/DemoBookRecord.cs：演示书籍数据资产类型。
- 新增 Assets/Scripts/Interaction/DemoBookTarget.cs：数据引用及 IInteractable 接口。
- 新增 Assets/Scripts/Interaction/BookPreviewInteractor.cs：3 米中心射线、E 键及 UI 互斥。
- 修改 Assets/Scripts/Visuals/MilanQualityOptions.cs：仅在主 UI 打开时隐藏旧 OnGUI 画质提示，画质行为保持原样。
- 新增 Assets/ScriptableObjects/UIDemo/BookPreview_1.asset 至 BookPreview_3.asset，包含对应 .meta。
- 修改主场景：现有 RecoveryMenu 关联演示数据；玩家追加 BookPreviewInteractor；三本书追加 DemoBookTarget 与 trigger BoxCollider；保留既有建筑与玩家碰撞。
- 场景修改前快照：Assets/Scenes/MilanLibrary_Magic_V6_BeforeUI.unity。不覆盖旧归档版本。
- 文档：Docs/UIPrototypeDesign.md、本记录与版本 README。

## 最小检查结果

### 恢复任务后的最终核对

2026-10-13 恢复后 MCP 连接正常，确认仍为 1.0.5/Unity6000.3.25f1 主场景。主场景无未保存修改，Missing Script=0、Missing Mesh=0，3 个记录引用与 3 个书籍检测目标有效；不重写暂停期间的场景或代码。

最终阅读截图为 `06-ReadingFinalVerified.png`，捕获前后均确认当前状态为 Reading。正文内容高度约 723.33，视口高度 292，滚动灵敏度 24；实际派发 Scroll 事件后内容位置变化，随后 Reading→Book→Explore 返回通过，玩家 GameplayInputEnabled 恢复。最终 Console Error=0、Warning=0，退出 Play Mode。当前范围内未发现未解决的错误，跨平台、多分辨率、真实业务和完整构建仍按末尾限制说明验收。

- 脚本已在 Unity 实际编译；资源导入完成。最终 Console Error=0、Warning=0。
- 主场景 Missing Script=0，Missing Mesh=0；三份演示数据与三个书籍触发体引用有效；场景已保存。
- Play Mode 内 API 状态回归：打开菜单禁用玩家输入；阅读→书籍→探索；归档/检索→馆藏导览；关闭后输入恢复。
- 使用临时 Input System 虚拟 Keyboard：E 打开书籍详情，Esc 关闭菜单；设备在 finally 中移除。
- 使用现有相机及物理射线：1.5 米识别演示书籍，4 米目标为空；UI 打开后射线目标清空。测试相机姿态及控制器启用状态随后恢复。
- 20 次快速开关后的状态检查通过。按钮点击以 EventSystem RaycastAll 定位，并派发 PointerClick，成功恢复探索。首次在同一帧大量重建后立即查询图形射线产生陈旧测试结果；等待布局帧后复测通过，不将该次失败计作通过证据。
- 阅读页 ScrollRect 响应滚轮事件；采用 24 滚动灵敏度及正文实际 preferredHeight，避免仅有固定高度导致正文截断。
- Game View 已观察中文、版式、封面、按钮与正文；图像见同目录 PNG。03-ReadingPreview-Verified.png 是已核对的阅读图；第一次 03-ReadingPreview.png 捕获了布局过渡帧，不作为视觉通过证据。

## 手工复核步骤

1. 打开 Assets/Scenes/MilanLibrary_Magic_V6.unity，Play 后按 Esc。
2. 点击馆藏界面预览，再点任意馆藏卡→阅读排版预览；滚轮阅读，Esc 返回书籍，再 Esc 恢复探索。
3. 瞄准上传区附近的大型悬浮书，或二楼两侧桌上演示书，3 米内出现提示，E 打开。移开或超距应隐藏。
4. 在菜单切换操作说明、设置、归档与检索预览，确认返回路径正确；未接入功能不能选文件、执行搜索或写入资料。
5. 确认继续探索后鼠标重新锁定、移动恢复；返回入口沿用安全恢复功能。

## 限制

这里只是 UI 原型：未实现业务数据存储、真实文件读取、上传、搜索、图像预览或 Office/PDF 外部打开。退出按钮只在独立运行程序实际退出。当前 Windows 中文使用系统微软雅黑；未验收缺少中文字体的系统、多分辨率、手柄、正式构建和完整玩家回归。最小静态及运行检查不能保证后续改动或所有设备绝对无 bug。
