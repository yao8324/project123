# 1.0.2 版本说明

- **版本类型：** 1.0 系列修订版（城堡内部建模样板）。
- **上一版本：** [1.0.1](../1.0.1/README.md)。
- **工程版本：** Unity `6000.3.24f1`，与上一版相同；用 Unity Hub 打开本目录 `1.0.2/`。
- **本次任务：** 从现有 White Castle 素材制作可继续搭建魔法图书馆的中央大厅挖空样板，保存 Blender 工程、预览和可复现脚本。

## 主要改动

- 从上一版完整复制 Unity 工程及资源、`.meta`、包配置、项目设置和项目规则。原有场景与 C# 脚本未修改。
- 在 `Blender/WhiteCastle_Hollow_Working.blend` 中保留原始城堡主体副本，另建 `WhiteCastle_Hollow_Core`，清出中央约 `76 × 25 × 57` Blender 单位的大厅，并在正面中央切出约 16 单位宽的入口通道。
- 添加 `NEW - library room shell` 集合，包含大厅地面、天花板、内墙及入口上沿，供后续布置书架、楼梯与灯光。
- 将可找到的 FBX 贴图重新链接到本版 `Assets/_WhiteCastle/Textures/` 的相对路径；保存室内、外观及剖视预览。
- 在 `Tools/Blender/` 中附上挖空生成和预览渲染脚本；`.blend` 使用 Git LFS 管理。

## 使用方法

1. 用 Unity Hub 打开 `1.0.2/`，启动场景仍为 `Assets/Scenes/LibraryMain.unity`。本次 Blender 样板尚未接入 Unity 场景。
2. 用 Blender 5.2 或兼容版本打开 [挖空工程](Blender/WhiteCastle_Hollow_Working.blend)。在 3D 视图按数字小键盘 `0` 看内部相机；详细操作见 [Blender 说明](Blender/README.md)。
3. [外观预览](Blender/white_castle_hollow_exterior.png)、[入口视角](Blender/white_castle_hollow_from_door.png)和[大厅剖视](Blender/white_castle_hollow_cutaway.png)可用于检查这次建模范围。
4. 需要重建时，在本版目录运行 Blender 后台脚本 `Tools/Blender/hollow_white_castle_mesh.py`；它读取同版 `Assets/_WhiteCastle/Meshes/Test2.fbx`，不会修改源 FBX。

## 验证结果

- Blender 5.2.1 后台成功从原 FBX 重建、保存并重新打开 `.blend`，生成三张预览图。
- 网格切割前后，对同一大厅采样点向前、向右投射射线：原主体均命中，挖空后的主体均无命中；具体数值见 [检查报告](Blender/white_castle_hollow_report.json)。
- 从城堡正面预览确认外立面仍可辨认；大厅剖视图显示地面和围护面。
- 原 Unity 工程来自远端 `1.0.1` 的完整副本；本次未修改 C# 或 Unity 场景，因此未重新运行 Unity 编译和场景测试。

## 已知问题

- 这是中央大厅的建模样板，不是整座城堡的完整可行走室内。外部装饰网格局部可能伸入大厅，需要后续逐处清理。
- 新建地面和内墙尚无最终石材材质、门框、柱廊、楼梯或碰撞体；模型未导出为新的 FBX，也未放回 Unity 场景。
- 原资源缺少 `Dots2.png` 和 `Wall2.png` 两张 FBX 引用贴图，Blender 材质预览可能仍显示局部缺失。结构预览使用 Workbench 着色，不代表最终渲染效果。
- 上一版的资料导入、书架归档、检索和阅读闭环仍未实现。
