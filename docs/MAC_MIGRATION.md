> 当前 Windows 0.1.75.2：按用户明确要求移除相册功能、底栏全部照片常驻并标记已上台。旧相册照片迁入原栏目且先独立备份；schema 4，旧版请勿回写。最新范围及验收见 [0.1.75.2 检查点](CHECKPOINT_0.1.75.2.zh-CN.md)，下文保留历史。

# Mac 布局迁移

读取基线 v0.1.72 的 `TableModel.Saved` JSON：Mac 默认位置为 `~/Library/Application Support/OurLightTable/layout-v1.json`。导入不需要修改 Mac 文件。

1. 将照片复制到 Windows，保持所选来源根目录下的相对文件夹结构。
2. 在「文件 → 导入 Mac 版布局」选择 JSON。
3. 输入原 Mac 文件夹，例如 `/Users/name/Pictures`，选择对应 Windows 文件夹，例如 `D:\Photos`。不同磁盘、图库或缓存位置可添加多个映射；较长的根目录优先。
4. 检查照片、散片、相册、缺失和无法读取的文件数量。确认后导入。

会保留散片 UUID、归一化位置、宽度、旋转、世界尺寸、fitScale、相机、相册袋格和整页位置、混合层级。Foundation CGPoint 的 `[x,y]` 编码已处理。缺少相册或相机字段的旧布局使用原版默认值。

仅识别 `Library/Caches/com.apple.SwiftUI.Drag-<UUID>/` 形态的旧拖片副本，且映射后的副本与原图库同名文件长度和 SHA256 都相同，才合并引用；保留上层散片的 UUID 和几何。其他同名文件、不同字节和孤立缓存不会合并。这里的 SHA256 用于本地内容比较。

不接受网络 URL、未映射根目录、越界路径、Windows 保留文件名或两个 Mac 文件映射为同一 Windows 路径。缺失文件仍保留布局；文件存在但无法解码会在启动准备中提示。

提交前先保存当前内存布局，再在 `%LOCALAPPDATA%/LightTable/layouts/imports/<时间-随机ID>/` 存下 `windows-before-import.json` 和 `macOS-layout-v1.json`。Mac JSON 为导入时原字节，随后通过原子替换保存 Windows 格式。归档或保存失败不会替换当前内存文档。用户照片始终只读。

验证范围：核心合成 Saved fixture、真实 WIC 方向尺寸、Session 替换、独立 Store 恢复、GPU 绘制与备份字节已通过；真实 Mac 产生的用户布局、完整文件夹选择/确认 UI 和跨系统实机操作仍需验收。

WIC 尺寸读取使用 [BitmapFrame 的方向后尺寸](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapframe?view=winrt-19041)，只读取几何，不为整个图库生成高清预览。
