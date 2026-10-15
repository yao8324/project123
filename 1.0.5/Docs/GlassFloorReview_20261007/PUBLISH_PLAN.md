# GlassFloor 源码归档范围

目标：原私有仓库 `https://github.com/yao8324/project123.git`，现有 `lixiang` 分支，现有本机 Git 凭据身份 `laozifeiyi`。发布前已通过 GitHub API 验证身份、private、push 权限与基线 `1976c775fba34fa9ee295647ce40dd058a4f50cf`；不会将连接器的其他账号用于写入。

按根 AGENTS/README/ARCHITECTURE 采用新的三段版本目录，完整保留 Assets、Packages、ProjectSettings、Docs、Tools 与重建必需 Blender 文件。继承原 `.gitattributes`，大型源资源使用 Git LFS。代码与场景来自已验证 CandidateProject，发布阶段只更正文档和补足 Texture_Credits、CloudRepair 原生依赖及几何验证输入。

旧版本目录只读；不修改 main、不建 release/glassfloor 分支、不强推、不修改可见性/权限、不修改日常脏工作树。清理集合尚未执行。运行 App/ZIP 不进入普通 Git 或 LFS；GitHub Release 是否另行发布以明确发布记录为准，本文件不表示已上传附件。

原本机运行包和源码 ZIP 的哈希见 DELIVERY.json/source_package.json；这两个文件是验收证据，不是新版本目录逐字节打包声明。源码 ZIP 后补的 CloudRepair、素材声明和版本归档说明以当前 Git 目录为准。验证证据中的“未 commit/push”描述的是验收当时状态，不表示当前远端状态。

本次已确认归档编号为 **1.0.4 候选修订版**。用户在获知栏杆冲刺跳越现状后明确答复“ 不管他，直接推送 ”，接受现状发布；该答复不表示追加几何修复或删除授权。
