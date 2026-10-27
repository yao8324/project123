# 可用画质与鼠标灵敏度设置

修改：Assets/Scripts/UI/RecoveryMenu.cs、Assets/Scripts/Player/FirstPersonController.cs、Assets/UI/Fonts/ArchiveChineseSDF.asset。

设置页显示可点击的均衡（F8）、高画质（F9），调用现有 MilanQualityOptions.Select，不增加第二套快捷键逻辑。选中项使用金色底色和亮色文字，现有 F8/F9 切换后，打开的设置页同步显示实际 QualitySettings 级别。默认仍是高画质。

灵敏度滑条范围 0.25～3.00x，以原 0.08 的 mouseSensitivity 为 1.00x。通过 FirstPersonController.SetMouseSensitivityMultiplier 更新实际 UpdateLook 使用的参数。数值限制范围并拒绝 NaN/Infinity。打开 UI 期间玩家移动/视角仍被禁用，关闭后使用调整后的灵敏度。调整在本次运行内生效，尚未增加跨运行的配置保存。音量保持待接入。

验证：编译完成。UI PointerClick 切换 Balanced/Ultra 成功，实际 URP renderScale 分别为 0.85 和 1.0；滑条 PointerDown/Drag 命中 MouseSensitivity，实际玩家倍数更新为 2.512097，恢复 1.00x 后重新打开设置显示 1.00x。字体缺字和溢出均为 0。重新进入 Play Mode 后完成正常 UI 测试，Console Error/Warning 为 0。

测试过程尝试在暂停的编辑器注入虚拟键鼠，未取得有效输入读数，设备增删产生 InputAction 内部诊断；未将其作为硬件键鼠通过的证据。虚拟设备已移除并退出该次 Play，清理测试日志后重新进入 Play，正常 UI 测试没有再产生这些诊断。

手动验证：Play → Esc → 设置预览；点击均衡（F8）或高画质（F9）观察选中状态；也可按 F8/F9 检查快捷键同步。拖动灵敏度到 0.50x，关闭菜单移动鼠标，再调整到 2.00x 比较相同鼠标位移的转向幅度；打开 UI 时视角应保持不动。停止 Play 后设置恢复项目默认。

截图：Settings-Functional-Final.png。
