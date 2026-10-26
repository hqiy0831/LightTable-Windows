# Windows 架构

锁定 v0.1.72，SHA 见 BASELINE.md。Swift 源码仅作为参考保留。

## 依赖

- .NET SDK 10.0.403 / C#，运行时随发布文件打包。
- Windows App SDK 2.5.1（NuGet），WinUI 3 原生 XAML 控件与窗口。
- Microsoft.Graphics.Win2D 1.4.0（NuGet），Direct2D GPU 台面和镜片。
- Windows SDK .NET 投影 10.0.19041，最低目标 Windows 10 2004；Windows 11 是主要验收平台。最低目标不等于已验证所有旧系统。
- Microsoft 官方 [Win2D 入门](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/quick-start)、[部署概览](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/deploy-overview)、[发行通道](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels) 已查询；具体 API 通过实际还原和编译确认。

## 模块边界

`Core` 不依赖 WinUI：文档、相册、几何、灯光响应、幻灯运动、更新平台筛选及持久化。`App/State` 将文档与临时 UI 状态分离；300ms 防抖保存，关闭同步刷盘。`App/Windows` 组织窗口、菜单、相册编辑、引导及恢复。`Interaction` 处理鼠标、键盘和拖放。

`Imaging/Decoding` 使用 WIC 对输入进行缩放解码、EXIF 方向处理和 ICC → sRGB 转换。缩略图和高清解码各有独立并发限制；相同文件/修改时间/长度/尺寸请求合并。内存缓存 160MiB，预览磁盘 750MiB，缩略图磁盘 100MiB。缓存只允许在应用 cache 子目录淘汰。

`Rendering/Lighting` 在 Direct2D 生成 source / lit / boosted / dark / halo 五层静态纹理；拖动与镜片不重新解码、不逐帧做模糊。共享 32³ 浮点 LUT 执行线性灯光增益和高光肩部压缩。`Rendering/Canvas` 复用光学纹理、按视野剔除、圆形裁切和坐标变换完成台面与镜片；GPU 预算 384MiB。设备资源重建时废弃旧代请求与纹理。

0.1.72.2按用户要求将dark槽用于开灯0%的共同基准。LampState与凑近状态独立，等可见GPU片面就绪后才开始2s高光时钟；亮度掩模在HLSL中保护暗部与中间调。Space/Tab由根Preview事件提前消费，避免焦点Button把Space转换成灯光Click。MagnificationSwitch的单胶囊滑块和镜片倍率共享.26s目标时长；应用默认显式动画，独立减少动态效果配置。时钟完成仍提交最后一帧。

ResourceEpoch为每代资源提供可取消就绪任务，旧完成不能标记新代就绪；后台真实设备错误经IsDeviceLost确认后才RaiseDeviceLost。CanvasControl新设备重建台面、膜层、收片，幻灯恢复当前图而不重播声音。DPI变化保留不依赖DPI的静态资源；跨设备未就绪时不混用图像。诊断使用Show(false)，不主动抢占用户前台。

设备替换诊断不在Window.Closed时提前关闭仍由Win2D控件持有的CustomDevice；控件完成异步卸载后释放引用。关闭后的绘制/计时/刷新均受生命周期保护；隔离诊断崩溃日志写入其data-root。验收必须同时检查报告和进程退出码，不能只读Passed=true。

幻灯是独立原生全屏窗口，保留原台面模型。随机顺序仅属于播放状态；旧片保留至新片解码完成，随后启动出片、黑场、入片。预计算边缘、投影暗角与焦点软图。音频使用 MediaPlayer，音量 .20、首张和换片两个 cue；默认原创合成，允许用户提供已有授权的本地 WAV。

播放状态提取为可注入解码器、时钟、延迟和声音cue的 `ProjectionPlayback`；取消代号防止迟到结果更换当前画面，展示就绪前不发出首次声音。`ProjectionStore`以租用引用保护当前图，缓存最多3个可淘汰条目；静态sharp/soft/glow来自lit透光层。84%双轴fit与80/120/180ms换片参数来自固定基线。`CollectionRenderer`只捕获可见底栏缩略图，以Transform3DEffect执行盒前后分层收片，台面文档不参与动画。

`AlbumMembranes`按袋格几何缓存最多32个前/后矢量命令列表，装订孔为真正的透明排除区域。目录使用原始source图像且不继承台面旋转；底栏从GPU生成138×132相册微缩图。

## 数据目录

`%LOCALAPPDATA%/LightTable/` 下分为 `config`、`layouts`、`cache/previews`、`cache/thumbnails`、`logs`、`updates`。`--data-root <路径>` 仅用于隔离测试。布局写临时文件、JSON 完整性校验、Flush(true)、File.Replace 和 .bak 备份。照片来源始终以只读 FileStream 打开，不重新压缩或覆盖。

Mac Saved迁移独立于正常Windows加载：明确根目录映射，拒绝不安全/歧义路径，方向后几何仅经WIC元数据读取；先备份当前Windows布局与Mac原字节再提交文档。缺失引用保留。详见MAC_MIGRATION。

## 更新边界

读取 GitHub 正式 Releases，仅筛选更高版本 Windows x64 EXE。没有 Windows 包时保持当前应用。下载使用 HTTPS，支持 Windows 专用 SHA256 清单；没有可信发行者签名配置，自动执行安装保持禁用，用户手动安装。SHA256 不表示发行者身份可信。

## 尚未闭合的架构验收

已实现LoupeEdge与FusedOptical两个HLSL，后者替换逐帧LUT生成；原子保存增加独立恢复文件保护。GPU条目仅保留图像元数据，按实际显示细节保留mip；稳态图像与相机命令列表复用。P3/HDR链路、巨量照片长期淘汰稳定性、光学色彩一致性、设备丢失和跨DPI迁移仍需实现或验证。
