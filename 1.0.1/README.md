# 1.0.1 版本说明

## 本次修改

### 玩家移动

- 左 Shift 冲刺改为基础移动速度的倍率计算，默认倍率为 1.5。
- 可在 `Player > First Person Controller > Movement > Sprint Multiplier` 中调整冲刺倍率。

### 跳跃与落地

- 落地检测不再要求物体必须挂载 `GroundSurface`。
- 玩家脚下命中正常、非 Trigger 的碰撞体时即可恢复跳跃，因此落到桌面后可以再次跳跃。
- 可通过 `Ground Layers` 控制哪些碰撞层可以作为地面。

### 蹲下

- 按住 C 平滑蹲下，松开 C 站起。
- 蹲下时同步降低 CharacterController 和玩家镜头。
- 头顶存在障碍时不会强行站起，避免穿模。
- 可在 `Player > First Person Controller > Crouch` 中调整蹲下高度、过渡速度和顶棚检测层。

### 输入绑定

- 鼠标滚轮向上：`Previous`。
- 鼠标滚轮向下：`Next`。
- `Previous` 和 `Next` 当前只完成输入绑定，尚未连接具体业务功能。

## 验证建议

1. 在 `LibraryMain` 场景运行游戏。
2. 测试地面起跳、落到桌面后再次起跳。
3. 按住左 Shift，确认移动速度变为普通速度的 1.5 倍。
4. 按住并松开 C，确认玩家可以平滑蹲下和站起。
5. 在桌子或低矮障碍下松开 C，确认空间不足时保持蹲姿。
6. 滚动鼠标滚轮，确认 Input System 能分别触发 `Previous` 和 `Next`。
