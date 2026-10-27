> **当前 0.1.75.3：新增栏目备份/导入。** 底栏全部照片原字节复制、相对路径清单，可由用户保存网盘；导入新建独立栏目、空白台面，既有栏目和原图不动。先读 [当前检查点](docs/CHECKPOINT_0.1.75.3.zh-CN.md) 及 [备份说明](docs/COLUMN_BACKUP.zh-CN.md)。下文保留历史，既有功能修正与剩余完整移植目标保留。

> **当前版本 0.1.75.2：按用户要求删除相册功能，底栏始终保留全部照片并标记「已上台」。** 已上台照片不隐藏；100 张图库、60 张上台仍显示 100 张，另 40 张保持。先读 [最新检查点](docs/CHECKPOINT_0.1.75.2.zh-CN.md)。旧相册照片迁入原栏目，独立备份保留；这是明确授权的功能范围变更，完整移植目标的其余部分保留。下文为历史。

> **当前 Windows 版本 0.1.75.1；已同步上游 v0.1.75（827ca13d947e4472675bbe409837c0188a9385d9）。** 最新功能与实际验证见 [检查点](docs/CHECKPOINT_0.1.75.1.zh-CN.md) 和 [同步记录](docs/UPSTREAM_SYNC_0.1.75.zh-CN.md)。下文保留交接历史，完整移植目标不变。

> **最新：0.1.72.6 栏目、渐进载入与普通镜片倍率修复。** 请先读 [本版检查点](docs/CHECKPOINT_0.1.72.6.zh-CN.md)，下文为保留的历史；完整移植目标不变。

> **最新：0.1.72.5开发候选检查点，继续开发中。** 请先读[docs/CHECKPOINT_0.1.72.5.zh-CN.md](docs/CHECKPOINT_0.1.72.5.zh-CN.md)；下文保留历史上下文，完整目标不变。

> **2026-10-26接手更新：源码/开发构建已到0.1.72.4。** 四档倍率、输入冲突修复与sRGB亮度权重修正已编译；88核心/22原生通过。真实输入验证因用户物理Esc停止，尚未重新打包。完整移植和验收目标保留，最新范围、失败记录及待办请先读[0.1.72.4检查点](docs/CHECKPOINT_0.1.72.4.zh-CN.md)。下文为原交接历史，不代表当前仍停工。

# LightTable Windows 完整交接 · 已暂停

用户要求停止当前开发并转交其他Agent。当前目标已经暂停，不继续修改产品实现。最新补充需求是：**倍率可选1×、2×、4×、8×，按键为胶囊状。此需求尚未实现，必须作为下一位Agent的优先工作。**

## 先读这些文件

1. `docs/handoff/ORIGINAL_REQUEST.zh-CN.txt`：用户完整原始指令，完整移植范围保持不变。
2. `AGENT_PROMPT.zh-CN.md`：可直接交给下一位Agent的提示词。
3. 本文件：停工时的最新状态，优先于旧进度报告中尚未更新的描述。
4. `docs/PROGRESS.md`、`docs/PORTING_MATRIX.md`、`docs/KNOWN_ISSUES.md`、`docs/OPTICAL_CACHE_REPORT.md`。
5. `docs/USER_BUGFIX_REPORT.md`：此前六项用户问题及验证范围。

## 源码和交付包是两个不同检查点

- 工作区：`D:\UserData\Documents\ChatGPT\电子观片台`。
- 当前源码/开发构建版本：**0.1.72.3**，Solution及App最近构建成功，0警告0错误。
- 已交付的`dist/Windows-x64/`、ZIP、EXE安装器仍是**0.1.72.2**，不含最新五层缓存改动。不要把最新源码测试结果当作dist测试结果。
- 最新开发程序：`LightTable.Windows/App/bin/Release/net10.0-windows10.0.19041.0/win-x64/LightTable.exe`，必须连同该目录的DLL、PRI、Resources和自包含运行时使用。
- 历史0.1.72.2便携包20项检查、进程退出0，核心73项通过。原始源码Swift v0.1.72固定SHA为`1a998cfce9096ac7b42e26aba67c922885fd43e2`，位于`upstream/`，未修改。
- Git分支`codex/windows-port`尚未创建首次提交：没有配置提交作者身份，不要冒用原作者或编造署名。完整工作目录和.git都保留，不能只取Git已提交文件。

## 最新已实现内容

已有原生WinUI 3/Win2D桌面程序：照片导入/分级WIC解码、GPU灯箱与放大镜、散片交互、胶片袋相册、机械幻灯及合成音、原子布局保存和恢复、Mac布局映射、受限安全手动更新、EXE与便携构建脚本。不是Web套壳。

0.1.72.2修复：Space凑近.50s、返回.34s；预览键事件避免Space误点击灯光Button；关灯=开灯0%；高光局部过曝约2s回落；Tab .26s变焦；两档原倍率/8×位于一个可点击/拖动的胶囊。显式动画默认启用，独立于当前系统关闭的动画设置；有“减少动态效果”菜单。

0.1.72.3新增：

- 五层source/lit/dark/flare/boosted以Brotli容器原子持久化；固定头、身份、尺寸、逐层SHA256校验，失败重建，不写原图。
- 缓存身份已改为源文件元数据+解码bucket+请求栅格上限+配方；**在读取源像素前命中五层缓存**，台面和投影均已接入。
- `cache/optical-v1/`字节预算750MiB，**最新文件数上限2048**；原版48项对1000张小栅格会反复淘汰，Windows保留字节预算并提高文件数上限。部分旧文档仍写48，本文件和当前代码为准。
- 清理超过一小时、命名符合程序格式、非reparse且未被占用的遗留临时缓存；保留新文件、锁定文件、无关文件和已提交层。
- 双路后台构建、请求合并、按设备租约复用不可变色表、立即释放临时特效图；跨设备代次拒绝迟到结果。
- 默认100%稳态直接复用lit；渲染宽高比/尺寸/备用图改索引，避免多次全图库扫描。
- 增加`--optical-cache-probe <request.json>`独立诊断入口和`Scripts/Test-OpticalCache.ps1`，验证跨进程冷/热缓存，诊断窗口置底、不抢前台。

## 最近真实证据

- 核心**80/80**通过，含新的临时文件恢复测试；记录`docs/evidence/cache-first-core.trx`。此轮在文件数上限由48改为2048之前运行；之后只改了该常量并重新构建，不冒称全套核心再次跑过。
- 原生**21/21**通过，进程退出0、无crash.log；`cache-first-integration.json`、`cache-first-native.json`。包括删除源预览缓存后的纯光学命中、独占锁定源图后仍能恢复、五层GPU读回一致。此轮也早于2048常量调整。
- **10张、1000张各两个独立进程**跨进程检查通过，源预览缓存已删除，热运行源解码0、源磁盘预览读取0、光学生成0，全部五层GPU哈希一致，两进程退出均0。
- 最新1000张探针（2048上限）冷运行约13981ms、热运行约1938ms、1000次光学命中；`docs/evidence/cache-first-process-1000.json`。这调用生产工厂，不等于完整应用启动/呈现帧率。
- 更早的完整1000张交互短测Draw CPU P95从初版342ms降至94.19ms，点灯阶段仍112.65ms，有明显性能欠缺。最近缓存前置/容量调整后尚未重跑完整四规模交互短测，不能将探针热耗时当作60FPS证据。
- 真实键鼠已验证Space短按/松开、Space+Tab组合、灯光Button焦点Space保持灯亮、胶囊点击/拖动。工具没有指定持续按住时长接口，持续物理长按未执行；持续状态动画有原生计时/GPU帧证据。
- `AppSha256`探针字段只绑定EXE宿主，不足以绑定托管逻辑；请同时核对交接清单中的LightTable.dll、LightTable.Core.dll及源文件哈希。

## 下一步优先事项

1. 实现用户新要求：**1×、2×、4×、8×，一个胶囊分段控件，滑块连续过渡；点击、拖动、Tab循环四档都生效**。现有`bool Eight`、`Action<bool> Selected`和0/1插值必须改为数值/枚举，不能只改标签。明确倍率坐标含义，不能把旧“原倍率”简单改名为1×却保留其他实际倍率。保留Space临时状态、灯开关独立、.50/.34/.26时序和减少动态效果。
2. 相关位置：`App/Controls/MagnificationSwitch.cs`、`App/State/TableSession.cs`、`App/Windows/MainWindow.cs`、`Interaction/TableInput.cs`、`Core/Optics/LoupePose.cs`、`Core/Optics/LightResponse.cs`、`Rendering/Canvas/TableRenderer.cs`及相应测试。
3. 继续大图库加载/点灯性能，保留原始慢数据；当前每次缓存写入仍扫描/排序目录，提高到2048后需量化冷写入成本。探针1000张已证明热缓存复用，但不替代完整启动和四规模回归。
4. Display P3/线性光色彩链路仍未实现；当前WIC统一sRGB。不要先裁剪到sRGB再宣称保留P3。实现色彩变化需更新缓存配方。
5. 完整原版语义漏项审计、真实UI输入/相册/迁移/机械幻灯、多屏/系统DPI、真实驱动恢复、长期性能、干净Windows安装等仍未闭合；不能宣称完整功能等价。
6. 新源码重测、发布构建、完整便携验收和安装验收后再更新dist版本与交付报告。

## 工具环境和约束

- SDK：`%LOCALAPPDATA%\LightTableBuild\dotnet\dotnet.exe`，10.0.403；WinUI/Windows App SDK2.5.1、Win2D1.4.0，.NET10 win-x64自包含。
- Inno Setup7：`%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe`。外部工具未复制进项目；新机器可按global.json和包锁重新配置，不依赖旧开发机路径运行发布包。
- 常用命令：`dotnet build LightTable.Windows.sln -c Release --no-restore`；`dotnet test LightTable.Windows/Tests/LightTable.Core.Tests.csproj -c Release`；`./LightTable.Windows/Scripts/Build-Release.ps1`；`Test-Portable.ps1`；`Test-OpticalCache.ps1 -Count 1000 -Bucket 256 -RasterLimit 64`。
- 用户现有开始菜单快捷方式指向`F:\codex test\LightTable\LightTable.exe`，不是本工作区产物。不得为了测试覆盖、移动或删除它。安装脚本预检保护了它；本轮安装未执行。旧检查点安装/卸载/重装成功不代表最新安装器验收。
- 没有WindowsSandbox.exe/干净Windows镜像结果；没有macOS参考运行环境，不能声称逐像素一致。
- 原录音缺额外再分发权，目前合成音；发行签名未配置，自动执行更新禁用。保留版权和第三方许可，不添加个人署名。
- 用户重新授权了键鼠操作；使用合适computer-use工具，尊重输入保护，不能用PowerShell UIA注入绕过。应用自身诊断不等同真实硬件输入。
- 全量压缩包包括源码、.git、upstream、dist、bin/obj、.tools、所有TestResults、真实NASA预览数据、日志与报告。部分测试路径超过260字符，解压工具需支持长路径。完整目录无需解压也能在现有工作区继续。
