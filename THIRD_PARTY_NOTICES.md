# 许可与素材说明

LightTable 原项目代码：Copyright (c) 2026 fuyuan9972-svg，MIT，完整 LICENSE 随附。上游完整说明保存在 `upstream/THIRD_PARTY_NOTICES.md`，应用包含 `UPSTREAM_THIRD_PARTY_NOTICES.md`。

Windows 使用 .NET 10、Microsoft.WindowsAppSDK 2.5.1、Microsoft.Graphics.Win2D 1.4.0 及其依赖，各组件的版权与许可适用相应 NuGet 包中的声明；Microsoft 运行时随 self-contained 构建打包。构建脚本提取17个应用/构建包的 nuspec、license/notice 文件及运行时许可到 licenses/。Win2D 1.4.0 的 nuspec EULA URL 当前重定向至无关网页，因此保留原链接、上游源码 MIT LICENSE 和明确的来源说明，不能把无关页面伪称为 EULA；二进制包许可的精确核对仍是发行审计未闭合项。Sparkle 不参与 Windows 运行或打包。

Windows 默认机械音效由本工程数学合成，代码位于 Audio/MechanicalAudio.cs，作为项目代码按 MIT 提供。没有将上游非 MIT 原录音复制进 Windows 发行包。本地用户可放置已有合法授权的 WAV；此能力不为第三方音频授予许可。

开发测试使用 NASA/JPL 公开地球影像 PIA00122、PIA00123、PIA00342，来源记录在本地 TestResults/fixtures/nasa-sources.json。测试照片不随程序分发，不表示 NASA 对本应用的认可。使用遵循 [NASA 媒体指南](https://www.nasa.gov/nasa-brand-center/images-and-media/)。
