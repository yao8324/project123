# 宽幅魔法入口重做

2026-10-14。场景 Assets/Scenes/MilanLibrary_Magic_V6.unity。

## 最终设计
- 中央原约4米入口拓宽至约6.24米，门扇净体约6.1米宽、5米高，外门框约7.7米宽。关闭时双扇轻微搭接，并由双面黄铜压条遮盖中央接缝。
- 深色门体、深青珐琅内板、黄铜构件、双扇完整星盘封印、紫晶符文。所有图案随对应门扇运动，内外双面均有装饰。
- 实测入口回廊底部高5.65米，因此最终门整体Y缩放0.83，门冠最高约5.52米；上方补齐结构，门框两侧柔和暖光。
- 自动门根对象 ArcaneEntranceDoor；旧门重命名为 _Backup_ArcaneEntranceDoor 并停用。
- 改造前完整备份：Assets/Scenes/MilanLibrary_Magic_V6_BeforeGrandEntrance.unity。
- 立面原模型资源未覆盖：中央附近的立面网格使用独立副本拓宽，对应MeshCollider同步换用副本。

## 修改文件
- Assets/Scripts/Visuals/Editor/GrandEntranceBuilder.cs：编辑器生成工具，创建初始宽幅门、独立立面副本和装饰网格；最终回廊高度适配、灯光与上封口在场景中完成。
- Assets/Scripts/Interaction/AutomaticEntranceDoor.cs：90度默认开启角、可配置门扇安全区域；新门设置6米开启、8米关闭，保持平滑双向开关。
- Assets/Art/Models/GrandEntrance/：场景专用模型、金紫及深色材质。
- Assets/Scenes/MilanLibrary_Magic_V6.unity：新门、框、上封口、照明、入口立面与碰撞。

## 验证
- 编译成功，Console Error=0。
- Play Mode调用正式Tick逻辑，20毫秒步长：内侧开启、外侧开启、门口停留不关闭、离开关闭、关闭到70.6%时靠近重新开启，通过。
- 真实CharacterController.Move：X=-2.5、0、2.5三条路径均从内侧Z=1.4穿过至Z=-3.4；中央反向通行通过。
- 关闭状态45条射线覆盖中央接缝、左右边缘、下沿和不同高度，45/45命中新门实体。截图验证内外侧。
- 最终截图：GrandEntrance-Final-Closed.png、GrandEntrance-Final-Exterior.png、GrandEntrance-Final-Open.png。
- 手动验证：进入Play后转向入口，靠近自动开门；分别从通道中央和左右通行；离开8米后确认关闭，观察中央压条和门边无透光缝。
