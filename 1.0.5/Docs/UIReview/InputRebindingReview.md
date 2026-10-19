# 探索指南与按键换绑

指南左侧为操作名称，右侧为实际绑定，保留细横线分隔和悬停反馈。移动的前/后/左/右分别换绑；冲刺、跳跃、蹲下、查看演示书籍、菜单、重新锁定鼠标均可点击换绑。鼠标环视是连续轴输入，通过设置页调整灵敏度，不作为按键替换。

## 操作

Play → Esc → 操作说明 → 点击右侧按键 → 按下新键。Esc 取消，取消或冲突保留旧键。同一时刻只有一个换绑操作，目标 InputAction 在等待时禁用，结束后恢复。换绑键在完成这一帧不会同时触发菜单等世界行为。退出页面/关闭菜单/退出运行会取消未完成换绑。

同键冲突被拒绝。Esc 为取消与菜单安全返回键，F8/F9 为画质快捷键，方向键为保留的移动备用键；它们不能分配给其他操作。菜单可绑定额外主键，Esc 始终保留返回能力。恢复默认按键恢复所有可调整项。

## 输入与存储

新增 Assets/Scripts/Core/LibraryInputBindings.cs 及其 meta。玩家在运行时克隆 InputSystem_Actions，并初始化统一服务，不修改原始 inputactions 资源。玩家移动、冲刺、跳跃、蹲下读取克隆后的动作；菜单/重新锁定/查看书籍使用同一服务中的按钮动作。书籍提示按当前 Interact 绑定显示，不再固定 E。模板原 Interact 有 Hold，本 Demo 使用新按钮动作 ArchiveInteract 保持单次按下查看，不增加 Hold 要求。

配置以稳定操作 id 与 control path 保存到 Application.persistentDataPath/input-bindings.json，使用临时文件后替换，不写入 Assets。重新运行加载并校验完整布局，包括用户经临时键完成的两键互换。文件非法时回到默认并记录警告。原输入资源和场景资产无需变更。

## 修改文件

- Assets/Scripts/Core/LibraryInputBindings.cs（新增）
- Assets/Scripts/Player/FirstPersonController.cs
- Assets/Scripts/UI/RecoveryMenu.cs
- Assets/Scripts/Interaction/BookPreviewInteractor.cs
- Assets/Scripts/Interaction/DemoBookTarget.cs
- Assets/UI/Fonts/ArchiveChineseSDF.asset（补齐新增字）

## 验证

编译完成；最终 Console Error/Warning 为 0。指南截图检查通过，最终缺字/文字溢出均为 0。

UI PointerClick 启动 Jump 换绑，原动作暂时禁用。通过原生 RebindingOperation 的候选与 Complete 流程选定 J，实际 Jump controls 为 /Keyboard/j，原 Space 不再属于键盘跳跃动作；动作恢复启用。重复 C 被拒绝并保留 J，F8 被拒绝。取消 Sprint 后恢复启用。重新初始化服务从 JSON 加载到 J，恢复默认回到 Space。测试后恢复原配置文件；初次无配置时留下空覆盖的默认配置。

前进改 U 后 Move controls 包含 U、不含 W，方向键备用绑定仍保留；交互改 I 后书籍提示包含 [I]；菜单改 Tab 后按钮显示实际绑定，并保留 Esc。测试后均回到原默认。以上为 Play Mode 中的 UI、原生换绑完成、动作绑定及配置验证；没有把程序选择候选视为真人物理按键操作测试。

手工回归：跳跃改 J，退出 UI 后在地面按 J，应跳跃，Space 不应再跳；冲刺改其他键检查速度；前进改 U 检查 U/W；交互改 I 检查提示及开卡；菜单改 Tab 检查 Tab 开关及 Esc 返回；Esc 取消换绑；选择冲突键应保留旧键；停止并重新 Play 检查配置保留；恢复默认检查所有默认操作。

截图：Guide-Rebind-Final.png。
