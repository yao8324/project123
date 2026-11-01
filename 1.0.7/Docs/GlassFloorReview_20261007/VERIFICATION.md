# 玻璃与二楼木地板修订验证（候选交付，护栏风险待决定）

候选工程路径为本项目，不是旧云端包。2026-10-07 接到真实反馈后继续修订；上一轮107项结果保留在历史报告，不能替代本轮。

## 根因与修订

Blender 彩窗 Base Color 与 Emission Color 都连接同一张纹样图片，Emission Strength 0.02。旧导出器只导出了 BaseMap，却把 Emission Color 的未使用默认白色导成常量；Unity 的 _EMISSION 因此把黑铅条及彩色纹样一起抬白。44张同相机A/B确认：接回 EmissionMap 消除泛白；单独关反射、关AA或提升渲染比例都不能解决。实际运行场景没有 Global Volume、Bloom 或雾，不能依据建场脚本预设将它们归因。

在同机位中央窗格裁剪区域，旧图HSV平均饱和度0.091，接回纹样后0.365；亮度P90–P10从0.097变为0.396。这是图像对比指标，不是光学真实度评定。保留正常玻璃高光、全局720p/.85/SMAA；纹理最终保留原512×2048 DXT1，使用Trilinear/aniso8；运行对象占用1,399,120字节。曾试NPOT原始尺寸，但当前Unity/Metal回退为RGBA32约14.9MB，故弃用该方案，不以显存堆清晰。

原始 Blender Transmission=0、Alpha=1，Unity本来使用Opaque。本轮没有把透明玻璃强制改不透明来掩盖问题。测试过半透明分支，但它会混入背景、削弱黑铅条且需要更完整的透明排序处理；最终保留原有深度写入方式，恢复有纹样的弱发光。自然彩色透光感仍是实时近似，没有实现物理折射、光谱透射或投射彩色光斑。

二楼原有侧廊从纵向z6开始，入口侧z0..6无楼板；新建x±12、z0..6、顶面6m的144㎡连续承重楼板。26块现有/新增二楼楼面铺设3870块0.24×1.6m错缝木板表面，4mm缝，下方连续碰撞支持；沿世界坐标维持纹理方向。仅退休原南桥z6的32个内部栏杆视觉/代理件，中庭z9、两侧、后殿与楼梯边界保留，未封中央挑空。没有删除原场景或Package。

## 本轮当前实测

- 同一新场景已重新运行原95条世界路线与12项恢复/手部检查，107/107通过、Console Error/Exception均0；日志Regression02是本轮执行。
- 新楼面49条路线加恢复/手部12项，共61项：60通过、1失败，Console Error/Exception均0。入口横向往返、纵向新旧拼接、左右侧廊、两侧楼梯出口/上段双向、后殿12接缝及新可达两窗均已真实Controller+InputSystem虚拟输入执行。
- 14,400条向下射线覆盖新增144㎡楼板，0缺失支撑。
- 失败是中庭栏杆冲刺跳跃可翻越，角色落至一层；不是楼板穿透。现有护栏约1.23m，不能写成防跌落通过。是否提高可见栏杆正等待用户外观选择，未加隐形高墙或修改跳跃。
- 27窗外侧、27窗内侧及补充角度/四个楼面机位共66张真实离屏截图，运行0错误。外侧27组逐图核对：纹样颜色与铅条恢复、没有白雾遮罩；部分内侧机位被既有书架遮挡，不算完整内侧视觉覆盖。侧窗近景保留局部灯具镜面高光，不将高光误记为白雾。
- 最终DXT1材质四机位各8次、两状态共64次离屏采样，0运行错误。去前2次后中位数（旧白色发光→纹样发光，ms）：内侧14.578→13.430、斜视16.249→17.465、正面外侧34.027→31.680、侧外28.090→27.013。没有一致方向的耗时上升，但顺序/编辑器/GPU读回均有影响，不能推导standalone帧率保证。
- 本轮所有输入由Editor虚拟设备注入，未抢占鼠标/窗口，不是真人验收。耗时包含离屏GPU读回，不能称作M3 standalone帧率。

## C# 修改与验证步骤

- `Assets/Scripts/Visuals/Editor/MilanV6SceneBuilder.cs`：导出Mat新增emissionTexture字段并绑定EmissionMap。
- `Assets/Scripts/Visuals/AcceptanceTestOnly/GlassMaterialReview.cs`：运行时克隆材质/渲染设置、固定相机单变量对比与重复采样；不保存场景修改。
- `Assets/Scripts/Visuals/AcceptanceTestOnly/Editor/GlassReviewLauncher.cs`：静音批处理进入Play，无GameView聚焦。
- `Assets/Scripts/Visuals/AcceptanceTestOnly/Editor/GlassFloorFinalization.cs`：只刷新本工程生成建筑，保留97灯与玩家/UI/环境，配置单张彩窗导入；新构建标识与旧App区分。
- `Assets/Scripts/Visuals/AcceptanceTestOnly/ReviewPlayTests.cs`：增加楼板跨缝/楼梯端点路线和新增楼面14,400点支持扫描。

所有C#修改后已运行Unity6000.3.25f1编译。最早两次A/B脚本假设场景存在Volume导致测试定位Exception，已改按实际场景可选读取；这两次失败不计通过。最终DXT1截图首次启动另遇一次Unity Burst原生编译线程崩溃（FinalVisual03），保留日志；相同设置重试FinalVisual04成功完成66张、0错误，没有禁用Burst。崩溃轮次不计通过。之前A/B、场景刷新与楼面测试均已完成，日志中无C#编译错误和运行Console错误。

重测：设置 `MILAN_REVIEW_SUITE` 指向本轮floor_suite.json，`MILAN_REVIEW_OUTPUT`为新报告目录，`MILAN_FLOOR_REVIEW=1`，用batchmode/noaudio执行 `MilanAcceptance.ReviewPlayLauncher.Run`。玻璃通过`MILAN_GLASS_VIEWS/OUTPUT/VARIANTS`与`MilanAcceptance.GlassReviewLauncher.Run`复测。所有相机和路线JSON随证据一起保留。

## 发布边界

中文README、GitHub发布白名单已准备，未commit/push。用户知识资料业务未新增或冒充实现；原始/上一验收Blender保留。没有触碰日常脏工作树、删除用户文件、恢复旧App或关闭用户Player。

## 新候选运行包

macOS Universal构建成功（arm64/x86_64），0错误、1警告：未登录Unity Cloud，因此未上传原生调试符号。代码签名通过严格校验；这不等于Apple公证。新入口为`Builds/GlassFloor_20261007/MilanLibrary_GlassFloor.app`，显示名Milan Library Glass Floor 20261007，应用标识`com.arcanearchive.milan.glassfloor20261007`，短版本2026.10.7.2。未自动启动它，未测本轮standalone FPS。原项目产品名/版本/应用标识在构建后恢复。

此包用于已修玻璃与入口木地板验收，保留栏杆跳越待定风险，不是全部防跌落验收通过声明。
