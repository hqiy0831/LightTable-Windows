> **最新0.1.72.5：** [本轮实现、真实输入、失败记录与待验收范围](CHECKPOINT_0.1.72.5.zh-CN.md)。下文为保留的历史记录。

> **最新检查点0.1.72.4：** 见[本轮实现、真实证据与未完成事项](CHECKPOINT_0.1.72.4.zh-CN.md)。此页下文保留历史检查点，不应覆盖最新状态。

# 开发进度（2026-10-26，持续更新）

**已按用户要求暂停并交接。停工时的最新状态以根目录AGENT_HANDOFF.md为准。新增1×/2×/4×/8×四档胶囊需求尚未实现，交由下一位Agent执行。** 后续新增的缓存前置、80项核心/21项原生和1000张跨进程热缓存证据亦已写入交接文件；下文部分为之前检查点。

目标仍为完整 v0.1.72 Windows 移植，当前未完成最终验收。

## 已完成

- 克隆原仓库；锁定 tag、SHA、正式 Release 及版本差异。
- 安装隔离 .NET SDK 10.0.403 到 `%LOCALAPPDATA%/LightTableBuild/dotnet`。
- 确认原版光学参数、机械时序、分级预览、相册 lattice 与持久化结构。
- 独立 WinUI 3 / Win2D 原生工程已编译和真实启动；Swift 原码以固定 SHA 的 submodule 保留。
- WIC 导入、分级缓存、GPU 光学、放大镜、散片交互、相册、机械幻灯、原子保存和安全手动更新代码已接入。
- 当前0.1.72.2核心73项、原生20项通过，含用户六项修复的GPU/实际计时范围以及新设备/WARP和控件DPI；用户再次授权后，真实Space短按/组合、按钮焦点Space、胶囊点击/拖动通过，持续物理长按未执行。较早的63/16检查点保存在历史报告。
- 最新自包含Release、便携ZIP、未签名EXE已生成；当前最终包复测以portable.json为准。较早检查点修复publish漏PRI后，安装目录9项集成和安装/卸载/重装两轮通过。当前安装回归受已有F盘用户快捷方式保护，未修改其现有程序。
- 实际准备 1000 张不同 NASA/JSC JPEG，10/100/500/1000 规模已完成两轮有限压力运行；不以这些短运行宣称长期稳定或60FPS。
- 已实现原版 SleeveMotion 袋口拉出/落片/返回、边缘平移、LoupeDepth 凑近、径向 lip HLSL，以及低分配 FusedOptical HLSL。
- GPU 缓存解除对 CPU 解码数组的引用，添加按屏幕所需细节的 GPU mip、稳态光学纹理和相机可复用命令列表；命令缓存/直接绘制的 GPU 平均字节差约 0.00020/255。
- 实现可见底栏透视收片与幻灯盒分层；投影使用lit透光、84%双轴fit、原版焦点叠加，播放状态机覆盖慢源/中断/失败/就绪/声音cue。
- 实现Mac Saved显式文件夹映射、旧拖片字节核验、上层身份保留、原字节归档和原子导入；新增真实相册底栏预览、透明装订孔共享膜层及旋转混排比例恢复。

## 当前工作

六项修复后的继续开发已进入0.1.72.3源码检查点：五层光学磁盘缓存已接入台面/幻灯，新增6项核心、1项实际GPU检查通过；总计79核心/21原生，进程退出0且无crash.log。新源码还未重新打包，dist交付仍为0.1.72.2。详见OPTICAL_CACHE_REPORT。

真实四规模短测发现性能问题，已保留初版1000张约342ms Draw P95及后续各轮原始数据。后台临时图立即释放、色表租约复用、逐片索引及默认100%直接用lit已接入；最后一项已通过21项原生检查。最新1000张整体Draw P95为94.19ms，加载/平移/点灯/凑近阶段分别92.46/3.19/112.65/43.26ms，私有内存采样峰值818.71MiB；进程退出0。仍需优化，不能宣称60FPS。下一步先完成纯光学缓存命中绕过源预览解码、热缓存启动、大图库加载/点灯优化和临时文件恢复，再推进P3；不能因单片热缓存49ms到13ms就宣称全图库完成。

用户最新优先事项为六项交互/灯光修复，已实现到0.1.72.2；73项核心和20项原生检查通过。此前设备重建先行完成18项原生有限验证。详细改动和真实输入限制见USER_BUGFIX_REPORT。交付的新版需要完整解压运行，F盘旧快捷方式不会自动更新。

打包收尾抓到诊断关闭后ObjectDisposedException：旧报告Passed=true不能证明退出正常。已修复CustomDevice过早释放、关闭后回调和动画终点帧；最终便携包在TestResults/portable-20261026-004546-6f2524独立解压验证20项通过、进程退出0、无crash.log，源码哈希与构建清单一致。最终Solution编译0警告0错误。

这六项收尾后，继续P3工作链、五层光学磁盘缓存、原版语义完整审计和UI/系统验收。不能把当前修复检查点当作全移植完成。

## 待完成

- P3色彩工作链、五层光学磁盘缓存、原版完整视觉/动画核对、真实Mac布局文件与迁移UI验收。
- 相册/散片完整 UI 输入、Space 持续按下/松开、倍率拖动、幻灯实际输入与音频同步。
- DPI/多屏/GPU设备丢失、长期稳定性、超大图/文件占用/断网恢复和安装卸载/重装；当前机器不能证明干净系统兼容性。
- 发行签名及原录音再分发权；Win2D NuGet 旧 EULA URL 已失效，已保留 nuspec、来源 MIT LICENSE 与审计说明，二进制许可精确核对应继续检查。
- 对 SOURCE_SYMBOLS 和 PORTING_MATRIX 做完整漏项审查。未运行的测试不得标 Tested。

## 已知约束

原版音频不提供额外再分发授权；缺 macOS 运行环境与原版截图，不能宣称像素一致；当前机器不是干净 Windows 验收镜像。用户此前通过物理 Esc 停止 computer-use；此后保持不操作其桌面，集成与压力检查通过应用自身隔离诊断入口运行，不能写成 UI 自动化通过。

用户开始菜单快捷方式目标是`F:\codex test\LightTable\LightTable.exe`，它确实存在且不属于本工作区安装沙箱；不能为了验收移动、覆盖或删除。Test-Installer已在预检退出。WindowsSandbox.exe不存在，可选功能列表也没有DisposableClientVM。

本轮用户明确要求先做后测试，尝试对独立0.1.72.2窗口进行computer-use回归：成功观察界面，首次点击因用户输入被拒绝，刷新发现窗口被最小化，随后停止键鼠输入。不能将该次操作写成空格/Tab/拖动通过；隔离测试实例已清理，F盘程序未动。原生诊断已改Show(false)，后续不主动抢占前台。

用户随后明确“你现在可以执行键鼠操作”。独立新版实测Space短按与松开、Space+Tab组合、灯光Button焦点Space均保持灯亮；胶囊点击8×和拖回原倍率出现滑块中途位置及正确终点。证据ui-authorized.json绑定实际EXE哈希，ui-authorized-input.log保留生产预览键事件。工具没有长按时长参数，持续物理按住仍不记执行；滑杆0%尝试遭输入保护拒绝，未记通过。测试实例已清理。

## 恢复工作入口

- SDK：`%LOCALAPPDATA%/LightTableBuild/dotnet/dotnet.exe`。
- 构建：`LightTable.Windows/Scripts/Build-Release.ps1`；测试：`dotnet test LightTable.Windows/Tests/LightTable.Core.Tests.csproj -c Release`。
- 集成：`LightTable.exe --data-root <全新隔离目录> --integration-fixtures <TestResults/fixtures>`。结果在该目录 logs/integration.json，GPU快照同目录。
- 压力：`LightTable.Windows/Scripts/Run-Benchmarks.ps1`，数据集在 TestResults/real-photos-nasa。保留各次 logs/benchmark.json，不能覆盖早期慢结果以掩盖问题。
- 当前 benchmark 会真实进入应用并渲染台面/镜片，但不注入用户键鼠；GPU呈现时间、完整UI输入和声音仍需独立验收。
- 便携自动验收：`LightTable.Windows/Scripts/Test-Portable.ps1`；先核对本地SHA清单，再在新目录解压执行20项原生检查并核对进程退出。构建同时写build-manifest.json，绑定源码与核心二进制哈希。
- 设备重建参考微软官方Handling device lost：CanvasControl可自动处理Draw/CreateResources异常；后台绘图在IsDeviceLost确认后调用RaiseDeviceLost。健康设备调用RaiseDeviceLost会报错，不能充当故障注入。现有有限测试使用应用内部CustomDevice替换；仍需真实驱动恢复及多显示器验收，不禁用用户显卡。
- Git提交暂未建立：当前机器没有配置提交作者身份，未擅自冒用原作者或添加个人署名。源码已进入索引，分支codex/windows-port；这不影响编译和本地交付。
