using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using LightTable.Core;
using LightTable.Windows.Imaging;
using LightTable.Windows.Rendering;
using LightTable.Windows.Audio;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using global::Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    sealed record IntegrationResult(string Name,bool Passed,string Evidence,double Milliseconds);
    async Task RunIntegration(string fixtureRoot)
    {
        var results=new List<IntegrationResult>();
        async Task Check(string name,Func<Task<string>> action)
        {
            var watch=Stopwatch.StartNew();
            try{var evidence=await action();results.Add(new(name,true,evidence,watch.Elapsed.TotalMilliseconds));}
            catch(Exception ex){results.Add(new(name,false,ex.ToString(),watch.Elapsed.TotalMilliseconds));}
            session.Storage.Log($"集成检查 {name}: {results[^1].Passed}");
        }
        void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        try
        {
            if(session.Document.Photos.Count!=0||session.Document.Slides.Count!=0||session.Document.Albums.Count!=0)
                throw new InvalidOperationException("集成测试仅允许空白隔离数据目录，拒绝覆盖已有布局。");
            await renderer.DeviceReady;
            var originals=Directory.EnumerateFiles(fixtureRoot,"*.jpg",SearchOption.TopDirectoryOnly).Order().ToArray();
            Require(originals.Length>=3,"需要至少3张真实 JPEG 测试照片");
            var originalHashes=originals.ToDictionary(p=>p,p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
            await Check("WIC-JPEG-and-content-dedup",async()=>
            {
                await session.ImportAsync(originals);Require(session.Document.Photos.Count==originals.Length,"真实 JPEG 导入数量错误");
                var duplicate=await new PhotoImporter(session.Images).ImportAsync(originals,session.Document.Photos);
                Require(duplicate.Photos.Count==0&&duplicate.Duplicates==originals.Length,"重复路径未去重");
                var copy=Path.Combine(session.Storage.Root,"duplicate.jpg");File.Copy(originals[0],copy,true);
                duplicate=await new PhotoImporter(session.Images).ImportAsync([copy],session.Document.Photos);
                Require(duplicate.Photos.Count==0&&duplicate.Duplicates==1,"不同路径的重复内容未去重");
                Require(duplicate.AcceptedPaths.SequenceEqual(new[]{originals[0]}),"重复内容拖放没有解析为原图库引用");
                return $"{originals.Length} 张 JPEG，重复路径与相同内容副本均跳过";
            });
            await Check("Import-metadata-only-and-cross-column-reuse",()=>CheckMetadataImport(originals));
            await Check("WIC-common-codecs-roundtrip",async()=>
            {
                var expected=new byte[16*12*4];for(int y=0;y<12;y++)for(int x=0;x<16;x++){var p=(y*16+x)*4;expected[p]=(byte)(x*15);expected[p+1]=(byte)(y*20);expected[p+2]=137;expected[p+3]=255;}
                foreach(var (name,id) in new[]{("png",BitmapEncoder.PngEncoderId),("bmp",BitmapEncoder.BmpEncoderId),("tiff",BitmapEncoder.TiffEncoderId)})
                {
                    var path=Path.Combine(session.Storage.Root,$"codec.{name}");
                    using(var file=File.Create(path))using(var random=file.AsRandomAccessStream())
                    {var encoder=await BitmapEncoder.CreateAsync(id,random);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,16,12,96,96,expected);await encoder.FlushAsync();}
                    var image=await session.Images.GetAsync(path,256);Require(image.Width==16&&image.Height==12,$"{name} 尺寸不匹配");
                    Require(image.Pixels.SequenceEqual(expected),$"{name} 无损像素往返不一致");
                }
                return "PNG/BMP/TIFF 首帧16×12、BGRA8不透明像素逐字节相同";
            });
            await Check("WIC-EXIF-eight-orientations-and-PNG-alpha",()=>ImagingChecks.OrientationAndTransparency(session.Images,session.Storage.Root));
            await Check("Recursive-natural-hidden-long-path-and-locked",()=>ImagingChecks.RecursiveAndLongPaths(session.Images,session.Storage.Root,originals));
            await Check("Missing-corrupt-and-request-coalescing",async()=>
            {
                var missing=false;try{await session.Images.GetAsync(Path.Combine(session.Storage.Root,"missing.png"));}catch(FileNotFoundException){missing=true;}Require(missing,"缺失文件未失败");
                var corrupt=Path.Combine(session.Storage.Root,"corrupt.jpg");await File.WriteAllTextAsync(corrupt,"not an image");bool rejected=false;
                try{await session.Images.GetAsync(corrupt);}catch{rejected=true;}Require(rejected,"损坏图片未拒绝");
                var before=session.Images.DecodeCount;var loaded=await Task.WhenAll(Enumerable.Range(0,20).Select(_=>session.Images.GetAsync(originals[0],1536)));
                Require(loaded.All(p=>ReferenceEquals(p,loaded[0])),"相同尺寸请求没有合并");Require(session.Images.DecodeCount-before<=1,"同一请求发生多次解码");
                return $"缺失/损坏拒绝；20并发请求产生 {session.Images.DecodeCount-before} 次解码，共享同一结果";
            });
            await Check("Disk-cache-corruption-regeneration",async()=>
            {
                var image=await session.Images.GetAsync(originals[0],768);
                var cache=Path.Combine(session.Storage.Root,"cache","previews",image.Key+".br");Require(File.Exists(cache),"没有磁盘缓存");
                await File.WriteAllBytesAsync(cache,[0,1,2]);var fresh=new ImageStore(session.Storage);var rebuilt=await fresh.GetAsync(originals[0],768);
                Require(rebuilt.Width==image.Width&&rebuilt.Pixels.SequenceEqual(image.Pixels),"损坏缓存无法重新生成");Require(fresh.DecodeCount==1,"没有从原图重新解码");
                var hot=new ImageStore(session.Storage);await hot.GetAsync(originals[0],768);Require(hot.DiskHits==1&&hot.DecodeCount==0,"热磁盘缓存未命中");
                return "仅修改隔离cache文件；损坏后重解码，与原预览相同；新实例命中磁盘";
            });
            await Check("GPU-five-optical-layers-disk-cache",()=>CheckOpticalDiskCache(originals[0]));
            await Check("GPU-lighting-and-snapshots",async()=>
            {
                session.Fit(true);foreach(var path in session.Document.PlacedPaths)await renderer.PrepareAsync(path,2200);
                using var dark=renderer.RenderSnapshot(false,0);using var lit=renderer.RenderSnapshot(true,0);
                await dark.SaveAsync(Path.Combine(session.Storage.Root,"logs","table-dark.png"),CanvasBitmapFileFormat.Png);
                await lit.SaveAsync(Path.Combine(session.Storage.Root,"logs","table-lit.png"),CanvasBitmapFileFormat.Png);
                var s=session.Document.Slides[1];var p=Geometry.Place(session.Document,s);
                var darkPixels=dark.GetPixelColors((int)(p.Center.X-p.Width/2),(int)(p.Center.Y-p.Height/2),Math.Max(1,(int)p.Width),Math.Max(1,(int)p.Height));
                var litPixels=lit.GetPixelColors((int)(p.Center.X-p.Width/2),(int)(p.Center.Y-p.Height/2),Math.Max(1,(int)p.Width),Math.Max(1,(int)p.Height));
                var darkMean=darkPixels.Average(c=>(c.R+c.G+c.B)/3d);var litMean=litPixels.Average(c=>(c.R+c.G+c.B)/3d);
                Require(litMean>darkMean*2.5&&litMean>5,"开灯没有产生有效透光图像");
                Require(renderer.GpuBytes<=384L*1024*1024,"GPU预算超限");
                return $"真实Direct2D静态层，片内平均暗态={darkMean:F3}，亮态={litMean:F3}；GPU估算 {renderer.GpuBytes} bytes。此读回不证明整套UI。";
            });
            await Check("GPU-command-cache-camera-equivalence",()=>
            {
                using var initial=renderer.RenderSnapshot(true,0,true);
                var v=session.Document.Viewport;v.Zoom(1.7,new(300,250));v.OffsetX+=18;v.OffsetY-=11;
                using var cached=renderer.RenderSnapshot(true,0,true);using var direct=renderer.RenderSnapshot(true,0,false);
                var a=cached.GetPixelBytes();var b=direct.GetPixelBytes();double sum=0;for(int i=0;i<a.Length;i++)sum+=Math.Abs(a[i]-b[i]);
                var mean=sum/a.Length;Require(mean<1.5,$"命令缓存变换与直接渲染差异过大，平均差 {mean}");
                session.Document.Viewport=new();return Task.FromResult($"相机锚点缩放+平移后，命令列表复用和直接GPU渲染每字节平均差={mean:F5}/255");
            });
            await Check("GPU-loupe-shader-and-close-views",async()=>
            {
                var slide=session.Document.Slides[1];var at=Geometry.Place(session.Document,slide).Center;renderer.Pointer=at;
                using var ordinary=renderer.RenderLensSnapshot(0,0);
                using var native=renderer.RenderLensSnapshot(1,0);
                using var eight=renderer.RenderLensSnapshot(1,3);
                using var midway=renderer.RenderLensSnapshot(.5,.35);
                var origin=at+renderer.BoardOrigin;
                var pixels=ordinary.GetPixelColors((int)origin.X-20,(int)origin.Y-20,40,40);
                Require(pixels.All(c=>c.A==255)&&pixels.Any(c=>c.R+c.G+c.B>30),"普通镜片中心没有不透明的图像");
                Require(ordinary.GetPixelColors(0,0,1,1)[0].A==0,"普通镜片污染圆形以外画面");
                var center=LoupeGeometry.CloseCenter(origin,new(ordinary.Size.Width,ordinary.Size.Height));
                var n=native.GetPixelColors((int)center.X-5,(int)center.Y-5,10,10);var e=eight.GetPixelColors((int)center.X-5,(int)center.Y-5,10,10);
                Require(n.All(c=>c.A==255)&&e.All(c=>c.A==255),"凑近1×或8×中心存在透明像素");
                await ordinary.SaveAsync(Path.Combine(session.Storage.Root,"logs","loupe-ordinary.png"),CanvasBitmapFileFormat.Png);
                await native.SaveAsync(Path.Combine(session.Storage.Root,"logs","loupe-native.png"),CanvasBitmapFileFormat.Png);
                await eight.SaveAsync(Path.Combine(session.Storage.Root,"logs","loupe-eight.png"),CanvasBitmapFileFormat.Png);
                await midway.SaveAsync(Path.Combine(session.Storage.Root,"logs","loupe-midway.png"),CanvasBitmapFileFormat.Png);
                return "原版0.90/0.97径向lip HLSL实际运行，普通镜透明边界、1×与8×中心有效；4张GPU快照已保存。未替代Space按键验收。";
            });
            await Check("GPU-ordinary-and-close-actual-1-2-4-8-sampling",()=>CheckActualLoupeSampling());
            await Check("Independent-workspaces-GPU-and-persistence",()=>CheckIndependentWorkspace());
            await Check("Panel-resize-layout-and-settings",()=>CheckPanelSizingAndSettings());
            await Check("Bounded-live-canvas-draw-recovery",()=>CheckBoundedDrawRecovery());
            await Check("GPU-projector-optics-cache-and-composite",async()=>
            {
                var before=JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions);
                using var projection=new ProjectionStore(session.Images,board.Device);
                using var a=await projection.GetAsync(originals[0]);
                var parallel=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>projection.GetAsync(originals[0])));
                Require(parallel.All(p=>ReferenceEquals(p.Photo,a.Photo)),"投影请求未共享GPU层");foreach(var lease in parallel)lease.Dispose();
                var photo=a.Photo;var w=(int)photo.Sharp.Size.Width;var h=(int)photo.Sharp.Size.Height;
                Require(photo.Sharp.GetPixelColors(0,0,1,1)[0].A==0&&photo.Soft.GetPixelColors(0,0,1,1)[0].A==0,"投影圆角没有透明边缘");
                var decoded=await session.Images.GetAsync(originals[0],2200);using var tables=new OpticalTables(board.Device);using var optical=new OpticalPhoto(board.Device,decoded,tables);
                var expected=optical.Lit.GetPixelColors(w/2-3,h/2-3,6,6);var actual=photo.Sharp.GetPixelColors(w/2-3,h/2-3,6,6);
                Require(expected.Zip(actual).All(p=>Math.Abs(p.First.R-p.Second.R)<=1&&Math.Abs(p.First.G-p.Second.G)<=1&&Math.Abs(p.First.B-p.Second.B)<=1),"投影中心不是透光层");
                using var settled=new CanvasRenderTarget(board.Device,960,540,96);using var focus=new CanvasRenderTarget(board.Device,960,540,96);
                using(var ds=settled.CreateDrawingSession()){ds.Clear(Colors.Black);ProjectionRenderer.Draw(ds,photo,new(ProjectionPhase.Settled,0,1,1),new(960,540));}
                using(var ds=focus.CreateDrawingSession()){ds.Clear(Colors.Black);ProjectionRenderer.Draw(ds,photo,new(ProjectionPhase.Arriving,0,.5,1),new(960,540));}
                var settledMean=settled.GetPixelColors(440,230,80,80).Average(p=>(p.R+p.G+p.B)/3d);var focusMean=focus.GetPixelColors(440,230,80,80).Average(p=>(p.R+p.G+p.B)/3d);
                await settled.SaveAsync(Path.Combine(session.Storage.Root,"logs","projector-settled.png"),CanvasBitmapFileFormat.Png);
                await focus.SaveAsync(Path.Combine(session.Storage.Root,"logs","projector-focus.png"),CanvasBitmapFileFormat.Png);
                var fit=ProjectionMotion.Fit(new(960,540),photo.Aspect);var rect=new global::Windows.Foundation.Rect(480-fit.Width/2,270-fit.Height/2,fit.Width,fit.Height);var spill=Math.Max(fit.Width,fit.Height)*photo.GlowOutset;
                global::Windows.UI.Color[] Layer(CanvasBitmap bitmap,global::Windows.Foundation.Rect target,float opacity)
                {
                    using var isolated=new CanvasRenderTarget(board.Device,960,540,96);
                    using(var ds=isolated.CreateDrawingSession()){ds.Clear(Colors.Black);ds.DrawImage(bitmap,target,new global::Windows.Foundation.Rect(0,0,bitmap.Size.Width,bitmap.Size.Height),opacity);}
                    return isolated.GetPixelColors(440,230,80,80);
                }
                var sharpLayer=Layer(photo.Sharp,rect,1);var softLayer=Layer(photo.Soft,rect,1);var glowLayer=Layer(photo.Glow,new(rect.X-spill,rect.Y-spill,rect.Width+spill*2,rect.Height+spill*2),.20f);
                var focused=focus.GetPixelColors(440,230,80,80);double error=0;
                for(int i=0;i<focused.Length;i++)
                {
                    // Source-over soft at .5 attenuates the underlying glow by .5;
                    // plusLighter sharp then adds .5. This is the upstream stack,
                    // whose glow intentionally differs during the focus ramp.
                    error+=Math.Abs(focused[i].R-Math.Min(255,(softLayer[i].R+sharpLayer[i].R+glowLayer[i].R)*.5));
                    error+=Math.Abs(focused[i].G-Math.Min(255,(softLayer[i].G+sharpLayer[i].G+glowLayer[i].G)*.5));
                    error+=Math.Abs(focused[i].B-Math.Min(255,(softLayer[i].B+sharpLayer[i].B+glowLayer[i].B)*.5));
                }
                error/=focused.Length*3;Require(settledMean>1&&error<1.5,$"焦点叠加不符合sourceOver/plusLighter: 平均误差{error:F6}");
                using(var ds=focus.CreateDrawingSession()){ds.Clear(Colors.Black);ProjectionRenderer.Draw(ds,photo,new(ProjectionPhase.Black,-1,0,0),new(960,540));}
                Require(focus.GetPixelColors().All(p=>p.R==0&&p.G==0&&p.B==0),"遮光器黑场泄露投影");
                await settled.SaveAsync(Path.Combine(session.Storage.Root,"logs","projector-settled.png"),CanvasBitmapFileFormat.Png);
                foreach(var path in originals.Skip(1).Take(2).Append(Path.Combine(session.Storage.Root,"codec.png"))){using var loaded=await projection.GetAsync(path);}
                Require(projection.Count==3,"投影LRU没有限制到3项");Require(a.Photo.Sharp.GetPixelColors(w/2,h/2,1,1)[0].A==255,"LRU淘汰了使用中的图片");
                Require(JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions)==before,"投影改变台面状态");
                return $"真实GPU透光中心与lit相差≤1/255，sharp/soft角透明；焦点叠加与独立通道算式平均差={error:F5}/255（50%/稳态均值比={focusMean/settledMean:F5}），黑场零RGB，3项LRU保留租用图，8并发共享";
            });
            await Check("GPU-collection-perspective-and-layout-immutability",async()=>
            {
                var before=JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions);collectionOverlay.Visibility=Visibility.Visible;
                try
                {
                    var photos=originals.Take(3).Select((path,i)=>new CollectionPhoto(path,new(50+i*110,460,90,56))).ToArray();await collectionRenderer.PrepareAsync(photos);
                    using var start=collectionRenderer.Snapshot(new(960,540),0);using var middle=collectionRenderer.Snapshot(new(960,540),.50);using var end=collectionRenderer.Snapshot(new(960,540),CollectionMotion.Duration(3));
                    Require(start.GetPixelColors(55,465,80,46).Any(p=>p.A>240&&p.R+p.G+p.B>30),"收片起点没有实际缩略图");
                    Require(start.GetPixelColors(0,0,1,1)[0].A==0,"收片前背景不是透明");Require(middle.GetPixelColors().Any(p=>p.A>240&&p.R+p.G+p.B>100),"透视中途没有图像");
                    Require(end.GetPixelColors().All(p=>p.A==255&&p.R==0&&p.G==0&&p.B==0),"收片结尾未完全黑场");
                    await middle.SaveAsync(Path.Combine(session.Storage.Root,"logs","collection-midway.png"),CanvasBitmapFileFormat.Png);
                    Require(JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions)==before,"收片改变台面状态");
                    return "实际Transform3DEffect透视、薄片旋转和盒前/后分层已绘制；开始透明、中途有图、结束全黑；台面JSON未改变。未替代入口点击测试。";
                }
                finally{collectionRenderer.Clear();collectionOverlay.Visibility=Visibility.Collapsed;}
            });
            await Check("Native-audio-prepare-and-playback-clock",async()=>
            {
                using var audio=new MechanicalAudio(session.Storage.Root,session.Storage.Log);await audio.PrepareAsync();Require(audio.IsPrepared,$"MediaPlayer准备失败: {audio.Error}");Require(audio.Volume==.20,"幻灯音量不是0.20");
                Require(audio.PositionSeconds==0,"声音在准备阶段提前播放");audio.Play(true);var watch=Stopwatch.StartNew();while(audio.PositionSeconds<=0&&watch.ElapsedMilliseconds<1800)await Task.Delay(20);
                var position=audio.PositionSeconds;audio.Stop();Require(position>0,"MediaPlayer播放时钟没有前进");
                foreach(var path in Directory.EnumerateFiles(Path.Combine(session.Storage.Root,"cache"),"Shutter*-synth-v1.wav"))
                {
                    var bytes=await File.ReadAllBytesAsync(path);Require(bytes.Length==73004&&BitConverter.ToInt32(bytes,24)==48000&&BitConverter.ToInt16(bytes,22)==2,"合成WAV规格错误");
                    Require(bytes.Skip(44).Any(p=>p!=0),"合成声音全为静音");
                }
                return $"两路真实MediaPlayer已MediaOpened，AutoPlay关闭，.20音量，首次cue播放时钟到{position:F4}s；48kHz立体声PCM .38s。未声称原录音音色或扬声器主观同步一致。";
            });
            await Check("GPU-album-perforations-reuse-and-catalog-pose",async()=>
            {
                var page=new AlbumPage{Name="膜层检查",Angle=37};page.Fill(FilmFormat.Film135);page.Pockets[0].Path=originals[0];
                using var membrane=new AlbumMembranes(board.Device);var back=membrane.Get(page,false);var front=membrane.Get(page,true);
                Require(ReferenceEquals(back,membrane.Get(page,false))&&ReferenceEquals(front,membrane.Get(page,true)),"膜层未复用");
                using var transparent=new CanvasRenderTarget(board.Device,620,690,96);
                using(var ds=transparent.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(back);ds.DrawImage(front);}
                for(int row=0;row<9;row++)Require(transparent.GetPixelColors(15,59+row*72,3,3).All(p=>p.A==0),"装订孔未透明镂空");Require(transparent.GetPixelColors(100,20,1,1)[0].A>0,"膜基底丢失");
                using var catalogA=new CanvasRenderTarget(board.Device,620,690,96);using var catalogB=new CanvasRenderTarget(board.Device,620,690,96);
                using(var ds=catalogA.CreateDrawingSession()){ds.Clear(Colors.Transparent);renderer.AlbumPreview(ds,page,620,690);}page.Angle=0;
                using(var ds=catalogB.CreateDrawingSession()){ds.Clear(Colors.Transparent);renderer.AlbumPreview(ds,page,620,690);}
                Require(catalogA.GetPixelBytes().SequenceEqual(catalogB.GetPixelBytes()),"相册目录预览继承了台面旋转");
                await transparent.SaveAsync(Path.Combine(session.Storage.Root,"logs","album-membrane.png"),CanvasBitmapFileFormat.Png);
                await catalogA.SaveAsync(Path.Combine(session.Storage.Root,"logs","album-catalog.png"),CanvasBitmapFileFormat.Png);
                for(int i=0;i<40;i++){var variant=new AlbumPage();variant.Add(FilmFormat.Film135,i%13,i/13);membrane.Get(variant,i%2==0);}Require(membrane.Count<=32,"膜层缓存超过32项");
                return "实际GPU9个装订孔中心全透明，膜基底有alpha；同袋格前后层对象复用、32项上限；目录预览不受台面37°旋转影响。";
            });
            await Check("Albums-order-and-layout-recovery",async()=>
            {
                var page=new AlbumPage{Name="集成测试页"};page.Fill(FilmFormat.Film135);page.Pockets[0].Path=originals[0];session.Document.Albums.Add(page);session.PlaceAlbum(page);
                var trayItem=trayItems.First(p=>p.AlbumId==page.Id);await LoadThumbnail(trayItem);for(int attempt=0;trayItem.Thumbnail==null&&attempt<10;attempt++)await Task.Delay(32);
                Require(trayItem.Thumbnail is Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap {PixelWidth:138,PixelHeight:132},"相册底栏没有真实GPU缩略图");
                await Task.Delay(32);if(tray.ContainerFromItem(trayItem) is FrameworkElement container&&FindTrayImage(container) is {} image)
                {
                    // The resizable tray scales the 69x66 album frame. XAML
                    // still rounds its final edges to physical pixels.
                    var tolerance=1/root.XamlRoot.RasterizationScale+.0001;
                    Require(Math.Abs(image.Width-trayItem.ImageWidth)<.01&&Math.Abs(image.Height-trayItem.ImageHeight)<.01&&Math.Abs(image.ActualWidth-image.Width)<=tolerance&&Math.Abs(image.ActualHeight-image.Height)<=tolerance,$"相册底栏图像布局尺寸错误：actual={image.ActualWidth}×{image.ActualHeight}, requested={image.Width}×{image.Height}, dpi={root.XamlRoot.RasterizationScale}");
                    session.Storage.Log($"相册缩略图布局 actual={image.ActualWidth}×{image.ActualHeight}, dpi={root.XamlRoot.RasterizationScale}, tolerance={tolerance}");
                }
                var slide=session.Document.Slides[0];session.Select(slide.Id);session.TransformSelection(1.12,15);session.Document.Viewport.Zoom(1.7,new(400,250));session.Flush();
                var expected=JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions);
                var restored=new DocumentStore(session.Storage.Root).Load(out var warning);Require(warning==null,"布局恢复警告");
                Require(JsonSerializer.Serialize(restored,DocumentStore.JsonOptions)==expected,"完整布局、相册和相机往返不一致");
                var prior=File.ReadAllText(session.Storage.DocumentPath);session.Document.Remove([slide.Id,page.Id]);session.Notify();session.Flush();
                Require(session.Document.Photos.Count==originals.Length&&page.Pockets[0].Path==originals[0]&&page.Position==null,"移出丢失相册或图库内容");
                return "相册20格、照片旋转/比例、相机、混合层级保存与新Store恢复逐字节JSON一致；移出保留内容";
            });
            await Check("Mac-Saved-native-metadata-and-atomic-import",async()=>
            {
                var slideId=Guid.NewGuid();var pageId=Guid.NewGuid();string MacUrl(string path)=>"file:///Volumes/PortFixtures/"+Uri.EscapeDataString(Path.GetFileName(path));
                var bytes=JsonSerializer.SerializeToUtf8Bytes(new{urls=originals.Select(MacUrl),label="Mac布局集成",layout=new{slides=new[]{new{id=slideId,url=MacUrl(originals[0]),x=.45,y=.5,width=.38,angle=19}}},worldWidth=1180,worldHeight=550,fitScale=.8,viewport=new{scale=1.2,offsetX=-34,offsetY=17},
                    albums=new[]{new{id=pageId,name="迁移页",position=new[]{900d,275},pockets=new[]{new{id=Guid.NewGuid(),format="135",column=0,row=0,url=MacUrl(originals[1])}}}},tableOrder=new[]{pageId,slideId}});
                var converted=await Task.Run(()=>MacSavedImporter.Import(bytes,[new("/Volumes/PortFixtures",fixtureRoot)]));
                var before=session.Images.DecodeCount;
                foreach(var photo in converted.Document.Photos){photo.Aspect=await session.Images.ReadAspectAsync(photo.Path);var decoded=await session.Images.GetAsync(photo.Path,2200);Require(Math.Abs(photo.Aspect-decoded.Aspect)<.01,"迁移照片真实尺寸未恢复");}
                var old=JsonSerializer.SerializeToUtf8Bytes(session.Document,DocumentStore.JsonOptions);var archive=session.ImportMacDocument(converted.Document,bytes);renderer.DocumentChanged();
                Require(File.ReadAllBytes(Path.Combine(archive,"macOS-layout-v1.json")).SequenceEqual(bytes),"Mac导入备份字节不一致");Require(File.ReadAllBytes(Path.Combine(archive,"windows-before-import.json")).SequenceEqual(old),"Windows导入前布局备份字节不一致");
                var restored=new DocumentStore(session.Storage.Root).Load(out var warning);Require(warning==null&&restored.Slides[0].Id==slideId&&restored.Slides[0].Angle==19&&restored.TableIds.SequenceEqual(new[]{pageId,slideId}),"迁移身份、位置或混合层级丢失");
                Require(JsonSerializer.Serialize(restored,DocumentStore.JsonOptions)==JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions),"导入后重新读取不一致");
                foreach(var path in session.Document.PlacedPaths)await renderer.PrepareAsync(path,2200);using var snapshot=renderer.RenderSnapshot(true,0);await snapshot.SaveAsync(Path.Combine(session.Storage.Root,"logs","mac-migrated.png"),CanvasBitmapFileFormat.Png);
                return "Swift Saved结构通过实际WIC方向尺寸读取、Session替换、原子保存、新Store恢复与GPU渲染；Mac原字节/Windows当前布局备份一致。文件夹映射弹窗尚未输入验收。";
            });
            await Check("GPU-zero-baseline-and-selective-two-second-ignition",CheckIgnitionAndZeroBaseline);
            await Check("GPU-sRGB-chromatic-highlight-luminance",CheckSrgbHighlightPrimaries);
            await Check("GPU-fused-lamp-reference-equivalence",CheckFusedLampEquivalence);
            await Check("GPU-progressive-command-cache-final-refresh",CheckProgressiveSceneCache);
            await Check("GPU-scene-raster-lifetime-and-budget",CheckSceneRasterLifetime);
            await Check("Realtime-Space-focal-and-capsule-animation",CheckRealTimeViewingAnimations);
            await Check("GPU-device-replacement-and-stale-upload",CheckGraphicsRecreation);
            await Check("GPU-control-DPI-recreation-and-hit-coordinates",CheckControlDpi);
            await Check("Original-photo-immutability",()=>
            {
                foreach(var (path,hash) in originalHashes)Require(File.Exists(path)&&hash==Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),"原图被修改或删除");
                return Task.FromResult($"{originalHashes.Count} 张原始JPEG哈希与操作前一致");
            });
        }
        catch(Exception ex){results.Add(new("Harness-prerequisite",false,ex.ToString(),0));}
        var report=new{Date=DateTimeOffset.Now,Scope="生产类集成检查；不替代输入自动化、原版视觉和多屏验收",AppReducedMotion=session.ReduceMotion,SystemAnimationsEnabled=new global::Windows.UI.ViewManagement.UISettings().AnimationsEnabled,Results=results,Passed=results.All(r=>r.Passed),MemoryBytes=session.Images.MemoryBytes,session.Images.MemoryHits,session.Images.DiskHits,session.Images.DecodeCount};
        await File.WriteAllTextAsync(Path.Combine(session.Storage.Root,"logs","integration.json"),JsonSerializer.Serialize(report,DocumentStore.JsonOptions));
        Environment.ExitCode=report.Passed?0:1;Close();
    }
}
