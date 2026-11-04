# 剧情交互与环境声来源

本次使用以下作者发布的 CC0 素材，选取纸张、门、机关、室内底声与地下环境等片段，经剪裁、滤波、混合、响度调整及循环接缝处理后接入游戏。魔法反馈还使用 RPG 音效素材。各输出文件的处理记录、SHA256、原始下载链接及来源包校验见 [audio-sources.json](audio-sources.json)。

| 作者 | 素材 | 用途 |
| --- | --- | --- |
| StarNinjas | [10 Book Page Flips](https://opengameart.org/content/10-book-page-flips) | 打开与合上档案；纸张摩擦经低通后用于绳结/布料的轻微摩擦反馈 |
| laleksic | [Various Sound Effects](https://opengameart.org/content/various-sound-effects) | 门、机关、钥匙、箱体、冲击、钟声、环境木材声 |
| rubberduck | [80 CC0 RPG SFX](https://opengameart.org/content/80-cc0-rpg-sfx) | 力量起始、成功与封档包反馈 |
| LEGIT Audio | [The Shop 免费样本](https://opengameart.org/content/the-shop) | 室内空间底声，使用 OpenGameArt 页面提供的免费样本 |
| JaggedStone | [Loopable Dungeon Ambience](https://opengameart.org/content/loopable-dungeon-ambience) | 维护厅、调查及紧张阶段的环境层 |

许可：[CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)。作者未参与本项目制作；游戏鸣谢保留作者名单和来源说明。原有战斗音效及其他资源来源不在本次替换范围内。

复现：按 JSON 中链接下载原包，分别命名为 `book_flips.zip`、`various.zip`、`rpg.zip`、`room.zip` 和 `dungeon.ogg`，执行 `python3 Tools/ContestFeedback/prepare_audio.py --sources <目录>`。需要本机 ffmpeg。游戏运行使用已经随源码提交的 WAV，无需联网或安装音频工具。
