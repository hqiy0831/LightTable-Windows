> **最新0.1.72.5：** [本轮实现、真实输入、失败记录与待验收范围](CHECKPOINT_0.1.72.5.zh-CN.md)。下文为保留的历史记录。

> **最新检查点0.1.72.4：** 见[本轮实现、真实证据与未完成事项](CHECKPOINT_0.1.72.4.zh-CN.md)。此页下文保留历史检查点，不应覆盖最新状态。

# 六项交互与灯光修复 · Windows 0.1.72.2

本报告对应用户提出的六项问题。Windows构建版本升为0.1.72.2；原Mac基线仍为v0.1.72，Swift源码未修改。

| 问题 | 实现修改 | 实际验证及边界 |
|---|---|---|
| 按住空格缺少凑近过渡 | 本应用默认启用动画，增加「观片 → 减少动态效果」独立开关；16ms计时推进.50s进入/.34s返回，打断保持当前位置 | 真实UI计时和GPU中途帧通过；本机SystemAnimationsEnabled实测false，应用ReducedMotion实测false；真实持续按键见下方限制 |
| 松开空格后关灯 | Space/Tab由PreviewKeyDown/Up提前消费，防止焦点按钮收到Space并生成Click；临时凑近状态与LampState独立 | 生产状态检查IgnitionAt不重置；重新授权后真实Space短按、组合及Button焦点Space按下/松开均保持lit=True |
| 关灯亮度不对 | 亮度滑杆支持0–160%；关灯与开灯0%共用同一静态光学基准，台面背景使用同一基准 | 单片及整台GPU像素逐通道差≤1/255通过，避免两套采样路径造成差异 |
| 点灯缺少高光过曝回落 | 等可见片面就绪后启动点灯时钟；峰值.16s、保持到.28s、约2s结束；线性亮度高光掩模驱动曝光，暗部/中间调受保护 | 实际HLSL/GPU灰阶fixture：高光峰值+14、近白+16；暗部/中间调0；2s逐像素回到稳态 |
| 凑近时Tab切倍率无动画 | 独立.26s可中断变焦时钟，原倍率与8×几何连续混合；重复按键不反复重启目标 | 凑近状态下正反两个方向实际中间值通过；真实Space+Tab短组合日志确认close=True时eight切为True并保持灯亮；持续长按组合未执行 |
| 两倍率需同一胶囊和滑块 | 一个144×34胶囊，内部70px宽选中滑块；两侧可点击，并支持横向拖动选择；Tab与点击共用状态 | 真实XAML截图与计时检查通过；重新授权后外部坐标点击8×、从8×拖回原倍率，视觉确认滑块中途和原倍率终点 |

## 两秒高光的像素证据

下表来自实际Direct2D/HLSL读回；数值是相对稳态的8位灰阶增量。它不是整张图的白色覆盖层。

| 点灯后秒数 | 黑/暗部/中间调 | 高光200 | 近白235 |
|---|---|---|---|
| .16 | 0 / 0 / 0 | +14 | +16 |
| .28 | 0 / 0 / 0 | +14 | +16 |
| .60 | 0 / 0 / 0 | +13 | +15 |
| 1.10 | 0 / 0 / 0 | +8 | +13 |
| 1.60 | 0 / 0 / 0 | +1 | +4 |
| 2.00 | 0 / 0 / 0 | 0 | 0 |

源Mac settling为1.10s；本次按用户要求改为2s，并将曝光限制到高光区域。关灯基准也按用户要求替换原版近黑dark层。原始基线记录保留，不能将这些有意调整称为逐像素复制Mac。

## 输入验证限制

computer-use已观察到独立0.1.72.2窗口及其真实控件。首次点击被工具以“user input was detected in this window”拒绝；重新观察时窗口已被最小化。因此停止桌面输入，没有将这些操作记为成功，也没有操作F盘现有程序。隔离测试进程随后清理，数据保留在TestResults/ui-user-bugs-*。

用户随后明确“你现在可以执行键鼠操作”，已对最终发布目录的独立实例重新测试：Space短按/松开保持灯亮；Space+Tab在held=True、close=True时切为eight=True；灯光Button焦点的Space按下/松开均被消费且lit=True；实际坐标点击8×和拖回原倍率观察到滑块移动和终点。日志ui-authorized-input.log与ui-authorized.json绑定实际EXE哈希。滑杆0%尝试再次遇到输入保护，未执行；之前的GPU像素检查仍是该项证据。

目前工具press_key接口没有独立按下/保持/松开时长参数，不能假造持续物理长按输入证据。其真实短组合约10ms；持续动画回归是生产状态保持一段时间，驱动真实DispatcherQueue计时、WinUI控件和GPU帧，验证范围分别记录。

动画检查还要求实际Canvas Draw提交进入/倍率/返回的精确终点帧，不仅检查时钟值；完成瞬间会请求最后一帧，避免停在略接近终点的位置。

微软说明Button会将Space/Enter转为Click，预览键事件可提前处理快捷键；参见[键盘事件](https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-events)和[预览快捷键处理](https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-accelerators)。实现已由冒泡事件改为预览事件。

## 此前进行中的设备重建工作

该项已先行完成有限验证：资源代次可失效、等待并拒绝迟到结果；后台设备错误与普通图片错误分开；DPI变化保留静态纹理；收片重新加载静态图；幻灯重建当前图而不重播首张声。真实CanvasControl先替换新硬件设备、再替换WARP，以及控件DPI 125/150/200%检查通过。

这是应用设备替换及控件DPI，不是禁用显卡、驱动拔除或系统多显示器验收。健康设备不能用RaiseDeviceLost强行模拟；参见[微软设备丢失处理](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/handling-device-lost)。诊断入口采用AppWindow.Show(false)，不主动抢占前台。

最终包初测曾出现“20项报告通过但关闭后进程崩溃”：设备替换诊断在控件异步卸载前显式关闭CustomDevice，触发ObjectDisposedException。已改为让Win2D控件完成设备引用释放，并防止关闭后的绘制/计时回调；不是吞掉异常。源码后续20项通过且进程退出0、无crash.log。失败证据保留于device-shutdown-failure.json。诊断窗口现置底并禁用实时鼠标命中，防止用户移动鼠标改变读回fixture；这不属于真实输入验证。

最终便携包独立解压复测20项通过、进程退出0、无隔离crash.log；73项核心测试通过，最终Solution编译0警告0错误。原始证据：`docs/evidence/user-bug-core.trx`、`user-bug-integration.json`、`device-integration.json`、`portable.json`和`magnification-capsule.png`；最终核验记录为release-final.json。当前安装回归仍因已有用户快捷方式保护而未执行；以便携验收结果和SHA清单识别新构建。F盘旧快捷方式不会自动指向这个新版。
