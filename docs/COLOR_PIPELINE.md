# 当前色彩链路与宽色域验收边界

当前生产链为：原图只读 → WIC按EXIF方向缩放、ICC转换到sRGB → 预乘BGRA8分级预览 → 五层sRGB BGRA8光学缓存 → 单次FusedLamp着色器（解预乘、sRGB解码为线性光、增益/高光肩部、重新编码及预乘） → SDR CanvasControl。

0.1.72.4已把亮度权重修正为sRGB对应的(.2126,.7152,.0722)，并以配方v4使原P3权重光学缓存失效。0.1.72.5将既有显示特效合并为一次四输入shader；45组实际GPU对照最大差1/255，含0/64/128/255透明度。静态层算法和缓存内容未因此改变，不为仅改变显示通路再次重建五层缓存。

这条链只有灯增益/曝光运算处于浮点线性光，散射静态层仍沿用当前sRGB编码值的混合逻辑。WIC早期转换已丢失超sRGB信息，不能声称保留Display P3。普通台面栅格缓存也不会增加源图色域或精度。

## 下一阶段必须一起修改

1. 从解码层保留源ICC、原色坐标、传递函数和高位深数据；不能先经过ColorManageToSRgb再转换回P3。
2. 明确定义线性Display P3或scRGB工作像素格式，使用FP16/浮点离屏目标。给CPU/GPU/磁盘缓存同时加色彩空间、像素格式及配方身份，并修订字节预算和有效数据校验。
3. 在该工作空间核对高光提取权重、屏幕/加法混合、模糊与黑位保护；所有中间值在最终显示映射前保留负值/超1值。
4. 在最终输出端处理显示器色彩状态。微软指出Win2D Advanced Color需要较低层CanvasSwapChain/CanvasSwapChainPanel；CanvasControl SDR目标不足以证明宽色域显示。应保留受控SDR回退，处理DPI/设备恢复和跨显示器输出变化。
5. 用带ICC的P3/AdobeRGB/JPEG/PNG/TIFF、EXIF方向、透明边、灰阶/高光与超sRGB色块验证，比较转换前后数据。需要证明输入宽色域没有提前裁剪，线性增益发生在正确原色空间，而不是只观察屏幕“更鲜艳”。最终仍需实际宽色域显示器和Mac参考验证。

## 官方依据

- [W3C sRGB线性亮度定义](https://www.w3.org/WAI/WCAG21/Understanding/relative-luminance.html)
- [微软Advanced Color显示链与FP16/scRGB](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range)
- [WIC原生像素格式](https://learn.microsoft.com/en-us/windows/win32/wic/-wic-codec-native-pixel-formats)
- [Win2D ColorManagementEffect](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_ColorManagementEffect.htm)
- [Win2D预乘alpha](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/premultiplied-alpha)

完整宽色域输入、缓存与显示尚未实现，不将此文档当成功能完成证据。
