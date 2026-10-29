# main v0.1.0 运行验收

本目录保存2026-10-29针对实际整合工程运行的证据，历史成员交付记录仍保留在原路径。

- [技能检查](RuntimeAcceptance.json)：既有36项技能与流程断言。
- [动作回归](MotionFlowAcceptance.json)：既有25项动作、切招、胜负和返回断言。
- [编辑器最终状态](FinalEditorValidation.json)：Unity版本、实际起止时间、退出Play Mode、编译/场景状态以及历史记录恢复结果。
- `Captures/Skills/` 和 `Captures/Motion/`：本次主相机渲染，不含Overlay HUD；用于检查新美术与既有战斗共存，不是人工试玩录像。

## 重跑

关闭该工程的其他Unity实例，从工程根`1.0.6/`或替换为它的绝对路径执行：

```bash
"<Unity 6000.3.25f1可执行文件>" -batchmode -projectPath "<1.0.6工程绝对路径>" -executeMethod ArcaneArchive.Visuals.Editor.MainV1AcceptanceBatch.Run -logFile "<日志绝对路径>"
```

不要添加`-quit`：运行器等待Play Mode、两个检查流程及清理完成后自行退出。正常完成退出0，失败退出1并生成`AcceptanceFailure.json`。启动新一次检查会清除本目录上一次生成的结果JSON与截图；README保留。

旧验收器的输出位置硬编码在`Docs/BossDesign/ArcaneEffects20261027`。本入口先将该目录备份到忽略的`Temp/`，运行后把本次结果/截图归档到本目录，再恢复历史文件；普通失败也尝试恢复。若操作系统强制终止进程而未执行清理，应先根据`Temp/MainV1EvidenceBackup-*`恢复历史输出，再开始新一轮。

## 覆盖边界

这些自动用例验证其明确覆盖的战斗和动作路径，不代表所有控制器通行、目标设备性能或完整真人试玩通过。旧HUD/剧情身份泄露、空中暂停、隔墙吞印、近墙发射和墙裙碰撞等已知问题继续由本周任务处理，见[发布已知问题](../../../发布记录/v0.1.0/已知问题.md)。本版没有宣称新的剧情开场已实现，也不含独立运行包。
