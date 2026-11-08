# 1.0.9 发布核验

来源：远端 exe 的 0cb7d51336ccfe7e4e6bcd6b37a0939f9e6531d2（1.0.8），以及用户本轮授权的缚魂者 V3 实现。未采用另一工作区正在制作的骑士改动。

- 244 项新整合 Play Mode 断言通过，Failure: none。覆盖五类小怪、首关实际射线攻击、死亡重试、控制台返回、原玩家恢复、典狱长/圣骑士实际近战碰撞与最终通关。
- 旧版 304 项报告属于历史 1.0.8；新版第一章改用新的战斗测试，断言数量不能直接比较为覆盖退步。
- Windows x64 完整构建成功：0 错误、18 警告，1,189,779,428 字节。18 条均为 Ray Tracing Shader compilation 平台不支持提示；原始日志保存在本地 build-windows.log，摘要见 WindowsBuild.txt。
- 构建含主书本场景和 SoulbinderComplete 场景。未采用只替换单关 exe 的方式。
- 旧 1.0.8 源码与旧完整版目录不修改。旧 ZIP 850,113,179 字节，SHA-256 0e91f676ac60849ad75a4d9b7e73b511fab85f13a7b44f9e1da167424916518c。
- 受控测试不是真人首次通关。Windows 实机、真人难度和长时间性能仍待复验。
- Unity Editor QuickSearch 在 Play Mode 启动时可能出现已知 SearchDatabase 编辑器异常；验收只过滤 UnityEditor.Search 堆栈，战斗期间未记录游戏运行异常。
