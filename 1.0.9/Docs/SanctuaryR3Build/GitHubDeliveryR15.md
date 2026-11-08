# GitHub 试玩交付记录

核验时间：2026-11-07T23:34:56+08:00。

[打开 R15 预发布下载页](https://github.com/yao8324/project123/releases/tag/knight-sanctuary-playtest-20261107)

下载版本标签：`knight-sanctuary-playtest-20261107`，对应提交 `5a40a02c382c9f6c7ef71cfa56b7b0b64e714bd1`。发布状态已确认：非草稿、预发布。

## 下载文件

| 文件 | 字节数 | SHA256 |
|---|---:|---|
| [KnightSanctuary-R15-macOS.zip](https://github.com/yao8324/project123/releases/download/knight-sanctuary-playtest-20261107/KnightSanctuary-R15-macOS.zip) | 819390554 | `557da33227d1cb4d60a9d4c8669bf964b46d37d0c1471bd7cc3cac5e91bc9d0c` |
| [KnightSanctuary-R15-UnityProject.zip](https://github.com/yao8324/project123/releases/download/knight-sanctuary-playtest-20261107/KnightSanctuary-R15-UnityProject.zip) | 1387685156 | `750d68342ff4d7af29500beb021286bd6fe8d9238c0c0f740996d6ebad2afc06` |
| [KnightSanctuary-R15-DesignSources.zip](https://github.com/yao8324/project123/releases/download/knight-sanctuary-playtest-20261107/KnightSanctuary-R15-DesignSources.zip) | 1430193202 | `906e087832d29ba6c4da6f5f7d895cb5a54732ac832c8619eb7aa7bf8b6706fa` |

另附 README-R15.md、ReleaseManifest.json 和 SHA256SUMS.txt。三份 ZIP 的远端大小及 GitHub 返回的 SHA256 与本地已校验包一致。

Mac 试玩下载 macOS 包即可；两个源码包解压到同一父目录、合并 `1.0.8` 文件夹，包含实际 LFS 模型内容。不要把 GitHub 自动生成的 Source code ZIP 当作这些完整源码包的替代品。

## 开发分支与后续修订

[当前开发分支](https://github.com/yao8324/project123/tree/codex/knight-sanctuary-20261107) 已另外包含 R16/R17 的盔甲、头盔和披布修订；本次交付记录前的开发提交为 `cc8d6166d5964deff0f3d92a9e56b886c4a4eb3f`。这些后续模型修改通过了 Unity 受控检查，尚未包含在 R15 Mac 与源码 ZIP 中。

R15 是已运行的 Mac 开发试玩版，没有 Windows 运行包。R17 的完整人工通关、独立 Mac 重打包、持续性能和最终美术相似度验收仍未完成。三个 Boss 的剧情串联由整合同学负责。本次未合并 main。
