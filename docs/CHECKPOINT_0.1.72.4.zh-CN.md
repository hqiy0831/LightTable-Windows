# 0.1.72.4 接手检查点（2026-10-26）

完整移植目标仍以 `handoff/ORIGINAL_REQUEST.zh-CN.txt` 为准，没有删减功能或验收项。此轮恢复开发，保留原始压缩包、Git 与 upstream；交接清单中 86 个源码条目初始哈希均匹配。原目录仅有压缩包，实际源码解到本目录 `LightTable`。

## 已实现

- 凑近倍率改为 LoupePower 四档 1× / 2× / 4× / 8×，替换 bool Eight。倍率指相对屏幕台面场景的缩放；普通圆镜保持1.5×，与原图像素1:1无关，也不再将旧原倍率简单改名为1×。
- 单个196×34 DIP胶囊、48 DIP间隔滑块、整个胶囊共用鼠标捕获表面。点击/横向拖动/Tab循环走真实倍率状态。数值按log2插值，滑块按段位置插值；缓动作用于每次过渡的时间，从当前渲染值重定向，避免快速反向跳变。
- 保留Space .50s进入/.34s返回，倍率.26s与减少动态效果。松开Space不修改Lamp。
- 录屏9.567秒、1920×1080已读取并提取20个时间点，帧图在TestResults/current-video。修复双输入通路：鼠标/笔只由捕获指针处理，不再重复处理Manipulation；触摸在手势起点命中目标并冻结目标集合。从背景开始的平移/捏合只改变相机，不因旧选择移动照片。增加指针身份、取消及无按钮移动清理；触摸多选和相册一起应用位移。
- 色彩阶段修复：WIC现为ICC→sRGB BGRA8，不能套用P3亮度权重。LUT与FusedOptical HLSL改为线性sRGB亮度系数(.2126,.7152,.0722)，重编译shader，缓存配方升级 `srgb-bgra8-scatter-v4-srgb-luminance-zero-floor`。旧光学缓存身份自然失效。
- 性能小改：Refresh中的底栏上台标记改为HashSet查询，移除逐项扫描全台面的二次复杂度；源预览/光学缓存仅超预算时排序。仍逐次扫描目录，没有宣称解决所有冷写瓶颈。增加压力测试阶段进度文件，超时也保留阶段。

## 真实验证

- .NET SDK10.0.403在新机器隔离安装；Solution Release构建0警告0错误。
- 核心88/88，0跳过，见evidence/checkpoint-0.1.72.4/core.trx。
- 最终原生22/22、进程退出0、无crash.log，见native.json与native-process.json。包含四档实际渲染倍率、滑块终点、Space中间帧/返回与灯光独立、六块彩色GPU LUT与sRGB理论亮度差≤2/255、五层缓存、DPI/设备重建等既有检查。应用内设置状态不等于硬件输入。
- 此前两轮原生相册断言失败保留在TestResults/current/{segmented-native,interaction-native}。实际125%DPI下请求69×66 DIP，实际68.800003×65.599998 DIP；改为保持请求尺寸断言并容忍1物理像素取整。另一次滑块断言存在独立时钟终点的竞态，已等待滑块和渲染双方完成，原失败在final-native中保留。
- 真实鼠标：单击照片再拖空白台面，三个世界坐标完全不变，仅相机偏移(0,0)→(60,43.199997)；直接拖照片只改变该片，保留相机和另外两片坐标。输入日志见ui-input.log。
- 最终共用捕获表面已实测1↔8双向拖动、Tab 1→2→4→8→1。4×/8×/1×点击有生产状态日志；2×点击尝试没有对应日志，不能声称本轮四档点击全部硬件验收完成。应用内四档渲染与核心分段命中均通过。
- 真实Space短按与松开：close true→false而lit始终true；Button焦点Space也保持lit=true。工具没有可指定时长的持续按键接口，持续物理长按未测；Space+Tab组合尝试未送达应用，不能记通过。
- 用户物理Esc停止computer-use；已停止一切后续界面输入。最后启动的隔离UI实例未继续操作，其数据位于TestResults/current/ui。

## 性能（当前机器，不与旧机器直接比较）

Windows11 Pro22631，i7-9750H，12逻辑处理器；可见RTX2070Super Max-Q、Intel UHD630与虚拟显示适配器。测试为真实NASA预览JPEG、生产应用内诊断，非硬件输入或GPU呈现计时。

交互修复后、色彩/排序小改前：10/100/500张完整短测CPU Draw P95为2.18/22.10/58.90ms。首次1000张超过120s被watchdog终止，失败保留在benchmarks-initial.log，无最终报告，不能记通过。

随后独立1000张扩展运行退出0，无crash.log：导入9130.67ms，平铺/GPU准备43585.47ms，120步平移23246.48ms，缩略图重访31090.93ms，Draw P95=98.1636ms、P99=203.8832ms、Max406.1063ms。见benchmark-1000-extended.json。这是色彩修复前的中间构建，不能冒充最终源码性能验收；没有达到60FPS。最终色彩/排序小改后四规模未重跑。

## 未完成与交付边界

- 完整Display P3/浮点线性工作链未实现。当前sRGB早期裁剪仍存在；本次权重修复不恢复P3色域。应从保留ICC/宽色域源解码、FP16静态层与缓存格式，到受控色彩输出一并设计，更新配方，并用超sRGB色块与实际显示器验收。
- 1000张性能、完整原版语义审计、真实触摸/触控板、持续Space、所有相册/迁移/幻灯输入、多屏/系统DPI/驱动恢复、长期稳定和干净系统安装仍未闭合。原有矩阵条目全部保留。
- 此轮尚未打包新dist/ZIP/EXE，原交接压缩包中的dist仍0.1.72.2。可运行开发构建是 `LightTable.Windows/App/bin/Release/net10.0-windows10.0.19041.0/win-x64/LightTable.exe`，必须保留同目录全部依赖。源码/开发构建0.1.72.4，不得称最终发行。
- 未动用户F盘程序/快捷方式，未设置Git身份/提交/签名，未改upstream。

## 参考

[W3C sRGB相对亮度](https://www.w3.org/WAI/WCAG21/Understanding/relative-luminance.html)；[微软Advanced Color显示链](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range)；[Win2D ColorManagementEffect](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_ColorManagementEffect.htm)。仅修改LUT权重不足以实现端到端宽色域。
