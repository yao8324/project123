# 用户追加：结尾实时过场 · 待验收

交付标识 `PROG-ENDING-20261104` 来自 2026-11-04 用户追加需求和明确提交授权，仅用于跟踪本次交付，不冒充管理者派发的正式周任务编号。目标分支为 program；是否采用及进入 main 由管理者决定。

## 完成什么

玩家将本批遗赠交给安全转移箱后，先保存通关检查点，再自动播放约 26 秒的 Unity 实时过场。封档包落入箱内、箱盖闭合、三缺环封印稳定、环境灯光恢复，镜头抬起回望档案馆；沿用 D022、D023、D024 三段剧情字幕，接入原创程序合成配乐、箱扣和远处撤离脚步。播放完进入结尾界面。

支持 Esc 暂停、设置后继续、跳过、重看结尾和制作鸣谢。跳过与重看不会重复完成剧情事件或生成第二批遗赠。继续已完成存档直接显示结尾界面，可自行重看。

### 两笔提交与依赖来源

1. `feat(story): integrate contest runtime required by ending cutscene`：从 art 提交 `0253ec7f0d733e834fefb7a5719341dd7633574d` 移植 `1.0.6/Assets` 内新增的短篇剧情源码、场景、字体、美术依赖与对应 meta，并保留原候选的工程内说明和历史证据。没有整条合并 art 分支，也没有带入 ArtStudies、场景成员任务目录或仓库外策划文件。原视频留在 art，不重复收录。
2. `feat(ending): add realtime legacy transfer cutscene`：本次结尾动画及交付证据。需要前一笔剧情依赖，不能仅把第二笔套用在尚无短篇剧情的 v0.1.0 上。

主要变更位于：

- `1.0.6/Assets/Contest20261107/Resources/Ending20261104/`：Timeline、四个动画、封印材质与 WAV 音轨及 meta。
- `1.0.6/Assets/Scripts/Visuals/ContestEndingCutscene.cs`：运行时播放、字幕时序、暂停、取消和场景恢复。
- `1.0.6/Assets/Scripts/Core/ContestStoryGame.cs`、`UI/ContestHud.cs`：交付触发、结尾界面、重看和跳过。
- `1.0.6/Assets/Scripts/Visuals/Editor/ContestEndingBuilder.cs`、`ContestEndingAcceptance.cs`：资源生成、无存档预览和专项检查。
- `1.0.6/Assets/Scripts/Visuals/Editor/ContestEndingVideoExporter.cs`：固定时间采样实际 Unity 场景，可导出观看用视频帧；游戏内仍播放实时动画。
- `1.0.6/Assets/Scripts/Core/ContestCheckpointStore.cs`：整合复测发现检查点偶发写入冲突后，保留原子替换并对短时 IOException 做有限重试，失败时保留旧记录；补充真实文件锁检查。
- `1.0.6/Assets/Scripts/Visuals/Editor/ContestAcceptance.cs`、`Core/ContestBuildProbe.cs`：现有流程等待过场完成；编辑器测试隔离输入和存档。
- `1.0.6/.gitattributes`：保留 Unity 空 YAML 字段自带的行尾空格，源码和文档仍做严格空白检查；原有 LFS 规则保留。

## 怎么验证

Unity **6000.3.25f1** 打开仓库的 `1.0.6/`，打开 `Assets/Scenes/IMMonsterContest20261107.unity` 并运行。正常完成故事、力量引导和取包，再对转移箱按当前交互键，即会进入过场。检查 Esc 暂停与继续、右下角跳过、结尾“重看结尾”、鸣谢返回、主菜单继续及新游戏。

若只查看动画，使用 `IM Monster → Ending → Preview ending (no save)`。预览构造内存状态，不写入玩家正常存档。

已在从远端 program 建立的隔离工作区重新导入、编译并运行检查：编译错误 0，最终结尾专项复测 51 项通过，最终完整流程 170 项通过，游戏运行错误 0。最终报告与关键截图放在 [EndingDelivery20261104](../../../1.0.6/Docs/Contest20261107/EndingDelivery20261104/)，原始临时日志和逐帧录屏不提交。

## 还有什么问题

- 本次为待验收的源码候选，没有重新构建独立 Windows 运行包，旧包不包含新增动画。
- 本轮 Unity 启动有 1 条已记录的 Editor.Search 索引异常，未阻断游戏验证；完整报告中与游戏错误分开保留，不能声称整个 Console 零错误。
- 音频文件采样、时间线绑定和播放状态做自动检查；自动会话没有取得有效混音采样，实际听感仍需人工试听。
- 仍需首次接触游戏的玩家完整试玩；本次不代表目标设备性能、旧 Boss 战全部缺陷或旧周任务所有工作包验收完成。
- art 来源的 2026-11-03 报告是历史证据，不能代替本次 program 隔离整合验证。资源原始来源仍见候选的 AssetSources 说明。
