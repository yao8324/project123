# Arcane Archive 魔法图书馆环境 Demo 美术施工方案

> 本文基于 2026-09-20 的只读审计。它是后续场景施工的依据，不执行任何场景、材质、Prefab、脚本或 Package 修改。

## 已确认的工程状态

- Unity：`6000.3.24f1`。
- 渲染管线：URP 17.3.0；当前图形管线资源为 `Assets/Settings/Library3D/Library3DURP.asset`。
- 当前场景：`Assets/Scenes/LibraryMain.unity`。
- 玩家：`Player` 位于约 `(0, 0.05, -10)`，有 `CharacterController` 与第一人称控制组件。
- 保留的行走碰撞：`LibraryRoot/GameplayFoundation/Floor` 与 `LibraryRoot/GameplayFoundation/Ground` 均有 1 个 Collider；`Ground` 同时有 `GroundSurface`。
- 现有场景已有 WhiteCastle 四壁、入口、高窗、柱与檐口，以及 Stylized Library 家具。后续施工应在此基础上增补细节，不能替换或删除已验证的 Floor、GroundSurface、玩家或其功能脚本。

## 素材包职责

| 素材包 | 职责 | 使用原则 |
| --- | --- | --- |
| WhiteCastle | 建筑结构：四周石墙、南入口拱、石柱、高窗、横梁、檐口、火盆、墙面细节 | 仅承担固定建筑与大型结构；为墙、柱、入口添加或保留 Collider。 |
| Stylized Library（URP） | 室内陈设：书架、书本、桌椅、地毯、壁炉、卷轴、地球仪和陈列装饰 | 使用 `Prefabs/URP` 作为默认来源；家具不应缩窄中央主通道。 |

## WhiteCastle 素材审计

| 需求 | 精确路径 | 适用位置与说明 |
| --- | --- | --- |
| 城堡外墙 | `Assets/_WhiteCastle/Prefab/WCArchElemWall_3.prefab` | 四周主体墙；适合连续拼接。`Wall_1`、`Wall_2` 可用于局部矮墙或收边。 |
| 大门或拱门 | `Assets/_WhiteCastle/Prefab/WCArchElemArch_1_Full.prefab` | 南侧入口中心，面向北；未找到命名明确的独立“Door”Prefab。 |
| 石柱 | `Assets/_WhiteCastle/Prefab/WCArchElemCollumn1_Full.prefab` | 四角与北墙壁炉两侧。 |
| 高窗 | `Assets/_WhiteCastle/Prefab/WCArchElemWindow1_Full.prefab` | 北墙三扇高窗；`Window2_Full`、`Window3_Full` 可作为变体。 |
| 横梁 | `Assets/_WhiteCastle/Prefab/WCArchElemBeam_1.prefab` | 天花下方沿 X/Z 轴的木梁或石梁网格；另有 `Beam_2` 至 `Beam_4` 与 `BeamVertical_*`。 |
| 壁炉 | 未找到 WhiteCastle 命名的 Fireplace Prefab | 结构壁炉由北墙、柱和拱件围合；炉体使用 Stylized Library 的 URP 壁炉。 |
| 火盆或火焰 | `Assets/_WhiteCastle/Prefab/WCArchBraizer.prefab` | 南入口两侧、北墙壁炉两侧和书架端头的火盆；素材本身不是粒子火焰。 |
| 地面和天花板结构 | `Assets/_WhiteCastle/Prefab/WCArchElemFlat_1.prefab`、`Assets/_WhiteCastle/Prefab/WCArchElemFlat_2.prefab`、`Assets/_WhiteCastle/Prefab/WCArchElemBeam_1.prefab` | Flat 用于局部石质边框、窗台或吊顶镶板；Beam 用于天花骨架。没有命名明确的整块地板或穹顶 Prefab，因此继续保留现有 Floor/GroundSurface。 |

WhiteCastle 的 110 个 Prefab 均已读取得到受支持的 `Universal Render Pipeline/Lit` Shader；没有发现会在当前 URP 配置下必然呈粉红的 Prefab。

## Stylized Library 素材审计

| 需求 | 精确 URP 路径 | 推荐用途 |
| --- | --- | --- |
| 书架 | `Assets/Stylized_Library/Prefabs/URP/P_Bookcase_01.prefab`；`P_Bookcase_02.prefab`；`P_Shelf.prefab` | 西、东两侧分类书架，朝向中央。 |
| 书籍 | `Assets/Stylized_Library/Prefabs/URP/P_Book_01.prefab` 至 `P_Book_08.prefab`；`P_Stack.prefab` | 书架填充、检索台和上传台装饰；业务书籍数据不硬编码在这些对象上。 |
| 长桌 | `Assets/Stylized_Library/Prefabs/URP/P_Table.prefab` | 两张首尾相接形成中央长桌；上传区、检索台分别使用单桌。 |
| 地毯 | `Assets/Stylized_Library/Prefabs/URP/P_Carpet.prefab` | 中央长桌下方，定义中轴区域。 |
| 椅子 | `Assets/Stylized_Library/Prefabs/URP/P_Armchair.prefab`；`P_Pouf.prefab` | 长桌两端或壁炉阅读角；是装饰与可视化座位，不实现坐下功能。 |
| 卷轴 | `Assets/Stylized_Library/Prefabs/URP/P_Scroll_01.prefab` 至 `P_Scroll_07.prefab` | 上传区、检索台及书架端头。 |
| 地球仪 | `Assets/Stylized_Library/Prefabs/URP/P_Globe.prefab` | 北侧检索台的主识别物。 |
| 灯具 | 未找到命名明确的灯具或吊灯 Prefab | 以 WhiteCastle 火盆、现有 Light 和基础几何吊架替代；后续可加入 URP Point Light。 |
| 魔法或装饰道具 | `Assets/Stylized_Library/Prefabs/URP/P_Clock.prefab`、`P_Compass.prefab`、`P_Gramophone.prefab`、`P_Telescope.prefab`、`P_Pot_01.prefab` 至 `P_Pot_05.prefab` | 作为书桌、壁炉、阅读角的陈设；无现成魔法阵或粒子 Prefab。 |
| 壁炉 | `Assets/Stylized_Library/Prefabs/URP/P_Fireplace.prefab` | 北墙中心，检索台后方。 |

`Assets/Stylized_Library/Prefabs/URP` 与 `Assets/Stylized_Library/Prefabs/Built-In` 均各含 48 个 Prefab。当前逐个读取后，两套的材质均为受支持 Shader，没有发现粉红材质；但后续施工仍统一使用 `URP` 目录，避免来源混用和将来的包更新风险。Built-In 目录只作为资源比对，不纳入施工来源。

## 固定空间布局与建议变换

| 区域 | 推荐对象 | 建议中心坐标 | 建议 Y 旋转 | 施工约束 |
| --- | --- | ---: | ---: | --- |
| 南侧入口与出生点 | `WCArchElemArch_1_Full`、两座 `WCArchBraizer` | 入口 `(0, 0, -13.7)`；玩家 `(0, 0, -10)` | `0°` | 玩家面向北；入口两侧火盆约 `(-2.5, 0, -12.8)`、`(2.5, 0, -12.8)`。 |
| 西侧课程/个人资料架 | `P_Bookcase_01`、`P_Bookcase_02`、`P_Shelf` | `X=-11.5`，Z 为 `-5、0、5` | `90°` | 朝向大厅中心；不得侵入 X=-8 以东的主通道。 |
| 东侧代码/参考资料架 | `P_Bookcase_01`、`P_Bookcase_02`、`P_Shelf` | `X=11.5`，Z 为 `-5、0、5` | `-90°` | 朝向大厅中心；与西侧书架形成清晰对称。 |
| 上传区 | `P_Table`、`P_Suitcase_01`、`P_Stack`、`P_Scroll_01` | `(-7, 0, -4)` | `45°` | 桌前留至少 1.5 米活动空间，作为未来 UploadStation 的落点。 |
| 中央长桌与地毯 | 两张 `P_Table`、`P_Carpet`、`P_Armchair` | `(0, 0, -0.5)` | `0°` | 桌沿南北向布置；两侧椅子不得挡住中轴。 |
| 吊灯与视觉焦点 | WhiteCastle `WCArchElemBeam_1` + 火盆/现有灯光 | `(0, 2, 0)` | `0°` | 仅作视觉焦点；避免把可碰撞几何降到玩家头部。 |
| 检索台 | `P_Table`、`P_Globe`、`P_Scroll_02`、`P_Book_01` | `(0, 0, 7)` | `180°` | 朝向南方入口，成为北侧明确目的地。 |
| 北墙、壁炉和高窗 | `P_Fireplace`、`WCArchElemWindow1_Full`、`WCArchElemCollumn1_Full` | 壁炉 `(0, 0, 12.8)`；高窗 `(-6,0,13.28)`、`(0,0,13.28)`、`(6,0,13.28)` | 壁炉 `180°`；高窗 `180°` | 检索台至壁炉前至少保留 2 米距离。 |

## 建议的场景层级

```text
LibraryRoot
├── GameplayFoundation
│   ├── Floor
│   └── Ground
├── Environment
│   ├── SouthEntrance
│   │   ├── Arch
│   │   └── Braziers
│   ├── WestWall
│   ├── EastWall
│   ├── NorthWall
│   │   ├── FireplaceStructure
│   │   └── Cornice
│   ├── Columns
│   ├── HighWindows
│   └── CeilingBeams
├── LibraryFurniture
│   ├── CoursePersonalShelves
│   ├── CodeReferenceShelves
│   ├── UploadArea
│   ├── CentralTable
│   ├── CentralCarpet
│   ├── ReadingCorner
│   └── SearchStation
├── Lighting
│   ├── DirectionalLight
│   ├── ChandelierLights
│   ├── BrazierLights
│   ├── FireplaceLight
│   └── WindowFillLights
└── _BackupEnvironment
```

## 灯光设计

- 保留低强度的冷色 Directional Light，作为高窗进入大厅的月光或阴天环境光。
- 吊灯区域使用一盏暖色 Point Light，色温建议约 2600–3000K；范围控制在中央地毯与长桌，不照亮全部大厅。
- WhiteCastle 火盆与北侧壁炉使用小范围暖色 Point Light；视觉材质与实际光源位置一致。
- 三扇北侧高窗附近使用低强度冷蓝色补光，形成“冷窗光 + 暖火光”的层次。
- 先控制总光源数量：大厅 1、入口 2、壁炉 1、检索台 1、上传区 1、窗补光 3；如性能或过曝出现问题，优先降低范围、强度与阴影数量，而非额外加灯。

## 风险与替代方案

| 风险或缺口 | 影响 | 替代方案 |
| --- | --- | --- |
| WhiteCastle 没有明确命名的独立门 Prefab | 南入口缺少可开关的门扇 | 使用现有石拱作为开放入口；P0 不实现开门。 |
| WhiteCastle 没有 Fireplace Prefab | 北墙缺少一体化城堡壁炉 | `P_Fireplace.prefab` 嵌入 WhiteCastle 北墙与柱之间。 |
| 没有明确的吊灯、蜡烛、粒子或魔法阵 Prefab | 中央焦点与魔法氛围有限 | 用 `WCArchElemBeam_1`、火盆、Point Light 和现有基础几何吊架组成，粒子留给 P1。 |
| WhiteCastle 与 Stylized Library 画风、比例不同 | 结构与家具可能显得不协调 | WhiteCastle 只放在外围结构；Stylized Library 只放室内近景。先在 Game View 校准家具缩放，不缩放墙模块。 |
| 家具 Collider 可能挡住路径 | 第一人称行走受阻 | 固定建筑和书架保留 Collider；桌椅和纯装饰在测试后再决定是否保留 Collider。 |
| 光源过多或重叠 | URP 下过曝或性能下降 | 采用上述预算；先关闭阴影，再减少灯光范围与强度。 |
| Built-In 与 URP 文件夹并存 | 后续人员可能混用资源 | 场景实例统一从 `Assets/Stylized_Library/Prefabs/URP/` 创建。 |

## 施工前检查清单

1. 保留 `Floor` 与 `GroundSurface`，不改变其碰撞体。
2. 保留玩家出生点 `(0, 0, -10)` 和面向北的方向。
3. 在每次批量实例化后，以第一人称 Play Mode 检查入口、中央通道、书架区、上传区与检索台的可达性。
4. 以 Game View 检查粉红材质、过曝和高窗透光效果；发现异常时停止使用对应 Prefab，并回到本表指定替代方案。
