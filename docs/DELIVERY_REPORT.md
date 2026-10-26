> **最新0.1.72.5：** [本轮实现、真实输入、失败记录与待验收范围](CHECKPOINT_0.1.72.5.zh-CN.md)。下文为保留的历史记录。

> **最新检查点0.1.72.4：** 见[本轮实现、真实证据与未完成事项](CHECKPOINT_0.1.72.4.zh-CN.md)。此页下文保留历史检查点，不应覆盖最新状态。

# Windows 0.1.72.2 开发构建交付检查点

**完整功能等价尚未达到；本次优先交付用户要求的六项交互和灯光修复。** 上一检查点报告保留于archive/DELIVERY_0.1.72.1.md。

## 基线、架构与模块

原项目v0.1.72，Commit `1a998cfce9096ac7b42e26aba67c922885fd43e2`，正式发布时间2026-10-25 18:29:15 UTC；`upstream/`固定submodule未修改。

C#/.NET 10、WinUI 3/Windows App SDK 2.5.1、Win2D 1.4.0/Direct2D、WIC、MediaPlayer；自包含Windows x64原生程序。Solution为LightTable.Windows.sln。Core负责文档、几何、相册、光照、播放状态、迁移与持久化；Imaging负责解码/分级缓存；Rendering负责台面、HLSL镜片、投影、收片和膜层；Interaction负责输入；App负责窗口、指引、启动和更新；Audio提供机械声。

## 本版行为

- 动画默认在应用内启用，系统动画关闭时仍可呈现显式凑近/倍率动画；菜单保留减少动态效果。
- Space提前由Preview事件处理，防止焦点灯光按钮收到Space并触发Click；进入.50s、返回.34s，临时状态不改变LampState。
- 亮度支持0–160%；关灯与开灯0%共用基准。
- 高光掩模驱动点灯过曝并约2s回落；等可见片面就绪后才启动时钟，避免慢加载吞掉动画。
- 凑近时Tab及倍率选择使用.26s连续变焦；两倍率位于同一胶囊，选中滑块平移，支持点击和拖动。
- 资源代次、后台设备错误、DPI静态资源保留、收片重建及幻灯无重复声音恢复已补齐有限检查。

六项对应证据与限制详见USER_BUGFIX_REPORT。

## 构建与测试

```powershell
& "$env:LOCALAPPDATA\LightTableBuild\dotnet\dotnet.exe" build LightTable.Windows.sln -c Release --no-restore -v minimal
./LightTable.Windows/Scripts/Build-Release.ps1
./LightTable.Windows/Scripts/Test-Portable.ps1
```

Release编译和Inno安装器编译已成功。核心73/73通过，0跳过；最终便携包独立解压后20项原生WIC/WinUI/Direct2D/MediaPlayer检查通过，进程退出0、无隔离crash.log，包含真实计时中途帧、胶囊XAML截图、0%像素基线、高光局部回落，以及新硬件/WARP和控件DPI替换。便携包SHA与portable.json一致；最终核验记录为release-final.json。Solution编译0警告0错误，日志为solution-build.log；未声称在Visual Studio IDE中调试过。

用户重新授权键鼠后，computer-use在独立0.1.72.2实例实际执行Space短按/松开、Space+Tab组合、灯光Button焦点下Space、胶囊点击8×与拖回原倍率；日志和界面确认松开不关灯、组合切倍率和滑块移动。证据见ui-authorized.json及ui-authorized-input.log。工具没有长按时长参数，不能将持续物理长按写成已执行；持续状态动画由真实计时、XAML和GPU渲染检查覆盖。

## 交付文件

| 产物 | 本机绝对路径 |
|---|---|
| 源码与Solution | `D:\UserData\Documents\ChatGPT\电子观片台\LightTable.Windows.sln` |
| Release程序 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\Windows-x64\LightTable.exe` |
| EXE安装器 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\LightTable-Windows-Setup-x64.exe` |
| 便携ZIP | `D:\UserData\Documents\ChatGPT\电子观片台\dist\LightTable-Windows-Portable-x64.zip` |
| 完整性清单 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\SHA256SUMS-Windows.txt` |

必须完整解压并保留运行时、DLL、PRI和Resources。新版窗口标题显示0.1.72.2，F盘旧快捷方式未替换。当前Authenticode为NotSigned；SHA256与build-manifest.json仅追踪构建和完整性，不能证明发行者身份。

源文件均保留在工作区；Git未配置提交身份，未擅自冒用作者或添加个人署名提交。

## 覆盖、性能和限制

功能矩阵105行：39行Tested、62行Implemented、2行Not Started、2行Blocked；Tested只代表本行限定的证据范围，不是功能完成百分比。仍需完整源码语义漏项复核。

此前真实NASA/JSC预览JPEG的10/100/500/1000张短测全部导入成功。导入含缩略图100.2/472.2/2256.6/4677.9ms，CPU Draw P95为0.21/0.34/4.87/6.20ms，1000张结束GPU纹理估计63.15MiB。这是此前代码检查点数据；本版未重跑四规模性能，不可据此宣称60FPS、全尺寸RAW或长期稳定。

本轮安装回归仍受已有指向F盘程序的开始菜单快捷方式保护，未覆盖或移动它。较早安装/卸载/重装两轮通过不能替代本版；当前无WindowsSandbox.exe或干净镜像结果。

P3/HDR工作链、五层光学磁盘缓存、完整真实UI输入、系统DPI/多屏、真实驱动丢失、长期性能、真实Mac文件和视觉比较仍未闭合。原录音再分发权未取得，默认合成音色不同；可信发行签名及远端Windows更新资产未配置，自动执行更新禁用。Win2D旧二进制EULA链接失效的精确许可审计未闭合。

架构、逐项状态、测试、性能、使用与问题分别见ARCHITECTURE、PORTING_MATRIX、TEST_REPORT、PERFORMANCE_REPORT、README-Windows、MAC_MIGRATION和KNOWN_ISSUES。本版2s点灯与关灯0%为用户指定调整，不冒称与Mac逐像素一致。
