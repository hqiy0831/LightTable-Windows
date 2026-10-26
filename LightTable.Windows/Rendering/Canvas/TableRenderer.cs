using System.Diagnostics;
using System.Numerics;
using LightTable.Core;
using LightTable.Windows.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.UI;
using global::Windows.Foundation;
using global::Windows.UI;

namespace LightTable.Windows.Rendering;

sealed class TableRenderer : IDisposable
{
    readonly TableSession session;
    readonly CanvasControl canvas;
    readonly CanvasControl lensCanvas;
    readonly TransitionClock approachClock=new();
    readonly SmoothTransitionClock focalClock=new();
    CanvasRenderTarget? lensTarget;
    PixelShaderEffect? lensShader;
    RectD? loupeSourceRegion;
    CanvasCommandList? sceneCache;
    CanvasRenderTarget? sceneRaster;
    Rect sceneRasterBounds;
    public long SceneRasterBytes=>sceneRaster==null?0:(long)sceneRaster.SizeInPixels.Width*sceneRaster.SizeInPixels.Height*4;
    public long SceneRasterBuildCount {get;private set;}
    public long SceneRasterDisposeCount {get;private set;}
    bool drewCachedScene,drewRasterScene;
    bool sceneRasterAttempted;
    int sceneCacheKey;
    long sceneCacheResourcesRevision;
    double sceneCacheBuiltAt;
    long resourcesRevision;
    double recordedScale;
    double currentLoupeMagnification=1.5;
    public PointD BoardOrigin {get;set;}
    public TrayFlight? Flight {get;set;}
    public async Task PrepareFlight(string path)
    {
        try{await PrepareAsync(path,220);}catch(Exception ex){session.Storage.Log($"袋口预览失败 {path}: {ex.Message}");}
    }
    readonly Dictionary<string,(OpticalPhoto Photo,long Used)> gpu=[];
    readonly Dictionary<string,Task> pending=[];
    readonly Dictionary<string,string> failed=[];
    readonly Dictionary<string,double> rasterFootprints=new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,double> aspectIndex=new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> albumPaths=new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string,(string Key,OpticalPhoto Photo)> bestAvailable=new(StringComparer.OrdinalIgnoreCase);
    long indexedRevision=-1;
    readonly CanvasTextFormat loadingFormat=new(){FontSize=11,HorizontalAlignment=CanvasHorizontalAlignment.Center,VerticalAlignment=CanvasVerticalAlignment.Center};
    OpticalTables? tables;
    AlbumMembranes? membranes;
    public long ResourcesRevision=>resourcesRevision;
    public long CachedResourcesRevision=>sceneCacheResourcesRevision;
    public bool ResourcesPending=>pending.Count>0;
    public long SceneBuildCount {get;private set;}
    public long SceneReuseCount {get;private set;}
    readonly ResourceEpoch epoch=new();
    CanvasDevice? resourceDevice;
    bool disposed;
    public Task DeviceReady=>WaitForDevice();
    public long DeviceGeneration=>epoch.Version;
    internal Func<string,int,Task>? BeforeUpload {get;set;}
    long generation=>epoch.Version;
    long frame;
    double lastScale=-1,lastTransformAt;
    public Color PanelColor=>Panel(session.Lamp.Sample(Seconds,session.ReduceMotion));
    public event Action? ResourceReady;
    public static double Seconds=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    public PointD Pointer {get;set;}=new(300,250);
    public bool PointerInside {get;set;}
    public RectD? Marquee {get;set;}
    double closeBlend,focalBlend,lastFrame=Seconds;
    public long GpuBytes=>gpu.Values.Sum(v=>v.Photo.Cost)+SceneRasterBytes;
    public List<double> FrameTimes {get;}=[];
    public string? PerformancePhase {get;set;}
    public sealed record DrawSample(string Phase,double Seconds,double CpuMs,double Flare,int Pending,bool CommandCached,bool Close,bool RasterCached);
    public List<DrawSample> DrawSamples {get;}=[];
    public bool IsAnimating=>session.Lamp.Animating(Seconds,session.ReduceMotion) || !session.BrightnessSettled || approachClock.Active(Seconds) || focalClock.Active(Seconds) || Math.Abs(closeBlend-(session.CloseViewing?1:0))>.001 || Math.Abs(focalBlend-(int)session.Power)>.001 || Seconds-lastTransformAt<.15;
    public double CloseProgress=>closeBlend;
    public double FocalProgress=>focalBlend;
    public double RenderedCloseProgress {get;private set;}
    public double RenderedFocalProgress {get;private set;}
    public double RenderedMagnification {get;private set;}=LoupeGeometry.Magnification;
    public bool TickAnimations(double now)
    {
        var oldClose=closeBlend;var oldFocal=focalBlend;
        approachClock.Target(session.CloseViewing?1:0,now,session.ReduceMotion?0:session.CloseViewing?LoupeGeometry.EnterDuration:LoupeGeometry.ExitDuration);
        focalClock.Target((int)session.Power,now,session.ReduceMotion?0:LoupeGeometry.FocalDuration);
        closeBlend=approachClock.Value(now);focalBlend=focalClock.Value(now);
        return oldClose!=closeBlend||oldFocal!=focalBlend;
    }
    public TableRenderer(TableSession session,CanvasControl canvas,CanvasControl lensCanvas)
    {
        this.session=session;this.canvas=canvas;this.lensCanvas=lensCanvas;
        canvas.CreateResources+=CreateResources;
        canvas.Draw+=Draw;
        lensCanvas.Draw+=DrawLens;
    }
    void CreateResources(CanvasControl sender,CanvasCreateResourcesEventArgs args)
    {
        if(disposed)return;
        if(args.Reason==CanvasCreateResourcesReason.DpiChanged&&ReferenceEquals(resourceDevice,sender.Device)&&tables!=null)
        {lensTarget?.Dispose();lensTarget=null;ClearSceneCache();lastScale=-1;canvas.Invalidate();lensCanvas.Invalidate();return;}
        epoch.Begin();ClearDeviceResources();resourceDevice=sender.Device;resourceDevice.DeviceLost+=DeviceLost;
        tables=new(resourceDevice);membranes=new(resourceDevice);
        lensShader=new PixelShaderEffect(File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,"Resources","LoupeEdge.bin"))){Source1Mapping=SamplerCoordinateMapping.Offset,Source1Interpolation=CanvasImageInterpolation.Linear};
        epoch.Complete(generation);ResourceReady?.Invoke();canvas.Invalidate();lensCanvas.Invalidate();
    }
    void DeviceLost(CanvasDevice sender,object args)=>canvas.DispatcherQueue.TryEnqueue(()=>{if(!disposed&&ReferenceEquals(resourceDevice,sender))InvalidateDevice();});
    void InvalidateDevice(){if(disposed)return;epoch.Begin();ClearDeviceResources();canvas.Invalidate();lensCanvas.Invalidate();}
    void ClearDeviceResources()
    {
        if(resourceDevice!=null)resourceDevice.DeviceLost-=DeviceLost;resourceDevice=null;resourcesRevision++;
        ClearSceneCache();foreach(var entry in gpu.Values)entry.Photo.Dispose();gpu.Clear();pending.Clear();failed.Clear();tables?.Dispose();tables=null;membranes?.Dispose();membranes=null;lensTarget?.Dispose();lensTarget=null;lensShader?.Dispose();lensShader=null;
    }
    async Task WaitForDevice()
    {
        while(!disposed)
        {
            var ticket=epoch.Capture();
            try
            {
                await ticket.Ready;if(epoch.IsCurrent(ticket)&&resourceDevice!=null&&ReferenceEquals(resourceDevice,canvas.Device))return;
                await Task.Delay(Timeout.Infinite,ticket.Cancellation);
            }
            catch(OperationCanceledException)when(!epoch.IsCurrent(ticket)){}
        }
        throw new ObjectDisposedException(nameof(TableRenderer));
    }
    public bool HandleDeviceError(Exception error)=>!disposed&&GraphicsRecovery.Handle(resourceDevice,error,InvalidateDevice,session.Storage.Log);
    OpticalPhoto? Get(string path,int bucket,bool hd=false)
    {
        var raster=RasterLimit(path,bucket,hd);var key=$"{path}|{bucket}|{raster}";
        if(gpu.TryGetValue(key,out var exact)){gpu[key]=(exact.Photo,frame);return exact.Photo;}
        if(indexedRevision!=resourcesRevision)
        {
            bestAvailable.Clear();foreach(var entry in gpu)
            {
                var photo=entry.Value.Photo;
                if(!bestAvailable.TryGetValue(photo.Decoded.Path,out var prior)||Math.Max(photo.Decoded.Width,photo.Decoded.Height)>Math.Max(prior.Photo.Decoded.Width,prior.Photo.Decoded.Height))bestAvailable[photo.Decoded.Path]=(entry.Key,photo);
            }
            indexedRevision=resourcesRevision;
        }
        bestAvailable.TryGetValue(path,out var old);
        if(!pending.ContainsKey(key)&&!failed.ContainsKey(key)&&(hd||old.Photo==null||Seconds-lastTransformAt>=.12))_ = Load(path,bucket,key,generation,raster);
        if(gpu.TryGetValue(key,out var justLoaded)){gpu[key]=(justLoaded.Photo,frame);return justLoaded.Photo;}
        if(old.Photo!=null){gpu[old.Key]=(old.Photo,frame);return old.Photo;}
        return null;
    }
    void RefreshRasterFootprints()
    {
        rasterFootprints.Clear();aspectIndex.Clear();albumPaths.Clear();var dpi=canvas.Dpi/96d;
        foreach(var photo in session.Document.Photos)aspectIndex.TryAdd(photo.Path,photo.Aspect);
        foreach(var slide in session.Document.Slides)
        {
            var p=IndexedPlacement(slide);var size=Math.Max(p.Width,p.Height)*dpi;
            if(size>rasterFootprints.GetValueOrDefault(slide.Path))rasterFootprints[slide.Path]=size;
        }
        foreach(var album in session.Document.Albums)foreach(var pocket in album.Pockets)if(pocket.Path is {} path)albumPaths.Add(path);
    }
    Placement IndexedPlacement(Slide slide)=>Geometry.Place(session.Document,slide,aspectIndex.GetValueOrDefault(slide.Path,1.5));
    int RasterLimit(string path,int bucket,bool hd)
    {
        if(!hd&&bucket>256)return bucket;
        var longest=Math.Max(64,rasterFootprints.GetValueOrDefault(path)*(hd?currentLoupeMagnification:1));
        if(Flight?.Path==path)longest=Math.Max(longest,Math.Max(Flight.Frame.Width,Flight.Frame.Height)*(canvas.Dpi/96d));
        if(albumPaths.Contains(path))return bucket;
        return new[]{64,128,256,384,512,768,1024,1536,2200}.FirstOrDefault(n=>n>=longest,2200);
    }
    Task Load(string path,int bucket,string key,long token,int raster=2200)
    {
        if(pending.TryGetValue(key,out var existing))return existing;
        var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[key]=completion.Task;
        _=LoadCore(path,bucket,key,token,raster,completion);return completion.Task;
    }
    async Task LoadCore(string path,int bucket,string key,long token,int raster,TaskCompletionSource completion)
    {
        CanvasDevice? usedDevice=null;
        try
        {
            if(BeforeUpload is {} beforeUpload)await beforeUpload(path,bucket);
            if(token!=generation||tables==null)return;
            usedDevice=resourceDevice;if(usedDevice==null)return;var photo=await OpticalPhoto.CreateAsync(usedDevice,session.Images,path,bucket,raster);
            if(disposed||token!=generation||!ReferenceEquals(resourceDevice,usedDevice)){photo.Dispose();return;}
            if(gpu.TryGetValue(key,out var replaced)){ClearSceneCache();replaced.Photo.Dispose();}gpu[key]=(photo,frame);
            resourcesRevision++;
            while(GpuBytes>384L*1024*1024&&gpu.Count>1)
            {
                var oldest=gpu.Where(e=>e.Key!=key).MinBy(e=>e.Value.Used);
                ClearSceneCache();oldest.Value.Photo.Dispose();gpu.Remove(oldest.Key);
            }
        }
        catch(Exception ex){if(!disposed&&token==generation&&!GraphicsRecovery.Handle(usedDevice,ex,InvalidateDevice,session.Storage.Log)){failed[key]=ex.Message;session.Storage.Log($"图像/光学处理失败 {path}: {ex}");}}
        finally
        {
            if(token==generation){pending.Remove(key);canvas.Invalidate();lensCanvas.Invalidate();ResourceReady?.Invoke();}
            completion.TrySetResult();
        }
    }
    public Task PrepareAsync(string path,int bucket)=>EnsureAsync(path,bucket,bucket);
    async Task EnsureAsync(string path,int bucket,int raster)
    {
        while(!disposed)
        {
            await DeviceReady;var token=generation;var key=$"{path}|{bucket}|{raster}";
            if(!gpu.ContainsKey(key))await Load(path,bucket,key,token,raster);
            if(token!=generation)continue;
            if(failed.TryGetValue(key,out var failure))throw new InvalidDataException(failure);
            if(gpu.ContainsKey(key))return;
            throw new InvalidOperationException("GPU预览未能准备完成");
        }
        throw new ObjectDisposedException(nameof(TableRenderer));
    }
    public void RetryFailures()=>failed.Clear();
    public void DocumentChanged(){ClearSceneCache();resourcesRevision++;failed.Clear();canvas.Invalidate();lensCanvas.Invalidate();}
    public async Task PrepareVisibleTable()
    {
        await DeviceReady;
        RefreshRasterFootprints();
        foreach(var slide in session.Document.Slides)
        {
            var p=IndexedPlacement(slide);if(!p.Intersects(new RectD(0,0,canvas.ActualWidth,canvas.ActualHeight)))continue;
            var bucket=Geometry.PreviewBucket(p.Width,p.Height,canvas.Dpi/96d);var raster=RasterLimit(slide.Path,bucket,false);
            await EnsureAsync(slide.Path,bucket,raster);
        }
    }
    public CanvasRenderTarget RenderSnapshot(bool lit,double flare,bool useCommandCache=false)
    {
        if(tables==null)throw new InvalidOperationException("GPU设备资源未就绪");
        RefreshRasterFootprints();
        var sample=new LightSample(lit?1:0,flare,lit?.5:0);
        var result=new CanvasRenderTarget(canvas.Device,(float)canvas.ActualWidth,(float)canvas.ActualHeight,96);
        using(var ds=result.CreateDrawingSession()){ds.Clear(Panel(sample));if(useCommandCache)DrawTable(ds,sample);else Scene(ds,sample,false);}
        return result;
    }
    public void AlbumPreview(CanvasDrawingSession ds,AlbumPage album,double width,double height)
    {
        if(resourceDevice==null||!ReferenceEquals(resourceDevice,ds.Device))return;
        RefreshRasterFootprints();
        var scale=Math.Min(width/620,height/690);
        DrawAlbum(ds,album,LightResponse.Sample(10,true),false,new Placement(new(width/2,height/2),620*scale,690*scale,0),true);
    }
    void Draw(CanvasControl sender,CanvasDrawEventArgs args)
    {
        if(disposed)return;
        var begin=Seconds;frame++;drewCachedScene=drewRasterScene=false;
        var scale=session.Document.Viewport.Scale*session.Document.FitScale*(canvas.Dpi/96d);
        if(Math.Abs(scale-lastScale)>.00001){lastScale=scale;lastTransformAt=begin;}
        lastFrame=begin;
        RefreshRasterFootprints();TickAnimations(begin);TryIgniteVisibleFilms(begin);var sample=session.Lamp.Sample(begin,session.ReduceMotion);
        var ds=args.DrawingSession;ds.Clear(Panel(sample));
        if(tables==null)return;
        DrawTable(ds,sample);
        if(session.Document.TableIds.Any()==false)
        {
            using var fmt=new CanvasTextFormat{FontFamily="Segoe UI Variable",FontSize=14,HorizontalAlignment=CanvasHorizontalAlignment.Center,VerticalAlignment=CanvasVerticalAlignment.Center};
            ds.DrawText(session.Document.Photos.Count==0?"把照片放在光里":"从底栏拖片到台面，或选择「一键平铺」",new Rect(0,0,canvas.ActualWidth,canvas.ActualHeight),Color.FromArgb(255,97,92,80),fmt);
        }
        if(Marquee is {} r){ds.FillRectangle(ToRect(r),Color.FromArgb(25,35,96,170));ds.DrawRectangle(ToRect(r),Color.FromArgb(180,35,96,170),1);}
        lensCanvas.Invalidate();
        var elapsed=(Seconds-begin)*1000;
        if(FrameTimes.Count<10000)FrameTimes.Add(elapsed);
        if(PerformancePhase is {} phase&&DrawSamples.Count<10000)DrawSamples.Add(new(phase,begin,elapsed,sample.Flare,pending.Count,drewCachedScene,session.CloseViewing,drewRasterScene));
        TrimGpuBudget();
    }
    void TryIgniteVisibleFilms(double now)
    {
        if(!session.Lamp.Pending)return;var view=new RectD(0,0,canvas.ActualWidth,canvas.ActualHeight);
        var visible=session.Document.Slides.Where(s=>IndexedPlacement(s).Intersects(view)).Select(s=>s.Path).Concat(
            session.Document.Albums.Where(a=>a.Position!=null&&Geometry.Place(session.Document,a).Intersects(view)).SelectMany(a=>a.Pockets).Select(p=>p.Path).OfType<string>()).Distinct(StringComparer.OrdinalIgnoreCase);
        var available=gpu.Values.Select(g=>g.Photo.Decoded.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool ready=visible.All(path=>available.Contains(path)||failed.Keys.Any(key=>key.StartsWith(path+"|",StringComparison.OrdinalIgnoreCase)));
        session.Lamp.TryIgnite(now,ready);
    }
    int SceneKey(LightSample sample)
    {
        var h=new HashCode();h.Add(sample.Transmission);h.Add(sample.Flare);h.Add(session.Brightness);h.Add(session.Document.FitScale);h.Add(session.Document.WorldWidth);h.Add(session.Document.WorldHeight);h.Add(Flight?.Id);h.Add(session.ShowSelection&&!session.Loupe);
        foreach(var s in session.Document.Slides){h.Add(s.Id);h.Add(s.Path);h.Add(s.X);h.Add(s.Y);h.Add(s.Angle);h.Add(s.Width);}
        foreach(var a in session.Document.Albums){h.Add(a.Id);h.Add(a.Position);h.Add(a.Angle);h.Add(a.Scale);foreach(var p in a.Pockets){h.Add(p.Id);h.Add(p.Path);h.Add(p.Column);h.Add(p.Row);h.Add(p.Format);}}
        foreach(var id in session.Document.TableIds)h.Add(id);foreach(var id in session.Selection.Order())h.Add(id);return h.ToHashCode();
    }
    void DrawTable(CanvasDrawingSession ds,LightSample sample)
    {
        if(sample.Flare>0||sample.Transmission>0&&sample.Transmission<1||!session.BrightnessSettled){Scene(ds,sample,false);return;}
        var key=SceneKey(sample);var camera=session.Document.Viewport;
        var matrix=Matrix3x2.CreateScale((float)camera.Scale)*Matrix3x2.CreateTranslation((float)camera.OffsetX,(float)camera.OffsetY);
        // Reuse the current loading snapshot between batches of uploaded films.
        // Layout/selection/light changes are immediate; only arriving resources
        // are coalesced for <=50 ms, with an immediate final refresh on completion.
        bool newResources=resourcesRevision!=sceneCacheResourcesRevision&&(pending.Count==0||Seconds-sceneCacheBuiltAt>=.05);
        if(sceneCache!=null&&(key!=sceneCacheKey||newResources||recordedScale!=camera.Scale&&Seconds-lastTransformAt>=.12))ClearSceneCache();
        if(sceneCache==null)
        {
            SceneBuildCount++;
            var commands=new CanvasCommandList(canvas.Device);
            using(var recorded=commands.CreateDrawingSession())
            {
                Matrix3x2.Invert(matrix,out var inverse);recorded.Transform=inverse;Scene(recorded,sample,false,true);
            }
            sceneCache=commands;sceneCacheKey=SceneKey(sample);recordedScale=camera.Scale;sceneCacheResourcesRevision=resourcesRevision;sceneCacheBuiltAt=Seconds;
            if(pending.Count==0)BakeSceneRaster(commands,camera.Scale);
            var prior=ds.Transform;ds.Transform=matrix*prior;DrawCachedScene(ds);ds.Transform=prior;
            return;
        }
        if(!sceneRasterAttempted&&pending.Count==0)BakeSceneRaster(sceneCache,camera.Scale);
        SceneReuseCount++;var old=ds.Transform;ds.Transform=matrix*old;DrawCachedScene(ds);ds.Transform=old;
    }
    void DisposeSceneRaster(){if(sceneRaster!=null){sceneRaster.Dispose();sceneRaster=null;SceneRasterDisposeCount++;}}
    void ClearSceneCache(){sceneCache?.Dispose();sceneCache=null;DisposeSceneRaster();sceneRasterAttempted=false;}
    void DrawCachedScene(CanvasDrawingSession ds)
    {
        drewCachedScene=true;
        if(sceneRaster!=null){drewRasterScene=true;ds.DrawImage(sceneRaster,sceneRasterBounds,new Rect(0,0,sceneRaster.Size.Width,sceneRaster.Size.Height),1,CanvasImageInterpolation.Linear);}
        else if(sceneCache!=null)ds.DrawImage(sceneCache);
    }
    void BakeSceneRaster(CanvasCommandList commands,double cameraScale)
    {
        DisposeSceneRaster();
        sceneRasterAttempted=true;
        // This is a scene cache, never a replacement for the independent HD
        // loupe. Large/unbounded worlds keep the command-list fallback.
        var bounds=commands.GetBounds(canvas);var scale=cameraScale*canvas.Dpi/96d;
        if(!double.IsFinite(bounds.X)||!double.IsFinite(bounds.Y)||!double.IsFinite(bounds.Width)||!double.IsFinite(bounds.Height)||bounds.Width<=0||bounds.Height<=0)return;
        var left=Math.Floor(bounds.X*scale)-2;var top=Math.Floor(bounds.Y*scale)-2;
        var width=Math.Ceiling((bounds.X+bounds.Width)*scale)-left+2;var height=Math.Ceiling((bounds.Y+bounds.Height)*scale)-top+2;
        if(width<1||height<1||width>4096||height>4096||width*height*4>32L*1024*1024||GpuBytes+width*height*4>384L*1024*1024)return;
        var target=new CanvasRenderTarget(canvas.Device,(float)width,(float)height,96);
        try
        {
            using(var draw=target.CreateDrawingSession())
            {
                draw.Clear(Colors.Transparent);draw.Transform=Matrix3x2.CreateScale((float)scale)*Matrix3x2.CreateTranslation(-(float)left,-(float)top);draw.DrawImage(commands);
            }
            sceneRaster=target;SceneRasterBuildCount++;sceneRasterBounds=new(left/scale,top/scale,width/scale,height/scale);
        }
        catch{target.Dispose();throw;}
    }
    void Scene(CanvasDrawingSession ds,LightSample sample,bool hd,bool all=false)
    {
        var slides=session.Document.Slides.ToDictionary(s=>s.Id);var albums=session.Document.Albums.ToDictionary(a=>a.Id);
        foreach(var id in session.Document.TableIds)
        {
            if(slides.TryGetValue(id,out var slide))
            {
                if(Flight?.Path==slide.Path)continue;
                var p=IndexedPlacement(slide);
                if(!p.Intersects(new RectD(0,0,canvas.ActualWidth,canvas.ActualHeight))&&!hd&&!all)continue;
                if(hd&&loupeSourceRegion is {} region&&!p.Intersects(region))continue;
                var bucket=hd?2200:Geometry.PreviewBucket(p.Width,p.Height,canvas.Dpi/96d);
                var photo=Get(slide.Path,bucket,hd);
                DrawFilm(ds,photo,p,sample,!hd&&session.ShowSelection&&session.Selection.Contains(id)&&!session.Loupe);
            }
            else if(albums.TryGetValue(id,out var album))DrawAlbum(ds,album,sample,hd);
        }
    }
    void DrawFilm(CanvasDrawingSession ds,OpticalPhoto? photo,Placement p,LightSample sample,bool selected)
    {
        var original=ds.Transform;
        ds.Transform=Matrix3x2.CreateRotation((float)(p.Angle*Math.PI/180))*Matrix3x2.CreateTranslation((float)p.Center.X,(float)p.Center.Y)*original;
        var rect=new Rect(-p.Width/2,-p.Height/2,p.Width,p.Height);
        // Contact shadow and sub-DIP exposed substrate stay outside source pixels.
        ds.FillRectangle(new Rect(rect.X+.35,rect.Y+.8,rect.Width+.6,rect.Height+.85),Color.FromArgb(200,14,14,14));
        ds.DrawRectangle(new Rect(rect.X-1,rect.Y-1,rect.Width+2,rect.Height+2),Color.FromArgb(35,0,0,0),1.5f);
        if(photo!=null)ds.DrawImage(photo.View(sample,session.Brightness,tables!,session.BrightnessSettled),rect,new Rect(0,0,photo.Decoded.Width,photo.Decoded.Height),1,CanvasImageInterpolation.HighQualityCubic);
        else
        {
            ds.FillRectangle(rect,Color.FromArgb(255,40,38,33));
            if(p.Width>=80&&p.Height>=40)ds.DrawText("照片读取中或不可用",rect,Colors.LightGray,loadingFormat);
        }
        if(selected)
        {
            ds.DrawRectangle(new Rect(rect.X-3,rect.Y-3,rect.Width+6,rect.Height+6),Color.FromArgb(255,49,100,158),1.5f);
            foreach(var x in new[]{rect.X,rect.X+rect.Width})foreach(var y in new[]{rect.Y,rect.Y+rect.Height})ds.FillCircle((float)x,(float)y,4,Colors.White);
        }
        ds.Transform=original;
    }
    void DrawAlbum(CanvasDrawingSession ds,AlbumPage album,LightSample sample,bool hd,Placement? overridePlacement=null,bool catalog=false)
    {
        var p=overridePlacement??Geometry.Place(session.Document,album);var original=ds.Transform;
        var scale=(float)(p.Width/AlbumPage.Width);
        ds.Transform=Matrix3x2.CreateTranslation(-310,-345)*Matrix3x2.CreateScale(scale)*Matrix3x2.CreateRotation((float)(p.Angle*Math.PI/180))*Matrix3x2.CreateTranslation((float)p.Center.X,(float)p.Center.Y)*original;
        if(membranes!=null)ds.DrawImage(membranes.Get(album,false));
        foreach(var pocket in album.Pockets)
        {
            var r=pocket.Rect;
            if(pocket.Path!=null)
            {
                var aspect=session.Document.Aspect(pocket.Path);var side=Math.Min(r.Width-18,r.Height-28);var w=Math.Min(side,side*aspect);var h=w/aspect;
                var bucket=hd?2200:Geometry.PreviewBucket(w*scale,h*scale,canvas.Dpi/96d);
                var photo=Get(pocket.Path,bucket,hd);
                if(catalog)
                {
                    var rect=new Rect(r.Center.X-w/2,r.Center.Y-h/2,w,h);
                    if(photo!=null)ds.DrawImage(photo.Source,rect,new Rect(0,0,photo.Decoded.Width,photo.Decoded.Height));else ds.FillRectangle(rect,Color.FromArgb(255,9,9,9));
                }
                else DrawFilm(ds,photo,new(r.Center,w,h,0),sample,false);
            }
        }
        if(membranes!=null)ds.DrawImage(membranes.Get(album,true));
        if(!catalog&&!hd&&session.ShowSelection&&session.Selection.Contains(album.Id)&&!session.Loupe)ds.DrawRectangle(new Rect(-3,-3,626,696),Color.FromArgb(255,49,100,158),2);
        ds.Transform=original;
    }
    public Guid? Hit(PointD point)
    {
        foreach(var id in session.Document.TableIds.Reverse())
        {
            if(session.Document.Slides.Find(s=>s.Id==id) is {} slide&&Geometry.Place(session.Document,slide).Contains(point))return id;
            if(session.Document.Albums.Find(a=>a.Id==id) is {} album&&Geometry.Place(session.Document,album).Contains(point))return id;
        }
        return null;
    }
    public void SelectMarquee(RectD rect)
    {
        session.Selection.Clear();
        foreach(var s in session.Document.Slides)if(Geometry.Place(session.Document,s).Intersects(rect))session.Selection.Add(s.Id);
        foreach(var a in session.Document.Albums.Where(a=>a.Position!=null))if(Geometry.Place(session.Document,a).Intersects(rect))session.Selection.Add(a.Id);
        session.ShowSelection=true;session.Notify(false);
    }
    static Rect ToRect(RectD r)=>new(r.X,r.Y,r.Width,r.Height);
    Color Panel(LightSample s)
    {
        double t=s.Transmission,minimum=LightResponse.PanelWhite(0,0),white=minimum+(LightResponse.PanelWhite(session.Brightness,s.Flare)-minimum)*t;
        var neutral=t*Math.Clamp(session.Brightness,0,1);return Color.FromArgb(255,(byte)(255*white),(byte)(255*white*(.85/.89+(1-.85/.89)*neutral)),(byte)(255*white*(.75/.89+(1-.75/.89)*neutral)));
    }
    void DrawLens(CanvasControl _,CanvasDrawEventArgs args)
    {
        if(disposed)return;
        RefreshRasterFootprints();
        args.DrawingSession.Clear(Colors.Transparent);
        if(resourceDevice==null||!ReferenceEquals(resourceDevice,args.DrawingSession.Device))return;
        if(Flight is {} flight&&tables!=null)
        {
            var ds=args.DrawingSession;var frame=flight.Frame;
            if(flight.AlbumId is {} id&&session.Document.Albums.Find(a=>a.Id==id) is {} album)
                DrawAlbum(ds,album,new(1,0,.5),false,new Placement(frame.Center,frame.Width,frame.Height,flight.Angle));
            else
            {
                var photo=Get(flight.Path,flight.Expansion>.5?512:220);var aspect=photo?.Decoded.Aspect??session.Document.Aspect(flight.Path);
                var w=Math.Min(frame.Width,frame.Height*aspect);var flightLight=new LightSample(session.IsLit?1:1-flight.Expansion,0,.5);
                DrawFilm(ds,photo,new(frame.Center,w,w/aspect,flight.Angle),flightLight,false);
            }
            return;
        }
        if(!session.Loupe||!PointerInside||tables==null||lensShader==null)return;
        var size=new SizeD(lensCanvas.ActualWidth,lensCanvas.ActualHeight);
        var pose=LoupePose.Sample(size,closeBlend,LoupePowers.ScaleAt(focalBlend));var origin=Pointer+BoardOrigin;
        var center=origin+(LoupeGeometry.CloseCenter(origin,size)-origin)*pose.Travel;
        var sample=session.Lamp.Sample(Seconds,session.ReduceMotion);
        RenderLens(args.DrawingSession,size,pose,center,sample,closeBlend);
        RenderedCloseProgress=closeBlend;RenderedFocalProgress=focalBlend;RenderedMagnification=pose.Magnification;
    }
    void RenderLens(CanvasDrawingSession ds,SizeD viewport,LoupePose pose,PointD center,LightSample sample,double progress)
    {
        RefreshRasterFootprints();
        currentLoupeMagnification=pose.Magnification;
        var dpi=Math.Clamp(lensCanvas.Dpi,96,288);
        var side=(float)(session.CloseViewing||progress>0?LoupeGeometry.CloseDiameter(viewport):LoupeGeometry.Diameter);
        if(lensTarget==null||Math.Abs(lensTarget.Size.Width-side)>.5||lensTarget.Dpi!=dpi)
        {lensTarget?.Dispose();lensTarget=new CanvasRenderTarget(canvas.Device,side,side,dpi);}
        var half=side/2;var radius=(float)pose.Diameter/2;var sourceRadius=radius/pose.Magnification+4;
        loupeSourceRegion=new(Pointer.X-sourceRadius,Pointer.Y-sourceRadius,sourceRadius*2,sourceRadius*2);
        using(var surface=lensTarget.CreateDrawingSession())
        {
            surface.Clear(Colors.Black);
            surface.Transform=Matrix3x2.CreateTranslation(-(float)Pointer.X,-(float)Pointer.Y)*Matrix3x2.CreateScale((float)pose.Magnification)*Matrix3x2.CreateTranslation(half,half);
            using var clip=CanvasGeometry.CreateRectangle(surface,new Rect(0,0,canvas.ActualWidth,canvas.ActualHeight));
            using(surface.CreateLayer(1,clip)){surface.FillRectangle(new Rect(0,0,canvas.ActualWidth,canvas.ActualHeight),Panel(sample));Scene(surface,sample,true);}
        }
        var scale=dpi/96;
        lensShader!.Source1=lensTarget;lensShader.Properties["center"]=new Vector2(half*scale,half*scale);
        lensShader.Properties["radius"]=radius*scale;lensShader.Properties["strength"]=(float)(1-Geometry.Quintic(progress));
        lensShader.MaxSamplerOffset=(int)Math.Ceiling(radius*scale*.007)+1;
        if(pose.Surround>0)ds.FillRectangle(new Rect(0,0,viewport.Width,viewport.Height),Color.FromArgb((byte)(255*pose.Surround),0,0,0));
        using var circle=CanvasGeometry.CreateCircle(ds,(float)center.X,(float)center.Y,radius);
        using(ds.CreateLayer(1,circle))ds.DrawImage(lensShader,new Rect(center.X-radius,center.Y-radius,radius*2,radius*2),new Rect(half-radius,half-radius,radius*2,radius*2));
        var intensity=1-Geometry.Quintic(progress);
        ds.DrawCircle((float)center.X,(float)center.Y,radius-1.25f,Color.FromArgb((byte)(230*intensity),30,30,30),2.5f);
        ds.DrawCircle((float)center.X-1,(float)center.Y-1,radius-2,Color.FromArgb((byte)(90*intensity),255,255,255),1);
        // Thin grazing highlights stay at the glass lip; central sampling is untouched.
        for(int i=0;i<5;i++)ds.DrawCircle((float)center.X,(float)center.Y,radius*(.978f+i*.004f),Color.FromArgb((byte)((8+i*3)*intensity),255,255,255),.5f);
        TrimGpuBudget();
    }
    void TrimGpuBudget()
    {
        if(GpuBytes<=384L*1024*1024)return;
        ClearSceneCache();resourcesRevision++;
        while(GpuBytes>384L*1024*1024&&gpu.Count>1)
        {
            // Prefer superseded sizes of the same source before evicting the
            // only texture for a visible film.
            var entry=gpu.OrderBy(e=>gpu.Values.Count(v=>v.Photo.Decoded.Path==e.Value.Photo.Decoded.Path)>1?0:1).ThenBy(e=>e.Value.Used).First();
            entry.Value.Photo.Dispose();gpu.Remove(entry.Key);
        }
    }
    public CanvasRenderTarget RenderLensSnapshot(double progress,double blend)
    {
        var size=new SizeD(lensCanvas.ActualWidth,lensCanvas.ActualHeight);var pose=LoupePose.Sample(size,progress,LoupePowers.ScaleAt(blend));var origin=Pointer+BoardOrigin;
        var center=origin+(LoupeGeometry.CloseCenter(origin,size)-origin)*pose.Travel;
        var target=new CanvasRenderTarget(canvas.Device,(float)size.Width,(float)size.Height,96);
        using(var ds=target.CreateDrawingSession()){ds.Clear(Colors.Transparent);RenderLens(ds,size,pose,center,new(1,0,.5),progress);}
        return target;
    }
    public void Dispose(){if(disposed)return;disposed=true;canvas.CreateResources-=CreateResources;canvas.Draw-=Draw;lensCanvas.Draw-=DrawLens;epoch.Dispose();ClearDeviceResources();loadingFormat.Dispose();bestAvailable.Clear();}
}
