# 自动入口双开门

2026-10-14，主场景 Assets/Scenes/MilanLibrary_Magic_V6.unity。

## 实现
- 场景新增 Milan Library — Magic V6/ArcaneEntranceDoor，含 LeftHinge、RightHinge。两侧均有实心门板Collider，双面黄铜浮雕。
- 复用 Exterior_V3_DarkHistoricalDoorWood_382 和 V3_ReferenceExterior_V3_AgedDoorBronze_562 的模型与材质，生成独立适配入口的网格。
- Assets/Art/Models/ArchiveEntrance_WoodLeaf.asset、ArchiveEntrance_BronzeRelief.asset。
- 脚本 Assets/Scripts/Interaction/AutomaticEntranceDoor.cs，已显式绑定当前玩家与双门铰链。
- 门口正面/背面5.5米内自动开启，7米外关闭；横向超过5米关闭。垂直范围限定当前大厅楼层。
- 开门1.25秒，关门1.7秒，90度平滑摆动。根据首次靠近方向向远离玩家一侧打开；同次开启期间保持方向，避免切换方向扫过玩家。
- 门口及门扇摆动区域有人时保持开启；关门途中重新靠近立即反向打开；启闭不同距离避免反复开合。

## 验证
- Unity完成编译，Console Error=0。
- Play Mode中调用正式Tick逻辑以0.02秒步长验证：内侧开启90度、外侧开启-90度、离开关闭、门口停留保持、关至70.6%时返回重新打开、两阈值间保持状态，全部通过。
- 真实CharacterController.Move验证：关闭门阻挡；打开后从z=1走至-3，从z=-1走至3，双向通行通过。
- 截图 AutomaticEntrance-Closed.png、AutomaticEntrance-Open.png。
- 人工复验：Play后转身走向中央门，靠近自动开；穿过门走远自动关；重新靠近自动开；站在门洞中观察保持开启。
