# LightTable for Windows · 观片台

基于 macOS LightTable v0.1.72 的 Windows 原生移植。当前为 **0.1.72.6 未签名开发构建**，完整功能与视觉等价尚未验收。最新栏目、载入及倍率修复见 [本版检查点](docs/CHECKPOINT_0.1.72.6.zh-CN.md)，完整操作见 [使用说明](README.md)。

## 运行与安装

主要目标 Windows 11 x64 23H2+；最低编译目标 Windows 10 2004，旧版本尚未运行验收。程序使用 WinUI 3 / Win2D / .NET 10，自包含发布，无需 Visual Studio。

在生成发行构建后，运行 `dist/LightTable-Windows-Setup-x64.exe` 进行当前用户安装，或解压 `dist/LightTable-Windows-Portable-x64.zip` 到可写文件夹并运行 LightTable.exe。必须保留同目录运行时、DLL 和 PRI 等全部文件。便携包表示无需安装，默认数据仍位于 LocalAppData。

当前未正式代码签名，不得将未知发布者当作已验证身份。卸载保留用户数据目录；从台面移除照片只改变布局，原图不被修改、删除或重压缩。

## 使用

底栏可通过滚轮、横向滚动条和「上一屏/下一屏」浏览全部照片。「新建栏目」得到空白栏目，可独立添加并重命名。选择文件夹递归读取并替换当前栏目；「添加照片」追加到当前栏目。也可以将文件/文件夹拖到图库。选中底栏照片后点「移出栏目」或右键移除，不删除源文件，也不影响其他栏目和已上台照片。JPEG、PNG、TIFF、BMP 已做有限解码检查；HEIC/WebP/RAW 仍需逐格式验证。

从底栏拖动胶片到台面，或「一键平铺」重建全部散片摆放。拖动散片移动；选中后拖右下角缩放，Alt 拖动旋转，右键菜单提供旋转、缩放、摆正和移出。Ctrl 多选，Shift 拖动框选。滚轮平移、Ctrl+滚轮缩放。

开灯显示透光胶片，高光过曝后在约2秒内回落。亮度滑杆0–160%，0%与关灯共用基准；双击恢复100%。V使用放大镜；按住空格以.50s凑近，松开以.34s恢复，灯开关保持原状态。Tab、点击或拖动同一胶囊内的滑块，以.26s切换1×、2×、4×、8×。所选实际倍率同时作用于普通镜片与凑近状态，松开 Space 保留倍率。动画默认启用，可在「观片 → 减少动态效果」中调整。高清预览最长边2200px，8×不代表完整原图解码。

相册中可新建页，添加 135/120 袋格，点右侧照片装入；同规格袋格可拖动交换，右键清空照片或移除袋格。整页可放上灯箱。

幻灯片在独立全屏窗口按随机顺序放映。方向键换片，Esc 或「返回观片台」退出。默认机械音效为原创合成，音量 .20；可以把已有授权的 `ShutterClickRoom.wav` 与 `ShutterArrivalRoom.wav` 放到 `%LOCALAPPDATA%/LightTable/config/audio/`。上游原录音不随 Windows 构建分发。

| 快捷键 | 功能 |
|---|---|
| Ctrl+O / Ctrl+Shift+O | 选择文件夹 / 添加照片 |
| Ctrl+L | 灯箱开关 |
| A / V | 指针 / 放大镜 |
| Tab | 放大镜1× → 2× → 4× → 8× |
| Space 按住 | 凑近观察 |
| Shift+Z | 恢复默认尺寸并整理全局 |
| Ctrl+单击 / Shift+拖动 | 多选 / 框选 |
| Delete | 所选项目移出台面 |
| 方向键 / Esc | 幻灯换片 / 退出 |

## 保存和更新

布局自动保存至 `%LOCALAPPDATA%/LightTable/layouts/table.json`，保留 .bak。config、cache、logs、updates 分目录存储；缓存不包含可替代原图的完整分辨率文件。原照片需要保留在原位置；缺失时可以重试或先查看可用照片。

「文件 → 导入 Mac 版布局」可读取原版 `OurLightTable/layout-v1.json`。先将照片复制到 Windows，再指定 Mac 与 Windows 文件夹的对应关系。预览后确认导入，会替换当前布局，并将当前 Windows 布局和原 Mac JSON 存入 `layouts/imports/` 的独立备份。缺片位置保留；不按同名文件猜测路径。说明见 [Mac 布局迁移](docs/MAC_MIGRATION.md)。

启动后台检查和帮助菜单手动检查使用真实 GitHub Releases，仅识别 Windows x64 安装包。当前远端尚无 Windows 专用包，发行信任配置尚未建立，自动执行安装禁用。下载 SHA256 只能校验完整性，不能证明发行者身份。

## 构建与问题反馈

见 [构建说明](docs/BUILD_AND_RELEASE.md)、[测试报告](docs/TEST_REPORT.md) 与 [性能报告](docs/PERFORMANCE_REPORT.md)。编译器或其他软件阻止启动时，请保留 logs 内的错误记录。图片解码失败通常意味着损坏文件、路径失效或未安装对应系统 codec。详细限制全部列在已知问题中。

原版 SHA 与许可见 [基线](docs/BASELINE.md)、LICENSE 和 THIRD_PARTY_NOTICES.md。
