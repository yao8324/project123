# 默认高画质

2026-10-13，按用户要求将 1.0.5 默认画质改为 Ultra。

## 修改文件

- Assets/Scripts/Visuals/MilanQualityOptions.cs：无启动参数时使用 Ultra，保留显式 Balanced 参数及 F8/F9。
- Assets/Scripts/Visuals/Editor/MilanPerformanceSetup.cs：重建配置默认选择 Ultra，避免后续重建恢复为 Balanced。
- ProjectSettings/QualitySettings.asset：当前质量与 Standalone 默认均为 Ultra（索引 6）。
- ProjectSettings/GraphicsSettings.asset：默认渲染管线指向现有 MilanV6_URP 高画质资产。
- README.md：新增 1.0.5 当前默认画质说明，保留 1.0.4 历史记录。

## 验证

Unity 编译与资源导入完成，Console Error=0。Play Mode 启动实际档位为 Ultra，管线 renderScale=1；依次调用现有 Select("Balanced")、Select("Ultra") 均正确切换。测试后退出 Play Mode。

手工验证：打开 MilanLibrary_Magic_V6 并 Play，默认高画质；F8 切均衡，F9 切回高画质。没有重新构建独立运行包。
