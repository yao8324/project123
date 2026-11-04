# 当前目标逐项验收

目标：以已有概念图为参考继续搭建斗场，接入锁链/水晶可攻击、减伤、回血、全破僵直，重新展示概念图。法杖音效后续由用户选择，不是机制完成的前置条件。

|要求|权威证据|结论|
|---|---|---|
|大型开放Boss斗场、中央无大遮挡|SealedDuelArena.prefab；CrystalStage/verification.json中72条直径胶囊扫掠；NativeFinal运行截图|已由NativeFinal/01_intact_native.png确认|
|概念方向：双层哥特拱廊、破顶星空、暗材质、外围三晶|SealedDuelArenaBuilder的Walls/Gate/Vault/BuildStarSky；现有建筑证据；最终运行截图|已核对建筑源码、星空证据与最终运行截图|
|悬浮盘旋实体锁链与上升紫符文|CrystalArenaBuilder.MakeAssembly与CrystalArenaVisuals.Animate；近景渲染|已实现|
|先打断链再破晶，同一击不穿透两层|BossPowerNetwork.Hit；runtime-acceptance的实际弹体与防溢出断言；原生输入测试|最终运行重新通过|
|每颗10个百分点减伤，破晶逐颗扣除|BossPowerNetwork.DamageReduction与BossEncounter.DamageBoss；实际伤害100→70/80/90/100断言|已通过|
|持续回血且随活晶数减少|Regeneration与TickCrystalRegeneration；3/2/1/0份回血、封顶、暂停/死亡断言|已通过|
|全部破碎一次僵直，当前攻击中断|OnCrystalPowerExhausted、Stagger；时长、一次性、阶段切换、死亡优先断言|已通过|
|破晶对应紫光、光束和共鸣消失，暗残骸出现|ApplyState、BreakCrystal；原生25项检查；底座符文新增归属断言|最终包已确认，底座熄灭近景及全破截图一致|
|重试/退出恢复正确，无音轨残留|原生输入测试及返回/重进测试|最终运行重新通过|
|概念图重新发在聊天|本轮已view_image展示，最终答复再次嵌入原概念图|本轮已再次展示原概念图，最终答复附原图|

范围说明：艺术目标采用概念图的构图、空间、材质方向与链晶交互视觉，不宣称与AI概念图逐像素一致。待用户试听的法杖音效不擅自替换；19项既有选择已经按备注配置。

最终审查：Mac构建成功、6项视觉状态断言、72条中央胶囊扫掠、35项玩法断言及25项原生检查通过。NativeFinal错误数组为空。Unity编辑器搜索索引异常已单列于Handoff，未当作游戏错误或掩盖为全无日志错误。
