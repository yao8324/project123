# 指南与设置行式布局

修改文件：Assets/Scripts/UI/RecoveryMenu.cs。

探索指南改为八行单项，设置改为三行单项。左侧名称、右侧说明统一对齐，以细金色横线分隔。鼠标悬停时整行暖金底色、左侧金色标记与文字强调，移出恢复。使用统一 InfoRow 构建，不增加业务设置逻辑。

验证：Unity 编译完成；Console Error 为 0；指南八行；悬停进入/离开状态通过；所有显示字符缺字数为 0；布局完成后一号设置行的真实 UI 射线命中为 SettingsRow_0；设置页 Esc 返回菜单，菜单 Esc 恢复探索。Play Mode 检查后已退出。

手动验证：Play → Esc → 操作说明/设置预览，逐行悬停并移出；确认横线、底色和金色标记；按 Esc 返回及关闭菜单。

截图：SettingsRows-Hover.png。
