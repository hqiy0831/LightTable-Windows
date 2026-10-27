> 当前 Windows 0.1.75.3：新增按栏目导出/导入便携备份，底栏为准、原照片只读。参见 [当前检查点](CHECKPOINT_0.1.75.3.zh-CN.md) 和 [备份步骤](COLUMN_BACKUP.zh-CN.md)。下文为历史。

> 当前 Windows 0.1.75.2：按用户明确要求移除相册功能、底栏全部照片常驻并标记已上台。旧相册照片迁入原栏目且先独立备份；schema 4，旧版请勿回写。最新范围及验收见 [0.1.75.2 检查点](CHECKPOINT_0.1.75.2.zh-CN.md)，下文保留历史。

> 当前：Windows 0.1.75.1 / 上游 v0.1.75。请先读 [最新检查点](CHECKPOINT_0.1.75.1.zh-CN.md) 与 [上游同步](UPSTREAM_SYNC_0.1.75.zh-CN.md)；下文为历史记录，不代表最终包的验证状态。

> **最新0.1.72.5：** [本轮实现、真实输入、失败记录与待验收范围](CHECKPOINT_0.1.72.5.zh-CN.md)。下文为保留的历史记录。

> **最新检查点0.1.72.4：** 见[本轮实现、真实证据与未完成事项](CHECKPOINT_0.1.72.4.zh-CN.md)。此页下文保留历史检查点，不应覆盖最新状态。

# 实际测试记录

日期：2026-10-25至26（America/New_York）。环境：Windows 11 专业版 23H2 / 10.0.22631 x64；NVIDIA GeForce RTX 3080 Ti，驱动 32.0.16.1692；另有 MuMu Virtual Display Adapter。当前开发机并非干净验收镜像。

## 0.1.72.3 五层缓存源码检查点（尚未打包）

79项核心测试、21项原生检查通过，进程退出0且无crash.log。新增缓存合并/失效/校验/损坏重建/LRU/写入失败/限并发检查；实际GPU五层跨Store逐字节一致，六个亮度/点灯组合最大像素字节差0。具体证据optical-cache-core.trx、optical-cache-integration.json、optical-cache-native.json和绑定源码哈希的optical-cache-checkpoint.json。大图库性能仍不达标，纯光学命中绕过源解码和跨进程热启动未完成；详见OPTICAL_CACHE_REPORT。dist仍是0.1.72.2，不能套用此检查点测试结果。

## 0.1.72.2 六项修复与设备重建

源码检查点核心73/73通过，0跳过；20项真实WIC/WinUI/Direct2D/MediaPlayer检查通过。新增设备代次/迟到取消/新硬件与WARP替换、控件DPI125/150/200%、开灯0%与关灯共同像素基线、高光局部过曝2秒回落、实际.50/.34凑近与.26倍率中间值、单胶囊滑块及XAML截图。应用ReducedMotion=false且SystemAnimationsEnabled=false均为实际读数，验证了系统动画关闭时本应用仍能呈现这些显式动画。

首次真实桌面输入因用户输入被拒绝，随后测试窗口最小化而停止；用户再次明确授权后，独立新版实际执行Space短按/松开、Space+Tab组合、灯光Button焦点Space以及胶囊点击/拖动，界面及事件日志确认相应行为通过。持续物理长按未执行，因为工具无指定保持时长接口；持续动画仍由生产计时和GPU帧检查覆盖。详见USER_BUGFIX_REPORT、ui-authorized.json及ui-authorized-input.log。最终包的结果以portable.json为准；下方均为历史检查点。

便携初测发现报告20项通过后进程仍以C000027B退出；原因是设备替换诊断在Window.Closed过早关闭CustomDevice。修复设备所有权及关闭回调后，最终便携包独立解压再次执行20项检查全部通过，进程退出0且无隔离crash.log；失败记录device-shutdown-failure.json保留。新断言还检查实际Canvas终点帧，而不是只检查动画时钟。最终Solution编译0警告0错误。portable.json与release-final.json绑定此次通过的实际ZIP哈希。

## 最新检查点：投影、迁移、相册与图像边界

- 核心63/63通过，0跳过；含收片1/12/40张轨迹、84%双轴fit、慢解码和迟到取消、首次展示闸门、单次声音cue、600ms提示与失败重试、Mac路径映射/整盘根目录/旧别名/归档、1/40/300页旋转混排恢复、慢拖放的释放相机快照。
- 最近原生集成16/16通过，已实际运行WIC/Direct2D/MediaPlayer。新增TIFF EXIF1–8逐像素、PNG透明预乘、>300字符中文路径、递归隐藏/包目录跳过及独占文件错误处理。
- 投影中心与lit差≤1/255、sharp/soft角透明、8并发请求共享、三项LRU保留租用图、真实零RGB黑场通过；焦点50%输出符合soft source-over与sharp plusLighter逐通道算式。
- 收片使用真正Transform3DEffect：起点透明、途中有实际缩略图、结尾全黑，台面JSON不改变。共享相册膜层9孔中心透明，目录不继承台面旋转；实际底栏GPU缩略图138×132、实时Image布局69×66通过。
- 两路MediaPlayer发出MediaOpened，AutoPlay关闭且准备时位置为0；.20音量下首次cue使播放时钟前进。合成WAV为48kHz立体声PCM .38s。这不证明原录音音色或主观扬声器同步。
- Mac Saved经实际WIC元数据读取、Session替换、原子保存、新Store恢复及GPU绘制通过；Mac原字节和Windows导入前布局归档一致。fixture按上游编码形态构造，不能当作真实Mac用户文件或文件选择UI通过。
- 首次焦点集成断言误认为50%焦点应保持稳态总亮度；原版soft source-over会同时衰减glow，其均值约为稳态0.918。核对源叠加顺序后，改用独立图层通道算式检查，未为通过测试改变原版叠加行为。早期失败报告保存在TestResults/projector-native-*。

本检查点Release构建成功，便携包解压后16项集成通过、退出0。当前安装回归在启动安装器之前被保护检查阻止：开始菜单已有指向`F:\codex test\LightTable\LightTable.exe`的用户快捷方式，未替换该链接或现有程序。系统也没有可用WindowsSandbox.exe。结果见`docs/evidence/installer-current.json`与`portable.json`；`installer.json`和下方9项安装记录属于较早检查点，不能替代当前安装包验收。打包内报告按记录时间保留，最终压缩包哈希以dist/SHA256SUMS-Windows.txt为准。

## 较早检查点证据

- 当前核心测试36/36通过，0跳过。新增覆盖LoupeDepth几何、径向lip单调性、Space中断时钟、袋口抓取点及两个损坏布局另存保护。
- 9项生产类集成检查全部通过：真实JPEG/内容去重，PNG/BMP/TIFF不透明首帧像素无损，缺失/损坏拒绝及20并发合并，坏缓存重建和新Store磁盘命中，GPU光照，命令缓存相机等价，HLSL放大镜/原倍率/8×，相册/相机/层级恢复，3张原JPEG哈希不变。
- 命令缓存与直接GPU绘制在锚点缩放+平移后，每字节平均差约0.00020/255。此检查只证明该样例的缓存变换一致。
- 1000张不同NASA/JSC真实JPEG已下载，10/100/500/1000三轮短压力运行全部实际导入对应数量。原始慢结果保留，后续预算修复结果见PERFORMANCE_REPORT。
- 初次径向shader初始化错误使用不支持的HighQualityCubic sampler，日志捕获ArgumentException；改为合法Linear sampler后，HLSL绘制、中心不透明与圆外透明检查通过。
- 首个自包含Release、便携ZIP和未签名Inno Setup EXE已实际构建。文件存在不等于安装/卸载通过，后续源码修改后仍需重打包。

可提交原始证据在`docs/evidence/`，本地完整日志与GPU快照在TestResults和logs。诊断入口调用生产对象，不能写成真实键鼠UI自动化通过。

## 较早检查点的安装与便携验收

首次安装流程成功但已安装应用崩溃，原因是dotnet publish漏带`LightTable.pri`。已在工程加入PRI/XBF发布复制与缺失失败检查，重新构建后执行以下实际验证：

- 当前用户隔离目录安装成功，开始菜单快捷方式和卸载程序均存在。
- 从安装目录启动应用，9项生产类集成检查全部通过，进程退出0。
- 卸载成功，应用EXE与开始菜单快捷方式移除，隔离用户布局仍存在。
- 重装再卸载成功，用户布局继续保留。两轮记录在`docs/evidence/installer.json`，详细日志在logs/install-round-*.log和uninstall-round-*.log。
- 便携ZIP解压至新的TestResults/portable-*目录，确认本地coreclr.dll与LightTable.pri存在，再运行9项集成检查全部通过，退出0；旧记录保留为`docs/evidence/portable-before-projection.json`，当前20项结果见portable.json。
- 安装包Authenticode状态实测为NotSigned。SHA256清单仅用于完整性，不能当作发行者身份验证。

以上环境是当前开发主机，不能替代干净Windows镜像测试；未执行可视安装向导每一步的UI自动化。

## 已执行

- `.NET SDK 10.0.403` 安装及 NuGet 依赖还原成功。
- `dotnet build LightTable.Windows/App/LightTable.Windows.csproj -c Release -v minimal`：完整 UI 工程生成成功，0 警告、0 错误；日志 `logs/build-features.log`。此记录后的小修改仍需再编译。
- `dotnet test LightTable.Windows/Tests/LightTable.Core.Tests.csproj -c Release --logger "trx;LogFileName=core.trx" --results-directory TestResults`：29/29 通过，0 跳过，80ms（测试器报告）；原始结果 `TestResults/core.trx`，日志 `logs/test-core.log`。
- 首次核心测试发现光照曲线浮点误差可产生约 -6.7e-16 的 flare；已在 quintic 输出处 clamp，再运行 29/29 通过。
- 原生程序实际启动，WinUI UI Automation 树及真实截图显示欢迎卡片、菜单、工具条、空灯箱和图库。
- 通过命令行文件参数导入 3 张 NASA JPL JPEG，包含中文目录；真实 WIC 解码、缩略图及来源路径保存成功。数据隔离在 `TestResults/manual-data`。
- 通过 computer-use 实际点击「一键平铺」，3 张胶片出现；点击「开灯」，暗态照片切换到透光状态；按 V，倍率控件出现，移动至照片后圆形镜片显示放大图像。不能由这些单次截图证明完整动画曲线或图像质量一致。
- 原生多文件选择器能打开，已填写测试文件路径；一次确认点击因 PickerHost 窗口归属校验被拒绝，另一次键盘动作因用户同时输入中止。因此该路径不能记录为已完整通过导入。

## 已知失败与修复

首个纯代码 App 因缺少 PRI/XAML 资源无法启动；加入 App.xaml 并成功编译后，真实窗口已启动。早期入口 lambda 编译错误及命名空间冲突已修复。一个启动脚本的参数拼接错误造成测试路径非法；改为明确的参数串后，同一数据目录成功导入 3 张 JPEG。早期错误日志保留，不能把它们记为当前通过。

## 仍未验证

相册完整UI、Space按住/松开、8× Tab/拖动、批量选择/删除、完整重启准备UI、幻灯实际输入与主观音频同步、网络异常、JPEG方向/ICC/HEIC/RAW及多页TIFF、系统picker长路径、长期压力、DPI/多屏/GPU丢失、干净系统安装卸载，以及原版视觉一致性。已完成的有限范围以上述新证据和矩阵为准。

单元测试只证明其断言覆盖范围，不表示整套 UI 已经完成验收。
