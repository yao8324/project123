# 骑士身形与盔甲修订 R10（待最终验收）

依据用户“先调整骑士身形比例还有盔甲”的最新指令，本轮优先修订角色几何轮廓。以下为当前构建值，不代表已达到概念图90%相似度。保留72根骨骼、原骨长和三段动作时序。

| 部位 | 本轮改动 | 当前测量/参数 |
|---|---|---|
| 头盔 | 外壳、内衬和黄金细节一起缩小；保留真实T形开口 | 静止外壳宽0.2408m，高0.3114m，顶点高约2.2821m（含壳厚/倒角） |
| 胸甲 | 从胸到腰逐渐收紧，黄金浮雕与壳体同步变形 | 静止正面胸甲宽0.5805m；此处未把正面半壳深度当作整件厚度 |
| 腰甲 | 重做3层贴身叠片、内衬和细金边 | 静止外包宽0.3679m，深0.2600m，高0.2029m |
| 护颈 | 增加高领护甲，前沿降低留出活动空间 | 名义上缘1.945m，镶边1.946m；不是头颈运动碰撞认证 |
| 髋甲 | 护片及金边同步收窄 | X方向86%，深度90% |
| 大腿甲 | 保留腿骨，减少护甲鼓胀 | 围绕腿中心局部宽82%，深度85% |
| 前臂甲 | 沿肘到腕骨方向建锥形护壳 | 4mm壳厚，宽半径约86→55mm，深半径70→48mm |
| 肘甲 | 金色球状盖改为尖形象牙色护甲 | 4mm壳厚 |

## 输出与流程

- 可重复比例修订输入：Blender/SanctuaryR3/Paladin_R3_Hardware.blend。
- 最终本轮模型：Blender/SanctuaryR3/Paladin_R3_Animated.blend。
- Unity模型：Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx。
- 正面/侧前方检查图：Knight_Body_R10_Front.png、Knight_Body_R10_ThreeQuarter.png。仅拍摄这些图时隐藏翅膀和武器，交付模型仍包含这些部件。

## 修改脚本与验证入口

- Tools/SanctuaryR3/refine_body_proportions.py：本轮身形和盔甲构建；从独立Hardware基线执行，禁止对已变形结果反复缩放。
- Tools/SanctuaryR3/refine_feather_layers.py：只修正上一轮三组短羽的尺寸/位置。
- Tools/SanctuaryR3/refine_wing_hardware.py：保存可重复使用的Hardware基线，支持跳过中间FBX导出。
- Tools/SanctuaryR3/rebuild_character.py：接入完整顺序。
- 验证：check_wing_revision.py、check_wing_body_contact.py、check_cloth_motion.py、check_character_export.py及Unity SanctuaryR3CombatCheck.RefreshAllAndRun。最终结果见Progress.md末尾，不能用游戏逻辑通过替代外观/碰撞验收。

## 后续仍需处理

R11已将肩甲和髋甲改为弧面薄壳，并增加贴合曲面的细金边；R12已重做左手握姿、双手手背甲、护腕叠片及盾牌握把。当前整体预览见Knight_Body_Current_R12.png。膝踝连接、布料纹饰、甲面浮雕密度和全动作交叠仍需后续轮次。腿甲与布料120Hz检查仍有走动2条、冲刺4条交叠记录，跳跃0条；这不是全身无穿插认证。环境与完整交接包的最终整合仍保留在总目标内。

## R12 手甲修订与验收范围

- 左手四指各3段弯曲甲片，配对向拇指、深色掌心和曲面手背甲。
- 双腕各2层叠甲；保留现有骨骼、右手握剑姿态及三段动作。
- 盾牌增加半径18mm皮革握把与两端金属支架；支架接到实际盾面。
- 当前是固定握持手型，尚无独立手指骨骼。
- 三段动作各13个姿态的左手与握把表面检查均为0条交叠记录，仅覆盖该接触对。
- 修改脚本：refine_gauntlets.py、rebuild_character.py；检查脚本check_gauntlet_grip.py；新增独立导出export_character.py和整体预览preview_body_current.py。
- 最终文件仍为Paladin_R3_Animated.blend与同名FBX。Unity导入及运行验证结果以Progress.md最新条目为准。
