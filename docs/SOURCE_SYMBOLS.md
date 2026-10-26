# 上游源码符号与交互索引

自动索引读取所有受版本控制文件；二进制仅记录哈希。此索引不能替代逐项行为和视觉验收。

## Assets/README.md

## CHANGELOG.md

## Configuration/Updates.plist

## LICENSE

## Package.swift

## README.md

## Sources/LightTableApp/AlbumDrag.swift
- L4: `enum AlbumDrag {`
- L6: `static func provider(_ id: UUID) -> NSItemProvider {`
- L13: `static func pageID(from provider: NSItemProvider) async -> UUID? {`

## Sources/LightTableApp/AlbumSurface.swift
- L5: `@MainActor enum AlbumSurface {`
- L7: `static func image(_ page: AlbumPage, front: Bool) -> CGImage? {`

## Sources/LightTableApp/AlbumViews.swift
- L5: `struct AlbumArtwork: View {`
- L36: `private struct AlbumFilm: View {`
- L98: `struct AlbumTrayCard: View {`
- L111: `.onTapGesture(count: 2) { model.editingAlbumID = page.id; model.albumEditorPresented = true }`
- L117: `Button("编辑相册页") { model.editingAlbumID = page.id; model.albumEditorPresented = true }`
- L118: `Button("整页放上观片台") { model.placeAlbum(page.id) }`
- L123: `struct AlbumTableView: View {`
- L169: `.onChange(of: dragging) { _, active in if !active { endDrag() } }`
- L172: `Button("编辑相册页") { model.editingAlbumID = page.id; model.albumEditorPresented = true }`
- L173: `Button(model.selectedIDs.contains(page.id) && model.selectedIDs.count > 1`
- L182: `private func endDrag() {`
- L189: `struct AlbumEditor: View {`
- L203: `Button("整页放上观片台") { model.placeAlbum(page.id); dismiss() }`
- L206: `Button("完成") { dismiss() }.keyboardShortcut(.defaultAction)`
- L224: `Button("加袋格") { addPocket() }`
- L225: `Menu("整页袋格") {`
- L226: `Button("135 · 5 × 4") { fill(.film135) }`
- L227: `Button("120 · 3 × 3") { fill(.film120) }`
- L241: `Button("新建相册页") { _ = model.newAlbum() }`
- L253: `.onChange(of: model.editingAlbumID) { _, _ in selectedPocket = nil; note = nil }`
- L255: `Button("删除相册页", role: .destructive) { if let page { model.deleteAlbum(page.id) } }`
- L276: `if page != nil { Button("删除此页", role: .destructive) { deleting = true }.font(.caption) }`
- L281: `HStack { Text("选择照片").font(.headline); Spacer(); Button(action: model.choosePhotos) { Image(systemName: "plus") } }`
- L293: `private func editableSheet(_ page: AlbumPage) -> some View {`
- L297: `.onTapGesture(coordinateSpace: .local) { point in`
- L322: `.onDrop(of: [PhotoDrag.contentType], isTargeted: nil) { providers in`
- L331: `Button("清空照片") { model.updateAlbum(page.id) { $0.setPhoto(nil, in: pocket.id) } }`
- L332: `Button("移除袋格") { model.updateAlbum(page.id) { $0.remove(pocket.id) }; selectedPocket = nil }`
- L338: `private func addPocket(column: Int? = nil, row: Int? = nil) {`
- L345: `private func fill(_ format: FilmFormat) {`
- L350: `private func assign(_ url: URL) {`
- L362: `private struct AlbumLibraryPhoto: View {`

## Sources/LightTableApp/AppUpdater.swift
- L8: `final class AppUpdater: NSObject, ObservableObject, SPUUpdaterDelegate {`
- L27: `func start(beforeRelaunch: @escaping () -> Void) {`
- L58: `func checkForUpdates() {`
- L74: `alert.addButton(withTitle: "好")`
- L78: `func setAutomaticChecks(_ enabled: Bool) {`
- L83: `func updaterWillRelaunchApplication(_ updater: SPUUpdater) {`
- L91: `final class LaunchUpdateUserDriver: SPUStandardUserDriver {`
- L94: `override func showUserInitiatedUpdateCheck(cancellation: @escaping () -> Void) {`
- L95: `if !isCheckingAtLaunch { super.showUserInitiatedUpdateCheck(cancellation: cancellation) }`
- L98: `override func showUpdateFound(with appcastItem: SUAppcastItem, state: SPUUserUpdateState,`
- L104: `override func showUpdateNotFoundWithError(_ error: Error, acknowledgement: @escaping () -> Void) {`
- L109: `override func showUpdaterError(_ error: Error, acknowledgement: @escaping () -> Void) {`
- L114: `override func dismissUpdateInstallation() {`

## Sources/LightTableApp/BrightnessSlider.swift
- L4: `struct BrightnessSlider: NSViewRepresentable {`
- L9: `func makeCoordinator() -> Coordinator { Coordinator(value: $value) }`
- L11: `func makeNSView(context: Context) -> ResettableBrightnessSlider {`
- L23: `func updateNSView(_ slider: ResettableBrightnessSlider, context: Context) {`
- L28: `private func configure(_ slider: ResettableBrightnessSlider) {`
- L36: `final class Coordinator: NSObject {`
- L47: `final class ResettableBrightnessSlider: NSSlider {`
- L50: `override func mouseDown(with event: NSEvent) {`

## Sources/LightTableApp/CanvasNavigation.swift
- L5: `struct CanvasNavigation: NSViewRepresentable {`
- L11: `func makeNSView(context: Context) -> ScrollView { ScrollView() }`
- L12: `func updateNSView(_ view: ScrollView, context: Context) {`
- L16: `static func dismantleNSView(_ view: ScrollView, coordinator: ()) { view.stop() }`
- L18: `final class ScrollView: NSView {`
- L25: `override func hitTest(_ point: NSPoint) -> NSView? { nil }`
- L26: `override func viewDidMoveToWindow() {`
- L46: `func stop() {`

## Sources/LightTableApp/CanvasSelection.swift
- L6: `struct CanvasSelection: NSViewRepresentable {`
- L14: `func makeNSView(context: Context) -> MonitorView { MonitorView() }`
- L15: `func updateNSView(_ view: MonitorView, context: Context) {`
- L22: `static func dismantleNSView(_ view: MonitorView, coordinator: ()) { view.stop() }`
- L24: `final class MonitorView: NSView {`
- L37: `override func hitTest(_ point: NSPoint) -> NSView? { nil }`
- L39: `override func viewDidMoveToWindow() {`
- L52: `func handle(_ event: NSEvent) -> NSEvent? {`
- L89: `func cancel() {`
- L93: `private func releaseCursor() {`
- L96: `func stop() {`

## Sources/LightTableApp/ContentView.swift
- L6: `private enum TableStyle {`
- L15: `struct ContentView: View {`
- L70: `.onPreferenceChange(WorkbenchFrameKey.self) { canvasFrame = $0 }`
- L77: `.onPreferenceChange(SlideshowTraySnapshotKey.self) { slideshowTray = $0 }`
- L78: `.onPreferenceChange(LoupeSceneKey.self) { scene in`
- L105: `.onPreferenceChange(FeatureGuideBounds.self) { guideBounds = $0 }`
- L148: `.onChange(of: model.loupeEnabled) { _, enabled in if !enabled { loupe.cancel() } }`
- L149: `.onChange(of: model.activeTrayID) { _, _ in`
- L154: `.onChange(of: model.slideshowMode) { _, active in`
- L167: `.onReceive(NotificationCenter.default.publisher(for: NSWindow.didResignKeyNotification)) { _ in returnToSleeve() }`
- L175: `.onChange(of: model.albumEditorPresented) { _, opened in`
- L179: `Button("好") { model.message = nil }`
- L216: `Button("整理全局", action: model.fitToWindow).buttonStyle(.plain).font(.system(size: 11)).padding(.horizontal, 5)`
- L240: `}.buttonStyle(.plain).keyboardShortcut("l", modifiers: .command)`
- L256: `private func tool(_ symbol: String, _ help: String, active: Bool = false, action: @escaping () -> Void) -> some View {`
- L257: `Button(action: action) {`
- L272: `Button(action: model.chooseFolder) {`
- L283: `Button(action: model.placeAllPhotos) {`
- L317: `Button("放到灯板中央") { model.placeAtViewCenter(photo.url) }`
- L318: `Button(role: .destructive) {`
- L323: `Button("在 Finder 中显示") { NSWorkspace.shared.activateFileViewerSelecting([photo.url]) }`
- L336: `.onDrop(of: PhotoDrag.acceptedTypes, isTargeted: nil) { model.accept($0, at: nil, board: nil) }`
- L354: `Button(role: .destructive) {`
- L373: `.onPreferenceChange(TrayTabsWidthKey.self) { width in`
- L378: `.onChange(of: model.activeTrayID) { _, id in proxy.scrollTo(id, anchor: .trailing) }`
- L379: `.onChange(of: trayTabsWidth) { _, _ in proxy.scrollTo(model.activeTrayID, anchor: .trailing) }`
- L391: `private func startSlideshow() {`
- L401: `@MainActor private func prepareSlideshow(_ run: UUID) async {`
- L449: `private func targetSlide(for photo: Photo) -> Slide {`
- L453: `private func pullFromSleeve(_ photo: Photo, _ image: CGImage, _ sleeve: CGRect, _ value: DragGesture.Value) {`
- L477: `private func releaseFromSleeve(_ value: DragGesture.Value) {`
- L503: `private func returnToSleeve() {`
- L515: `private func releaseFlightCursor() {`
- L520: `private struct TrayPhoto: View {`
- L557: `.onEnded(onRelease)`
- L567: `.onChange(of: pulling) { _, active in`
- L571: `.onChange(of: model.thumbnails[photo.url] != nil) { _, available in`
- L594: `struct TableCanvas: View {`
- L695: `Button(action: model.removeSelected) { Label("收回底栏", systemImage: "arrow.down.to.line") }`
- L712: `.onPreferenceChange(SlidePresentationKey.self) { livePlacements = $0 }`
- L713: `.onChange(of: draggingID) { _, id in if id != nil { loupe.cancel() } }`
- L714: `.onChange(of: isPanning) { _, active in if !active { endPan() } }`
- L715: `.onChange(of: isCanvasZooming) { _, active in if !active { endCanvasZoom() } }`
- L739: `.onDrop(of: PhotoDrag.acceptedTypes + [AlbumDrag.contentType], isTargeted: $targeted) { providers, point in`
- L743: `.onChange(of: size, initial: true) { _, value in model.boardSize = value }`
- L766: `private func sceneItems(viewport: CGRect?, offset: CGPoint,`
- L774: `func append(_ photo: LoupeScenePhoto, url: URL? = nil) {`
- L788: `func surface(_ front: Bool) {`
- L811: `private func endPan() {`
- L817: `private func endCanvasZoom() {`
- L823: `struct LampPanel: View {`
- L863: `private struct SlideView: View {`
- L988: `.onChange(of: isDragging) { _, active in`
- L1002: `Button(selected && model.selectedIDs.count > 1 ? "收回所选 \(model.selectedIDs.count) 项到底栏" : "收回底栏") {`
- L1005: `Button("摆正") { model.update(slide.id) { $0.angle = 0 } }`
- L1007: `Button("逆时针旋转 15°") { model.update(slide.id) { $0.angle -= 15 } }`
- L1008: `Button("顺时针旋转 15°") { model.update(slide.id) { $0.angle += 15 } }`
- L1009: `Button("缩小胶片") { model.update(slide.id) { $0.width /= 1.12 } }`
- L1010: `Button("放大胶片") { model.update(slide.id) { $0.width *= 1.12 } }`
- L1044: `private func setDragCursor(active: Bool) {`
- L1050: `private func commit(_ change: (inout Slide) -> Void) {`
- L1063: `private func image(_ preview: PhotoPreview, size: CGSize) -> some View {`
- L1069: `struct TransmittedPhoto: View {`
- L1112: `private func layer(_ image: CGImage) -> some View {`
- L1118: `struct OpticalLoupe: View {`
- L1169: `private struct TrayTabsWidthKey: PreferenceKey {`
- L1171: `static func reduce(value: inout CGFloat, nextValue: () -> CGFloat) {`
- L1176: `private struct SlideshowTrayPhotoAnchor {`
- L1181: `private struct SlideshowTrayAnchors {`
- L1185: `func resolved(in proxy: GeometryProxy) -> SlideshowTraySnapshot {`
- L1195: `private struct SlideshowTrayAnchorsKey: PreferenceKey {`
- L1197: `static func reduce(value: inout SlideshowTrayAnchors, nextValue: () -> SlideshowTrayAnchors) {`
- L1204: `private struct SlideshowTrayPhotoSnapshot: Equatable {`
- L1209: `static func == (lhs: Self, rhs: Self) -> Bool {`
- L1214: `private struct SlideshowTraySnapshot: Equatable {`
- L1219: `private struct SlideshowTraySnapshotKey: PreferenceKey {`
- L1221: `static func reduce(value: inout SlideshowTraySnapshot, nextValue: () -> SlideshowTraySnapshot) { value = nextValue() }`

## Sources/LightTableApp/FeatureGuideState.swift
- L4: `enum FeatureGuideTarget: Hashable {`
- L7: `static func releaseTour(step: Int, loupeEnabled: Bool) -> Self {`
- L25: `final class FeatureGuideState {`
- L33: `enum Phase: Equatable {`
- L57: `func isWelcome(in window: UUID) -> Bool { owner == window && phase == .welcome }`
- L58: `func step(in window: UUID) -> Int? {`
- L63: `func presentAutomatically(in window: UUID, ready: Bool) {`
- L69: `func replay(in window: UUID) {`
- L76: `func didPresent(in window: UUID) {`
- L86: `func beginTour(in window: UUID) {`
- L91: `func advance(in window: UUID) {`
- L96: `func previous(in window: UUID) {`
- L101: `func dismissWelcome(in window: UUID) {`
- L106: `func finish(in window: UUID) {`

## Sources/LightTableApp/FeatureGuideViews.swift
- L4: `struct FeatureGuideCommand {`
- L7: `private struct FeatureGuideCommandKey: FocusedValueKey {`
- L16: `struct FeatureGuideCommands: Commands {`
- L20: `Button("本次更新…") { guide?.show() }`
- L26: `struct FeatureGuideWelcome: View {`
- L50: `Button("开始观片", action: close).buttonStyle(FeatureGuideButtonStyle(primary: true))`
- L51: `.keyboardShortcut(.defaultAction)`
- L61: `Button(action: close) { Image(systemName: "xmark").font(.system(size: 11)).frame(width: 26, height: 26) }`
- L65: `.onExitCommand(perform: close)`
- L68: `private func feature(_ icon: String, title: String, text: String, note: String? = nil) -> some View {`
- L81: `struct FeatureGuideButtonStyle: ButtonStyle {`
- L83: `func makeBody(configuration: Configuration) -> some View {`
- L92: `struct FeatureGuideAnchors: PreferenceKey {`
- L94: `static func reduce(value: inout [FeatureGuideTarget: Anchor<CGRect>], nextValue: () -> [FeatureGuideTarget: Anchor<CGRect>]) {`
- L99: `func featureGuideTarget(_ target: FeatureGuideTarget) -> some View {`
- L103: `struct FeatureGuideBounds: PreferenceKey {`
- L105: `static func reduce(value: inout CGRect, nextValue: () -> CGRect) { value = nextValue() }`
- L108: `struct FeatureGuideTour: View {`
- L169: `Button("跳过", action: close).buttonStyle(.plain).accessibilityLabel("跳过新功能指引")`
- L175: `Button("上一步", action: previous).buttonStyle(.plain).font(.system(size: 12))`
- L178: `Button(step + 1 == FeatureGuideState.tourStepCount ? "开始观片" : "下一步", action: next)`
- L188: `.onChange(of: card.size.height, initial: true) { _, height in cardHeight = height }`
- L198: `struct FeatureGuideWindow: NSViewRepresentable {`
- L200: `func makeNSView(context: Context) -> ObserverView { ObserverView() }`
- L201: `func updateNSView(_ view: ObserverView, context: Context) { view.onKey = onKey }`
- L202: `static func dismantleNSView(_ view: ObserverView, coordinator: ()) { view.stop() }`
- L203: `final class ObserverView: NSView {`
- L206: `override func hitTest(_ point: NSPoint) -> NSView? { nil }`
- L207: `override func viewDidMoveToWindow() {`
- L215: `private func report() {`
- L221: `func stop() { observers.forEach(NotificationCenter.default.removeObserver); observers = [] }`

## Sources/LightTableApp/FilmContactSurface.swift
- L5: `enum FilmContactVariant: Int, CaseIterable {`
- L6: `case settled, leftCurl, rightCurl`
- L9: `static func forFilm(_ id: UUID) -> Self {`
- L19: `enum FilmContactTexture {`
- L26: `static func image(for variant: FilmContactVariant) -> CGImage? { images[variant.rawValue] }`
- L28: `private static func makeImage(_ variant: FilmContactVariant) -> CGImage? {`
- L92: `struct FilmContactSurface: View, Equatable {`

## Sources/LightTableApp/FilmSleeve.swift
- L4: `struct FilmSleeve: View {`
- L58: `struct FilmSleeveSurface: View {`
- L63: `func line(_ points: [CGPoint], color: Color, width: CGFloat = 0.5) {`

## Sources/LightTableApp/LightTableApp.swift
- L4: `struct LightTableApp: App {`
- L15: `.onReceive(NotificationCenter.default.publisher(for: NSApplication.willTerminateNotification)) { _ in model.flushSave() }`
- L27: `Button("检查更新…", action: updater.checkForUpdates)`
- L36: `Button("从文件夹导入…", action: model.chooseFolder).keyboardShortcut("o")`
- L38: `Button("新建底栏") { model.newTray() }`
- L40: `Button("添加照片…", action: model.choosePhotos).keyboardShortcut("o", modifiers: [.command, .shift])`
- L43: `CommandMenu("观片") {`
- L44: `Button(model.isLit ? "关灯" : "开灯") { model.isLit.toggle() }.keyboardShortcut("l", modifiers: .command)`
- L46: `Button("普通指针") { model.loupeEnabled = false }.keyboardShortcut("a", modifiers: [])`
- L48: `Button("放大镜") { model.loupeEnabled = true }.keyboardShortcut("v", modifiers: [])`
- L50: `Button("整理全局", action: model.fitToWindow).keyboardShortcut("z", modifiers: .shift)`
- L52: `Button(model.selectedTrayPhoto != nil ? "从底栏删除照片" : (model.selectedIDs.count > 1 ? "收回所选 \(model.selectedIDs.count) 项到底栏" : "收回底栏"), action: model.deleteSelection)`
- L53: `.keyboardShortcut(.delete, modifiers: [])`

## Sources/LightTableApp/LoupeExperience.swift
- L5: `enum LoupeCloseMagnification: CaseIterable {`
- L10: `func magnification(in size: CGSize) -> CGFloat {`
- L15: `enum LoupeGeometry {`
- L22: `static func ease(_ progress: CGFloat) -> CGFloat {`
- L27: `static func closeDiameter(in size: CGSize) -> CGFloat {`
- L31: `static func rasterDiameter(in size: CGSize, closeViewing: Bool) -> CGFloat {`
- L36: `static func closeCenter(from origin: CGPoint, in size: CGSize) -> CGPoint {`
- L39: `func axis(_ value: CGFloat, _ length: CGFloat) -> CGFloat {`
- L46: `static func magnification(for diameter: CGFloat) -> CGFloat {`
- L50: `static func sourceViewport(point: CGPoint, in size: CGSize, closeViewing: Bool,`
- L62: `enum SlidePresentation {`
- L63: `static func placement(_ slide: Slide, board: CGSize, aspect: CGFloat, zoom: Double,`
- L78: `struct SlidePresentationKey: PreferenceKey {`
- L80: `static func reduce(value: inout [UUID: SlidePlacement], nextValue: () -> [UUID: SlidePlacement]) {`
- L85: `struct LoupeSceneKey: PreferenceKey {`
- L87: `static func reduce(value: inout LoupeSceneInput?, nextValue: () -> LoupeSceneInput?) {`
- L93: `@MainActor @Observable final class LoupeExperience {`
- L116: `func cycleMagnification() {`
- L121: `func press() -> Bool {`
- L131: `func release(reduceMotion: Bool) {`
- L144: `func cancel() {`
- L151: `private func finishReturn() {`
- L156: `private func updateCursorVisibility() {`
- L162: `private func setCursorHidden(_ hidden: Bool) {`
- L169: `struct LoupeOverlay: View {`
- L190: `Color.clear.onChange(of: proxy.size, initial: true) { _, size in experience.viewingSize = size }`
- L199: `struct LoupeDepth {`
- L223: `func rearCenter(origin: CGPoint, target: CGPoint) -> CGPoint {`
- L232: `struct LoupeSampling {`
- L262: `func input(scene: LoupeSceneInput, displayScale: CGFloat) -> LoupeDisplayInput {`
- L268: `private struct LoupeSampledImage: View {`
- L278: `.onChange(of: input, initial: true) { _, value in`
- L285: `struct LoupeApproach: View, Animatable {`
- L321: `struct LoupeGlassPose: Equatable {`
- L328: `func axis(_ position: CGFloat, _ length: CGFloat) -> CGFloat {`
- L343: `struct LoupeGlassSurface: View {`
- L391: `struct LoupeKeyboard: NSViewRepresentable {`
- L399: `func makeNSView(context: Context) -> MonitorView { MonitorView() }`
- L400: `func updateNSView(_ view: MonitorView, context: Context) {`
- L407: `static func dismantleNSView(_ view: MonitorView, coordinator: ()) { view.stop() }`
- L409: `final class MonitorView: NSView {`
- L425: `override func viewDidMoveToWindow() {`
- L448: `func handle(_ event: NSEvent) -> NSEvent? {`
- L470: `func reset() { held = false; tabHeld = false; cancelled?() }`
- L471: `private func updateAvailability() { availabilityChanged?(enabled && windowActive) }`
- L472: `func stop() {`

## Sources/LightTableApp/LoupeMagnificationControl.swift
- L4: `struct LoupeMagnificationControl: View {`

## Sources/LightTableApp/LoupeRaster.swift
- L5: `struct LoupeRasterRequest: Equatable, @unchecked Sendable {`
- L35: `private final class LoupeRasterWorker: @unchecked Sendable {`
- L41: `func render(_ request: LoupeRasterRequest) async -> CGImage? {`
- L44: `dispatchPrecondition(condition: .onQueue(queue))`
- L61: `@MainActor @Observable final class LoupeRaster {`
- L74: `func request(_ value: LoupeRasterRequest) {`
- L83: `private func drain(token: Int) async {`
- L101: `func cancel() {`

## Sources/LightTableApp/LoupeRenderer.swift
- L5: `struct LoupeSceneInput: Equatable {`
- L15: `struct LoupeDisplayInput: Equatable {`
- L33: `struct LoupeScenePhoto: Equatable {`
- L43: `static func == (lhs: Self, rhs: Self) -> Bool {`
- L62: `final class LoupeRenderer {`
- L86: `static func sourceViewport(point: CGPoint, diameter: CGFloat, magnification: CGFloat) -> CGRect {`
- L92: `func render(preview: PhotoPreview, point: CGPoint, imageSize: CGSize,`
- L102: `func render(photos: [LoupeScenePhoto], point: CGPoint, panelBounds: CGRect? = nil,`
- L117: `func composite(photos: [LoupeScenePhoto], point: CGPoint, panelBounds: CGRect? = nil,`

## Sources/LightTableApp/OpticalDisplay.swift
- L8: `struct OpticalDisplay: NSViewRepresentable {`
- L15: `func makeNSView(context: Context) -> OpticalMetalView { OpticalMetalView() }`
- L16: `func updateNSView(_ view: OpticalMetalView, context: Context) {`
- L24: `struct OpticalRetryBudget {`
- L28: `mutating func beginNewInput() {`
- L33: `mutating func nextDelay() -> TimeInterval? {`
- L41: `mutating func succeeded() {`
- L46: `final class OpticalMetalView: MTKView {`
- L81: `private struct CacheParams: Equatable {`
- L103: `override func hitTest(_ point: NSPoint) -> NSView? { nil }`
- L120: `private func invalidateCache() {`
- L127: `private func beginNewInput() {`
- L132: `private func cancelRetry() {`
- L139: `func noteDrawFailure(_ reason: String) {`
- L159: `func noteDrawSuccess() {`
- L164: `private func retryFired(token: Int, revision: Int) {`
- L172: `func set(preview: PhotoPreview?, image: CGImage?, transmission: Double,`
- L191: `override func viewDidMoveToWindow() {`
- L197: `override func viewDidChangeBackingProperties() {`
- L204: `override func draw(_ dirtyRect: NSRect) { enqueue() }`
- L206: `private func enqueue() {`
- L214: `private struct EncodedSurface {`
- L220: `private enum LiveEncode {`
- L231: `private static func drawPending() {`
- L243: `static func drawVisible(_ views: [OpticalMetalView], renderer: CIContext,`
- L285: `private func prepareDraw() -> Bool {`
- L292: `private func encode(into buffer: MTLCommandBuffer, renderer: CIContext) -> LiveEncode {`
- L303: `func encode(into texture: MTLTexture, commandBuffer buffer: MTLCommandBuffer, renderer: CIContext) -> Bool {`
- L346: `private func makeCacheTexture(width: Int, height: Int) -> MTLTexture? {`
- L354: `private func fitted(_ image: CIImage, to size: CGSize) -> CIImage? {`
- L363: `func composedInput() -> CIImage? {`

## Sources/LightTableApp/PhotoDrag.swift
- L6: `enum PhotoDrag {`
- L10: `private struct Reference: Codable { let version: Int; let url: URL }`
- L12: `static func provider(for photo: Photo) -> NSItemProvider {`
- L23: `static func sourceURL(from provider: NSItemProvider) async -> URL? {`
- L34: `static func fileURL(from provider: NSItemProvider) async -> URL? {`

## Sources/LightTableApp/PhotoOptics.swift
- L6: `enum PhotoOptics {`
- L67: `static func fusedOptical(_ image: CIImage, lampBrightness: Double, exposureStops: Double) -> CIImage {`
- L81: `static func mix(_ from: CIImage, _ to: CIImage, _ amount: Double) -> CIImage {`
- L88: `static func multiplied(_ image: CIImage, _ level: Double) -> CIImage {`
- L95: `static func composite(preview: PhotoPreview, transmission: Double, flare: Double,`
- L116: `static func lamp(_ image: CIImage, brightness: Double) -> CIImage {`
- L124: `static func ignition(_ image: CIImage, flare: Double, brightness: Double) -> CIImage {`
- L132: `static func sdrImage(preview: PhotoPreview, transmission: Double, flare: Double, brightness: Double) -> CGImage? {`

## Sources/LightTableApp/PhotoStore.swift
- L7: `final class PhotoPreview: @unchecked Sendable {`
- L49: `func viewing(transmitted: Bool) -> PhotoPreview {`
- L64: `enum TablePreviewSizing {`
- L65: `static func maxPixel(size: CGSize, displayScale: CGFloat) -> Int {`
- L73: `struct TablePreviewRequest: Hashable {`
- L83: `actor PhotoStore {`
- L87: `private struct PreviewKey: Hashable { let url: URL; let maxPixel: Int }`
- L95: `func preview(_ url: URL, maxPixel: Int = 2200) -> PhotoPreview? {`
- L131: `private func renderTransmission(_ input: CIImage, boosted: Bool) -> CGImage? {`
- L147: `private func outwardScatter(_ highlights: CIImage, radius: Double, strength: Double) -> CIImage {`
- L169: `private func renderSwitchOnHalo(_ input: CIImage) -> CGImage? {`
- L211: `private static func cube(_ image: CIImage, data: Data) -> CIImage {`
- L219: `private static func luminance(_ r: Float, _ g: Float, _ b: Float) -> Float {`
- L223: `private static func smoothstep(_ start: Float, _ end: Float, _ value: Float) -> Float {`
- L228: `private static func makeCube(_ transform: (Float, Float, Float) -> (Float, Float, Float)) -> Data {`
- L243: `func thumbnail(_ url: URL) async -> CGImage? {`
- L247: `private func touch(_ url: PreviewKey) {`
- L253: `private func decode(_ url: URL, maxPixel: Int) -> CGImage? {`
- L268: `enum PhotoPreviewDiskCache {`
- L274: `private static func folder(_ url: URL, maxPixel: Int, root: URL) -> URL? {`
- L283: `static func load(_ url: URL, maxPixel: Int, root: URL = directory) -> PhotoPreview? {`
- L285: `func image(_ name: String) -> CGImage? {`
- L296: `static func save(_ preview: PhotoPreview, for url: URL, maxPixel: Int, root: URL = directory) {`
- L310: `private static func trim(root: URL, keeping current: URL) {`
- L334: `actor PhotoThumbnails {`
- L339: `func thumbnail(_ url: URL) -> CGImage? {`
- L363: `enum PhotoThumbnailCache {`
- L367: `static func key(_ url: URL) -> String? {`
- L375: `static func cached(_ url: URL, directory root: URL = directory) -> CGImage? {`
- L382: `static func save(_ image: CGImage, for url: URL, directory root: URL = directory) {`
- L402: `struct Photo: Identifiable, Equatable, Sendable {`
- L408: `enum PhotoLibrary {`
- L410: `static func imageAspect(_ url: URL) -> Double {`
- L422: `static func legacyDragAliases(in urls: [URL], cacheDirectory: URL? = nil) -> [URL: URL] {`
- L442: `private static func isLegacyDragCache(_ url: URL, cacheRoot: URL) -> Bool {`
- L450: `static func supported(_ url: URL) -> Bool {`
- L458: `static func scan(_ inputs: [URL]) -> [URL] {`

## Sources/LightTableApp/SleeveFlight.swift
- L4: `struct SleeveFlight {`
- L5: `enum Phase { case dragging, landing, returning }`
- L19: `struct SleeveFlightView: View {`
- L54: `struct WorkbenchFrameKey: PreferenceKey {`
- L56: `static func reduce(value: inout CGRect, nextValue: () -> CGRect) { value = nextValue() }`

## Sources/LightTableApp/SleeveMotion.swift
- L5: `enum SleeveMotion {`
- L6: `static func smooth(_ value: CGFloat) -> CGFloat {`
- L11: `static func expansion(origin: CGRect, pointer: CGPoint, start: CGPoint) -> CGFloat {`
- L16: `static func frame(origin: CGRect, pointer: CGPoint, start: CGPoint, target: CGSize) -> CGRect {`

## Sources/LightTableApp/SlideshowEntry.swift
- L5: `struct SlideshowEntryPhoto: Identifiable {`
- L12: `struct SlideshowEntry: View {`
- L60: `private func cards(destination: CGPoint, foreground: Bool) -> some View {`
- L69: `@MainActor private func animateCollection() async {`
- L116: `@MainActor private func finish(_ run: UUID) {`
- L123: `private struct SlideshowEntryCard: View {`
- L140: `struct SlideshowEntryPose {`
- L171: `private struct SlideshowEntryFlight: AnimatableModifier {`
- L183: `func body(content: Content) -> some View {`
- L194: `private struct SlideshowEntryBoxBack: View {`
- L217: `private struct SlideshowEntryBoxFront: View {`
- L235: `private struct SlideshowEntryBoxTop: Shape {`
- L236: `func path(in rect: CGRect) -> Path {`

## Sources/LightTableApp/SlideshowMechanicalSound.swift
- L5: `enum SlideshowSoundResource {`
- L8: `static func url(named name: String = "ShutterClick", in bundle: Bundle = .main) -> URL? {`
- L24: `final class SlideshowMechanicalSound {`
- L27: `private struct PreparedAudio: @unchecked Sendable {`
- L32: `private struct AudioData: Sendable {`
- L38: `func load(_ name: String) -> Data? {`
- L48: `func prepare() async {`
- L54: `func makePlayer(_ data: Data?) -> AVAudioPlayer? {`
- L72: `func outgoing() { play(outgoingPlayer) }`
- L73: `func firstArrival() { play(firstArrivalPlayer) }`
- L75: `private func play(_ player: AVAudioPlayer?) {`
- L81: `func stop() {`

## Sources/LightTableApp/SlideshowMotion.swift
- L5: `struct ProjectionTravelPose {`
- L10: `static func at(_ transport: Double, container: CGSize, image: CGSize) -> Self {`
- L25: `struct ProjectionTravel: AnimatableModifier {`
- L34: `func body(content: Content) -> some View {`

## Sources/LightTableApp/SlideshowProjection.swift
- L5: `struct ProjectionPhoto: @unchecked Sendable {`
- L19: `func fittedSize(in container: CGSize) -> CGSize {`
- L27: `actor SlideshowProjectionStore {`
- L33: `func photo(_ url: URL) async -> ProjectionPhoto? {`
- L45: `private func touch(_ url: URL) {`
- L50: `func render(_ source: CGImage, url: URL) -> ProjectionPhoto? {`
- L77: `func grayscale(_ bytes: [UInt8]) -> CGImage? {`
- L91: `func masked(_ image: CIImage) -> CGImage? {`
- L117: `final class SlideshowPlayback {`
- L119: `enum Phase { case waiting, departing, black, arriving, settled }`
- L152: `func show(_ url: URL, reduceMotion: Bool) {`
- L223: `func beginPresentation() {`
- L229: `private func closeShutter() {`
- L238: `private func startArrival() {`
- L255: `private func reveal() {`
- L261: `func cancel() {`
- L272: `func waitForPendingLoad() async { await task?.value; await arrivalTask?.value }`

## Sources/LightTableApp/SlideshowView.swift
- L3: `struct SlideshowView: View {`
- L41: `Button("重试") { loadCurrent() }`
- L42: `Button("下一张", action: model.nextSlide)`
- L50: `Button(action: model.exitSlideshow) {`
- L70: `.onChange(of: model.currentSlideURL, initial: true) { _, _ in loadCurrent() }`
- L79: `private func loadCurrent() {`
- L83: `@MainActor private static func makePlayback() -> SlideshowPlayback {`

## Sources/LightTableApp/SlideshowWindow.swift
- L5: `struct SlideshowWindow: NSViewRepresentable {`
- L12: `func makeCoordinator() -> Coordinator { Coordinator() }`
- L14: `func makeNSView(context: Context) -> WindowView {`
- L22: `func updateNSView(_ view: WindowView, context: Context) {`
- L28: `static func dismantleNSView(_ view: WindowView, coordinator: Coordinator) {`
- L33: `final class WindowView: NSView {`
- L35: `override func viewDidMoveToWindow() {`
- L39: `override func hitTest(_ point: NSPoint) -> NSView? { nil }`
- L44: `final class Coordinator: NSObject, NSWindowDelegate {`
- L45: `private enum Phase { case windowed, entering, fullscreen, exiting, closed }`
- L62: `func update(ready: Bool, next: @escaping () -> Void, previous: @escaping () -> Void,`
- L72: `func attach(to newWindow: NSWindow?) {`
- L114: `func detach() {`
- L133: `private func beginPresentationIfReady() {`
- L148: `private func reportReady() {`
- L154: `private func finish() {`
- L164: `func windowWillEnterFullScreen(_ notification: Notification) {`
- L169: `func windowDidEnterFullScreen(_ notification: Notification) {`
- L179: `func windowDidFailToEnterFullScreen(_ window: NSWindow) {`
- L189: `func windowWillExitFullScreen(_ notification: Notification) {`
- L194: `func windowDidExitFullScreen(_ notification: Notification) {`
- L204: `func windowDidFailToExitFullScreen(_ window: NSWindow) {`
- L212: `func windowWillClose(_ notification: Notification) {`
- L219: `override func responds(to selector: Selector!) -> Bool {`
- L223: `override func forwardingTarget(for selector: Selector!) -> Any? {`

## Sources/LightTableApp/StartupPreparationView.swift
- L6: `struct StartupPreparationView: View {`
- L62: `Button("先查看可用照片") { model.continueStartup() }`
- L64: `Button("重新读取") { Task { await model.retryStartup() } }`
- L66: `.keyboardShortcut(.defaultAction)`
- L77: `.onDrop(of: ["public.file-url"], isTargeted: nil) { _ in false }`
- L79: `.onChange(of: model.startupAttempt) { _, _ in`

## Sources/LightTableApp/TableModel.swift
- L6: `struct PhotoTray: Identifiable, Codable, Equatable {`
- L12: `struct TrayPhotoSelection: Equatable {`
- L19: `final class TableModel {`
- L88: `enum StartupPhase { case preparing, finishing, needsAttention, ready }`
- L94: `struct SurfaceRequest: Hashable { let preview: TablePreviewRequest; let attempt: Int }`
- L173: `func beginLightPreparation(_ id: UUID) { preparingLightSurfaces.insert(id) }`
- L175: `func finishLightPreparation(_ id: UUID) {`
- L186: `func prepareStartup() async {`
- L208: `func retryStartup() async {`
- L220: `func continueStartup() {`
- L226: `private func finishStartupIfReady() {`
- L231: `func cacheThumbnail(_ image: CGImage, for url: URL) {`
- L238: `func preparePreview(_ url: URL) async -> PhotoPreview? {`
- L257: `func tablePreviewMaxPixel(for url: URL, displayScale: CGFloat) -> Int {`
- L265: `func prepareTablePreview(_ url: URL, maxPixel: Int) async -> PhotoPreview? {`
- L298: `func newTray() -> UUID {`
- L309: `func selectTray(_ id: UUID) {`
- L317: `func selectTrayPhoto(_ url: URL, in trayID: UUID) {`
- L327: `func deleteSelection() {`
- L334: `func deleteTray(_ id: UUID) {`
- L353: `func deletePhoto(_ url: URL, fromTray trayID: UUID) {`
- L368: `private static func uniqueURLs(_ urls: [URL]) -> [URL] {`
- L373: `func chooseFolder() {`
- L382: `func choosePhotos() {`
- L390: `func importURLs(_ urls: [URL], into trayID: UUID? = nil) async {`
- L407: `func place(_ url: URL, x: Double, y: Double) {`
- L417: `func setSelection(_ ids: Set<UUID>) {`
- L424: `func select(_ id: UUID, extending: Bool = false, showIndicator: Bool = true) {`
- L440: `func update(_ id: UUID, change: (inout Slide) -> Void) { layout.update(id, change); save() }`
- L441: `func removeSelected() {`
- L455: `private func trimReturnedPhotoPreviews() {`
- L470: `func select(in rect: CGRect) {`
- L483: `private static func intersects(_ rect: CGRect, placement p: SlidePlacement) -> Bool {`
- L495: `func clear() {`
- L503: `func enterSlideshow() {`
- L510: `func exitSlideshow() {`
- L516: `func nextSlide() {`
- L521: `func previousSlide() {`
- L537: `func panBy(_ delta: CGSize) {`
- L543: `func zoomCanvas(by factor: Double, around point: CGPoint? = nil) {`
- L552: `func screenPlacement(_ slide: Slide, aspect: Double) -> SlidePlacement {`
- L557: `func normalizedWorldPoint(_ point: CGPoint) -> CGPoint {`
- L562: `func placeAtViewCenter(_ url: URL) {`
- L567: `func fitToWindow() {`
- L580: `func placeAllPhotos() {`
- L614: `func layerIndex(_ id: UUID) -> Double { Double(tableIDs.firstIndex(of: id) ?? 0) }`
- L615: `private func synchronizeOrder() { tableOrder = tableIDs }`
- L620: `func updateAlbum(_ id: UUID, _ change: (inout AlbumPage) -> Void) {`
- L624: `func deleteAlbum(_ id: UUID) {`
- L630: `func placeAlbum(_ id: UUID, at point: CGPoint? = nil) {`
- L637: `func albumFrame(_ page: AlbumPage) -> CGRect {`
- L644: `private func fitAlbumsAndSlides() {`
- L682: `func accept(_ providers: [NSItemProvider], at point: CGPoint?, board: CGRect?) -> Bool {`
- L693: `func acceptDrop(_ providers: [NSItemProvider], at point: CGPoint?, board: CGRect?, into trayID: UUID? = nil) async -> Bool {`
- L740: `private struct Saved: Codable {`
- L754: `private static func remapping(_ saved: Saved, aliases: [URL: URL]) throws -> Saved {`
- L779: `func save() {`
- L789: `func flushSave() {`
- L795: `private func persist(to storageURL: URL) {`

## Sources/LightTableApp/TrayAvailability.swift

## Sources/LightTableApp/UpdateConfiguration.swift
- L4: `enum UpdateConfiguration: Equatable {`
- L11: `static func read(_ info: [String: Any], isApplication: Bool) -> Self {`

## Sources/LightTableCore/AlbumPage.swift
- L4: `public enum FilmFormat: String, Codable, CaseIterable, Sendable {`
- L10: `public struct AlbumPocket: Identifiable, Codable, Equatable, Sendable {`
- L24: `public struct AlbumPage: Identifiable, Codable, Equatable, Sendable {`
- L37: `public func accepts(_ pocket: AlbumPocket, excluding ids: Set<UUID> = []) -> Bool {`
- L52: `public mutating func setPhoto(_ url: URL?, in id: UUID) {`
- L56: `public mutating func remove(_ id: UUID) { pockets.removeAll { $0.id == id } }`
- L70: `public mutating func fillEmptyPage(with format: FilmFormat) {`
- L76: `public func sanitized() -> AlbumPage {`
- L87: `public static func pocketRect(_ pocket: AlbumPocket) -> CGRect {`
- L93: `public static func imageRect(_ pocket: AlbumPocket, aspect: Double) -> CGRect {`

## Sources/LightTableCore/LightResponse.swift
- L5: `public struct LightResponseSample: Equatable, Sendable {`
- L17: `public enum LightResponse {`
- L26: `public static func panelWhite(brightness: Double = 1, flare: Double = 0) -> Double {`
- L34: `public static func photoLevel(brightness: Double) -> Double {`
- L39: `public static func photoBrightnessMix(brightness: Double) -> Double {`
- L44: `public static func photoGlowMix(flare: Double, brightness: Double) -> Double {`
- L50: `public static func photoExposureStops(flare: Double, brightness: Double) -> Double {`
- L56: `public static func sample(elapsed: TimeInterval, isLit: Bool,`
- L79: `private static func smoothstep(_ value: Double) -> Double {`

## Sources/LightTableCore/StartupPresentationProgress.swift
- L4: `public enum StartupPresentationProgress {`
- L7: `public static func preparation(completed: Int, total: Int, elapsed: TimeInterval) -> Double {`
- L17: `public static func photoCount(total: Int, progress: Double) -> Int? {`

## Sources/LightTableCore/TableLayout.swift
- L3: `public struct Slide: Identifiable, Codable, Equatable, Sendable {`
- L21: `public func normalized() -> Slide {`
- L25: `private static func finite(_ n: Double, in range: ClosedRange<Double>, fallback: Double) -> Double {`
- L31: `public struct TableLayout: Codable, Equatable, Sendable {`
- L36: `public mutating func place(_ url: URL, x: Double, y: Double) -> UUID {`
- L48: `public mutating func update(_ id: UUID, _ change: (inout Slide) -> Void) {`
- L53: `public mutating func bringToFront(_ id: UUID) {`
- L58: `public mutating func remove(_ id: UUID) { slides.removeAll { $0.id == id } }`
- L59: `public mutating func clear() { slides.removeAll() }`
- L64: `public mutating func fitToWindow(boardWidth: Double, boardHeight: Double,`
- L114: `public func sanitized() -> TableLayout {`
- L127: `public enum TableCoordinates {`
- L130: `public static func topmostImageHit(_ slides: [Slide], x: Double, y: Double,`
- L143: `public static func fit(width: Double, height: Double) -> (width: Double, height: Double) {`
- L148: `public static func placement(_ slide: Slide, boardWidth: Double, boardHeight: Double,`
- L158: `public static func localPoint(x: Double, y: Double, centerX: Double,`
- L166: `public struct SlideImageHit: Sendable {`
- L173: `public struct SlidePlacement: Equatable, Sendable {`
- L182: `public struct TableViewport: Codable, Equatable, Sendable {`
- L193: `public func screen(x: Double, y: Double) -> (x: Double, y: Double) {`
- L196: `public func world(x: Double, y: Double) -> (x: Double, y: Double) {`
- L199: `public func screen(_ p: SlidePlacement) -> SlidePlacement {`
- L204: `public mutating func zoom(to value: Double, aroundX x: Double, aroundY y: Double) {`
- L212: `public static func edgeVelocity(x: Double, y: Double, width: Double, height: Double) -> (x: Double, y: Double) {`
- L213: `func axis(_ position: Double, _ length: Double) -> Double {`

## THIRD_PARTY_NOTICES.md

## Tests/AlbumChecks.swift
- L6: `@MainActor static func main() async throws {`
- L8: `func check(_ yes: Bool, _ label: String) {`
- L14: `check(small.pockets.count == 20 && small.pockets.last?.column == 12 && small.pockets.last?.row == 9, "135 page has exactly five columns and four rows")`
- L15: `check(small.add(.film135) == nil && small.add(.film120) == nil, "full page refuses extra pockets")`
- L18: `check(large.pockets.count == 9 && large.pockets.last?.column == 10 && large.pockets.last?.row == 8, "120 page has exactly three columns and three rows")`
- L22: `check(mixed.add(.film135, column: 4, row: 0) == nil, "mixed formats cannot overlap")`
- L23: `check(mixed.add(.film120, column: 12, row: 10) == nil && mixed.add(.film135, column: -1, row: 0) == nil,`
- L26: `check(mixed.pockets.contains { $0.format == .film120 } && mixed.pockets.filter { $0.format == .film135 }.count > 1,`
- L33: `check(true, "mixed auto-placement never overlaps existing pockets")`
- L37: `check(small.move(firstID, column: 3, row: 0), "matching occupied pockets can exchange positions")`
- L38: `check(small.pockets.first { $0.id == firstID }?.url == urlA && small.pockets.first { $0.id == secondID }?.url == urlB &&`
- L41: `check(!mixed.move(a, column: 5, row: 0) && mixed == before, "invalid mixed-size swap is atomic")`
- L43: `check(mixed.pockets.first { $0.id == b }?.url == urlB, "replacement changes only the selected pocket")`
- L45: `check(mixed.pockets.first { $0.id == b }?.url == nil && mixed.pockets.contains { $0.id == b }, "clearing a photograph preserves its bag slot")`
- L53: `check(true, "all photo aspect ratios fit without cropping")`
- L56: `check(largeRect.width > smallRect.width * 1.5, "120 film has a larger fixed footprint")`
- L72: `check(ordering.tableIDs == [placedAlbum, secondFilm, firstFilm],`
- L74: `check(ordering.layerIndex(placedAlbum) == 0 && ordering.layerIndex(secondFilm) == 1`
- L77: `check(ordering.tableIDs == [placedAlbum, firstFilm],`
- L92: `check(accepted && abs(frame.midX - drop.x) < 1e-8 && abs(frame.midY + 64 - drop.y) < 1e-8, "whole-page drop preserves pointer position under camera transforms")`
- L94: `check(model.albums.count == 1 && model.layout.slides.isEmpty && model.onTableAlbums.count == 1, "repeated page drop does not duplicate or scatter its films")`
- L97: `check(model.albums[0].pockets == pockets, "moving a whole page leaves its internal positions unchanged")`
- L101: `check(model.tableIDs.last == id, "page can be placed above loose films")`
- L103: `check(model.tableIDs.last == slideID, "loose film can be placed above a page")`
- L106: `check(model.albums[0].pockets == pockets && model.layout.slides.count == 1 &&`
- L110: `check(restored.albums == model.albums && restored.layout == model.layout && restored.tableIDs == model.tableIDs,`
- L112: `check(restored.allPhotos.contains { $0.url == photo }, "page-only photo references remain available in the shared album library")`
- L118: `check(legacyModel.albums.isEmpty && legacyModel.layout == model.layout, "old layouts without album fields remain readable")`
- L120: `check(model.albums.count == 1 && model.onTableAlbums.isEmpty && model.albums[0].pockets == pockets,`
- L123: `check(model.albums.count == 1 && model.onTableAlbums.isEmpty && model.layout.slides.isEmpty,`
- L125: `check(try Data(contentsOf: photo) == bytes, "album operations never rewrite source files")`
- L128: `check(AlbumSurface.image(mixed, front: true) === front, "membrane images are reused while moving the magnifier")`
- L134: `check(output.width == 520 && output.height == 520, "transparent album surfaces render in the high-resolution lens")`
- L135: `check(LoupeScenePhoto(preview: nil, center: center, imageSize: CGSize(width: 100, height: 80)).bounds ==`

## Tests/CloseupChecks.swift
- L5: `private final class KeyWindow: NSWindow {`
- L9: `private struct PreferenceProbe: View {`
- L15: `.onPreferenceChange(SlidePresentationKey.self, perform: receive)`
- L19: `private actor RasterGate {`
- L23: `func render(_ request: LoupeRasterRequest) async -> CGImage? {`
- L27: `func finish(_ image: CGImage) {`
- L35: `@MainActor static func main() async throws {`
- L38: `func check(_ condition: Bool, _ message: String) {`
- L42: `check(contactImages.reduce(0) { $0 + $1.bytesPerRow * $1.height } == 108 * 1024,`
- L46: `check((0..<100).allSatisfy { _ in FilmContactTexture.image(for: variant) === contact },`
- L48: `check(contact.width == 96 && contact.height == 96, "contact variants keep the small fixed raster size")`
- L65: `check(clearPhoto, "every contact variant leaves all photograph pixels transparent and uncropped")`
- L66: `check(clearOuterEdge && visibleSide, "thin sides remain visible and shadows fade before the texture boundary")`
- L67: `check(premultiplied, "edge blending has valid premultiplied color without bright fringes")`
- L69: `check(contactPixels[0] != contactPixels[1] && contactPixels[0] != contactPixels[2] && contactPixels[1] != contactPixels[2],`
- L84: `check(leftLocalized && rightLocalized, "curl differences stay inside fixed corner caps rather than stretching along the photograph")`
- L87: `check(Set(shapes.map(\.rawValue)).count == 3, "saved placements distribute across all three contact shapes")`
- L88: `check(zip(contactIDs, shapes).allSatisfy { FilmContactVariant.forFilm(UUID(uuidString: $0.0.uuidString)!) == $0.1 },`
- L94: `check(LoupeGeometry.magnification == 1.5, "finding lens uses 1.5x")`
- L95: `check(LoupeGeometry.rasterDiameter(in: CGSize(width: 2200, height: 1100), closeViewing: false) == 260,`
- L99: `check(LoupeGeometry.rasterDiameter(in: window, closeViewing: true) == diameter,`
- L103: `check(abs(close.width - original.width) < 1e-8 && abs(close.minX - original.minX) < 1e-8 &&`
- L110: `check(beginning.scale == 1 && beginning.apertureScale == 1 && beginning.surround == 0,`
- L112: `check(middle.scale > middle.apertureScale && middle.scale / middle.apertureScale < 1.1,`
- L115: `check(early.scale > 1 && early.apertureScale > 1,`
- L118: `check(late.scale < ending.scale && late.apertureScale < ending.apertureScale,`
- L120: `check(abs(ending.scale - 3) < 1e-8 && abs(ending.apertureScale - 3) < 1e-8 && ending.surround == 1,`
- L133: `check(continuousAdvance, "101 samples advance film and aperture without overshoot or image recoil")`
- L134: `check(covered, "film always covers the aperture with no uncovered ring")`
- L146: `func center(_ p: CGFloat) -> CGPoint {`
- L174: `check(smoothEdges, "side and corner avoidance has continuous velocity throughout approach and return")`
- L175: `check(forwardEdges, "side and corner approach keeps its endpoints and never recoils")`
- L176: `check(alignedEdges, "film and aperture share the optical axis while avoiding window edges")`
- L177: `check(smoothScales, "shared film and circle timeline has no velocity breaks")`
- L180: `check(flat.scale == 1 && flat.apertureScale == 1, "small windows never invent an extra magnification at progress \(p)")`
- L187: `check(ordinary.diameter == 260 && ordinary.magnification == 1.5 && ordinary.contentScale == 1,`
- L190: `check(abs(end.magnification - 8) < 1e-8 && end.diameter == closeDiameter && end.contentScale == 1,`
- L193: `check(abs(returning.magnification * 260 / returning.diameter - 1.5) < 1e-8,`
- L214: `check(continuous, "8x approach and reversed return stay continuous and never overshoot at \(Int(window.width))pt")`
- L215: `check(containsField, "scene selection covers every intermediate field, including windows above native 8x, at \(Int(window.width))pt")`
- L216: `check(unchangedOriginal, "original mode retains the existing fixed sampling and layer animation at \(Int(window.width))pt")`
- L223: `check(originalField == eightField, "changing focal target retains both endpoint scenes at \(Int(window.width))pt")`
- L243: `check(focalScale, "focal transition follows continuous proportional magnification in both directions at \(Int(window.width))pt")`
- L244: `check(boundedRaster, "focal switching keeps the aperture and drawable size fixed at \(Int(window.width))pt")`
- L245: `check(fieldCoverage, "scene culling covers switching during approach and return at \(Int(window.width))pt")`
- L246: `check(coveredClip, "focal switching never shrinks the source below the lens clip at \(Int(window.width))pt")`
- L259: `check(smooth && abs(previousStrength - 0.75) < 1e-8,`
- L265: `check(approach.progress == 0.7 && approach.magnificationBlend == 0.38 && approach.scene.point == point,`
- L268: `check(approach.magnificationBlend == 0.35 && approach.progress == 0.7,`
- L271: `check(invalidBlend.magnification.isFinite && invalidBlend.contentScale.isFinite, "invalid focal ratio falls back to the original finite sampling")`
- L277: `check(abs(enlarged.width / baseline.width - 1.8) < 1e-8 && slide.width == 0.3,`
- L279: `check(rotated.angle == 37 && abs(rotated.width - enlarged.width) < 1e-8,`
- L298: `check(eightRequest.magnification == eightInput.magnification && eightRequest.diameter == eightInput.diameter,`
- L300: `check(abs(quickEight.diameter / quickEight.magnification! - eightRequest.diameter / eightRequest.magnification!) < 1e-8,`
- L304: `check(glassRequest.quick.refractionScale == 0.4, "quick fallback retains glass refraction strength")`
- L311: `check(near.blueComponent > 0.7 && far.redComponent > 0.7,`
- L313: `func photo(_ placement: SlidePlacement) -> LoupeScenePhoto {`
- L318: `func render(_ p: SlidePlacement, diameter: CGFloat = 260) -> CGImage {`
- L323: `func pixel(_ image: CGImage) -> NSColor {`
- L327: `check(before.redComponent > 0.7 && before.blueComponent > 0.7 && during.redComponent < 0.2 && during.blueComponent > 0.7,`
- L330: `check(close.width == 810 && abs(pixel(close).blueComponent - during.blueComponent) < 0.01,`
- L335: `func rasterRequest(_ x: CGFloat) -> LoupeRasterRequest {`
- L339: `func waitForCalls(_ count: Int) async {`
- L348: `check(raster.image == nil, "request returns before background rendering finishes")`
- L353: `check(initialCalls.count == 2 && initialCalls[0].diameter == 260 && initialCalls[1].scene.point.x == 100,`
- L360: `check(raster.image === close, "latest view replaces quick frame at final resolution")`
- L364: `check(idleCalls == 2, "unchanged scene and animation progress do not trigger another render")`
- L370: `check(raster.image == nil, "cancelled in-flight render cannot repopulate a dismissed lens")`
- L377: `check(actualRaster.image?.width == 810, "production worker delivers final-resolution image asynchronously")`
- L390: `check(ordinaryCalls.count == 1 && ordinaryCalls[0].diameter == 260 && ordinaryCalls[0].displayScale == 2,`
- L399: `check(ordinaryCount == 1 && ordinaryRaster.image === image,`
- L411: `check(received[slide.id] == rotated, "SwiftUI geometry preference publishes live pinch and rotation")`
- L414: `check(state.closeMagnification == .original, "original close magnification remains the default")`
- L419: `check(!state.press(), "space without a visible lens does not start a close view")`
- L421: `check(!state.cursorHidden, "inactive lens does not hide the system pointer")`
- L423: `check(state.cursorHidden && !state.isClose, "ordinary visible lens hides the pointer before space is pressed")`
- L425: `check(!state.cursorHidden, "leaving the ordinary lens restores the pointer")`
- L427: `check(state.cursorHidden, "re-entering the ordinary lens hides the pointer")`
- L431: `check(!state.cursorHidden, "late scene and hover updates cannot hide the pointer in an inactive window or tool")`
- L434: `check(!state.cursorHidden, "a disappearing lens restores the pointer during a drag")`
- L436: `check(state.press() && state.isClose && state.cursorHidden, "space down enters and hides cursor")`
- L439: `check(state.sourcePoint == movedPoint, "held-space view follows pointer movement instead of locking the source")`
- L441: `check(state.sourcePoint == movedPoint && !state.cursorHidden, "leaving the canvas retains the viewed point and restores the cursor")`
- L444: `check(!state.isClose && state.anchor == movedPoint, "release returns at the latest viewed point")`
- L447: `check(state.sourcePoint == returnPoint, "pointer remains responsive while the close view returns")`
- L449: `check(state.press() && state.anchor == returnPoint, "repress reverses return without jumping to another source point")`
- L451: `check(state.isClose && state.anchor == returnPoint, "cancelled return cannot interrupt a new close view")`
- L453: `check(state.anchor == nil && state.cursorHidden && !state.isClose, "reduced motion returns immediately to the ordinary lens with its cursor still hidden")`
- L455: `check(state.anchor != nil && state.cursorHidden, "return retains its high-resolution scene until the motion finishes")`
- L457: `check(state.anchor == nil && state.cursorHidden, "normal return keeps the pointer hidden under the ordinary lens")`
- L459: `check(state.anchor == nil && !state.cursorHidden && !state.isClose, "cancellation balances cursor and clears close state")`
- L460: `check(state.closeMagnification == .eight, "pointer mode and cancellation retain the selected close magnification")`
- L466: `check(tracking.sourcePoint == tracking.pointer, "ordinary lens tracks the pointer directly")`
- L469: `check(tracking.sourcePoint == tracking.pointer, "approaching lens retains ordinary movement speed")`
- L472: `check(tracking.sourcePoint == tracking.pointer, "fully approached lens keeps full movement on both axes")`
- L475: `check(tracking.sourcePoint == tracking.pointer, "held space and direction reversal preserve direct tracking")`
- L478: `check(tracking.sourcePoint == outside, "hover exit retains the last inspected point")`
- L480: `check(tracking.sourcePoint == tracking.pointer, "hover re-entry resumes at the real pointer without a stored offset")`
- L483: `check(tracking.sourcePoint == tracking.pointer, "return retains the same movement speed without offset recovery")`
- L486: `check(tracking.sourcePoint == tracking.pointer, "repress during return preserves unified movement speed")`
- L489: `check(tracking.sourcePoint == tracking.pointer && tracking.cursorHidden,`
- L502: `func sampleOptic(_ scale: CGFloat, x: CGFloat, y: CGFloat) -> NSColor {`
- L513: `check(still.blueComponent > 0.7 && approaching.redComponent > 0.7,`
- L515: `check(sampleOptic(2, x: 5, y: 5).alphaComponent < 0.01,`
- L532: `func comparisonPixel(_ image: CGImage, x: Int, y: Int) -> NSColor {`
- L544: `func tablePixel(_ textured: Bool, transmission: Double = 1) -> NSColor {`
- L554: `check(tableOriginal.blueComponent > 0.7 && tableTexture.redComponent > 0.7,`
- L562: `check(abs(a.redComponent - b.redComponent) < 0.02 && abs(a.greenComponent - b.greenComponent) < 0.02 && abs(a.blueComponent - b.blueComponent) < 0.02,`
- L566: `check(abs(darkOriginal.redComponent - darkTexture.redComponent) < 0.01 &&`
- L569: `func comparisonScene(_ textured: Bool) -> LoupeSceneInput {`
- L574: `check(comparisonScene(false) != comparisonScene(true) && comparisonScene(true) == comparisonScene(true),`
- L579: `check(leftGlass.angle < rightGlass.angle && abs(leftGlass.angle) < 22 && abs(rightGlass.angle) < 22,`
- L583: `check(reducedA == reducedB, "reduced motion holds glass reflections still")`
- L585: `check(abs(closeGlass.intensity - 0.45) < 1e-8 && leftGlass.intensity == 1 && closeGlass.rimIntensity == 0,`
- L589: `check(invalidGlass.angle.isFinite && invalidGlass.intensity.isFinite,`
- L591: `func glassBitmap(_ pose: LoupeGlassPose) -> CGImage {`
- L624: `check(centreAlpha < 0.01, "glass surface leaves the central photograph fully clear")`
- L625: `check(edgeAlpha > 0.30 && edgeAlpha < 0.65 && reflectionChange > 0.005,`
- L627: `check(closeEdgeAlpha > 0.015 && closeEdgeAlpha < edgeAlpha * 0.65,`
- L629: `check(darkBevel > 0.02 && darkBevel < 0.16 && brightLip > 0.30 && brightLip < 0.65,`
- L635: `check(adjusted.x < 420 && adjusted.y < 320,`
- L641: `check(center.x >= radius && center.x + radius <= 840 && center.y >= radius && center.y + radius <= 640,`
- L644: `check(LoupeGeometry.closeCenter(from: CGPoint(x: 400, y: 320), in: windowSize) == CGPoint(x: 400, y: 320),`
- L659: `func key(_ type: NSEvent.EventType, repeat repeated: Bool = false,`
- L665: `check(monitor.handle(key(.keyDown)) == nil && monitor.handle(key(.keyDown, repeat: true)) == nil && presses == 1,`
- L667: `check(monitor.handle(key(.keyUp, modifiers: .shift)) == nil && releases == 1,`
- L669: `check(monitor.handle(key(.keyDown, modifiers: .command)) != nil && presses == 1,`
- L674: `check(monitor.handle(key(.keyDown)) != nil && presses == 1, "text input retains its space key")`
- L676: `check(monitor.handle(key(.keyDown, number: 0)) != nil && presses == 1, "other-window keys do not trigger this lens")`
- L679: `check(cancellations > 0 && monitor.handle(key(.keyUp)) != nil,`
- L682: `check(monitor.handle(key(.keyDown)) != nil && presses == 2, "pointer mode leaves space unhandled")`
- L687: `check(monitor.handle(key(.keyDown, code: 48)) != nil && cycles == 0,`
- L690: `check(monitor.handle(key(.keyDown, code: 48)) == nil && cycles == 1 && state.closeMagnification == .eight,`
- L692: `check(monitor.handle(key(.keyDown, repeat: true, code: 48)) == nil && cycles == 1,`
- L694: `check(monitor.handle(key(.keyUp, modifiers: .shift, code: 48)) == nil,`
- L696: `check(monitor.handle(key(.keyDown, code: 48)) == nil && cycles == 2 && state.closeMagnification == .original,`
- L700: `check(state.cursorHidden, "the active window restores ordinary lens cursor hiding")`
- L706: `check(monitor.handle(key(.keyDown, code: 48)) == nil && state.closeMagnification == .eight &&`
- L710: `check(monitor.handle(key(.keyUp)) == nil && !state.isClose && state.cursorHidden,`
- L713: `check(monitor.handle(key(.keyDown, modifiers: modifiers, code: 48)) != nil && cycles == 3,`
- L717: `check(monitor.handle(key(.keyDown, code: 48)) != nil && cycles == 3,`
- L720: `check(monitor.handle(key(.keyDown, number: 0, code: 48)) != nil && cycles == 3,`
- L722: `check(monitor.handle(key(.keyDown, repeat: true, code: 48)) != nil && cycles == 3,`
- L726: `check(monitor.handle(key(.keyUp, code: 48)) != nil &&`
- L729: `check(!state.cursorHidden && !state.cursorEnabled, "window focus loss releases the cursor even with an ordinary lens")`
- L731: `check(state.cursorHidden, "window focus return hides the cursor only for an available lens")`
- L734: `check(monitor.handle(key(.keyUp, code: 48)) != nil &&`
- L738: `check(cancellations > 1, "view teardown cancels input state and removes monitor")`
- L750: `func mouse(_ type: NSEvent.EventType, _ point: CGPoint, modifiers: NSEvent.ModifierFlags = .shift, number: Int? = nil) -> NSEvent {`
- L755: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 100, y: 100), modifiers: [])) != nil,`
- L757: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 100, y: 100), modifiers: .command)) != nil,`
- L759: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 100, y: 35))) != nil,`
- L761: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 100, y: 100), number: 0)) != nil,`
- L764: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 300, y: 260))) != nil && selectionStarts == 0,`
- L767: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 300, y: 260))) == nil && selectionStarts == 1,`
- L769: `check(selectionMonitor.handle(mouse(.leftMouseDragged, CGPoint(x: 100, y: 90), modifiers: [])) == nil &&`
- L772: `check(selectionMonitor.handle(mouse(.leftMouseUp, CGPoint(x: 600, y: 0), modifiers: [])) == nil && selectionEnds == 1 &&`
- L776: `check(selectionMonitor.handle(key(.keyDown, code: 53)) == nil && selectionCancels == 1,`
- L778: `check(selectionMonitor.handle(mouse(.leftMouseUp, CGPoint(x: 150, y: 170))) == nil && selectionEnds == 1,`
- L782: `check(selectionCancels == 2, "focus loss cancels selection and balances the crosshair cursor")`
- L784: `check(selectionMonitor.handle(mouse(.leftMouseDown, CGPoint(x: 120, y: 150))) != nil,`

## Tests/DropChecks.swift
- L7: `struct DropChecks {`
- L8: `struct Failure: Error, CustomStringConvertible { let description: String }`
- L9: `struct Saved: Codable { var urls: [URL]; var layout: TableLayout; var label: String }`
- L12: `static func main() async throws {`
- L18: `func check(_ value: Bool, _ label: String) throws {`
- L22: `func file(_ path: String, _ data: Data) throws -> URL {`
- L30: `try check(TablePreviewSizing.maxPixel(size: CGSize(width: 110, height: 74), displayScale: 2) == 256,`
- L32: `try check(TablePreviewSizing.maxPixel(size: CGSize(width: 110, height: 170), displayScale: 2) == 384,`
- L34: `try check(TablePreviewSizing.maxPixel(size: CGSize(width: 200, height: 130), displayScale: 1) == 256 &&`
- L37: `try check(TablePreviewSizing.maxPixel(size: CGSize(width: 250, height: 160), displayScale: 2) == 512 &&`
- L40: `try check(TablePreviewSizing.maxPixel(size: CGSize(width: 257, height: 170), displayScale: 2) == 768 &&`
- L43: `try check(TablePreviewSizing.maxPixel(size: .zero, displayScale: 2) == 256 &&`
- L56: `try check(CGImageDestinationFinalize(output), "create a full-size fixture for preview separation checks")`
- L61: `try check(small?.source.width == 256 && small?.source === repeatedSmall?.source,`
- L63: `try check(small.map { [$0.source, $0.dark, $0.lit, $0.flare, $0.boosted].allSatisfy { max($0.width, $0.height) <= 256 } } == true,`
- L65: `try check(sizingModel.preparedPreviews[previewFixture] == nil,`
- L69: `try check(large?.source.width == 768 && reduced?.source === large?.source,`
- L72: `try check(full?.source.width == 2200 && sizingModel.preparedTablePreviews[previewFixture]?.source === large?.source,`
- L78: `try check(pair.0 != nil && pair.0?.source === pair.1?.source,`
- L80: `try check(try Data(contentsOf: previewFixture) == originalFixture,`
- L107: `try check(startup.isPreparingStartup && startup.startupTotal == 2 && startup.startupCompleted == 0,`
- L115: `try check(startup.startupCompleted == 2 && startup.startupFailedURLs.isEmpty && startup.startupPhase == .finishing,`
- L117: `try check(startup.preparedPreviews[previewFixture]?.source.width == 2200 &&`
- L120: `try check(startup.preparedPreviews[startupTrayPhoto] == nil && startup.preparedPreviews[startupOffTablePhoto] == nil,`
- L123: `try check(startup.isPreparingStartup, "startup still waits for restored album display surfaces")`
- L125: `try check(!startup.isPreparingStartup, "last prepared surface releases the startup screen")`
- L127: `try check(startup.lightStartedAt != nil, "lamp responds immediately after startup readiness")`
- L130: `try check(startup.startupCompleted == 2 && !startup.isPreparingStartup &&`
- L133: `try check(try Data(contentsOf: startupStorage) == savedStartupBytes &&`
- L146: `try check(failedStartup.startupPhase == .needsAttention && failedStartup.startupCompleted == 0 &&`
- L153: `try check(!failedSkip.isPreparingStartup && failedSkip.layout.slides.count == 1,`
- L157: `try check(failedStartup.startupCompleted == 1 && failedStartup.startupFailedURLs.isEmpty &&`
- L161: `try check(!failedStartup.isPreparingStartup && failedStartup.preparedPreviews[brokenPhoto] != nil,`
- L170: `try check(!emptyStartup.isPreparingStartup && emptyStartup.startupTotal == 0 && emptyStartup.preparedPreviews.isEmpty,`
- L180: `try check(model.transmissionEnabled, "transmitted light is permanently enabled")`
- L181: `try check(model.layout == originalLayout && model.photos == originalPhotos &&`
- L184: `try check(model.thumbnails[original] === thumbnail, "placed photo can immediately reuse its tray thumbnail")`
- L185: `try check(prepared != nil && reused?.source === prepared?.source && model.preparedPreviews[original] != nil,`
- L189: `try check(provider.hasItemConformingToTypeIdentifier(PhotoDrag.contentType.identifier) &&`
- L193: `try check(accepted && model.photos.count == 1 && model.layout.slides.count == 1 && !model.isLit,`
- L199: `try check(model.isLit && model.layout.slides[0].id == firstID && model.layout.slides[0].url == original,`
- L202: `try check(model.photos.count == 1 && model.layout.slides.count == 1, "dropping onto tray does not import again")`
- L206: `try check(filesBefore == filesAfter && (try Data(contentsOf: original)) == image, "internal drop creates no files and leaves original bytes intact")`
- L211: `try check(model.photos.count == 2 && model.layout.slides.count == 2, "external file drop imports and places one photo")`
- L215: `try check(model.photos.count == 3 && model.layout.slides.count == 2, "external folder recursively fills tray without placing files")`
- L218: `try check(!rejected && model.photos.count == 3, "stale internal identity cannot import an arbitrary file")`
- L235: `try check(!lighting.isLit && lighting.lightStartedAt == nil && lighting.layout == beforeLightLayout && lighting.photos == beforeLightPhotos,`
- L237: `try check(!lighting.isLit && lighting.lightStartedAt == nil, "new table starts unlit with no illumination clock")`
- L241: `try check(firstStart >= firstStartLowerBound && firstStart <= ProcessInfo.processInfo.systemUptime,`
- L243: `try check(lighting.layout == beforeLightLayout && lighting.zoom == 1.3 && lighting.photos == beforeLightPhotos &&`
- L247: `try check(lighting.lightStartedAt == firstStart, "assigning an already lit lamp does not restart illumination")`
- L249: `try check(lighting.lightStartedAt == firstStart && lighting.layout == beforeLightLayout && lighting.photos == beforeLightPhotos,`
- L253: `try check(lighting.isLit && lighting.lightStartedAt == firstStart && lighting.photos == beforeLightPhotos && lighting.zoom == 1.3 &&`
- L259: `try check(lighting.lightStartedAt == nil && lighting.layout == beforeOffLayout && lighting.zoom == 1.3 &&`
- L263: `try check(lighting.lightStartedAt == nil, "assigning an already dark lamp keeps its clock cleared")`
- L267: `try check(secondStart > firstStart && secondStart >= secondStartLowerBound && secondStart <= ProcessInfo.processInfo.systemUptime &&`
- L275: `try check(preparing.lightStartedAt == nil, "lighting waits for all existing photo surfaces")`
- L277: `try check(preparing.lightStartedAt == nil, "first ready photo cannot ignite before the rest")`
- L280: `try check(synchronizedStart != nil, "last ready photo starts one shared illumination clock")`
- L282: `try check(preparing.lightStartedAt == synchronizedStart, "adding a photo while lit does not replay ignition")`
- L285: `try check(!preparing.isLit && preparing.lightStartedAt == nil, "cancelling pending ignition cannot turn the lamp back on")`
- L304: `try check(restored.photos.count == 4 && restored.photos.contains(where: { $0.url == original }) &&`
- L306: `try check(restored.photos.contains(where: { $0.url == distinct }) &&`
- L310: `try check(restored.layout.slides.count == 2 && restored.layout.slides.first?.id == distinctID &&`
- L314: `try check(backups.count == 1 && (try Data(contentsOf: backups[0])) == before, "migration writes an exact backup before replacing saved layout")`
- L317: `try check(again.photos.count == 4 && backupsAgain.count == 1 && again.layout == restored.layout, "migration is idempotent on the next launch")`
- L318: `try check((try Data(contentsOf: original)) == image && fm.fileExists(atPath: cached.path), "migration neither edits original images nor deletes old cache files")`
- L331: `try check(fitModel.pan == .zero && fitModel.worldSize == fitModel.boardSize,`
- L333: `try check(fitModel.zoom == 1 && fitModel.fitScale < 1 && fitModel.layout.slides.allSatisfy { $0.width == Slide.defaultWidth },`
- L335: `try check(fitModel.photos == fitPhotos && fitModel.selectedID == fitSelection && fitModel.isLit && fitModel.loupeEnabled &&`
- L337: `try check(fitModel.imageAspects.count == 12 && fitModel.imageAspects.values.allSatisfy { $0 == 1 },`
- L341: `try check(fitRestored.layout == fitModel.layout && fitRestored.fitScale == fitModel.fitScale, "fitted geometry and shared display scale survive relaunch")`
- L343: `try check(fitModel.layout.slides.isEmpty && fitModel.fitScale == 1, "clearing the table restores default scale for new photos")`
- L349: `try check(offscreen.x == -2 && offscreen.y == 3,`
- L353: `try check(navigation.layout.slides[0] == offscreen,`
- L357: `try check(navigation.screenPlacement(offscreen, aspect: 1) == beforeResize,`
- L365: `try check(abs(dropped.x - expected.x) < 1e-9 && abs(dropped.y - expected.y) < 1e-9 &&`
- L370: `try check(navigationRestored.layout == navigation.layout && navigationRestored.viewport == navigation.viewport &&`
- L374: `try check(navigation.pan == .zero && navigation.zoom == 1 && navigation.layout.slides[0].width == Slide.defaultWidth &&`
- L391: `try check(batch.selectedIDs == [batchIDs[0]] && batch.tableIDs.last == batchIDs[0] && !batch.showsSelection,`
- L394: `try check(!batch.showsSelection, "committing a dragged photo does not flash selection chrome back on")`
- L396: `try check(batch.showsSelection, "clicking the same photo after dragging restores explicit selection feedback")`
- L398: `try check(!batch.showsSelection && batch.selectedIDs == [batchIDs[0]],`
- L403: `try check(batch.showsSelection, "marquee selection reveals the same photo even when its ID set is unchanged")`
- L405: `try check(batch.selectedIDs == Set(batchIDs.prefix(2)), "command-click adds another photograph without clearing selection")`
- L406: `try check(batch.showsSelection, "command multi-selection keeps its visible selection feedback")`
- L408: `try check(batch.selectedIDs == [batchIDs[1]], "command-click toggles an already-selected photograph off")`
- L410: `try check(batch.selectedIDs.isEmpty && batch.selectedID == nil, "toggling the last selected item clears the primary selection")`
- L413: `try check(batch.selectedIDs == Set(batchIDs.prefix(2)), "selection rejects stale or foreign identifiers")`
- L415: `try check(batch.selectedIDs == [batchIDs[2]], "plain selection replaces a previous multi-selection")`
- L417: `try check(batch.selectedIDs.isEmpty, "legacy clear-selection action clears the complete set")`
- L420: `try check(batch.layout == untouchedLayout, "empty selection never removes a photograph")`
- L426: `try check(batch.selectedIDs == [batchIDs[0]], "marquee selects a rotated photograph after pan and camera zoom")`
- L429: `try check(batch.selectedIDs.isEmpty, "empty corners of a rotated bounding box are not mistaken for image pixels")`
- L436: `try check(batch.selectedIDs == Set(batchIDs.prefix(2)), "marquee selects all intersecting photos and excludes a distant photo")`
- L438: `try check(batch.selectedIDs == Set(batchIDs.prefix(2)), "reverse-direction marquee uses the same selection area")`
- L440: `try check(batch.selectedIDs.isEmpty, "zero-size marquee clears the previous selection safely")`
- L449: `try check(batch.selectedIDs.contains(batchAlbum), "marquee treats an album page as a table item")`
- L452: `try check(batch.layout.slides.map(\.id) == [batchIDs[2]], "batch removal keeps the unselected photograph in place")`
- L453: `try check(batch.onTableAlbums.isEmpty && batch.albums[0].pockets == albumPockets,`
- L455: `try check(batch.photos == batchPhotos && batch.isLit && batch.lightStartedAt == batchClock,`
- L457: `try check(batch.selectedIDs.isEmpty && !batch.tableIDs.contains(batchAlbum) &&`
- L461: `try check(batchRestored.layout == batch.layout && batchRestored.albums == batch.albums && batchRestored.photos == batchPhotos,`
- L463: `try check(batchRestored.selectedIDs.isEmpty, "selection remains transient across relaunch")`
- L465: `try check(batch.layout.slides.isEmpty && batch.fitScale == 1 && batch.photos == batchPhotos,`
- L468: `try check(Data(contentsOf: url) == image, "batch removal never deletes or changes source \(url.lastPathComponent)")`

## Tests/FeatureGuideChecks.swift
- L4: `@MainActor static func main() {`
- L9: `func expect(_ value: Bool, _ label: String) { precondition(value, label); count += 1 }`
- L17: `expect(state.available && !state.hasBeenPresented, "matching release starts unseen")`
- L19: `expect(state.phase == .idle, "busy windows defer notes")`
- L21: `expect(state.isWelcome(in: a), "ready window claims notes")`
- L22: `expect(!state.hasBeenPresented, "claim is not presentation")`
- L24: `expect(state.owner == a, "second window cannot duplicate")`
- L26: `expect(!state.hasBeenPresented, "wrong window cannot acknowledge")`
- L29: `expect(state.isWelcome(in: b), "closed unseen window does not lose notes")`
- L31: `expect(defaults.stringArray(forKey: FeatureGuideState.seenKey) == oldVersions + ["0.1.75"], "presentation persists once and keeps older versions")`
- L32: `expect(defaults.stringArray(forKey: FeatureGuideState.seenRevisionKey) == oldRevisions + [revision], "presentation preserves older content records")`
- L33: `expect(FeatureGuideState.tourStepCount == 0, "this adjustment release contains notes without an operation tour")`
- L35: `expect(state.isWelcome(in: b) && state.step(in: b) == nil, "notes-only release cannot start a tour")`
- L37: `expect(state.isWelcome(in: b), "tour navigation cannot change notes-only state")`
- L39: `expect(state.isWelcome(in: b), "other window cannot dismiss notes")`
- L41: `expect(state.phase == .idle && state.owner == nil, "start viewing closes notes")`
- L44: `expect(restarted.phase == .idle, "restart does not repeat notes")`
- L46: `expect(restarted.isWelcome(in: a), "help replays seen notes")`
- L48: `expect(restarted.owner == a, "help does not steal other window")`
- L50: `expect(restarted.phase == .idle, "close dismisses notes")`
- L52: `expect(!previousRelease.available, "old version cannot display current release notes")`
- L55: `expect(!unknown.available && unknown.phase == .idle, "unauthored future version stays quiet")`
- L56: `expect(!unknown.hasBeenPresented, "seen versions remain isolated")`
- L62: `expect(!upgraded.hasBeenPresented, "earlier test builds do not suppress the release notes")`
- L64: `expect(upgraded.isWelcome(in: a), "existing test users receive the new notes")`
- L66: `expect(!upgraded.hasBeenPresented, "unrendered notes are not acknowledged")`
- L68: `expect(upgraded.hasBeenPresented, "rendered notes are acknowledged")`
- L69: `expect(defaults.stringArray(forKey: FeatureGuideState.seenKey) == oldVersions + ["0.1.75"], "upgrading keeps version history without duplicates")`
- L70: `expect(defaults.stringArray(forKey: FeatureGuideState.seenRevisionKey) == oldRevisions + [revision], "upgrading preserves previous content records")`
- L74: `expect(upgradedRestart.phase == .idle, "notes do not repeat after restart")`

## Tests/LightChecks.swift
- L8: `struct LightChecks {`
- L9: `struct Failure: Error, CustomStringConvertible { let description: String }`
- L12: `static func require(_ yes: @autoclosure () -> Bool, _ message: String) throws {`
- L15: `static func check(_ name: String, _ work: () async throws -> Void) async {`
- L19: `static func main() async {`
- L39: `func sample(_ bytes: [UInt8], _ index: Int) -> [Double] {`
- L46: `await check("Black film borders remain black in steady and peak previews") {`
- L54: `await check("Transmission caches retain source tones before display-time lamp gain") {`
- L61: `await check("Highlight shoulder retains gradations without white clipping") {`
- L68: `await check("Neutral grays and colored swatches retain hue") {`
- L79: `await check("Unlit film is darker than before but retains faint scene information") {`
- L95: `await check("All previews preserve geometry and original file bytes") {`
- L105: `await check("Response starts dark, reaches one peak, and settles exactly") {`
- L122: `await check("Off and reduced-motion states never flash or oscillate") {`
- L132: `await check("Panel, photographs and edge light share one brief peak and recovery") {`
- L163: `await check("Light response has no jumps or velocity breaks at phase boundaries") {`
- L176: `await check("Switch-on halo leaves shadow and midtone exposure stable") {`
- L183: `await check("Bright highlights show visible bloom without an overall exposure dip") {`
- L188: `await check("Low lamp brightness also reduces the switch-on transient") {`
- L202: `await check("Continuous brightness keeps panel and photo levels monotonic") {`
- L220: `await check("Comparison selects source or cached transmission without processing again") {`
- L231: `await check("Flat color and gray areas keep original levels and saturation") {`
- L237: `await check("Source midtone edge contrast remains unchanged without sharpening") {`
- L248: `await check("Light spills just outside highlights while bright interiors retain detail") {`
- L255: `func change(_ x: Int) -> Double {`
- L275: `func apertureSample(_ bytes: [UInt8], _ x: Int) -> Double {`
- L278: `await check("Aperture scatter decays smoothly into nearby black film") {`
- L292: `await check("Higher lamp scatter grows locally without fogging distant film") {`
- L301: `await check("Aperture interiors retain brightness and original geometry") {`
- L309: `await check("Ignition spreads beyond apertures and settles onto persistent transmission") {`
- L315: `func level(_ time: Double) -> Double {`
- L324: `await check("Original comparison removes persistent scatter without regenerating the preview") {`
- L331: `await check("Warm and saturated colors preserve original channel relationships") {`
- L346: `static func writeFixture(_ url: URL, colors: [[CGFloat]]) throws {`
- L357: `static func writeApertures(_ url: URL) throws {`
- L373: `static func pixels(_ image: CGImage, space: CGColorSpace = CGColorSpace(name: CGColorSpace.sRGB)!) -> [UInt8] {`
- L382: `static func luminance(_ rgb:[Double]) -> Double { 0.2126*rgb[0]+0.7152*rgb[1]+0.0722*rgb[2] }`
- L383: `static func linear(_ x:Double) -> Double { x <= 0.04045 ? x/12.92 : pow((x+0.055)/1.055,2.4) }`

## Tests/LightTableCoreTests/StartupPresentationProgressTests.swift
- L4: `final class StartupPresentationProgressTests: XCTestCase {`
- L5: `func testPhotoCountBeginsAtHalfwayForEveryLibrarySize() {`
- L7: `XCTAssertNil(StartupPresentationProgress.photoCount(total: total, progress: 0.499))`
- L8: `XCTAssertEqual(StartupPresentationProgress.photoCount(total: total, progress: 0.5), 1)`
- L9: `XCTAssertEqual(StartupPresentationProgress.photoCount(total: total, progress: 1), total)`
- L13: `func testEntireCountOccupiesOnlyTheSecondHalf() {`
- L14: `XCTAssertEqual(StartupPresentationProgress.photoCount(total: 20, progress: 0.75), 10)`
- L15: `XCTAssertEqual(StartupPresentationProgress.photoCount(total: 22, progress: 0.75), 11)`
- L16: `XCTAssertEqual(StartupPresentationProgress.photoCount(total: 100, progress: 0.75), 50)`
- L17: `XCTAssertEqual(StartupPresentationProgress.photoCount(total: 100, progress: 0.9), 80)`
- L20: `func testRealWorkDrivesProgressWithoutFixedDuration() {`
- L21: `XCTAssertEqual(StartupPresentationProgress.preparation(completed: 10, total: 20, elapsed: 0), 0.5)`
- L22: `XCTAssertEqual(StartupPresentationProgress.preparation(completed: 10, total: 20, elapsed: 200), 0.5)`
- L23: `XCTAssertEqual(StartupPresentationProgress.preparation(completed: 10, total: 100, elapsed: 200), 0.1)`
- L24: `XCTAssertLessThanOrEqual(StartupPresentationProgress.preparation(completed: 0, total: 100, elapsed: 3600), 0.08)`
- L25: `XCTAssertLessThan(StartupPresentationProgress.preparation(completed: 100, total: 100, elapsed: 3600), 1)`
- L28: `func testCounterNeverMovesBackwardsOrExceedsTheTotal() {`
- L35: `XCTAssertGreaterThanOrEqual(progress, previousProgress)`
- L36: `XCTAssertLessThan(progress, 1)`
- L39: `XCTAssertGreaterThanOrEqual(count, previousCount)`
- L40: `XCTAssertLessThanOrEqual(count, total)`
- L44: `XCTAssertNil(StartupPresentationProgress.photoCount(total: 0, progress: 1))`
- L45: `XCTAssertNil(StartupPresentationProgress.photoCount(total: 22, progress: .nan))`
- L46: `XCTAssertEqual(StartupPresentationProgress.preparation(completed: 0, total: 0, elapsed: 1), 0)`
- L47: `XCTAssertEqual(StartupPresentationProgress.preparation(completed: -1, total: 20, elapsed: .nan), 0)`

## Tests/LightTableCoreTests/TableLayoutTests.swift
- L4: `final class TableLayoutTests: XCTestCase {`
- L7: `func testDropKeepsLocationAndDoesNotArrangeExistingSlides() {`
- L11: `XCTAssertEqual(table.slides.first?.id, a)`
- L12: `XCTAssertEqual(table.slides.first?.x, 0.27)`
- L13: `XCTAssertEqual(table.slides.first?.y, 0.61)`
- L16: `func testRepeatedDropMovesExistingSlideAndRaisesIt() {`
- L20: `XCTAssertEqual(table.place(photo, x: 0.4, y: 0.6), a)`
- L21: `XCTAssertEqual(table.slides.count, 2)`
- L22: `XCTAssertEqual(table.slides.last?.id, a)`
- L23: `XCTAssertEqual(table.slides.last?.x, 0.4)`
- L26: `func testInvalidTransformCannotLoseSlide() {`
- L31: `XCTAssertEqual(slide.x, -20); XCTAssertEqual(slide.y, 999)`
- L32: `XCTAssertEqual(slide.width, 0.12); XCTAssertEqual(slide.angle, 0)`
- L35: `func testLayoutRoundTripPreservesRotationScaleAndOrder() throws {`
- L40: `XCTAssertEqual(try JSONDecoder().decode(TableLayout.self, from: data).sanitized(), table)`
- L43: `func testRemoveAndClearDoNotTouchOriginalFile() throws {`
- L50: `XCTAssertTrue(table.slides.isEmpty)`
- L51: `XCTAssertEqual(try Data(contentsOf: file), Data([1, 2, 3]))`
- L54: `func testResizePreservesNormalizedLayout() {`
- L57: `XCTAssertEqual(small.width, 800)`
- L58: `XCTAssertEqual(small.height, 700)`
- L59: `XCTAssertGreaterThan(large.width, small.width)`
- L62: `func testLoupeCoordinatesInvertQuarterTurn() {`
- L64: `XCTAssertEqual(p.x, 30, accuracy: 1e-9); XCTAssertEqual(p.y, 0, accuracy: 1e-9)`

## Tests/LoupeChecks.swift
- L5: `struct LoupeChecks {`
- L6: `static func main() throws {`
- L20: `func render(_ point: CGPoint, refracts: Bool) -> CGImage {`
- L25: `func pixels(_ image: CGImage) -> [UInt8] {`
- L54: `func fadedGlass(_ strength: CGFloat) -> [UInt8] {`
- L75: `func rampPixels(_ refracts: Bool) -> [UInt8] {`
- L108: `func scaledRamp(_ refracts: Bool) -> CGImage {`
- L129: `func solid(_ r: CGFloat, _ g: CGFloat, _ b: CGFloat) -> PhotoPreview {`
- L143: `func cropRender(_ viewport: CGRect?) -> CGImage {`
- L166: `func scene(_ photos: [LoupeScenePhoto], point: CGPoint = CGPoint(x: 150, y: 150), panel: CGRect? = nil) -> [UInt8] {`
- L170: `func rgb(_ bytes: [UInt8], _ x: Int, _ y: Int) -> [Int] {`
- L174: `func isRed(_ value: [Int]) -> Bool { value[0] > 150 && value[1] < 70 && value[2] < 70 }`
- L175: `func isBlue(_ value: [Int]) -> Bool { value[2] > 150 && value[0] < 70 && value[1] < 70 }`
- L213: `func detailRender(width: CGFloat, magnification: CGFloat, scale: CGFloat,`
- L223: `func assertSameDetail(_ small: CGImage, _ large: CGImage) {`
- L257: `func illuminated(_ flare: Double, _ brightness: Double) -> [UInt8] {`

## Tests/OpticsChecks.swift
- L7: `@MainActor static func main() async throws {`
- L10: `func check(_ yes: Bool, _ message: String) {`
- L15: `func image(_ r: CGFloat, _ g: CGFloat? = nil, _ b: CGFloat? = nil) -> CGImage {`
- L19: `func pixel(_ input: CIImage, x: CGFloat = 32, y: CGFloat = 32) -> [Float] {`
- L28: `func lit(_ source: CGImage, transmitted: Bool = true, t: Double = 1) -> CIImage {`
- L33: `check(PhotoOptics.fusedOpticalKernel != nil, "SDR lamp and ignition kernel compiles")`
- L34: `check(PhotoOptics.ignitionKernel != nil, "ignition exposure kernel compiles")`
- L35: `func exposed(_ source: CGImage, time: Double, brightness: Double = 1) -> CIImage {`
- L43: `check(peakMiddle > 0.31 && peakMiddle < 0.32, "trial ignition raises midtones by 0.8 stops within SDR")`
- L44: `check(pixel(exposed(brightScene, time: LightResponse.peakTime))[0] > 0.94 && pixel(exposed(brightScene, time: LightResponse.peakTime))[0] < 0.96,`
- L46: `check(abs(pixel(exposed(middle, time: 0.28))[0] - peakMiddle) < 0.001, "overexposure holds briefly before recovery")`
- L50: `check(value <= lastExposure + 0.001 && value >= 0.179, "recovery is monotonic without undershoot at step \(step)")`
- L53: `check(abs(lastExposure - 0.18) < 0.002, "recovery restores the original steady exposure exactly")`
- L54: `check(abs(pixel(exposed(middle, time: 1.10))[0] - 0.18) < 0.002, "photo exposure restores exactly at 1.10 seconds")`
- L55: `check(pixel(exposed(image(0), time: LightResponse.peakTime))[0] < 0.001 &&`
- L57: `check(pixel(exposed(middle, time: LightResponse.peakTime, brightness: 0.1))[0] < peakMiddle, "dim lamp also limits ignition exposure")`
- L61: `check(value > lastHighlight && value <= 1.002 && value + 0.002 >= Float(tone),`
- L68: `check(abs(ignitionBase[1] / ignitionBase[0] - ignitionPeak[1] / ignitionPeak[0]) < 0.003 &&`
- L77: `check(zip(pixel(separate), pixel(fused)).allSatisfy { abs($0 - $1) < 0.003 },`
- L83: `check(abs(pixel(lit(source))[0] - Float(value)) < 0.002,`
- L86: `check(PhotoOptics.lampKernel != nil, "linear transmission kernel compiles")`
- L87: `func powered(_ source: CGImage, _ brightness: Double) -> [Float] {`
- L93: `check(abs(high - low * 1.6) < 0.002, "linear power scales 1.6x at tone \(tone)")`
- L103: `check(true, "brightness slider remains monotonic within ordinary white")`
- L110: `check(true, "high lamp preserves highlight steps without EDR")`
- L113: `check(abs(lampNormal[1] / lampNormal[0] - lampStrong[1] / lampStrong[0]) < 0.003 &&`
- L120: `check(abs(pixel(CIImage(cgImage: strongLens), x: 16, y: 16)[0] - powered(middle, 1.6)[0]) < 0.005,`
- L124: `check(zip(sdr, raw).allSatisfy { abs($0 - $1) < 0.002 }, "SDR preserves the complete source color")`
- L126: `check(zip(sdr, original).allSatisfy { abs($0 - $1) < 0.002 }, "original and transmitted default tones agree")`
- L127: `check(pixel(lit(warm, t: 0))[0] < 0.02, "unlit film remains dark")`
- L134: `check(lens.bitsPerComponent == 8 && !lens.bitmapInfo.contains(.floatComponents), "lens exports ordinary 8-bit P3")`
- L135: `check(abs(pixel(CIImage(cgImage:lens),x:16,y:16)[0]-pixel(lit(source))[0])<0.005,`
- L143: `check(abs(pixel(CIImage(cgImage: ignitionLens), x: 16, y: 16)[0] - pixel(tableIgnition)[0]) < 0.005,`
- L155: `check(abs(value - pixel(tablePanel)[0]) < 0.002 && value <= previousPanel + 0.002,`
- L160: `check(LightResponse.photoExposureStops(flare: 0, brightness: 1) == 0 &&`
- L165: `check(view.colorPixelFormat == .bgra8Unorm && view.isPaused && view.enableSetNeedsDisplay,`
- L167: `check((view.layer as? CAMetalLayer)?.presentsWithTransaction == true,`
- L171: `check((view.layer as? CAMetalLayer)?.wantsExtendedDynamicRangeContent == false,`
- L177: `func renderSurface(_ surface: OpticalMetalView) async throws -> [UInt8] {`
- L199: `func liveInput(_ x: CGFloat, diameter: CGFloat, width: CGFloat = 64, brightness: Double = 1) -> LoupeDisplayInput {`
- L205: `func setLive(_ input: LoupeDisplayInput) {`
- L214: `check(x < 32 ? value[0] > value[2] * 5 : value[2] > value[0] * 5,`
- L216: `check(!view.autoResizeDrawable && view.drawableSize == live.pixelSize && composed.extent.size == live.pixelSize,`
- L222: `check(view.loupe?.scene.point.x == 48 && pixel(latest, x: diameter, y: diameter)[2] > 0.5,`
- L228: `check(before[0] > 0.7 && after[0] < 0.1 && after[2] > 0.5,`
- L234: `check(Int(liveBytes[centre + 2]) > Int(liveBytes[centre]) * 5,`
- L237: `check(view.loupe == nil && view.autoResizeDrawable, "leaving live lens restores normal photograph drawable sizing")`
- L261: `check(graph.extent.size == CGSize(width: 150 * scale, height: 120 * scale) &&`
- L273: `check(samePixels, "viewport crop preserves both axes and original source pixels at \(scale)x")`
- L278: `check(zip(clear, bent).allSatisfy { abs($0 - $1) < 0.002 },`
- L288: `check(expectedChannel == gpuChannel && gpuChannels[gpuChannel] > 80,`
- L291: `func markerFrame(_ transmission: Double, _ flare: Double, brightness: Double = 1) async throws -> [UInt8] {`
- L295: `func correctOrientation(_ bytes: [UInt8]) -> Bool {`
- L296: `func rgb(_ x: Int, _ y: Int) -> (Int, Int, Int) {`
- L306: `check(correctOrientation(off), "unlit production frame has correct orientation and visible content")`
- L307: `check(markerView.cachedStableTexture == nil, "unlit first draw does not also compute a lit cache")`
- L309: `check(correctOrientation(peak), "ignition peak has correct orientation and visible content")`
- L310: `check(markerView.cachedStableTexture == nil, "animation does not build or read a stable texture")`
- L312: `check(correctOrientation(steady), "first stable production frame has correct orientation")`
- L313: `check(markerView.cachedStableTexture != nil, "GPU completion publishes the successfully copied stable frame")`
- L316: `check(cached == steady && markerView.cachedStableTexture === cachedTexture,`
- L321: `check(correctOrientation(secondOff) && correctOrientation(secondPeak) && secondSteady == steady,`
- L323: `check(peak[centre + 2] > steady[centre + 2], "production SDR frame retains ignition overexposure")`
- L325: `check(brighter != steady && correctOrientation(brighter), "brightness invalidates the cache without flipping the photograph")`
- L331: `check(staleView.encode(into: staleTexture, commandBuffer: staleBuffer, renderer: renderer), "replacement regression encodes initial cache")`
- L337: `check(staleView.cachedStableTexture == nil, "stale GPU completion cannot overwrite a replacement photograph")`
- L340: `func renderSized(_ surface: OpticalMetalView, _ width: Int, _ height: Int) async throws -> [UInt8] {`
- L366: `check(first == expected && reused == expected && resizedView.cachedStableTexture?.width == width`
- L375: `check(retry.nextDelay() == 1.0 / 60.0, "first retry is deferred")`
- L376: `check(retry.nextDelay() == 0.05, "second retry backs off")`
- L377: `check(retry.nextDelay() == 0.15, "third retry backs off")`
- L378: `check(retry.nextDelay() == nil, "retry budget is finite")`
- L379: `check(retry.nextDelay() == nil, "exhausted budget stays exhausted")`
- L381: `check(retry.revision != previousRevision && retry.attempts == 0,`
- L385: `check(retry.attempts == 0, "successful work clears failure count")`
- L394: `check(queued.count == 1 && queued[0].delay == 1.0 / 60.0 && retryView.pendingRetry != nil,`
- L397: `check(queued.count == 1 && retryView.retry.attempts == 1,`
- L401: `check(retryView.retry.revision != firstRevision && retryView.pendingRetry == nil && retryView.retry.attempts == 0,`
- L406: `check(currentToken != nil && retryView.pendingRetry == currentToken,`
- L409: `check(retryView.pendingRetry == nil && queued.isEmpty,`
- L412: `check(queued.count == 1 && queued[0].delay == 0.05, "next failure of the same input backs off")`
- L415: `check(queued.count == 1 && queued[0].delay == 0.15, "third failure of the same input backs off")`
- L418: `check(queued.isEmpty && retryView.pendingRetry == nil, "production retries stop at the budget")`
- L421: `check(queued.isEmpty, "unchanged SwiftUI updates do not rearm an exhausted budget")`
- L424: `check(queued.count == 1, "changed input can retry again after exhaustion")`
- L427: `check(retryView.pendingRetry == nil && retryView.retry.attempts == 0, "GPU success clears failure state")`
- L429: `check(retryView.pendingRetry == nil && queued.isEmpty, "success invalidates an already queued retry")`
- L435: `check(retryView.retry.revision != detachRevision && retryView.pendingRetry == nil && queued.isEmpty,`
- L440: `func drawWithoutBuffer(_ views: [OpticalMetalView]) {`
- L452: `check(queued.count == 1 && queued[0].delay == 1.0 / 60.0,`
- L457: `check(queued.count == 1 && queued[0].delay == delay && sizeRetryView.retry.revision == sizeRevision,`
- L462: `check(queued.isEmpty && sizeRetryView.retry.attempts == 3 && sizeRetryView.pendingRetry == nil,`
- L466: `check(queued.count == 1 && queued[0].delay == 1.0 / 60.0 &&`
- L475: `check(queued.count == 1 && newSizeToken != nil && sizeRetryView.pendingRetry == newSizeToken,`
- L478: `check(queued.count == 1 && sizeRetryView.pendingRetry == newSizeToken &&`
- L497: `check(bufferRequests == requestsBeforeIdle && queued.isEmpty &&`
- L502: `check(bufferRequests == requestsBeforeIdle + 1 && queued.count == 1 && queued[0].delay == 0.05 &&`

## Tests/SleeveChecks.swift
- L5: `struct SleeveChecks {`
- L6: `static func main() {`
- L8: `func check(_ result: Bool, _ label: String) {`
- L15: `check(rest == origin, "pickup starts at the exact sleeve frame")`
- L18: `check(extracted.size == origin.size && extracted.minX == origin.minX + 10, "film slides out at sleeve size before enlarging")`
- L31: `check(monotonic, "extraction scale changes continuously without reversing")`
- L32: `check(bounded, "extraction never overshoots the final size")`
- L33: `check(anchored, "the grabbed point remains under the pointer throughout enlargement")`
- L35: `check(SleeveMotion.frame(origin: origin, pointer: outside, start: start, target: target).size == target,`
- L38: `check(SleeveMotion.expansion(origin: origin, pointer: below, start: start) == 0,`
- L41: `check(resumed == origin && SleeveMotion.smooth(-1) == 0 && SleeveMotion.smooth(2) == 1,`

## Tests/SlideshowChecks.swift
- L9: `private final class ProjectionGate {`
- L16: `func load(_ url: URL) async -> ProjectionPhoto? {`
- L23: `func complete(_ photo: ProjectionPhoto) {`
- L28: `private final class WeakPhotoPreview {`
- L34: `struct SlideshowChecks {`
- L36: `static func main() async throws {`
- L38: `func check(_ condition: Bool, _ message: String) {`
- L43: `func waitUntil(_ condition: () -> Bool) async throws {`
- L50: `func settleCallbacks() async throws {`
- L59: `func photo(_ url: URL) -> ProjectionPhoto {`
- L71: `check(racing.requestedURL == b && racing.photo?.url == b, "fast B replaces the pending request for slow A")`
- L75: `check(racing.requestedURL == b && racing.photo?.url == b && !racing.failed,`
- L87: `check(cancelled.requestedURL == nil && cancelled.photo == nil && cancelled.shutter == 0 && !cancelled.failed,`
- L101: `check(retrying.failed && retrying.photo == nil, "failed loads become an explicit failure state")`
- L104: `check(attempts == 2 && !retrying.failed && retrying.photo?.url == a,`
- L108: `check(retrying.failed && retrying.photo == nil, "another unreadable slide reports failure")`
- L111: `check(!retrying.failed && retrying.photo?.url == c, "a failed slide can be skipped for the next readable photo")`
- L123: `check(awaitingWindow.photo?.url == a && awaitingWindow.shutter == 0 && departures == 0 && firstArrivals == 0 && preparations == 1,`
- L126: `check(awaitingWindow.phase == .arriving && awaitingWindow.shutter == 1 && departures == 0 && firstArrivals == 1,`
- L129: `check(cueOrder == ["first"], "duplicate window readiness never repeats the first-arrival cue")`
- L131: `check(awaitingWindow.shutter == 1 && awaitingWindow.focus == 1 && awaitingWindow.transport == 0,`
- L137: `check(awaitingWindow.shutter == 1 && awaitingWindow.photo?.url == a && awaitingWindow.transport == 1 &&`
- L141: `check(awaitingWindow.shutter == 0 && awaitingWindow.photo?.url == a && departures == 1 && stops == stopsAtRequest,`
- L144: `check(awaitingWindow.photo?.url == a && awaitingWindow.shutter == 0 && cueOrder == ["first", "out"] && stops == stopsAtRequest,`
- L147: `check(ProcessInfo.processInfo.systemUptime - began >= 0.20 && awaitingWindow.photo?.url == b &&`
- L151: `check(awaitingWindow.phase == .settled && awaitingWindow.shutter == 1 &&`
- L156: `check(stops == stopsBeforeCancel + 1, "leaving the slideshow stops its mechanical sound")`
- L159: `check(firstArrivals == 1, "reentering stays silent until the presentation is ready")`
- L161: `check(firstArrivals == 2 && departures == 1,`
- L171: `check(cancelledArrival.photo == nil && cancelledArrival.shutter == 0 && cancelledArrival.transport == -1,`
- L185: `check(interrupted.phase == .black && interrupted.shutter == 0 && fastStops == 3,`
- L188: `check(interrupted.photo?.url == c && interrupted.phase == .settled && fastDepartures == 1 && fastStops == 3,`
- L194: `check(interrupted.photo?.url == a && fastDepartures == 2 && fastStops == 5,`
- L199: `check(interrupted.shutter == 0, "interrupting an arrival immediately hides the moving obsolete photo")`
- L201: `check(interrupted.photo?.url == c && interrupted.phase == .settled && fastDepartures == 3 && fastStops == 7 && fastFirstArrivals == 1,`
- L209: `check(interrupted.photo == nil && interrupted.shutter == 0 && fastDepartures == cuesBeforeExit && fastStops == stopsBeforeExit + 1,`
- L221: `check(interrupted.photo == nil && interrupted.shutter == 0 && fastDepartures == departureCuesBeforeExit &&`
- L232: `check(failedPresentation.failed && failedPresentation.shutter == 0 && failedCues == 0,`
- L236: `check(failedCues == 1 && failedPresentation.photo?.url == b,`
- L247: `check(reduced.photo?.url == a && reduced.transport == 0 && reduced.shutter == 1 && reduced.focus == 1,`
- L249: `check(reducedCues == 0 && reducedFirstArrivals == 1, "reduced motion uses the same latter-half first-arrival cue")`
- L252: `check(reduced.photo?.url == b && reduced.phase == .settled && reducedCues == 1 && reducedStops == 2 && reducedFirstArrivals == 1,`
- L269: `check(slowPlayback.photo?.url == a && slowPlayback.shutter == 1 && slowPlayback.transport == 0 &&`
- L274: `check(slowPlayback.photo?.url == b && slowPlayback.transport == 0 && slowDepartures == 1 && slowStops == 2,`
- L288: `check(awaitingAudio.shutter == 0 && audioReadyFirstCues == 0,`
- L292: `check(awaitingAudio.phase == .settled && audioReadyFirstCues == 1,`
- L308: `check(cancelledAudio.photo == nil && cancelledAudio.shutter == 0 && cancelledAudioCues == 0,`
- L316: `check(entryStart.center == CGPoint(x: sleeve.midX, y: sleeve.midY) && entryStart.scale == 1 && entryStart.opacity == 1,`
- L318: `check(entryArc.center.y < slot.y && entryArc.opacity == 1 && !entryArc.behindFront,`
- L320: `check(entryEnd.center == slot && entryEnd.behindFront && entryEnd.opacity == 0,`
- L328: `check(resting.x == 0 && resting.y == 0 && resting.angle == 0,`
- L330: `check(incoming.x + slideSize.width / 2 < -stage.width / 2 && incoming.angle < 0,`
- L332: `check(arc.x > incoming.x && arc.x < 0 && arc.y < incoming.y,`
- L334: `check(outgoing.x + slideSize.width / 2 < -stage.width / 2 && outgoing.y > 0 && outgoing.angle > 0,`
- L341: `check(rendered.url == a && rendered.sharp.width == source.width && rendered.sharp.height == source.height,`
- L347: `check(colorDifference(color(sharp, x, y), color(baseline, x, y)) < 0.01,`
- L351: `check(colorDifference(color(sharp, x, y), color(baseline, x, y)) < 0.01,`
- L355: `check(leftAlpha[0] < 0.01 && leftAlpha[1] > leftAlpha[0] && leftAlpha[2] > leftAlpha[1],`
- L357: `check(leftAlpha[8] > 0.99 && color(sharp, 50, 50).alphaComponent > 0.99,`
- L359: `check(color(sharp, 500, 0).alphaComponent < 0.01 && color(sharp, 500, 8).alphaComponent > 0.99 &&`
- L362: `check(color(sharp, 0, 0).alphaComponent < 0.01 && color(sharp, 999, 599).alphaComponent < 0.01,`
- L364: `check(abs(color(soft, 1, 300).alphaComponent - color(sharp, 1, 300).alphaComponent) < 0.01 &&`
- L367: `check(colorDifference(color(soft, 499, 150), color(baseline, 499, 150)) > 0.03,`
- L369: `check(colorDifference(color(sharp, 50, 50), color(baseline, 50, 50)) > 0.015 &&`
- L374: `check(glowImage.width <= 610 && glowImage.height <= 610 && rendered.glowOutset >= 0.10,`
- L376: `check(color(glow, 0, glowImage.height / 2).alphaComponent < 0.01 &&`
- L379: `check(color(glow, 40, glowImage.height / 2).alphaComponent > 0.01,`
- L388: `check(shutterDigest == "5534a1a4f544100b73a72ef31e8853543dba20c66f1db9186cfb147ae162aa25",`
- L391: `check(shutterAudio.fileFormat.sampleRate == 48_000 && shutterAudio.fileFormat.channelCount == 2 &&`
- L405: `check(shutterBuffer.frameLength == 9_126 && channelSamples.allSatisfy { $0.isFinite && abs($0) <= 1 } &&`
- L408: `check(SlideshowSoundResource.playbackVolume == 0.20,`
- L415: `check(room.length == dryFrames + 8_640 && room.fileFormat.sampleRate == 48_000 && room.fileFormat.channelCount == 2,`
- L425: `check(allSamples.allSatisfy { $0.isFinite && abs($0) < 0.92 } && allSamples.contains { abs($0) > 0.05 },`
- L428: `check(tail.contains { abs($0) > 0.0001 } && tail.last == 0 && samples[0][0] == 0,`
- L436: `check(url.standardizedFileURL.path.hasPrefix(bundle.bundleURL.standardizedFileURL.path + "/") && packaged == expected,`
- L446: `check(appAudioURL.standardizedFileURL.path.hasPrefix(appBundle.bundleURL.standardizedFileURL.path + "/"),`
- L449: `check(appAudio == shutterData, "the standalone app contains the complete selected recording")`
- L457: `check(fitted.width <= container.width * 0.84 + 0.001 && fitted.height <= container.height * 0.84 + 0.001 &&`
- L463: `check(departure.x + halfWidth < -container.width / 2,`
- L471: `check(!empty.slideshowMode && empty.currentSlideURL == nil && empty.nextSlideURL == nil,`
- L485: `check(model.slideshowMode && model.slideshowOrder.count == 3 && Set(model.slideshowOrder) == Set([a, b, c]),`
- L488: `check(model.nextSlideURL == model.slideshowOrder[1], "next-slide preload target follows the randomized queue")`
- L490: `check(model.currentSlideURL == model.slideshowOrder.last, "previous navigation wraps to the last photo")`
- L492: `check(model.currentSlideURL == first, "forward navigation returns from the last photo to the first")`
- L494: `check(model.currentSlideURL == first, "forward navigation loops after one complete queue")`
- L495: `check(model.layout == layout && model.selectedID == selected && model.isLit == initiallyLit &&`
- L499: `check(!model.slideshowMode && model.currentSlideURL == nil && model.nextSlideURL == nil && model.slideshowOrder.isEmpty,`
- L501: `check(model.layout == layout && model.selectedID == selected && model.isLit == initiallyLit && model.loupeEnabled,`
- L530: `check(references.allSatisfy { $0.value != nil }, "the first twelve disk-loaded previews remain in the memory cache")`
- L535: `check(references[0].value == nil, "the thirteenth disk hit releases the oldest memory-cached preview")`
- L536: `check(references.dropFirst().allSatisfy { $0.value != nil }, "disk-hit eviction retains exactly the twelve recent preview objects")`
- L538: `check(latest != nil && latest === thirteenth.value, "a recent preview reuses the identical in-memory object")`
- L540: `check(reread?.source.width == 12 && reread?.source.height == 8,`
- L542: `check(references[1].value == nil, "reloading the oldest preview evicts the next least-recent disk-loaded object")`
- L543: `check(try Data(contentsOf: cacheURLs[0]) == Data("metadata-only-0".utf8),`
- L551: `private static func weakPreview(in store: PhotoStore, url: URL) async -> WeakPhotoPreview? {`
- L556: `private static func makeQuadrants(width: Int, height: Int) -> CGImage {`
- L573: `private static func color(_ bitmap: NSBitmapImageRep, _ x: Int, _ y: Int) -> NSColor {`
- L577: `private static func colorDifference(_ a: NSColor, _ b: NSColor) -> CGFloat {`

## Tests/SmokeChecks.swift
- L8: `struct SmokeChecks {`
- L9: `struct CheckFailure: Error, CustomStringConvertible {`
- L15: `static func require(_ condition: @autoclosure () -> Bool, _ message: String) throws {`
- L19: `static func check(_ name: String, _ body: () async throws -> Void) async {`
- L30: `static func main() async {`
- L39: `await check("Drop coordinates remain exact; adding a photo never arranges earlier slides") {`
- L49: `await check("Repeated drop keeps one identity, moves it and raises it") {`
- L61: `await check("Loupe always samples the visible photo without raising covered slides") {`
- L69: `func hit(_ x: Double, _ y: Double) -> SlideImageHit? {`
- L81: `await check("Loupe hit testing respects rotated photo and mount geometry") {`
- L94: `await check("Transform normalization preserves unbounded positions and rejects nonfinite numbers") {`
- L107: `await check("JSON roundtrip preserves geometry and stacking order") {`
- L117: `await check("Restoration sanitizes repeated URLs/IDs, remote URLs and out-of-range geometry") {`
- L132: `await check("Remove and clear leave original file bytes intact") {`
- L146: `await check("An invalid duplicate ID does not suppress a later valid restored photo") {`
- L159: `await check("Table fills workspace; loupe inverts rotated coordinates") {`
- L168: `await check("World placement preserves photos beyond every workspace edge") {`
- L189: `await check("Camera inverse, zoom anchor, and four edge velocities remain consistent") {`
- L208: `await check("Fit resets all sizes, preserves identities and rotations, and is repeatable") {`
- L225: `await check("Fit centers one default-sized slide and handles an empty table") {`
- L234: `await check("Fitted mixed photos never overlap or escape across counts and window shapes") {`
- L264: `await check("Fit uses the current window and ignores invalid geometry safely") {`
- L276: `await check("Folder scan recurses, filters hidden/packages/nonimages and deduplicates imports") {`
- L292: `await check("Preview and thumbnail stay bounded; light reveals detail without changing originals") {`
- L322: `static func writePNG(to url: URL, width: Int, height: Int) throws {`
- L342: `static func meanLuminance(_ image: CGImage) throws -> Double {`

## Tests/TrayChecks.swift
- L5: `private final class DeferredFile: @unchecked Sendable {`
- L16: `func finish(_ url: URL) {`
- L23: `struct Legacy: Codable { var urls: [URL]; var layout: TableLayout; var label: String }`
- L24: `@MainActor static func main() async throws {`
- L30: `func check(_ value: Bool, _ message: String) { precondition(value, message); count += 1; print("PASS \(message)") }`
- L32: `func photo(_ path: String) throws -> URL {`
- L43: `check(model.photos.map(\.url) == [a] && model.sourceLabel == "first", "first folder labels its tray")`
- L48: `check(model.photos.map(\.url) == [a, b], "another folder appends recursively without replacing existing photos")`
- L50: `check(model.photos.map(\.url) == [a, b] && model.sourceLabel == "first", "repeat import deduplicates and preserves the tray label")`
- L52: `check(model.photos.map(\.url) == [a, b] && model.message != nil && !model.isImporting, "empty or missing imports preserve the tray and finish loading")`
- L54: `check(model.trays.count == 2 && second != first && model.activeTrayID == second && model.photos.isEmpty, "plus creates and selects an empty independent tray")`
- L55: `check(model.layout == layout && model.selectedIDs == selection && model.isLit && model.lightStartedAt == clock`
- L58: `check(model.photos.map(\.url) == [c] && model.trays[0].urls == [a, b], "import goes only to the selected tray")`
- L60: `check(model.photos.map(\.url) == [a, b], "switching back restores the original tray")`
- L62: `check(model.activeTrayID == first, "unknown tray identity cannot change the selection")`
- L64: `check(model.photos.map(\.url) == [a, b] && model.trays[1].urls == [c, a], "an import keeps its original destination after selection changes")`
- L65: `check(Set(model.allPhotos.map(\.url)) == Set([a, b, c]) && model.allPhotos.count == 3, "shared album library deduplicates photos across trays")`
- L68: `check(Set(model.slideshowOrder) == Set([a, c]) && model.slideshowOrder.count == 2, "slideshow uses only the selected tray")`
- L72: `check(restored.trays == model.trays && restored.activeTrayID == second && restored.photos.map(\.url) == [c, a], "all trays, order, names and active selection survive restart")`
- L73: `check(restored.layout == layout && restored.pan == model.pan && restored.zoom == model.zoom, "tray persistence preserves table geometry and camera")`
- L75: `check(Set(saved.urls) == Set([a, b, c]), "legacy-compatible URL list retains every tray photo")`
- L78: `check(emptyRestored.activeTrayID == empty && emptyRestored.photos.isEmpty && emptyRestored.trays.count == 3, "empty selected trays stay empty after restart")`
- L80: `check(!emptyRestored.slideshowMode, "an empty selected tray cannot play photos from other trays")`
- L84: `check(migrated.trays.count == 1 && migrated.sourceLabel == "旧图库" && migrated.photos.map(\.url) == [a], "legacy libraries migrate to one deduplicated tray")`
- L85: `check(migrated.layout == layout, "legacy migration preserves the existing arrangement")`
- L87: `check(TableModel(storageURL: oldStorage).trays == migrated.trays, "legacy migration is stable across subsequent launches")`
- L99: `check(accepted && model.trays[0].urls == [a, b, c] && model.trays[1].urls == [c, a], "switching during asynchronous drop decoding cannot redirect the import")`
- L103: `check(internalDrop && model.layout.slides.contains { $0.url == b } && model.photos.isEmpty, "a shared-library internal drag places without adding to another tray")`
- L105: `check(model.photos.isEmpty, "placing an already known photo does not pollute the current tray")`
- L107: `check(Set(model.layout.slides.map(\.url)) == Set([a, c]) && model.trays[0].urls == [a, b, c], "one-click layout uses the active tray while keeping every tray intact")`
- L113: `check(deletion.trays.count == 1 && deletion.photos.map(\.url) == [a, b] && !deletion.canDeleteSelection,`
- L119: `check(deletion.selectedTrayPhoto == nil && deletion.selectedIDs.isEmpty && !deletion.canDeleteSelection`
- L123: `check(deletion.selectedTrayPhoto?.trayID == deletionFirst && deletion.selectedTrayPhoto?.url == b`
- L127: `check(deletion.selectedTrayPhoto == nil && deletion.selectedID == placedID,`
- L131: `check(deletion.selectedTrayPhoto == nil, "marquee selection cancels the tray photo deletion target")`
- L134: `check(deletion.selectedTrayPhoto == nil && !deletion.canDeleteSelection,`
- L138: `check(deletion.selectedTrayPhoto?.url == b, "an unknown tray cannot change the photo deletion target")`
- L140: `check(deletion.selectedTrayPhoto == nil && !deletion.canDeleteSelection,`
- L144: `check(deletion.selectedTrayPhoto == nil && deletion.activeTrayID == deletionSecond,`
- L149: `check(TableModel(storageURL: root.appendingPathComponent("deletion.json")).selectedTrayPhoto == nil,`
- L153: `check(deletion.selectedTrayPhoto == nil && !deletion.canDeleteSelection,`
- L169: `check(deletion.trays.count == 2 && deletion.layout.slides.count == 2 && !deletion.canDeleteSelection,`
- L172: `check(deletion.trays.map(\.id) == [deletionSecond] && deletion.activeTrayID == deletionSecond,`
- L174: `check(deletion.layout.slides.map(\.id) == [survivingSlide]`
- L177: `check(deletion.photos.map(\.url) == [a, c],`
- L179: `check(deletion.albums == albumsBeforeDeletion && deletion.onTableAlbums.map(\.id) == [album],`
- L181: `check(deletion.selectedIDs.isEmpty && deletion.selectedTrayPhoto == nil && !deletion.canDeleteSelection,`
- L183: `check(deletion.isLit && deletion.lightStartedAt == deletionLightClock`
- L187: `check(deletion.trays.map(\.id) == [deletionSecond] && deletion.layout.slides.map(\.id) == [survivingSlide],`
- L191: `check(afterDeletion.trays == deletion.trays && afterDeletion.layout == deletion.layout`
- L196: `check(deletion.layout.slides.isEmpty && deletion.photos.map(\.url) == [a, c]`
- L201: `check(Set(deletion.trays.map(\.name)).count == deletion.trays.count,`
- L205: `check(deletion.activeTrayID == deletionFourth && deletion.trays.map(\.id) == [deletionSecond, deletionFourth],`
- L209: `check(deletion.activeTrayID == deletionSecond && deletion.trays.count == 1,`
- L214: `check(deletion.activeTrayID == deletionSecond && deletion.trays.count == 1,`
- L218: `check(deletion.trays.count == 1 && deletion.selectedTrayPhoto?.url == a,`
- L222: `check(deletion.trays.count == 1 && replacementTray != deletionSecond && deletion.photos.isEmpty,`
- L224: `check(deletion.albums == albumsBeforeDeletion, "deleting the last tray still preserves albums")`
- L227: `check(emptyAfterDeletion.activeTrayID == replacementTray && emptyAfterDeletion.photos.isEmpty,`
- L231: `check(deletion.photos.map(\.url) == [a, b] && deletion.layout.slides.map(\.url) == [b],`
- L249: `check(!acceptedAfterDeletion && pendingDeletion.trays.count == 1`
- L253: `check(pendingDeletion.photos.isEmpty && pendingDeletion.activeTrayID != deletedDestination && !pendingDeletion.isImporting,`
- L276: `check(keyboard.selectedTrayPhoto?.url == a && keyboard.selectedIDs.isEmpty,`
- L281: `check(keyboard.selectedTrayPhoto?.trayID == keyboardFirst && keyboard.selectedTrayPhoto?.url == a,`
- L284: `check(keyboard.trays.first { $0.id == keyboardFirst }?.urls == [b]`
- L288: `check(keyboard.trays.first { $0.id == keyboardSecond }?.urls == [a, c]`
- L291: `check(keyboard.selectedTrayPhoto == nil && keyboard.selectedIDs.isEmpty && !keyboard.canDeleteSelection,`
- L294: `check(keyboard.photos.map(\.url) == [b] && keyboard.layout.slides.map(\.id) == [keyboardBSlide]`
- L299: `check(keyboardRestored.trays == keyboard.trays && keyboardRestored.layout == keyboard.layout`
- L304: `check(keyboard.layout.slides.isEmpty && keyboard.photos.map(\.url) == [b],`
- L308: `check(keyboard.photos.isEmpty && keyboard.activeTrayID == keyboardFirst && keyboard.trays.count == 2,`
- L313: `check(keyboard.selectedTrayPhoto?.url == a,`
- L316: `check(keyboard.selectedTrayPhoto == nil && !keyboard.canDeleteSelection && keyboard.activeTrayID == keyboardFirst,`
- L337: `check(singlePhoto.trays.first { $0.id == singlePhotoFirstTray }?.urls == [b]`
- L340: `check(!singlePhoto.tableOrder.contains(aSlideID) && singlePhoto.selectedIDs == [bSlideID],`
- L342: `check(singlePhoto.trays.first { $0.id == singlePhotoSecondTray }?.urls == [a, c]`
- L345: `check(singlePhoto.allPhotos.contains { $0.url == a },`
- L348: `check(singlePhoto.layout.slides.isEmpty`
- L354: `check(singlePhotoRestored.activeTrayID == singlePhotoFirstTray`
- L365: `check(singlePhoto.layout == singlePhotoLayout && singlePhoto.trays == singlePhotoTrays`
- L372: `check(singlePhoto.activeTrayID == singlePhotoSecondTray`
- L377: `check(singlePhoto.selectedIDs.isEmpty && singlePhoto.selectedTrayPhoto == nil`
- L381: `check(singlePhoto.trays.count == 2,`
- L386: `check(singlePhoto.photos.map(\.url) == [a] && singlePhoto.layout == remainingLayout`
- L390: `check(try Data(contentsOf: url) == image, "single-photo deletion leaves original unchanged: \(url.lastPathComponent)")`
- L392: `for url in [a, b, c] { check(try Data(contentsOf: url) == image, "original file remains unchanged: \(url.lastPathComponent)") }`

## Tests/TrayReturnChecks.swift
- L5: `@MainActor static func main() async throws {`
- L12: `func check(_ condition: Bool, _ label: String) {`
- L15: `func photo(_ name: String) throws -> URL {`
- L26: `check(model.trayPhotos.map(\.url) == originalOrder, "imported photos initially remain in the tray")`
- L29: `check(model.trayPhotos.map(\.url) == originalOrder.filter { $0 != b }, "placed photo disappears and remaining order stays stable")`
- L30: `check(model.photos.map(\.url) == originalOrder, "placing preserves source membership and order")`
- L32: `check(model.layout.slides.count == 1 && model.selectedID == bID, "repeated placement creates no duplicate")`
- L34: `check(model.trayPhotos.map(\.url) == originalOrder.filter { $0 != b }, "moving and rotating do not return a placed photo")`
- L38: `check(model.trayPhotos.map(\.url) == originalOrder && model.layout.slides.isEmpty, "Delete returns the film to its original ordered position")`
- L39: `check(model.thumbnails[b] === thumb, "returned film reuses its cached thumbnail")`
- L41: `check(model.trayPhotos.map(\.url) == originalOrder, "repeated Delete with no selection cannot duplicate photos")`
- L51: `check(model.activeTrayID == secondTray && model.trayPhotos.map(\.url) == [d], "mixed return keeps the active tray and never moves another tray's photo into it")`
- L53: `check(model.trayPhotos.map(\.url) == originalOrder, "mixed return restores each photo to its original tray")`
- L58: `check(restored.activeTrayID == secondTray && restored.trayPhotos.map(\.url) == [d], "restart restores current tray selection")`
- L60: `check(restored.trayPhotos.map(\.url) == originalOrder.filter { $0 != b }, "restart hides already-placed films in their original tray")`
- L64: `check(restored.selectedIDs.isEmpty && !restored.canDeleteSelection,`
- L68: `check(restored.trayPhotos.map(\.url) == [d], "Delete after restart and tray switch does not redirect the returned film")`
- L70: `check(restored.trayPhotos.map(\.url) == originalOrder, "returned film's original order survives restart")`
- L73: `check(afterReturn.trayPhotos.map(\.url) == originalOrder && afterReturn.layout.slides.isEmpty,`
- L79: `check(model.trayPhotos.isEmpty && Set(model.layout.slides.map(\.url)) == Set(originalOrder), "place all empties visible tray without losing library photos")`
- L81: `check(model.slideshowMode && Set(model.slideshowOrder) == Set(originalOrder), "empty visible tray still permits the current tray's complete slideshow")`
- L84: `check(model.trayPhotos.map(\.url) == originalOrder, "clear table returns every photo to its original tray")`
- L89: `check(model.layout == cancelledLayout && model.trayPhotos == cancelledPhotos, "invalid drop keeps photo in tray without changing table")`
- L91: `check(!model.trayPhotos.contains { $0.url == a }, "native internal drop also removes the visible tray entry")`
- L93: `check(model.trayPhotos.map(\.url) == originalOrder, "native drop and return maintain original order")`
- L95: `check(model.trayPhotos.map(\.url) == originalOrder, "reimporting returned photos does not duplicate them")`
- L98: `check(!model.trayPhotos.contains { $0.url == b }, "reimporting a placed photo keeps it off the tray")`
- L103: `check(model.albums.first { $0.id == album }?.pockets.contains { $0.url == c } == true,`
- L114: `check(cached.preparedPreviews.count == 14 && cached.preparedTablePreviews.count == 14, "on-table photos retain prepared previews")`
- L117: `check(cached.preparedPreviews.count <= 12 && cached.preparedTablePreviews.count <= 12,`
- L119: `check(cached.trayPhotos.count == 14 && cached.photos.count == 14, "cache trimming retains every returned photo")`
- L121: `check(try Data(contentsOf: url) == data, "original unchanged: \(url.lastPathComponent)")`

## Tests/UpdateChecks.swift
- L5: `@MainActor static func main() throws {`
- L7: `func check(_ value: Bool, _ description: String) {`
- L14: `check(UpdateConfiguration.read(info, isApplication: true) == .ready, "public release starts update checks")`
- L15: `check(UpdateConfiguration.read(info, isApplication: false) == .development, "command-line development run cannot update itself")`
- L16: `check(UpdateConfiguration.read([:], isApplication: true) == .development, "unconfigured development app cannot update itself")`
- L19: `check(UpdateConfiguration.read(local, isApplication: true) == .development, "local build cannot be replaced by a public release")`
- L23: `check(UpdateConfiguration.read(damaged, isApplication: true) == .invalid, "missing \(key) fails closed")`
- L27: `check(UpdateConfiguration.read(damaged, isApplication: true) == .invalid, "unexpected update host is rejected")`
- L31: `check(UpdateConfiguration.read(damaged, isApplication: true) == .invalid, "malformed signing key is rejected")`
- L34: `check(UpdateConfiguration.read(silentInstall, isApplication: true) == .invalid, "installation must follow an explicit user action")`
- L36: `check(comparator.compareVersion("0.1.68", toVersion: "0.1.69") == .orderedAscending, "new release is an upgrade")`
- L37: `check(comparator.compareVersion("0.1.69", toVersion: "0.1.69") == .orderedSame, "same version is not an update")`
- L38: `check(comparator.compareVersion("0.1.69", toVersion: "0.1.65") == .orderedDescending, "older GitHub release is not an update")`
- L39: `check(comparator.compareVersion("0.1.9", toVersion: "0.1.10") == .orderedAscending, "numeric version comparison handles digit boundaries")`
- L40: `check(info["SUEnableAutomaticChecks"] as? Bool == true, "automatic checks are enabled by default")`
- L41: `check(info["SUAutomaticallyUpdate"] as? Bool == false, "downloads are not started silently")`
- L46: `check(!cancelled && driver.isCheckingAtLaunch, "quiet launch check does not cancel the Sparkle session")`
- L50: `check(acknowledgements == 1, "no-update launch result is acknowledged without a dialog")`
- L52: `check(acknowledgements == 2, "launch network failure is acknowledged without a dialog")`
- L54: `check(!driver.isCheckingAtLaunch, "finishing launch check restores normal manual-check UI")`
- L58: `check(!developmentUpdater.isAvailable, "development process stays isolated on repeated start")`

## VERSION

## build_release.sh

## docs/RELEASING.md

## docs/audio/快门咔嚓.md

## docs/releases/v0.1.69.md

## docs/releases/v0.1.70.md

## docs/releases/v0.1.71.md

## docs/releases/v0.1.72.md

## docs/releases/v0.1.73.md

## docs/releases/v0.1.74.md

## docs/releases/v0.1.75.md

## docs/本次更新操作指引-v0.1.73.md

## docs/本次更新操作指引-v0.1.74.md

## scripts/build-app.sh

## scripts/check-albums.sh

## scripts/check-closeup.sh

## scripts/check-drops.sh

## scripts/check-feature-guide.sh

## scripts/check-light.sh

## scripts/check-loupe.sh

## scripts/check-optics.sh

## scripts/check-sleeves.sh

## scripts/check-slideshow.sh

## scripts/check-tray-return.sh

## scripts/check-trays.sh

## scripts/check-update-bundle.py

## scripts/check-updates.sh

## scripts/check.sh

## scripts/generate-app-icon.sh

## scripts/prepare-release.sh

## scripts/prepare-shutter-audio.py
