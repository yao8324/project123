# White Castle 挖空样板

打开 `WhiteCastle_Hollow_Working.blend`。这是从 `Assets/_WhiteCastle/Meshes/Test2.fbx` 制作的副本，原始 FBX 没有改动。

## 已完成

- 在城堡中央的 `WhiteCastle_Hollow_Core` 中移除内部隔墙和部分厚实结构。
- 在正面外墙之后留出约 76 × 25 × 57 Blender 单位的大厅空间，并通过中央墙体切出宽约 16 单位的入口通道。
- 加入名为 `NEW - library room shell` 的平整地面、内墙和天花板，供后续布置魔法图书馆。
- 在 `SOURCE - original core` 集合里保留未修改的主体，可以随时对照。

## 在 Blender 中查看

1. 双击 `.blend` 打开。Outliner 中的 `WhiteCastle_Hollow_Core` 是处理后的城堡主体。
2. 鼠标放在中间的 3D 视图上，按数字小键盘 `0` 可进入室内预览相机；按数字小键盘 `7` 可俯视，按住鼠标中键拖动可旋转视角。没有数字小键盘时，在顶部菜单选「视图 > 摄像机 > 活动摄像机」。
3. 要从上方看大厅，在 Outliner 中暂时关闭 `NEW - library room shell` 下 `Library ceiling` 的眼睛图标。
4. `white_castle_hollow_cutaway.png` 是只显示大厅结构的剖视示意图。

## 当前限制

这是可继续建模的结构样板，内墙和地面尚未做石材贴图、柱廊、楼梯、门框或碰撞检查。原始城堡的装饰网格仍保留，局部可能与新内墙相交；正式使用前需逐处检查。大部分 FBX 贴图已重新链接到同一工程的 `Assets/_WhiteCastle/Textures/`；原包中缺少 `Dots2.png` 和 `Wall2.png`，材质预览可能仍有缺失。整体模型尚未重新导出或放回 Unity 场景。
