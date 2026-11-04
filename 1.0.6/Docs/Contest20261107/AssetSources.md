# 比赛候选素材与AI辅助说明

本说明对应新增的IMMonsterContest20261107比赛场景，不把既有项目或未制作的长篇内容计入新增成果。最终参赛前，负责人仍需按真实名单补齐成员贡献及既有模型、贴图的取得凭证。

| 内容 | 实际来源与处理 | 核查状态 |
| --- | --- | --- |
| 图书馆及原材质贴图 | 本仓库1.0.6的MilanLibraryV6与其既有依赖；沿用木作、黄铜、皮革、纸张及绿灯 | 技术引用已核对；原始取得来源与分发权需持有人补齐，未冒称全部原创 |
| 维护厅与工作台 | 团队art分支制作源SealChamberStudy；按原路径与GUID导入运行工程 | 保留制作来源与历史；不重复导入Milan材质镜像 |
| 覆布遇难者与局部证据 | 本轮新增参数网格、简单局部形体和已有档案、蓝绳、工作牌组合 | 制作代码位于ContestSceneBuilder；无新增外部人物模型 |
| 左掌环纹、回路与转移箱 | 本轮新增网格组合及材质，箱体复用原库皮革黄铜 | 三缺环纹为比赛短篇新增设计，不冒称原长篇既定设定 |
| 界面与剧情代码 | 本轮AI辅助编写，使用已有Unity及Input System接口；原演示场景保留 | 交付附代码文件与实际运行检查，不等于真人试玩 |
| 文本与事件数据 | 用户原故事框架与本轮AI辅助补写的比赛策划、三档案和台词 | StoryContent.json保存正文，不在场景上硬编码书籍信息 |
| 新反馈音和氛围声 | 程序合成波形及固定种子噪声，无外部音乐或录音采样 | ContestStoryGame运行时生成；没有真人配音 |
| 中文界面字体 | Noto Sans CJK SC Regular，Adobe版权声明；官方notofonts/noto-cjk仓库 | 原文件未修改；OFL 1.1与版权NOTICE随字体和运行包提供 |

字体来源：[官方字体文件](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf) · [官方许可](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE)。具体文件SHA256及原字体版权文本见Assets/Contest20261107/Fonts/NOTICE.txt；OFL.txt保存完整许可证。

AI辅助范围包括本次策划补写、事件与字幕整理、运行代码、参数网格、界面及验证工具。没有在本轮生成AI配音、照片或外部风格插画。既有项目中其他AI或第三方参与范围，仍需相应成员按实际记录补充；不将AI工具列为真实学生成员。

本次比赛主线不包含典狱长战斗模型或新人物战斗演出，不用原Boss演示的验证报告冒充比赛流程验收。参赛成员应保留既有基础与赛期新增的对照说明。
