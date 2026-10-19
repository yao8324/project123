# Stage 0 3D URP 基础配置

## 目标

将项目从 Universal 2D 模板的初始状态调整为可承载 Arcane Archive 魔法图书馆第一人称 3D 开发的 URP 基础工程，同时保留原始 2D 场景和配置资产作为回退参考。

## 原始配置

- Unity 版本：6000.3.24f1。
- 原始模板：Universal 2D。
- 原始 URP 配置：`Assets/Settings/UniversalRP.asset` 使用 `Assets/Settings/Renderer2D.asset`。
- 原始场景：`Assets/Scenes/SampleScene.unity`，含正交 Main Camera 与 Global Light 2D。

## 本次调整

- 新建 `Assets/Settings/Library3D/Library3DURP.asset`，作为 3D URP 管线资产。
- 新建 `Assets/Settings/Library3D/Library3DRenderer.asset`，使用 URP 的 `UniversalRenderer`，不再以仅适用于 2D 灯光与 Sprite 的 Renderer2D 作为项目渲染器。
- 将 Graphics Settings 与全部六个 Quality 档位指向 `Library3DURP.asset`，避免质量切换后回退到 2D Renderer。
- 新建 `Assets/Scenes/LibraryMain.unity`，且场景根对象仅有：
  - `Main Camera`：透视投影，FOV 60。
  - `Directional Light`：启用阴影的平行光。
  - `Ground`：50 x 50 的基础平面。
- 使用内置天空盒和基础环境光作为初始环境。
- Build Settings 仅启用 `Assets/Scenes/LibraryMain.unity`；`SampleScene.unity` 保留在项目中，未删除也未改写。

## 验证结果

- Unity 已完成资源导入，编辑器状态为未编译、未进行资源更新，可继续操作。
- 运行时核验结果：`pipeline=Library3DURP`、`renderer=UniversalRenderer`、`orthographic=False`、`light=Directional`。
- Build Settings 核验结果：启用场景数为 1，场景为 `Assets/Scenes/LibraryMain.unity`。
- 场景层级核验结果：仅有 Main Camera、Directional Light、Ground 三个根对象。
- Unity Console Error：0。
- 已通过 Main Camera 截图确认天空盒、地面和透视视角可正常渲染。验证截图仅写入忽略的临时路径：`Temp/Stage0Verification/Stage0RenderSetupVerification.png`。

## 未包含内容

本阶段未创建玩家系统、书架、UI、导入功能、业务脚本或美术资源；未安装或删除任何 Package。
