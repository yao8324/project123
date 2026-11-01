# 缄默典狱长原生资源

- `SilentWarden_Combat_v2.blend`：85 根骨骼、12 个网格/材质组、17 段 `AA_` 动作，含刃杖握持与手部骨骼。
- `WardenSpellMeshes.blend`：金紫刻文法环、禁页和扩散冠环。

对应导出 FBX 位于 `Assets/Art/ForbiddenWarden/CombatV2/Models/`，Unity 已使用这些文件，运行时不依赖 Blender。使用 Git LFS 保留原生文件实体。

动作清单、导出检查、资源清理与实际 Unity 验收见 [CombatV2 文档](../../Docs/BossDesign/CombatV2/README.md)。此处是制作源文件，不要移入 Assets 触发自动 `.blend` 导入；修改模型后应明确导出 FBX，再检查单位、骨骼、动作名称和材质引用。
