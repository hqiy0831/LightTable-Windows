> **最新0.1.72.5：** [本轮实现、真实输入、失败记录与待验收范围](CHECKPOINT_0.1.72.5.zh-CN.md)。下文为保留的历史记录。

> 0.1.72.4新增四档倍率与选中后平移修复，当前88核心/22原生；真实输入与未验收范围见[检查点](CHECKPOINT_0.1.72.4.zh-CN.md)。以下历史测试范围仍保留。

# 功能移植矩阵

基线：v0.1.72 / `1a998cfce9096ac7b42e26aba67c922885fd43e2`。Mac 路径前缀 `upstream/Sources/LightTableApp/`，Core 文件位于 `upstream/Sources/LightTableCore/`。Windows 路径前缀 `LightTable.Windows/`。

状态只用 Not Started / Implemented / Tested / Blocked。Tested 仅表示本行所述测试范围，不能扩展至整个子系统；Implemented 仍需运行验收。未闭合的视觉、动画和异常项保持独立，不因相邻行已实现而删除。

| ID | 原版功能与边界 | macOS 实现 | Windows 实现 | 状态 | 实际测试 / 待测 |
|---|---|---|---|---|---|
| I01 | 单张/多张真实文件导入，中文路径 | TableModel.importURLs, PhotoLibrary | Imaging/Decoding/PhotoImporter | Tested | 3 张 NASA JPEG 经参数导入与显示；文件选择器确认路径仍待测 |
| I02 | 选择文件夹替换图库但保留台面 | TableModel.chooseFolder/importURLs(replace) | MainWindow.ChooseFolder, TableSession.ImportAsync | Implemented | 待实际递归导入与替换验证 |
| I03 | 添加照片追加图库 | TableModel.choosePhotos | MainWindow.ChoosePhotos | Implemented | picker 已打开，完整 UI 导入待测 |
| I04 | 递归、隐藏文件和包/链接边界 | PhotoLibrary.scan | PhotoImporter.Scan（跳过目录 reparse） | Tested | 实际递归/点隐藏/Hidden属性/已知Mac包目录跳过通过；其余包类型和reparse fixture待测 |
| I05 | 文件/文件夹外部拖放到图库 | PhotoDrag, TableModel.acceptDrop | MainWindow tray.Drop | Implemented | 待真实拖放 |
| I06 | 外部拖放到台面，沿拖放时视野坐标放置 | TableModel.acceptDrop | TableInput / DropAnchor / AcceptedPaths | Implemented | 已接入释放时相机/参考尺寸快照；核心相机后改仍不偏移通过，重复内容解析回原引用；真实拖放待测 |
| I07 | 内部拖放只传引用，无临时副本 | PhotoDrag.Reference | DataPackage LightTable.Path/Album | Implemented | 拖放及重复来源检查待测 |
| I08 | 重复路径去重，重复放置移动既有散片并置顶 | TableLayout.place | Core/TableDocument.Place | Tested | RepeatedPlacementMovesOneSlideAndBringsItForward |
| I09 | 重复内容识别 | 原版仅 canonical path 去重 | PhotoImporter SHA256 | Tested | 集成检查：相同路径与不同路径相同内容均跳过 |
| I10 | 自然排序、同名路径稳定次序 | PhotoLibrary.scan | NaturalPathComparer | Implemented | 递归中文照片2/3/10实际排序通过；完整locale与同名待测 |
| I11 | 损坏/缺失/占用文件提示，单张失败不清布局 | PhotoStore, TableModel.message | ImageStore, TableSession.Notice | Implemented | 各异常待测 |
| I12 | JPEG、EXIF 方向与 ICC | ImageIO thumbnail with transform | WIC + ColorManageToSRgb | Implemented | 普通JPEG显示；TIFF EXIF1–8逐像素和方向几何通过；JPEG方向/ICC fixtures仍待测 |
| I13 | PNG、TIFF、BMP | ImageIO | WIC | Tested | 不透明首帧16×12逐字节往返、PNG 0/128 alpha预乘差≤1通过；多页仍待测 |
| I14 | HEIC、WebP、AVIF、RAW 系统 codec | UTType.image / ImageIO | WIC 已安装 codec；失败提示 | Implemented | 尚无逐 codec 兼容性结论 |
| I15 | Unicode/长路径 | Foundation URL | Unicode .NET/WIC + longPathAware | Tested | 实际WIC读取>300字符中文路径成功；系统picker长路径待测 |
| I16 | 原图不修改、不删除、不重压缩 | TableLayout, PhotoStore | 只读 FileStream；文档移除 | Tested | 核心 remove/save/backup 测试验证原字节哈希；UI 删除路径待测 |
| T01 | 底栏胶片袋 104×70，照片 90×56 | FilmSleeve | MainWindow ItemTemplate | Tested | 3 张缩略图真实截图；薄膜外观仍待比对 |
| T02 | 底栏水平滚动/惰性缩略图 | ContentView.tray, TrayPhoto | ListView/ItemsStackPanel | Implemented | 超量图库虚拟化待测 |
| T03 | 台面已放照片标记 | FilmSleeve.onTable | TrayItem.Mark | Tested | 平铺后 3 张显示已上台 |
| T04 | 底栏双击/菜单居中放片 | TrayPhoto / contextMenu | PlaceTray | Implemented | UI 待测 |
| T05 | 在文件管理器中显示原图 | ContentView tray context | MainWindow.Reveal | Implemented | UI 待测 |
| T06 | 拉出袋口、放大、着陆与返回 | SleeveMotion, SleeveFlight | TrayInput / SleeveMotion / GPU flight | Implemented | 核心袋口清除、抓取点与插值通过；完整拖出/落片/取消UI待测 |
| T07 | 拖出到边缘自动平移 | TableViewport.edgeVelocity | SleeveMotion.EdgeVelocity / TickFlight | Implemented | 核心中心零速度及边界方向通过；持续拖动UI待测 |
| B01 | 自由移动散片/重叠/跨台面边缘 | SlideView, TableModel.update | TableInput | Implemented | 拖动与多片移动待测 |
| B02 | 无边框图像和细薄接触沿 | FilmContactTexture | Rendering/TableRenderer.DrawFilm | Implemented | 已观察，静态共享材质精确外观待补齐 |
| B03 | 普通拖动不闪蓝框，点击才选中 | TableModel.selectionIndicatorsVisible | ShowSelection，3px 拖动阈值 | Implemented | 待录制拖动验证 |
| B04 | 单击、Ctrl 多选、取消选择 | TableModel.select | TableInput / TableSession | Implemented | 组合输入 UI 待测 |
| B05 | Shift 框选旋转片 SAT 相交 | CanvasSelection, TableModel.intersects | Placement.Intersects | Tested | 核心非 AABB 假阳性用例；UI 框选待测 |
| B06 | 框选 Esc/失焦取消 | CanvasSelection.cancel | CancelGesture / Activated | Implemented | 失焦、快捷键冲突待测 |
| B07 | Delete 批量散片移出，相册回到底栏 | TableModel.removeSelected | TableDocument.Remove | Implemented | 核心保留原片通过，UI 批量待测 |
| B08 | Ctrl 点选时置顶及混合相册层级 | tableOrder / layerIndex | TableDocument.TableIds/BringToFront | Tested | 核心相册/散片顺序保存测试；UI 待测 |
| B09 | 单张旋转、缩放、摆正、±15° | SlideView gestures/context | 手柄、Alt 拖动、右键菜单、触摸 | Implemented | Windows 输入映射待测 |
| B10 | 世界与屏幕变换/锚点缩放 | TableViewport | Core.Viewport | Tested | 1000 组相机往返和锚点随机用例 |
| B11 | 滚动平移、Shift 横向、Ctrl 缩放 | CanvasNavigation | TableInput.PointerWheelChanged | Implemented | UI 待测 |
| B12 | 整理全局恢复默认尺寸且保持旋转 | fitToWindow | Geometry.Fit | Tested | 100 张混合比例核心布局用例；相册混排视觉待测 |
| B13 | 一键平铺替换散片，不清空图库 | placeAllPhotos | Geometry.Fit(replace) | Tested | 3 张真实 UI 平铺；核心 100 张来源保留 |
| B14 | 清空台面保留相册内容与图库 | TableModel.clear | TableSession.Clear | Implemented | UI 待测 |
| B15 | 调整窗口不改变世界坐标 | referenceSize/worldSize | Document.WorldWidth/Height | Implemented | 最大化、恢复、resize 待测 |
| L01 | 开关灯/多图一致状态 | TableModel.isLit/LightResponse | TableSession.ToggleLight / shared sample | Tested | 点击开灯，3 图由暗变亮；关灯与中断待测 |
| L02 | 亮度及双击100% | 原版10–160% BrightnessSlider | 用户要求0–160% / Slider | Implemented | 0%原生像素基准通过；实际滑杆输入待测 |
| L03 | 开灯高光过曝及回落 | 原版.12/.16/.28/1.10 | 用户要求.12/.16/.28/2.0 | Tested | 73核心含2s精确稳态；实际GPU高光+14/+16后归零、暗部/中间调0变化；不再声称原时长逐像素等价 |
| L04 | 关灯等于开灯0% | 原版dark .032+.0015已按用户要求替换 | 共用GPU零亮度基准 | Tested | 单片及整台逐通道差≤1/255；取代旧近黑层 |
| L05 | 高亮 .60–.92/rim/diffusion/boost scatter | renderTransmission | OpticalPhoto / OpticalTables | Implemented | GPU 绘制成功；色域、核半径和图像对比待测 |
| L06 | 亮部局部 halo、black protection | renderSwitchOnHalo | OpticalPhoto halo | Implemented | 待 GPU 像素检验 |
| L07 | 线性增益、.80/.85 肩部、高光色相保护 | PhotoOptics.fusedOptical | FusedOptical.hlsl | Implemented | 去除逐帧LUT纹理生成；GPU灯光与镜片快照通过，颜色金标准待测 |
| L08 | Display P3 一致工作空间 | PhotoOptics/OpticalDisplay | 当前 sRGB | Not Started | Windows 色彩管理链路待完善 |
| L09 | 减少动态效果 | accessibilityReduceMotion | 用户要求默认动画开启/独立菜单选项 | Implemented | 系统AnimationsEnabled=false时应用动画中间帧仍通过；减少效果菜单实际输入待测 |
| M01 | 圆镜260 / 1.5× / 跟随/层级/坐标 | LoupeGeometry/LoupeRenderer | TableRenderer.Scene + circle clip | Tested | 实际截图显示圆形放大；旋转/重叠/相册待测 |
| M02 | 放大镜模式隐藏选择反馈 | SlideView, TableModel.showsSelection | TableRenderer selection gate | Implemented | 选择后切换待测 |
| M03 | Space 按住进入、松开恢复、失焦取消 | LoupeKeyboard/LoupeExperience | TableInput KeyDown/Up/Activated | Implemented | 工具不支持持续按键的路径尚未验收 |
| M04 | 原倍率场宽与固定8×数学 | LoupeCloseMagnification | Core.LoupeGeometry | Tested | 3 种窗口尺寸核心用例 |
| M05 | Tab、点击、拖动倍率胶囊滑块 | LoupeMagnificationControl | MagnificationSwitch / PreviewKeyDown/Up | Implemented | 实际XAML单胶囊、滑块中间位置和双向倍率动画通过；键鼠输入因用户活动停止，未记通过 |
| M06 | .50/.34 凑近轨迹与 .26 连续变焦 | LoupeExperience close pose | LoupePose / TransitionClock | Implemented | 36项核心测试含凑近几何和中断连续性；GPU中途快照有效，原版视觉/按键待测 |
| M07 | 外侧90%起最大0.6%径向折射 | LoupeRenderer.edgeKernel | LoupeEdge.hlsl / PixelShaderEffect | Implemented | HLSL实际编译运行、中心图像与圆外透明验证通过；lip与macOS像素比对待做 |
| M08 | 独立2200高清，不每次读取原图 | PhotoStore/preparePreview | ImageStore HD bucket / GPU reuse | Implemented | cache 命中和移动无IO待测 |
| M09 | 一在途+可替换末次请求，避免过期帧 | LoupeRaster, OpticalDisplay | 直接 GPU 场景，代号废弃加载 | Implemented | 取消/过期小图/快速输入待测 |
| A01 | 建页、命名、选择、删除 | AlbumEditor | MainWindow.Albums | Implemented | UI 待测 |
| A02 | 15×12 lattice /135 20格/120 9格 | Core.AlbumPage | Core.AlbumPage | Tested | 容量和不重叠测试 |
| A03 | 加格/混合规格/空页整页填充 | AlbumEditor.addPocket/fill | MainWindow.Albums | Implemented | 核心边界通过；UI 待测 |
| A04 | 格装片/替换/清空/移除袋格 | AlbumEditor.assign/context | MainWindow.Albums | Implemented | UI 待测 |
| A05 | 拖格移动、同尺寸交换、无效交换原子性 | AlbumPage.move | AlbumPage.Move | Tested | 核心混合拒绝和带照片交换通过；UI 待测 |
| A06 | 透明袋膜、装订孔、焊缝、照片不裁剪 | AlbumSurface / AlbumViews | AlbumMembranes / TableRenderer.DrawAlbum | Implemented | GPU9孔中心全透明、膜层对象共享、32项缓存上限通过；相册镜片/原版外观待比对 |
| A07 | 整页上台/拖动/层级/移出保留内容 | AlbumDrag / TableModel | TableSession / TableInput | Implemented | 核心来源与布局保存通过；UI 待测 |
| A08 | 相册位置、旋转、比例保存 | AlbumPage.position（原版无页角度） | AlbumPage.Position/Angle/Scale | Implemented | Windows扩展，UI 待测 |
| A09 | 相册整页飞行、拖动和边缘跟随 | AlbumViews gestures | TrayInput / TickTableDrag | Implemented | 整页飞行与边缘跟随代码已接入；真实连续输入仍待测 |
| A10 | 底栏真实相册微缩图与目录方向 | AlbumTrayCard / catalog AlbumArtwork | LoadThumbnail / AlbumPreview | Tested | 实际GPU138×132缩略图、69×66实时Image布局通过；目录不继承37°台面角度 |
| A11 | 大量旋转相册混排并保存小比例 | TableModel.fitToWindow | Geometry.AlbumBounds / Sanitize | Tested | 1/40/300页与散片混排角点全部在窗口内，JSON恢复比例不跳回0.1 |
| S01 | 独立全屏黑底，84%宽/84%高 | SlideshowWindow/ProjectionPhoto.fittedSize | SlideshowWindow / ProjectionMotion.Fit | Implemented | 84%双轴fit核心通过；全屏、多屏、DPI待测。原先88%记录为审计错误，已纠正 |
| S02 | 随机顺序、不改变图库顺序 | TableModel.enterSlideshow | SlideshowWindow.order | Implemented | UI 及重复进出待测 |
| S03 | 首次底栏收片进入幻灯盒 | SlideshowEntry | CollectionMotion / CollectionRenderer / MainWindow.Collection | Implemented | 1/12/40片核心轨迹、GPU透视/前后层/结束全黑/不改JSON通过；真实入口及取消UI待测 |
| S04 | 等待源加载完成再出片，慢源不跑空 | SlideshowPlayback.show | ProjectionPlayback | Tested | 慢源保留旧片、迟到结果释放、600ms提示、失败重试、打断和展示就绪闸门核心通过 |
| S05 | 出片80ms、黑场120ms、入片180ms及方向 | SlideshowMotion / Playback | ProjectionMotion / SlideshowWindow | Tested | 核心时长/位姿通过，真实动画待测 |
| S06 | 遮光器80ms和焦点180ms | SlideshowPlayback.reveal | ProjectionPlayback / ProjectionRenderer | Implemented | 核心时序、真实GPU零RGB黑场和soft-sourceOver/sharp-Add通道算式通过；可见动画待测 |
| S07 | lit透光、暗角/羽化/缓存soft/glow | SlideshowProjectionStore.render | ProjectionStore / ProjectionPhoto | Tested | 透光中心差≤1/255、sharp/soft透明角、8并发共享、3项LRU保留当前租用图通过；Mac像素比对待做 |
| S08 | 首张半段声/后续完整声/.20/反射 | SlideshowMechanicalSound | MechanicalAudio 本地替换接口 | Blocked | 原录音再分发授权不明确；默认合成声音色不同 |
| S09 | 合成机械声可分发、音量同步 | Windows 等价替代 | MechanicalAudio.Play/PrepareAsync | Implemented | 两路MediaOpened、禁止提前播放、.20音量、实际播放时钟前进与48kHz/.38s WAV通过；主观同步与原音色未验收 |
| S10 | 方向键循环、Esc/返回、台面不变 | SlideshowView/TableModel | SlideshowWindow | Implemented | 状态快照/声音/UI 待测 |
| P01 | 来源顺序、散片、相册、混合层级保存 | TableModel.Saved | TableDocument / DocumentStore | Tested | JSON往返与混合层级核心测试 |
| P02 | 相机/参考尺寸/fitScale 保存 | Saved.viewport/worldSize | TableDocument | Tested | 核心相机保存恢复 |
| P03 | debounce、退出刷新、原子替换/备份 | save/flushSave | TableSession/DocumentStore | Tested | 原子保存、坏JSON备份恢复核心通过；强杀恢复待测 |
| P04 | Windows schema 0 迁移 | Saved 可选字段 | Sanitize / SchemaVersion | Tested | 缺相册字段迁移通过 |
| P05 | macOS Swift Saved 导入及旧 drag alias 迁移 | remapping / legacyDragAliases | MacSavedImporter / MainWindow.Migration | Implemented | 显式根映射、最长前缀、安全路径、已验证别名/上层ID、旧可选字段核心与真实WIC/Session/GPU往返通过；真实Mac文件和完整UI待测 |
| P06 | 缺片保留布局、不丢其他照片 | TableModel init/startup | Sanitize/PrepareStartup | Implemented | 单片失败/损坏布局 UI 待测 |
| P07 | 导入前保护当前布局与Mac原JSON | remapping backup | DocumentStore.Import | Tested | 当前内存文档先保存，Windows与Mac原字节独立归档，随后原子替换；新Store恢复一致 |
| C01 | 256/384/512/768/1024/1536/2200分级 | TablePreviewSizing | Geometry.PreviewBucket | Tested | 核心边界通过；DPI 下显示待测 |
| C02 | 缩放保留旧图，120ms后升级 | prepareTablePreview / .task | TableRenderer.Get/lastTransformAt | Implemented | 快速缩放和延迟小图不覆盖待测 |
| C03 | 相同请求合并、限并发、后台解码 | PhotoStore / request task | ImageStore.pending / GPU completion tasks | Tested | 20个相同尺寸并发请求仅解码1次且共享结果；GPU请求也已合并，取消仍待测 |
| C04 | CPU 内存LRU/磁盘预算/独立缩略图 | PhotoStore/PhotoThumbnails | ImageStore | Tested | 坏缓存重建像素相同、新实例命中磁盘、四规模预算观测通过；长期/耗尽场景待测 |
| C05 | GPU复用与384MiB淘汰 | OpticalDisplay cachedStableTexture | TableRenderer GPU cache | Implemented | 大批量/设备丢失/快速切换待测 |
| C06 | 启动去重，只准备台面/上台相册 | TableModel.startupURLs | TableDocument.PlacedPaths | Implemented | 大图库无预读、重启待测 |
| C08 | 五层光学磁盘缓存（source/lit/dark/flare/boosted） | PhotoPreviewDiskCache.layerNames | OpticalLayerCache / OpticalPhoto.CreateAsync；台面及投影共用 | Implemented | 新增6项核心及实际GPU五层跨Store往返通过；纯光学命中绕过源解码、跨进程、大图库性能和异常退出仍待完成，见OPTICAL_CACHE_REPORT |
| C07 | 启动真实进度、失败重试/先看可用 | StartupPreparationView/StartupProgress | MainWindow.PrepareStartup | Implemented | 前半阶段插值与张数展示尚需精确比对 |
| G01 | 首次本版指引一次，帮助重看 | FeatureGuideState/Views | ShowGuide/options | Tested | 首次卡片实际出现与开始操作；三步回退/跳过待测 |
| G02 | 指引锁定交互、不修改布局 | FeatureGuideState / hit testing | modal ContentDialog | Implemented | 版本配置/多窗口/回放待测 |
| U01 | 每进程启动一次真实检查、手动检查 | AppUpdater | UpdateService.CheckAsync | Implemented | 网络/多窗口/空结果UI 待测 |
| U02 | 24h 开关、启动无新版/断网静默 | AppUpdater automaticChecks | MainWindow.AfterStartup | Implemented | 周期字段生命周期与启动标记门控已修复；24h实际运行和断网待测 |
| U03 | 正式新版与完整日志，排除mac包/降级 | Sparkle platform feed | ReleasePolicy | Tested | Windows/macOS/HTTP/恶意host/降级核心用例 |
| U04 | 后台下载、完整性、网络中断/重试 | Sparkle | UpdateService.DownloadAsync | Implemented | 远端暂无Windows包，端到端待测 |
| U05 | 可信签名、保存后重启、失败保留原应用 | Sparkle signed feed/package | 未配置签名，自动执行禁用 | Blocked | 需要发行信任密钥/证书；手动安装是安全降级 |
| W01 | Windows 原生启动及资源PRI | macOS bundle | WinUI App.xaml / self-contained | Tested | 真机启动和完整界面截图已见 |
| W02 | 125/150/200%及多屏全屏迁移 | AppKit/Metal drawable | WinUI DIPs/PerMonitorV2 | Implemented | 控件DpiScale125/150/200、实际GPU和DIP命中通过；系统DPI/多屏迁移仍待验收 |
| W03 | Release、便携ZIP、EXE安装/卸载 | build-app.sh | Scripts/Build-Release / Inno Setup 7 | Implemented | 新Release及便携16项通过；旧检查点安装/卸载/重装两轮通过；本轮安装预检因已有F盘用户快捷方式停止并保留现状，干净机待测 |
| W04 | 性能10/100/500/1000真实照片 | 原版无量化承诺 | 真实NASA/JSC数据集 / BenchmarkRunner | Tested | 10/100/500/1000张各轮实际导入和短时绘制通过；不包含长期、完整GPU帧时和60FPS验收 |
| W05 | 异常退出/断网/缺片/资源丢失恢复 | 多模块 | 相关错误路径 | Implemented | 异常场景完整矩阵待执行 |
| W06 | GPU新设备、迟到加载与资源等待 | 原平台系统恢复 | ResourceEpoch / GraphicsRecovery | Tested | 实际CanvasControl替换新硬件设备再WARP；迟到上传重试、收片/投影重建且不重播声音通过；非真实驱动拔除 |

本矩阵需继续对 SOURCE_SYMBOLS.md 的菜单、测试断言和状态机逐项复核。任何发现的漏项应新增行；不能通过删除未完成行提高覆盖率。

| M10 | 用户四档1/2/4/8实际倍率、连续可中断插值 | Windows用户新增 | LoupePower / SmoothTransitionClock / MagnificationSwitch | Tested | 核心四档/窗口/中断，原生四档GPU与终点；真实Tab及双向拖动；2×硬件点击仍需补测 |
| B16 | 选中后平移不可额外移动照片 | Windows输入冲突修复 | TableInput / TableManipulation | Tested | 三项核心；真实鼠标选中→背景拖动只变相机、直接拖片正常；真实触摸仍待测 |
| L10 | sRGB像素使用对应线性亮度权重 | Windows sRGB阶段修复 | SrgbOptics / FusedOptical / 缓存配方v4 | Tested | 核心与6块GPU彩色色块≤2/255；端到端P3仍Not Started |

| B17 | 稳态台面栅格缓存与命令回退 | Windows性能实现 | TableRenderer | Tested | GPU相机等价、16次重建释放、32MiB预算；1000张平移短测3.59s；点灯仍慢 |
| L11 | 四输入光学shader与原图等价 | 原七节点链参考 | FusedLamp / ReferenceGraph | Tested | 45亮度/时刻/透明度GPU对照最大1/255；完整P3未完成 |
| A09 | 相册弹窗自适应及实时预览 | AlbumViews | MainWindow.Albums GPU快照 | Tested | 本机125%DPI真实20格/装入3片/交换/恢复/整页上台；极小窗口与全部系统DPI仍待测 |
| D05 | 当前便携候选验收 | Windows分发 | Test-Portable | Tested | 0.1.72.5实际解压25原生、退出0、自包含/PRI；非干净Windows |
| D06 | 独立身份安装/卸载/重装 | Windows分发 | Test-InstallerSandbox | Tested | 两轮、安装后25原生、布局保留、既有安装不变；生产覆盖升级/干净系统仍待测 |
