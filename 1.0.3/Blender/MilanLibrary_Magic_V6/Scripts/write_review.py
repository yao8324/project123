"""Generate a local review page and factual delivery notes from the actual audit."""
from pathlib import Path
import json,html
P=Path(__file__).resolve().parents[1]
r=json.loads((P/'Audit/geometry_validation.json').read_text())
reopen=json.loads((P/'Audit/fresh_reopen.json').read_text())
modules=json.loads((P/'Audit/module_append_validation.json').read_text())
views=[('10_Final_Overview','扩大后的知识上传与检索区','入口中轴视角。柜台宽度增加 22%、深度增加 60%，书本、天球及其底座与光环统一放大 1.7 倍。'),
('11_Final_Workzones','两侧工作区与陈设','原木柜体、绿玻璃阅读灯、参考古籍与羽毛笔组成完整工作台。旧柜台与旧装置已实际删除。'),
('12_Final_Upload','知识上传 · 悬浮古籍','放大的翻页书、金色符印与细碎星尘。保留做旧木作和黄铜层次。'),
('13_Final_Search','知识检索 · 星轨天球','完整装置等比例放大，实体黄铜环与蓝色星轨共同旋转。'),
('14_Final_ShelfMagic','书架顶端 · 悬浮典藏','8 本展示藏书分布于双面架上方，带轻微起伏、旋转、光弧与星尘。'),
('15_Final_BookSpines','藏书细节 · 发光书脊','752 本原有藏书增加细小符文与呼吸微光，原有书本定位保持不变。'),
('16_Final_Entrance','入口陈设 · 资料整理与等待阅读','原有桌椅、绿玻璃灯与书堆重新布置在入口两侧，补充旧装置移除后的空间。'),
('17_Final_Emblem','Maynooth · 贴墙金边校标','矩形白底已移除。原校标与 Maynooth 字样顺着后殿弧面安装，保留四色纹章，使用双层金色盾形镶边和薄浮雕金字。')]
figures=''.join(f'<figure id="view{i}"><a href="Evidence/{f}.png"><img src="Evidence/{f}.png" alt="{title}" loading="{"eager" if i==0 else "lazy"}" width="1800" height="1200"></a><figcaption><b>{i+1:02d} · {title}</b><p>{text}</p></figcaption></figure>' for i,(f,title,text) in enumerate(views))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>魔法图书馆 V6 · 实景验收</title>
<style>*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#101815;color:#e7e1d1;font-family:-apple-system,BlinkMacSystemFont,"PingFang SC",sans-serif;line-height:1.7}main{max-width:1320px;margin:auto;padding:44px 24px}header{padding:0 0 28px}small{letter-spacing:.18em;color:#b6a574}h1{font-size:clamp(27px,4vw,46px);font-weight:580;margin:12px 0}p{color:#c5c9bd}a{color:#e3c588;text-decoration:none}a:hover{text-decoration:underline}.metrics{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:28px 0}.metric{border:1px solid #3a463b;padding:15px;border-radius:8px}.metric b{display:block;font-size:25px;color:#e7d3a1}.metric span{font-size:14px;color:#b9c2b6}nav{display:flex;gap:18px;flex-wrap:wrap;margin:14px 0}figure{margin:28px 0 38px;border:1px solid #3a463b;border-radius:10px;overflow:hidden;background:#18211c}img{display:block;width:100%;height:auto}figcaption{padding:17px 22px}figcaption b{font-size:19px}figcaption p{margin:5px 0}.notes{background:#1a251f;border:1px solid #3a463b;border-radius:10px;padding:25px;margin:26px 0}.notes li{margin:8px 0}.files{display:flex;gap:12px;flex-wrap:wrap}.files a{border:1px solid #667158;border-radius:6px;padding:9px 14px}footer{font-size:13px;color:#a6b1a3;padding:24px 0}@media(max-width:650px){main{padding:24px 14px}.metrics{grid-template-columns:repeat(2,1fr)}figcaption{padding:14px}}</style>
<main><header><small>MILAN MAGIC LIBRARY / V6 / BLENDER</small><h1>扩展工作区 · 魔法藏书 · 金边校标</h1><p>本页全部图像均由交付的 Blender 场景直接渲染。1800 × 1200，Cycles，64 采样，帧 35。</p>
<div class="metrics"><div class="metric"><b>5.26 × 4.59 m</b><span>每侧柜台包围尺寸 · 高 1.066 m</span></div><div class="metric"><b>1.7×</b><span>魔法书与天球统一比例</span></div><div class="metric"><b>80 / 752</b><span>顶冠纹章 / 发光书脊</span></div><div class="metric"><b>8 本</b><span>新增悬浮展示藏书</span></div></div>
<nav><a href="#view0">整体</a><a href="#view2">上传区</a><a href="#view3">检索区</a><a href="#view4">书架魔法</a><a href="#view7">金边校标</a><a href="#checks">验证与文件</a></nav></header>'''+figures+'''
<section class="notes" id="checks"><h2>场景与验证</h2><ul>
<li>旧版两套柜台、旧书本、旧天球和遗留灯光共 190 个对象已从 V6 原生文件移除；原 V5 与修改前快照保留。</li>
<li>柜台占地面积扩大为原来的 1.952 倍。入口资料整理桌和灯具保留并重新布置；新增阅读灯、旧皮革参考书、墨水座和羽毛笔。</li>
<li>中央 6 米通道、柜台前方 1.8 米操作区和后方 1.8 米工作人员通路通过本次几何检查。</li>
<li>72 组书架、10,688 个槽位标记和 14,336 个预览书对象保持原定位；新增悬浮书属于展示道具，不计入藏书容量。</li>
<li>按最新要求移除了校标矩形白底，原校标改为贴合弧面的金边效果。保护范围内 50 个院标对象按授权修改，其余 783 个对象签名保持一致；外观主体未改变。</li>
<li>10 秒原生循环动画：24 fps，1–240 帧；对 9 个时间点采样，驱动无失效，帧 1 与 241 循环接点吻合。</li>
<li>完整文件已用独立 Blender 进程重开，三份原生模块包也已在空场景中追加验证。贴图打包在文件内。</li>
</ul><p>验证范围：静态几何包围盒检查、合并符文实际顶点检查、变换签名与动画采样。未进行 Unity 导入、导航烘焙、物理仿真或目标设备性能测试。材质、灯光及辉光以本页 Cycles 实景为准，实体/材质预览模式显示会有差别。</p></section>
<section class="notes"><h2>可编辑文件</h2><div class="files"><a href="MilanLibrary_Magic_V6_Assembly.blend">完整 Blender 模型</a><a href="Exports/V6_Updated_Workstations.blend">新版工作区模块</a><a href="Exports/V6_ShelfMagic_Overlay.blend">书架魔法叠加模块</a><a href="Exports/V6_DirectMountedEmblem.blend">贴墙金边校标模块</a><a href="Validation_Report.txt">验证报告</a><a href="full_scene_inventory.csv">全场景清单</a><a href="START_HERE.txt">打开说明</a></div><p>模块保留整馆世界坐标；完整模型已经包含这些模块。书架魔法包为叠加构件，基础书架在完整场景内。</p></section>
<footer>2026-10-03 · V6 实际场景交付 · 原始 V5 版本保留 · 木纹贴图来源与许可见 Texture_Credits.txt</footer></main></html>'''
(P/'Review.html').write_text(page)
report=f'''魔法图书馆 V6｜实际验证报告
日期：2026-10-03

结果：几何与保留项验证 {r['pass']}；原生文件独立重开 {reopen['pass']}；原生模块空场景追加 {modules['pass']}。

一、实际改动
- 实际删除旧对象 {r['removed_obsolete_object_count']} 个，残留名称 {r['obsolete_objects_still_present']}。
- 两侧柜台由约 4.309 × 2.866 米扩大到 5.257 × 4.586 米，高 1.066 米；宽 ×1.22、深 ×1.60、平面面积 ×1.952。
- 原生魔法装置统一 ×1.7；{r['magic_transform_comparisons']} 个对象世界矩阵比较，最大误差 {r['magic_transform_max_error']:.9g}。
- 柜台支撑体、皮革台面、雕刻和 26 个碰撞代理同步变换；两侧工作区各配置两盏绿玻璃阅读灯、参考古籍、墨水座与羽毛笔。
- 原有阅读桌椅、书堆和灯具保留。原第二组阅读桌布置到入口旧柜台区域，第一组向后移动 1.9 米，给工作人员留出空间。
- 72 组书架覆盖 80 个顶冠纹章，752 个既有预览书对象增加发光书脊；8 本悬浮展示书（2 本翻页、6 本闭合）。

二、保留与布局
- 原 V5 文件 SHA256 一致：{r['source_V5_sha256_unchanged']}。
- 外部建筑与院标保护范围共 {r['protected_exterior_and_emblem_objects']} 个对象；按最新用户要求修改 {len(r['authorized_emblem_changes'])} 个院标对象，另 {r['unchanged_protected_objects']} 个对象签名保持一致；未授权差异 {r['protected_changes']}。
- 删除 EX11_EmblemWall 与其旧代理；校标原有图形与四色珐琅保持，几何深度随弧形后殿表面调整。新增双层金色盾形镶边和薄浮雕 Maynooth 金字。原可编辑文字源明确隐藏，场景中仅显示弧面字网格。
- 检查 {r['layout_objects_checked']} 个布局对象，仅 8 个工作区交互/区域标记按预期移动。其他变化 {r['unexpected_layout_changes']}。
- 书架 {r['shelf_groups']} 组；槽位标记 {r['slot_markers']}；预览书对象 {r['preview_book_objects']}；可交互用户书 {r['interactive_user_books']}。
- 新增展示书不增加容量。上述槽位数字是保留的实际标记数，本次没有重新执行每个槽位的书板支撑验证。
- 新增艺术对象 {r['new_object_count']} 个；重复 ID {r['new_duplicate_ids']}；缺失必需元数据 {r['new_missing_metadata']}。

三、通行范围
- 中央 x∈[-3,3]，y∈[12,42]，z∈[0.1,2.2] 米范围：可见障碍 {r['aisle_6m_headroom_volume_obstacles']}。
- 两柜台前方各 1.8 × 1.8 米操作范围、后方各 1.8 米进出范围：未发现所测网格障碍。
- 台体与柱体包围盒未重叠。工作人员柜台开口宽 3.2696 米。
- 这是帧 35 的静态 AABB 检查；四个跨馆合并书脊符文网格采用实际顶点检查，避免其整体包围盒覆盖空通道的误报。不是完整三角形碰撞仿真或导航可达性证明。

四、动画
- 24 fps，240 帧周期，10 秒。采样帧 {r['animation']['sample_frames']}。
- 对象驱动拥有者 {r['animation']['object_driver_owners']}；灯光数据驱动拥有者 {r['animation']['light_driver_owners']}；材质驱动拥有者 {r['animation']['material_driver_owners']}。
- 无效驱动 {r['animation']['invalid_drivers']}；所有采样数值有限 {r['animation']['all_samples_finite']}。
- 帧 1/241 最大循环接点误差 {r['animation']['loop_endpoint_max_error']:.9g}。
- 悬浮书、翻页、星轨与书脊亮度保留可编辑的 Blender 原生驱动。

五、文件与证据
- 完整 Assembly 已在独立 Blender {reopen['blender_version']} 中重新打开。
- 外部图片均内嵌 {reopen['all_used_file_images_packed']}；被使用的外部字体均内嵌 {reopen['all_used_external_fonts_packed']}。
- 三份原生模块通过空场景 Append；图像缺失 {modules['missing_images']}；动画接点误差 {modules['loop_error']:.9g}。
- 八张最终图位于 Evidence/10–17，来自实际场景，1800 × 1200，Cycles 64 samples，帧 35；无生成式图片替代。
- 单位：米，Z 向上，正面 -Y。全量对象 CSV 包括隐藏技术对象，不能直接视为运行时导出白名单。
- Scripts 为实际逐步构建脚本；rebuild_native.py 可从 Before 快照重建，完整一键流程尚未在第二目录重复执行。
- 未做 Unity 导入、Unity 动画转换、导航烘焙、运行时碰撞或设备性能验证。

详细数据：Audit/geometry_validation.json、fresh_reopen.json、module_append_validation.json、animation_samples.json、removed_obsolete_objects.json、station_changes.json、shelf_magic.json、emblem_changes.json。
'''
(P/'Validation_Report.txt').write_text(report)
(P/'START_HERE.txt').write_text('''魔法图书馆 V6｜打开顺序

1. 打开 Review.html，查看八张真实场景图和改动说明。
2. 打开 MilanLibrary_Magic_V6_Assembly.blend。主场景 MilanLibrary_Final，主相机 CAM_V6_Overview；文件已包含完整馆体和 V6 改动。
3. 时间轴播放 1–240 帧，24 fps。材质和辉光使用 Cycles、AgX、Full_Collection 视图层及已配置的合成器；实体视图用于编辑。
4. File > Append：Exports/V6_Updated_Workstations.blend 中 V6_EXPORT_Updated_Workstations 是两个完整新版工作台；V6_ShelfMagic_Overlay.blend 中 V6_EXPORT_ShelfMagic_Overlay 是书架魔法叠加组件。V6_DirectMountedEmblem.blend 中 V6_EXPORT_DirectMountedEmblem 为贴墙金边校标组件。模块已包含于完整 Assembly，不要再向它重复追加。
5. full_scene_inventory.csv / full_module_manifest.json 是完整模型清单，包含隐藏对象；V6_new_objects.csv 是本次新增艺术对象。删除记录、改动记录见 Audit。export_manifest.json 说明三份模块的范围。
6. MilanLibrary_Magic_V6_Before.blend 是修改前快照。原始 V5 位于相邻目录并保持原文件。
7. 验证和实际限制见 Validation_Report.txt。文件校验值见 FILE_SHA256.json；包外另有 ZIP 校验文件。

重建：先复制整个交付目录，再用独立 Blender 运行 Scripts/rebuild_native.py。它从 Before.blend 开始，并会重写所在目录的 V6 Assembly。逐阶段脚本已实际执行，整套一键重建未在第二目录完整复跑。
坐标：米，Z 向上，正门朝 -Y。材质与原生动画已打包；未声称 Unity 导入或设备性能已通过。
''')
assert r['pass'] and reopen['pass'] and modules['pass'], 'Do not publish review as passed with a failed check.'
print('Review and delivery notes generated.')
