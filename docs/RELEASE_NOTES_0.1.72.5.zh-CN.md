# LightTable for Windows 0.1.72.5 · 开发预览

Windows x64 未签名开发候选，完整移植尚未验收。安装和操作方法见 [仓库首页](../README.md)。

## 本版变化

- 在一个胶囊分段控件中切换实际 1× / 2× / 4× / 8× 凑近倍率，支持点击、拖动、Tab 和平滑插值。
- 保留 Space 凑近/返回动画，松开 Space 保持灯开关状态。
- 修复选中照片后平移台面导致照片额外跟随移动的问题。
- 合并 GPU 灯光着色器，增加受预算约束的稳态场景缓存，并修复缓存资源释放问题。
- 修正 sRGB 光度系数；完整 P3 / FP16 仍未实现。
- 修复相册弹窗尺寸适配、预览空白及装片/交换后不刷新。
- 安装器版本改为从发布 DLL 读取，包内资源包含新增着色器。

## 下载

安装版：`LightTable-Windows-Setup-x64.exe`。

便携版：`LightTable-Windows-Portable-x64.zip`，完整解压后运行 `LightTable.exe`，保留同目录全部运行时、DLL 和 PRI。

目标 Windows 11 x64 23H2+；无需 Visual Studio 或开发 SDK。程序未签名。卸载保留用户布局与配置。更新前备份 `%LOCALAPPDATA%\LightTable\layouts`，并确认旧快捷方式没有启动旧版本。

## 已验证

- 核心测试 88/88，无跳过。
- 最终便携 ZIP 校验并全新解压，原生检查 25/25，进程退出 0。
- 独立 AppId / 快捷方式的安装沙盒两轮安装、卸载、重装；安装后的 25 项原生检查通过，原有生产注册信息及快捷方式哈希保持一致。
- GPU 融合灯光与参考图 45 组对照，最大像素差 1/255。
- 实际倍率输入、Space 释放灯状态以及相册装片/交换/恢复有操作证据。

这些结果来自开发机，不能代替干净系统、普通安装覆盖升级、全部真实输入及完整视觉等价验收。详细成功与失败记录保留在 [检查点](CHECKPOINT_0.1.72.5.zh-CN.md)。

## 尚未完成

完整 Display P3 / FP16、大图库 60 FPS、干净 Windows 安装、多屏/全部触摸/全部格式、长期稳定性、完整原版功能及视觉等价、正式签名和部分第三方二进制许可审计仍未闭合。应用内上游更新检查不能保证指向此 Windows 仓库，请手动从本仓库 Releases 下载。

## 构建文件 SHA256

```text
1cc06b5d4858b9700de1df4eb7666f805bff22fbe9d4c0ddc41a92b99b2e906a  LightTable-Windows-Portable-x64.zip
3de30acd6d4119c6f7c5948edfbaae5d442f83b238a868a1b05797fd0cd54940  LightTable-Windows-Setup-x64.exe
```

哈希用于完整性核对，不证明发行者身份。保留上游 MIT 版权和第三方许可声明。
