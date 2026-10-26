# Windows 开发构建交付检查点

日期：2026-10-25，纽约时间20:40后。**尚未达到完整功能等价，不能作为最终验收完成声明。**

## 基线与工程

原项目LightTable v0.1.72，Commit `1a998cfce9096ac7b42e26aba67c922885fd43e2`，正式发布于2026-10-25 18:29:15 UTC。原Swift源码以`upstream/`固定submodule完整保留，本轮`git -C upstream status --short`为空。

Windows使用C#/.NET 10、WinUI 3/Windows App SDK 2.5.1、Win2D 1.4.0/Direct2D、WIC和MediaPlayer；自包含Windows x64，无网页套壳。Solution为`LightTable.Windows.sln`。当前没有配置Git提交作者，未冒用原作者身份提交；源码在工作区和索引内。

模块包括Core文档/相册/几何/光照响应/播放状态/原子保存、Imaging解码与分级缓存、Rendering台面/光学/HLSL镜片/投影/收片/共享相册膜层、Interaction键鼠/触摸/拖放、Audio机械声音，以及WinUI窗口/启动/指引/更新/迁移。

## 构建与实际结果

```powershell
& "$env:LOCALAPPDATA\LightTableBuild\dotnet\dotnet.exe" build LightTable.Windows.sln -c Release --no-restore -v minimal
./LightTable.Windows/Scripts/Build-Release.ps1
./LightTable.Windows/Scripts/Test-Portable.ps1
```

Solution实际生成成功，0警告、0错误，日志`docs/evidence/solution-build.log`。Build-Release先运行核心测试63/63通过，再发布自包含程序和编译Inno安装器；核心TRX与release-build.log已保留。最后仅更新报告并重新打包，最终便携包再次解压运行16/16原生集成通过、退出0。

未安装完整Visual Studio，以上是.NET SDK对Solution的真实编译结果；不能声称已在Visual Studio IDE执行过调试。

## 产物位置

| 产物 | 本机绝对路径 |
|---|---|
| 源码和Solution | `D:\UserData\Documents\ChatGPT\电子观片台\LightTable.Windows.sln` |
| Release程序 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\Windows-x64\LightTable.exe` |
| x64安装器 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\LightTable-Windows-Setup-x64.exe` |
| x64便携包 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\LightTable-Windows-Portable-x64.zip` |
| SHA256清单 | `D:\UserData\Documents\ChatGPT\电子观片台\dist\SHA256SUMS-Windows.txt` |

版本0.1.72.1，Authenticode实测NotSigned。便携包必须完整解压，保留运行时、DLL、PRI和Resources。`build-manifest.json`记录源码及核心二进制哈希；哈希仅用于构建追踪和完整性，不能证明发行者身份。

最终便携包SHA256：`7ff0ed29e2006600f465a73207bca5c7ed332288e296aa036e98e965f09d7c0b`。

最终安装器SHA256：`e294fb8d8316408b077cf476362db7575ceb4d5428625985615e0072eb35e5e0`。

## 覆盖和本轮验证

PORTING_MATRIX当前104行：37行Tested、63行Implemented、2行Not Started、2行Blocked。Tested只表示该行限定的证据范围，不是功能完成百分比；机器符号索引也不能代替全部源码的语义审计。

本轮新增实际实现：底栏透视收片盒、lit投影透光层、84%双轴fit、80/120/180ms机械播放状态、三项投影LRU、音频准备闸门，Mac Saved显式根目录迁移与旧别名验证，真实相册微缩图、透明装订孔共享膜层，旋转相册混排/小比例恢复，以及慢拖放的释放相机快照。

63项核心覆盖几何、布局、光学响应、相册交换、备份恢复、更新筛选、镜片、收片、播放取消/慢源/重试/声音cue、Mac迁移/安全路径/原字节归档和旋转相册混排。

最终便携16项原生集成实际调用WIC、Direct2D和MediaPlayer：JPEG/内容去重；PNG/BMP/TIFF像素；TIFF EXIF八方向与PNG alpha；递归排序/隐藏与包目录/长路径/独占错误；缺失和请求合并；坏缓存恢复；GPU光照；相机命令缓存；HLSL镜片；投影透光/叠加/LRU；透视收片；声音后端；镂孔膜层；真实相册缩略图及保存；Mac迁移/元数据/原子导入/GPU；原JPEG字节不变。

这些诊断不注入键鼠，不能替代真实UI输入、Mac视觉、DPI或多屏验收。用户此前Esc停止computer-use后，本轮未恢复桌面键鼠自动操作。

## 安装回归的实际限制

较早检查点曾在工作区沙箱完成安装/卸载/重装两轮，安装目录9项集成通过，且用户布局保留。当前安装回归在启动安装器前退出：开始菜单存在指向`F:\codex test\LightTable\LightTable.exe`的用户快捷方式。保护检查未覆盖或移动它，未修改F盘程序。

当前机器没有WindowsSandbox.exe；未提供干净Windows镜像验收结果。当前安装器已成功编译，但**本轮安装/卸载没有通过**，不得把较早测试套用到新安装器。详见installer-current.json。

## 性能实测及边界

较早同日的真实NASA/JSC预览JPEG短测：10/100/500/1000张实际导入全部成功。导入含缩略图100.2/472.2/2256.6/4677.9ms；CPU Draw P95为0.21/0.34/4.87/6.20ms；1000张结束GPU纹理估计63.15MiB。

这些数据来自此前代码检查点，尚未重跑全部本轮功能；样本不是相机全尺寸RAW。CPU Draw不包含GPU呈现，不能据此宣称60FPS或长期稳定。原始慢结果及曾超预算的报告均保留，详见PERFORMANCE_REPORT。

## 未解决与未验证

- Display P3/HDR工作链和五层光学磁盘缓存尚未完成；当前WIC统一sRGB，磁盘主要缓存源预览，光学层在GPU重建。
- 真实Mac截图/录屏和用户Saved文件缺失；完整源语义审查、像素级一致性尚未闭合。
- 相册/散片完整UI输入、Space持续按住与释放、倍率拖动、幻灯方向键、启动卡片/引导和小窗口布局仍待完整验收。
- DPI125/150/200%、多屏、真实GPU设备丢失、长期压力及干净系统安装回归尚未通过。
- 原录音再分发授权未取得，默认合成机械声不能称原音色一致；发行签名未配置，自动执行更新禁用；远端无Windows发行资产，下载端到端未验收。
- JPEG方向、ICC、HEIC/WebP/RAW和多页TIFF兼容性仍需专用fixture；Win2D二进制旧EULA链接失效的精确许可审计尚未闭合。

架构、功能逐项状态、完整测试、性能、安装使用及限制分别见ARCHITECTURE、PORTING_MATRIX、TEST_REPORT、PERFORMANCE_REPORT、README-Windows、MAC_MIGRATION和KNOWN_ISSUES。工程目标仍在继续，当前交付为可运行的开发检查点。
