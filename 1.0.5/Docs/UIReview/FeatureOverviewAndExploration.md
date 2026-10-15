# 功能概述与深入探索入口

场景魔法书的交互提示移除“· 界面演示”，保留动态换绑提示，如 [E] 查看馆藏。当前演示魔法书打开功能概述，介绍个人文件馆藏与未知魔法/恐怖探索两条体验线，不把演示书籍当成已导入用户文件。

文案：收藏知识，探索未知；导入你的资料，将知识化为魔法书籍，在图书馆中归档、检索与阅读。步入书廊深处，探索未知的魔幻与恐怖，揭开被封存的秘密，迎接黑暗中的强敌。

深入探索按钮：暗酒红底、鎏金卷曲花纹、金框及眼形符号，悬停颜色变亮，0.16 秒渐变。花纹由原生 UI 顶点绘制，随 Canvas 缩放，无外部图片/Package；花纹不拦截鼠标事件。

点击进入“封印之下”入口页，展示未知回廊与首领遭遇的方向；明确 Boss 战尚未接入。返回按钮及 Esc 回功能概述，再按 Esc 恢复漫游。我的馆藏按钮仍进入真实用户馆藏的空状态 UI。

团队计划新增 Boss 战：本次仅提供概述文案与 UI 入口，不实现战斗、敌人 AI、攻击、血条、传送或新房间。后续独立规划探索/战斗场景与返回大厅流程；个人馆藏仍以用户文件为唯一业务数据来源，未来真实资料详情需与功能概述分开。

修改文件：Assets/Scripts/Interaction/DemoBookTarget.cs、Assets/Scripts/UI/RecoveryMenu.cs、Assets/Scripts/UI/GildedFiligree.cs（新增及 meta）、Assets/UI/Fonts/ArchiveChineseSDF.asset。

验证：编译完成，最终 Console Error/Warning 为 0；功能概述和探索页均无缺字及文本溢出；鎏金花纹 CanvasRenderer 有 820 个顶点并在最终截图显示；点击深入探索进入 DeepExplore，返回按钮与 Esc 回 Book，继续 Esc 恢复 Explore 与玩家输入。早期无花纹截图不是最终交付，已补齐 CanvasRenderer 后截图验证。

手动验证：Play → 瞄准魔法书 → 按当前交互键；确认提示无界面演示；功能概述中悬停、点击深入探索；查看探索入口；按 Esc 返回与关闭；从我的馆藏确认用户文件列表仍为空。

截图：FeatureOverview-GildedExplore-Final.png、DeepExplore-Entrance.png。

## 金紫封印样式调整（2026-10-14）
- 功能概述的标题、正文、三个按钮共用右侧内容中心轴 X=113；正文由左对齐改为居中。
- 我的馆藏、深入探索、返回探索改为同一行 Y=-151，两侧按钮160×40，中央按钮由300×70缩小到210×60。
- 深入探索采用暗紫底、金紫交织荆棘、竖瞳和菱形封印；悬停变亮。初始化立即设置底色，避免首帧从白色淡入。
- 修改脚本：Assets/Scripts/UI/RecoveryMenu.cs、Assets/Scripts/UI/GildedFiligree.cs。
- 编译完成，Console Error=0；Play Mode文字溢出=0，PointerClick跳转DeepExplore、Esc返回Book通过。截图 FeatureOverview-GoldViolet-1.png 是最终效果。
- 手动复验：Play后对准演示书按E，查看居中与按钮同排；悬停深入探索观察金紫高亮，点击后按Esc返回。
