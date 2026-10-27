> 当前 Windows 0.1.75.2：按用户明确要求移除相册功能、底栏全部照片常驻并标记已上台。旧相册照片迁入原栏目且先独立备份；schema 4，旧版请勿回写。最新范围及验收见 [0.1.75.2 检查点](CHECKPOINT_0.1.75.2.zh-CN.md)，下文保留历史。

> 当前版本 0.1.75.1：安装器版本从发布目录 LightTable.dll 读取；实际验证见 [最新检查点](CHECKPOINT_0.1.75.1.zh-CN.md)。源/便携/安装验证必须绑定同一托管 DLL 与三个 shader，不能只绑定 EXE 宿主。下文旧检查数量为历史。

# 构建和发行

开发依赖：Windows x64，.NET SDK 10.0.403（可按 global.json 使用相同 feature band），NuGet 联网。App.xaml 编译和 Windows SDK .NET 工具通过 NuGet 还原，不需要安装完整 Visual Studio。

```powershell
git submodule update --init --recursive
dotnet restore LightTable.Windows.sln
dotnet build LightTable.Windows/App/LightTable.Windows.csproj -c Release
dotnet test LightTable.Windows/Tests/LightTable.Core.Tests.csproj -c Release --logger trx
dotnet publish LightTable.Windows/App/LightTable.Windows.csproj -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -o dist/Windows-x64
```

本机隔离 SDK：`%LOCALAPPDATA%/LightTableBuild/dotnet/dotnet.exe`；若未加 PATH，可直接用完整路径。生产用户运行发布目录里的 LightTable.exe，不依赖这个开发 SDK。

全部打包入口：

```powershell
./LightTable.Windows/Scripts/Build-Release.ps1
```

可传 `-Dotnet <dotnet.exe>`、`-Iscc <ISCC.exe>` 或 `-SkipInstaller`。EXE 由官方 Inno Setup 7 编译；源码脚本在 Installer/LightTable.iss。构建会先执行核心测试，再 publish、复制文档、生成便携 ZIP 和未签名 EXE，并生成 SHA256SUMS-Windows.txt。

预期输出：`dist/Windows-x64/`、`dist/LightTable-Windows-Portable-x64.zip`、`dist/LightTable-Windows-Setup-x64.exe`。文件存在不等于安装和运行验收通过，以 TEST_REPORT 中实际记录为准。

`Test-Installer.ps1`在工作区TestResults/install-sandbox执行两轮真实静默安装/卸载并启动已安装应用的集成检查；会先确认没有已有LightTable注册和快捷方式，避免覆盖用户现有安装。旧检查点两轮通过；当前预检因F盘用户快捷方式停止，未修改现有安装。这个脚本不代表干净系统验证。

`Test-Portable.ps1`核对本地SHA清单、解压到全新TestResults目录、检查随包运行时和PRI，再运行原生集成。当前16项通过。`build-manifest.json`记录源码与核心二进制哈希，仅用于构建追踪，不代表发行者签名。

WinUI资源发布额外检查`LightTable.pri`；不能只复制EXE或省略PRI/XBF。曾因缺PRI出现安装成功但运行崩溃，现已修复并保留失败记录。

安装按当前用户写入 LocalAppData/Programs/LightTable，提供开始菜单和可选桌面快捷方式。卸载仅移除程序目录，保留 LocalAppData/LightTable 的布局与配置。不得将 cache 清理脚本指向 layouts 或照片目录。

正式发行前仍需：逐包第三方许可证审计、应用图标、可信签名方案、Windows 专用 release 资产、原照片异常测试、DPI/多屏和干净 Windows 的安装/卸载/重装。当前所有构建均明确标为未签名开发构建。

## 已有正式安装时的隔离验收

默认Test-Installer.ps1仍会保护既有安装和快捷方式。可运行`Test-InstallerSandbox.ps1 -Iscc <ISCC.exe>`，使用同一.iss与发布目录，在独立AppId和独立快捷方式下执行两轮安装/卸载/重装及隔离原生检查，前后比较既有安装注册信息与快捷方式哈希。此方式不等于普通安装包覆盖升级验收或干净Windows验收。

压力脚本默认300秒watchdog，允许通过-TimeoutSeconds调整；超时不是通过。保留benchmark-progress.json和原失败目录。
