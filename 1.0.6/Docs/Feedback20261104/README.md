# 剧情交互与声音反馈更新

2026年11月4日。基于 `art` 的 `0253ec7`，本轮只接入常驻准心、交互可用提示、分类音效和环境声。新三 Boss、多结局与碎片叙事继续作为方案保存；现有剧情、存档结构与单结局不变。

## 试玩入口

使用 Unity 6000.3.25f1 打开 `1.0.6/`，运行 `Assets/Scenes/IMMonsterContest20261107.unity`。本轮交付源码与随源码打包的音频；11月3日的Windows试玩包保留原版本，未重新构建。

## 改动

- 自由探索和力量引导时，屏幕中心持续显示圆点与细环，使用图形绘制和深色描边。可交互时圆环扩大并变亮；目标暂不可操作时使用琥珀色圆环，并显示原因。阅读、菜单及过场使用原有指针/字幕界面。
- 提示与实际操作共用距离、遮挡和剧情条件。已完成封档、已稳定节点、尚未满足条件的门及未携带封档包时的转移箱，均显示相应状态。提示读取当前按键绑定。
- 翻阅与合上档案使用不同纸张声音；系绳、工作牌、节点、门、力量开始/成功/过载、拾取和转移使用分类声音。被拒绝的操作与空按交互键不播放物件成功音。阅读按钮不再叠加通用点击音。
- 22个随包音频覆盖14类动作、7种环境状态及力量循环。环境随开场、灾变、维护厅、节点恢复、返回调查、救援和结尾变化，约1.2秒渐变切换。阅读时降低环境层，暂停冻结循环与渐变，菜单反馈仍可播放。
- 设置增加独立音效与环境音量，并保留主音量。新设置自动保存，非法数值会钳制或恢复默认。

素材、作者、许可及处理步骤见 [AUDIO_CREDITS.md](AUDIO_CREDITS.md) 和 [audio-sources.json](audio-sources.json)。

## 验证

- 修改前：本机Unity完整剧情检查171项通过，游戏运行错误为空，见 [baseline-validation.json](baseline-validation.json)。
- 音频接入后：完整剧情检查171项通过，包含档案核验、暂停、维护往返、两次身份揭示、力量过载/恢复、转移及结尾继续，见 [full-regression.json](full-regression.json)。
- 最终状态：反馈专项105项检查通过，覆盖真实物件音效路由、被拒绝操作无成功音、墙体遮挡、距离、改键、准心像素、全部音频资源信号、暂停中的渐变与循环、独立静音和错误音量数值，见 [all-checks.json](all-checks.json)。
- 测试先复现旧准心与旧音频缺失，再实现；画面复核发现自定义图形缺少CanvasRenderer，增加必需组件后通过中心像素检查。对应预期失败证据保留为 `*-red.json`。
- [常态准心](Captures/01_neutral.png)、[可交互](Captures/02_ready.png)、[暂不可交互](Captures/03_blocked.png)、[设置](Captures/04_settings.png) 已逐图查看。图片来自Unity Play Mode相机输出，捕获时临时将Overlay Canvas接到相机并随后还原；场景文件未改。

上述为macOS编辑器自动走查和画面复核，声音听感仍可通过实际试玩调整。完整流程日志保留有既有Unity Search服务异常及编辑器退出清理信息；游戏运行错误记录为空。本轮未宣称Windows运行包、目标设备性能或首次真人全程验收完成。

复跑命令：`Unity -batchmode -projectPath <1.0.6绝对路径> -executeMethod ArcaneArchive.Visuals.Editor.ContestFeedbackAcceptance.RunAll -logFile <日志>`。完整剧情用 `ArcaneArchive.Visuals.Editor.ContestAcceptance.Run`。自动检查只允许在独立batch工程内运行。

独立审阅未发现本次改动的实质回归；小项与既有路径待复验记录见 [REVIEW.md](REVIEW.md)。

## 主要文件

- `Assets/Scripts/Interaction/ContestInteractionFeedback.cs`：统一剧情交互条件与提示。
- `Assets/Scripts/UI/ContestReticle.cs`：常驻矢量准心。
- `Assets/Scripts/Core/ContestAudioDirector.cs`：分类声音、环境渐变、力量循环与音量总线。
- `Assets/Scripts/Core/ContestStoryGame.cs`：在实际操作与剧情事件接入反馈。
- `Assets/Scripts/UI/ContestHud.cs`：准心、语义化按钮音效和设置。
- `Assets/Scripts/Visuals/Editor/ContestFeedbackAcceptance.cs`：专项运行检查。
- `Assets/Contest20261107/Resources/ContestAudio/`：22个WAV、作者说明与配套meta。
- `Tools/ContestFeedback/prepare_audio.py`：音频准备与校验清单生成脚本。
