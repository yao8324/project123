# 1.0.5 Arcane Archive UI 原型设计

## 目标与边界

仅实现 UI 展示和导航，不接入资料上传、数据库、真实搜索、文件阅读、账号或 AI。保留原恢复菜单的安全返回入口能力。演示数据标注“展示馆藏”，避免把样本文字误认为用户资料。

工程为 Unity 6000.3.25f1、URP17.3.0、InputSystem1.20.0、uGUI2.0.0。场景 MilanLibrary_Magic_V6，当前玩家出生点为 (0,0.08,7.8)。已有 RecoveryMenu；旧 UIInputStateManager 与 PlayerInteractor 未挂载，不能直接叠加两个 Esc 或 E 监听器。

## 视觉规范

- 风格：深色木作与皮革、旧黄铜金边、象牙白正文；呼应哥特式石墙和彩窗。避免高亮霓虹和复杂粒子。
- 主背景 #211B18；正文 #EEE3CE；边框 #B89A59；封面采用孔雀绿、暗红、深蓝。
- Canvas 使用 Screen Space Overlay，参考 1280×720，Scale With Screen Size，宽高匹配 0.5。
- 居中面板 980×580；深色半透明全屏遮罩；标题 25–37，正文 18–23，注释 15。按钮高度 44–48，文字 hover/pressed 有反馈。
- 当前原型文字已改为 TextMeshPro SDF，字体为 Assets/UI/Fonts/ArchiveChineseSDF.asset（90 点采样，已烘焙当前文案）。当前文字不依赖运行机器系统字体；后续新增汉字须扩充字体资产。正式跨平台交付仍需核对字体分发及目标平台表现。
- UI 根在运行时生成 ArcaneArchiveUI，退出运行自动销毁，不向场景保存运行时面板。

## 页面与行为

| 页面 | 内容 | 动作与状态 |
| --- | --- | --- |
| 漫游 HUD | 小准星、底部书籍提示 | 未瞄准或超距时不显示书名；不自动打开详情 |
| Esc 菜单 | 继续探索、馆藏界面预览、操作说明、设置预览、返回入口、退出 | 统一管理输入和鼠标；返回入口沿用原功能 |
| 操作说明 | 左侧操作、右侧当前按键、恢复默认 | 支持键鼠按钮换绑，配置存 persistentDataPath；Esc 取消/安全返回；详见 UIReview/InputRebindingReview.md |
| 设置 | 可点击的两档画质、灵敏度滑条、音量占位 | 画质及灵敏度本次运行即时生效，F8/F9 共用现有切换逻辑；音量待接入，尚无跨运行配置保存 |
| 我的馆藏 | 用户文件列表结构、0 份空状态、导入/检索入口 | 不把演示数据计入馆藏；入口只打开流程预览，详见 CollectionUIDesign.md |
| 馆藏详情 | 封面、标题、类型、类别、关键词、摘要 | 阅读排版预览、返回探索；不显示虚构用户文件路径 |
| 阅读预览 | 羊皮纸色正文，ScrollRect | 滚轮/拖动阅读；只显示数据资产中的示例文案 |
| 归档预览 | 选择文件→编辑元数据→确认流程排版 | 选择文件按钮禁用并标“待接入”，不进行磁盘写入 |
| 检索预览 | 输入区域外观及空结果状态 | 没有查询行为，不显示伪造成功结果 |
| 退出确认 | 二次确认 | Esc 取消；Editor 中只显示停止 Play 的提示，Standalone 可退出 |

## 输入与返回规则

RecoveryMenu 为当前原型唯一 Esc 与主面板状态拥有者。一时刻仅一个主面板激活。Explore 按 Esc→Menu；Menu/Book 按 Esc→Explore；Reading→Book；Upload/Search→Gallery；Help/Settings/Gallery/Exit→Menu。右上 × 直接恢复探索。

打开面板禁用玩家移动和视角、释放鼠标、隐藏手部与世界提示；保留重力，不修改 Time.timeScale。关闭恢复此前手部显示状态，调用 WaitForReleasedControls，防止点击和持续按键穿透；LateUpdate 最终统一光标状态，兼容玩家现有 Esc 光标处理。

BookPreviewInteractor 是当前场景唯一 E 射线入口（旧 PlayerInteractor 不挂载）。距离 3 米、屏幕中心射线、尊重最近碰撞遮挡；只命中 DemoBookTarget 才提示。打开 UI、鼠标未锁定、移开视线、超距均隐藏提示。装饰书不是自动可交互。

## 数据与书籍对象

DemoBookRecord ScriptableObject 保存 id/title/category/fileType/keywords/summary/previewText/coverColor。演示书籍的 DemoBookTarget 只持有记录引用，不在场景对象上硬编码标题、路径或关键词。

选取现有三本书的书皮 MeshFilter 为检测目标；追加轻量 BoxCollider trigger，跟随已有书籍变换，不改建筑或书架物理碰撞。现有 Collider 保留。目标复用 IInteractable 接口，后续可用正式 BookView 替换；演示记录不会冒充 LibraryDatabase 或 KnowledgeBook。

## 最小验收

1. 等待 Unity 编译和资源导入完成，Console 无新增编译错误或运行异常。
2. 主场景组件、三份数据资产、触发体和记录引用均有效，无 missing script。
3. Esc 开关、子页返回、关闭后移动恢复；阅读回书籍；重复开关不叠加多个主面板。
4. 瞄准 3 米内书籍显示提示，E 开卡，超距/离开/被遮挡时隐藏。
5. 用鼠标实际点击一个按钮，确认 EventSystem 与 InputSystemUIInputModule 可用。
6. 检查菜单、书籍、阅读截图，确认中文、边框、内容和按钮不重叠。

静态检查和最小状态测试不能保证所有分辨率及后续业务接入均无问题，完整手工回归仍按此清单进行。

## 用户馆藏重做

馆藏页以用户成功导入的文件为数据边界，不再展示三张示例卡片。场景里的演示书仍用于独立交互预览。馆藏、导入、检索采用逐行横线分隔与悬停强调，业务未接入时不写文件、不显示成功记录。完整接入逻辑见 CollectionUIDesign.md。

## 输入换绑接入

探索指南支持实际 Input System 换绑，玩家使用运行时克隆的动作资源，书籍交互提示同步更新。操作说明左侧为动作、右侧为按键；换绑、冲突、取消、恢复默认与 JSON 配置重载规则见 UIReview/InputRebindingReview.md。
