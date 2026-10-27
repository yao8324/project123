# 用户馆藏 UI 与接入逻辑

## 馆藏的含义

馆藏是用户成功导入的本地文件集合。每份文件对应一条 KnowledgeBook 记录和一本场景书籍。装饰书、DemoBookRecord 演示数据和场景里已有的魔法书都不能自动成为用户馆藏。导入服务尚未接入时，界面显示 0 份、馆藏为空，不生成虚构记录。

## 视觉和导航

我的馆藏使用与探索指南、设置相同的细金色横线、逐行布局、名称与说明对齐、悬停底色及左侧金色标记。顶部显示成功导入的记录总数。导入资料和检索馆藏是可点击的整行入口，当前只打开流程预览。

列表列为标题/原文件名、格式、类别、书架位置。接入真实数据后，每条文件占一行；列表超出可见区域时滚动，长文件名截断并在详情完整显示。空列表不显示假书籍，也不提供无对象的阅读或定位按钮。分类与搜索无匹配时显示无结果，而不把数据库清空。

点击真实记录行打开详情；详情展示标题、原文件名、类型、类别、关键词、导入时间和归档位置，提供阅读/打开文件、定位书籍动作。读取成功与文件缺失、损坏、无系统关联程序的失败状态必须区分。文件缺失时保留元数据和清晰提示，不能显示打开成功。

Esc：馆藏返回主菜单；导入/检索返回馆藏；关闭主菜单恢复漫游。打开任何主面板时暂停玩家移动和视角并释放鼠标。所有页面使用现有统一 RecoveryMenu 状态管理，不增加另一套 Esc 监听器。

## 后续数据接入契约（本次未实现）

LibraryDatabase 是唯一的馆藏数据来源。UI、搜索结果和 BookView 均引用 KnowledgeBook.id；UI 不遍历场景装饰物推导馆藏，不从 demoBooks 生成用户列表。馆藏数来自成功提交的数据库记录。

KnowledgeBook 字段：id、title、sourceFileName、storedFilePath、fileType、category、keywords、importedAt、pageCount、summary、shelfId、shelfSlotIndex、coverColor、contentPreview。未解析内容时 pageCount/summary/contentPreview 可以为空或未知，不伪造解析结果。

导入顺序：选择支持的本地文件 → 校验可读性和格式 → 编辑标题/类别/关键词 → 查询书架空槽 → 将文件复制到 Application.persistentDataPath 下以唯一 id 命名的存储位置 → 保存 JSON 元数据 → 发布馆藏记录并生成 BookView。采用临时文件和可回滚提交，复制/保存/归档失败不增加计数、不留下半成功书籍；取消不修改数据库。书架无空槽时明确失败或待归档状态，不能伪造 shelfSlotIndex。重名文件不能覆盖：显示原始名称，以 id 区分存储，重复导入策略在正式接入时明确。

BookShelf 管理类别和空槽；BookView 仅呈现记录。列表定位与检索定位使用同一 id 高亮目标书籍和书架。重新启动从 JSON 恢复记录与槽位关系。

检索仅查询已导入记录的 title、sourceFileName、keywords，采用不区分大小写的模糊匹配，结果回到 KnowledgeBook。P0 不承诺对文件全文或 Office/PDF 内容进行搜索，不做 AI/RAG。

格式策略：TXT、Markdown 在 Unity 阅读面板显示；图片在 Unity 内预览；PDF、Word、PPT 保存元数据并调用系统默认程序打开。本次只展示规则，未增加文件选择、复制、解析、读取、搜索、书籍生成或磁盘写入业务。

## 本次变更与验证

RecoveryMenu.cs：移除馆藏页三张示例卡片，重做馆藏、导入预览、检索预览；统一复用 InfoRow 悬停与整行点击。
ArchiveChineseSDF.asset：烘焙新增中文，保留旧文字，保持 Static 字体资产，不依赖运行时系统字体。

手动验证：Play → Esc → 馆藏界面预览；确认 0 份和空状态、没有示例卡片；悬停两条入口后点击；导入页选择文件按钮应不可用；Esc 返回馆藏；检索页明确空馆藏和待接入；Esc 返回菜单再关闭，恢复移动。场景演示书的 E 交互仍可打开原有演示详情，但不增加用户馆藏数。

## 实测结果

Unity 编译完成，Console Error/Warning 为 0。馆藏页截图检查通过；馆藏、导入、检索三个页面缺字数为 0，文本溢出数为 0；字体保持 Static 且源字体引用为空。两条入口悬停进入/离开通过；点击导入进入 Upload，文件选择按钮禁用；Esc 返回 Gallery；点击检索进入 Search；Esc 返回馆藏、菜单，再恢复 Explore。测试后退出 Play Mode。截图：Docs/UIReview/Collection-Empty.png。
