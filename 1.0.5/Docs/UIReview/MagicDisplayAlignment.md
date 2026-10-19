# 魔法书与地球仪展示台对齐

修改场景：Assets/Scenes/MilanLibrary_Magic_V6.unity。
备份：Assets/Scenes/MilanLibrary_Magic_V6_BeforeDisplayAlignment.unity，保留修改前当前编辑状态。

原展示台中心 Z=13.97，而魔法装置中心 Z=14.13，偏前约 0.16 米。两套装置整体缩放至 0.8，按底座接触点重新对齐到 X=-5.8 / 5.8、Y=1.07、Z=13.97。底座底面与展示台顶面间距约 0.01 米。魔法书主体与翻页/金尘额外降低世界高度 0.25 米，魔法阵保留在底座上方。

新增静态父层级 MagicDisplay_Book/FloatingBook、MagicDisplay_Globe，统一组织原有动画父对象与静态网格。动画仍写入原动画节点的局部变换；空间调整施加于其外层父对象，因此不被动画覆盖。翻页和漂浮速度不变。保留原展示台、地板碰撞与书籍交互触发体；没有修改脚本、模型或材质。

Play Mode 中检查两侧第一人称眼高（1.7 米）的渲染。球体中心约 Y=2.09，当前书封中心约 Y=1.92。完整 45 秒循环取 121 个样本检查球体及六个环带，最大中心误差 0.000465 米。最后 Console Error/Warning 为 0。验证后退出 Play Mode，场景已保存。

预览：Docs/UIReview/MagicDisplays-Aligned.png。
手工检查：Play 后到两侧展示台附近，侧面及正面观察底座居中、魔法书离魔法阵的间距，以及地球仪转动时球体和环带是否保持同心。

## 二次修正：真正桌面支撑

上一轮仅对齐 V5_DisplayConsoles 小木托板，遗漏其位于弧形桌内侧缺口。桌面几何向下射线确认 X=5.8 时 Z=13.0～13.6 有顶面，Z=13.8～15.6 无顶面，因此原托板位置 Z=13.97 不适合作为落地的桌面陈设。

将两套装置底座移到 (+/-5.8,1.058,13.30)，整体缩放 0.7，书本保留额外下降 0.25 米。原 V5_DisplayConsoles 停用而非删除。地球仪底座周边半径 0.31 米的 16 个支撑射线全部命中桌面。新的备份为 Assets/Scenes/MilanLibrary_Magic_V6_BeforeDeskPlacement.unity。

本次截图使用实际玩家 Main Camera，眼高 1.68，玩家站在 (+/-5.8,0.08,11.6)，Game View 中包含正常手部和书籍提示：Book-OnDesk-GameView.png、Globe-OnDesk-GameView.png。画面确认底座放在真正桌面上，原悬空托板消失；书本/魔法阵/底座居中，球体/环带/底座对齐。测试仅临时移动 Play Mode 玩家，退出后恢复出生点。场景已保存，最终 Console Error/Warning 为 0。
