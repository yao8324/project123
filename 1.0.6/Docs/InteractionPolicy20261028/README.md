# 书籍入口、暂停和固定按键 · 2026-10-28

依用户选择修改当前 1.0.6：只有大厅与地球仪相平的《星图与魔法笔记》打开独立战斗页；书柜两本书只显示各自介绍。主菜单移除“深入探索”。暂停冻结玩家运动与战斗。鼠标左右键固定，换绑页只显示且不可点击。

修改脚本：DemoBookRecord.cs、DemoBookTarget.cs、RecoveryMenu.cs、FirstPersonController.cs、LibraryInputBindings.cs；修改三份 BookPreview 资产及两个旧战斗验收入口。添加 InteractionChineseSupplement 静态字形，BossChineseSDF 引用其作为 fallback。

历史 `Acceptance.json` 保存 42 条通过记录，但 complete=false，不能当作完整自动验收。之后按用户要求只进行最小检查。2026-10-29 已清理临时验收脚本；本次剑盾检查确认编译与 Console 无错误，并验证属性页暂停时角色位置、战斗时钟冻结，详见 ../MeleeCombat20261029/README.md。没有运行全面回归或独立构建。

10 月 29 日左键已改为装备剑盾的近战连斩，右键增加近战弹反，固定不可换绑的要求保持。
