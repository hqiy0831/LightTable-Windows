using System.Numerics;
using LightTable.Core;
using LightTable.Windows.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using global::Windows.Graphics.DirectX;
using global::Windows.UI;

namespace LightTable.Windows.Rendering;

// All five static optical layers are rendered once on Direct2D. No blur occurs
// in the drag/loupe/ignition loop. A fused HLSL pass performs hue-preserving SDR
// lamp and exposure shoulders in linear light, as in PhotoOptics.swift.
sealed class OpticalPhoto : IDisposable
{
    public DecodedPhoto Decoded {get;}
    public CanvasBitmap Source {get;}
    public CanvasBitmap Lit=>lit;
    readonly CanvasBitmap dark,lit,boosted,halo;
    readonly PixelShaderEffect fused;
    ReferenceGraph? reference;
    CanvasRenderTarget? stable;
    double stableBrightness=-1;
    double appliedBrightness=double.NaN,appliedFlare=double.NaN,appliedTransmission=double.NaN;
    readonly CanvasDevice device;
    public bool UsedCachedLayers {get;}
    static readonly SemaphoreSlim constructionLane=new(2);
    static readonly byte[] exposureCode=File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,"Resources","FusedOptical.bin"));
    static readonly byte[] lampCode=File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,"Resources","FusedLamp.bin"));
    public long Cost=>(long)Decoded.Width*Decoded.Height*4*(stable==null?5:6);
    public static Task<OpticalPhoto> CreateAsync(CanvasDevice device,ImageStore images,string path,int bucket=2200,int rasterLimit=2200)=>Task.Run(async()=>
    {
        bucket=Math.Clamp(bucket,128,2200);
        var key=ImageStore.SourceKey(path,bucket);
        return await CreateCoreAsync(device,key,path,bucket,images.OpticalCache,rasterLimit,async()=>
        {
            var decoded=await images.GetAsync(path,bucket).ConfigureAwait(false);
            if(decoded.Key!=key)throw new IOException("照片在读取过程中发生变化，请重试");
            return decoded;
        }).ConfigureAwait(false);
    });
    public static Task<OpticalPhoto> CreateAsync(CanvasDevice device,DecodedPhoto image,OpticalLayerCache cache,int rasterLimit=2200)=>Task.Run(()=>CreateCoreAsync(device,image.Key,image.Path,image.Bucket,cache,rasterLimit,()=>Task.FromResult(image)));
    static async Task<OpticalPhoto> CreateCoreAsync(CanvasDevice device,string key,string path,int bucket,OpticalLayerCache cache,int rasterLimit,Func<Task<DecodedPhoto>> decode)
    {
        using var tableLease=OpticalTablePool.Acquire(device);
        await constructionLane.WaitAsync().ConfigureAwait(false);
        OpticalPhoto? built=null;
        try
        {
            var pixels=await cache.GetOrBuildPreviewAsync(key,rasterLimit,async()=>
            {
                // Private tables and offscreen targets are never concurrently
                // drawn or disposed by the UI's device-generation callbacks.
                var image=await decode().ConfigureAwait(false);built=new(device,image,tableLease.Tables,rasterLimit);
                return built.ReadLayers();
            }).ConfigureAwait(false);
            return built??new OpticalPhoto(device,new(key,path,pixels.Width,pixels.Height,[],bucket),null,rasterLimit,pixels);
        }
        catch{built?.Dispose();throw;}
        finally{constructionLane.Release();}
    }
    static (int Width,int Height) RasterSize(DecodedPhoto image,int rasterLimit)
    {
        var ratio=Math.Min(1,(double)rasterLimit/Math.Max(image.Width,image.Height));
        return (Math.Max(1,(int)Math.Round(image.Width*ratio)),Math.Max(1,(int)Math.Round(image.Height*ratio)));
    }
    internal OpticalRaster ReadLayers()=>new(Decoded.Width,Decoded.Height,[Source.GetPixelBytes(),lit.GetPixelBytes(),dark.GetPixelBytes(),halo.GetPixelBytes(),boosted.GetPixelBytes()]);
    public OpticalPhoto(CanvasDevice device,DecodedPhoto image,OpticalTables tables,int rasterLimit=2200):this(device,image,tables,rasterLimit,null){}
    OpticalPhoto(CanvasDevice device,DecodedPhoto image,OpticalTables? tables,int rasterLimit,OpticalRaster? cached)
    {
        this.device=device;
        var (width,height)=RasterSize(image,rasterLimit);
        // GPU entries retain metadata only. Keeping decoded byte arrays here
        // would silently defeat the separate CPU LRU budget.
        Decoded=new(image.Key,image.Path,width,height,[],image.Bucket);
        var owned=new List<IDisposable>();T Own<T>(T value)where T:IDisposable{owned.Add(value);return value;}
        var graphs=new List<IDisposable>();T Graph<T>(T value)where T:IDisposable{graphs.Add(value);return value;}
        try
        {
        if(cached!=null)
        {
            cached.Validate(width,height);UsedCachedLayers=true;
            CanvasBitmap Upload(int layer)=>Own(CanvasBitmap.CreateFromBytes(device,cached.Layers[layer],width,height,DirectXPixelFormat.B8G8R8A8UIntNormalized,96,CanvasAlphaMode.Premultiplied));
            Source=Upload(0);lit=Upload(1);dark=Upload(2);halo=Upload(3);boosted=Upload(4);
        }
        else
        {
        ArgumentNullException.ThrowIfNull(tables);
        var uploaded=Own(CanvasBitmap.CreateFromBytes(device,image.Pixels,image.Width,image.Height,DirectXPixelFormat.B8G8R8A8UIntNormalized,96,CanvasAlphaMode.Premultiplied));
        if(width!=image.Width||height!=image.Height)
        {
            var scaled=Own(new CanvasRenderTarget(device,width,height,96));
            using(var ds=scaled.CreateDrawingSession()){ds.Clear(Microsoft.UI.Colors.Transparent);ds.DrawImage(uploaded,new global::Windows.Foundation.Rect(0,0,width,height),new global::Windows.Foundation.Rect(0,0,image.Width,image.Height),1,CanvasImageInterpolation.HighQualityCubic);}
            uploaded.Dispose();owned.Remove(uploaded);Source=scaled;
        }
        else Source=uploaded;
        image=Decoded;
        var dimension=Math.Max(image.Width,image.Height);
        var highlights=Graph(new TableTransfer3DEffect{Source=Source,Table=tables.Highlights});
        ICanvasImage Scatter(float radius,float strength)=>Graph(new ArithmeticCompositeEffect
        {
            Source1=Graph(new GaussianBlurEffect{Source=highlights,BlurAmount=Math.Max(.8f,radius),BorderMode=EffectBorderMode.Hard}),
            Source2=highlights,Source1Amount=strength,Source2Amount=-strength,MultiplyAmount=0,Offset=0,ClampOutput=true
        });
        ICanvasImage Transmitted(bool boost)=>Graph(new BlendEffect{Mode=BlendEffectMode.Screen,
            Background=Graph(new BlendEffect{Mode=BlendEffectMode.Screen,Background=Source,Foreground=Scatter(dimension*(boost?.0045f:.0035f),boost?.24f:.09f)}),
            Foreground=Scatter(dimension*(boost?.014f:.011f),boost?.085f:.025f)});
        lit=Own(Bake(device,image,Transmitted(false)));boosted=Own(Bake(device,image,Transmitted(true)));
        // User-requested ambient floor: OFF is exactly the settled ON/0%
        // optical image, rather than the older near-black macOS dark raster.
        dark=Own(Bake(device,image,Graph(new ColorMatrixEffect{Source=lit,ColorMatrix=Matrix((float)LightResponse.Level(0))})));
        var core=Graph(new ColorMatrixEffect{Source=Graph(new GaussianBlurEffect{Source=Graph(new TableTransfer3DEffect{Source=Source,Table=tables.SwitchHighlights}),BlurAmount=Math.Max(.8f,dimension*.0035f),BorderMode=EffectBorderMode.Hard}),ColorMatrix=Matrix(.22f)});
        var protectedCore=Graph(new BlendEffect{Mode=BlendEffectMode.Multiply,Background=core,Foreground=Graph(new TableTransfer3DEffect{Source=Source,Table=tables.BlackProtection})});
        halo=Own(Bake(device,image,Graph(new BlendEffect{Mode=BlendEffectMode.Screen,Background=protectedCore,Foreground=Scatter(dimension*.012f,.065f)})));
        foreach(var graph in Enumerable.Reverse(graphs))graph.Dispose();graphs.Clear();
        }
        fused=Own(new PixelShaderEffect(lampCode){Source1=lit,Source2=boosted,Source3=halo,Source4=dark,
            Source1Mapping=SamplerCoordinateMapping.OneToOne,Source2Mapping=SamplerCoordinateMapping.OneToOne,Source3Mapping=SamplerCoordinateMapping.OneToOne,Source4Mapping=SamplerCoordinateMapping.OneToOne});
        }
        catch{foreach(var item in Enumerable.Reverse(graphs.Concat(owned))){try{item.Dispose();}catch{/* Preserve the original device/build failure. */}}throw;}
    }
    public ICanvasImage View(LightSample sample,double brightness,OpticalTables tables,bool allowStable=true)
    {
        if(sample.Transmission<=0||LightResponse.Brightness(brightness)<=0&&sample.Flare<=0)return dark;
        // The default settled lamp is exactly the already baked lit layer.
        // Avoid building a redundant sixth texture for every film at ignition.
        if(sample.Transmission>=1&&sample.Flare<=0&&brightness==1)return lit;
        if(allowStable&&sample.Transmission>=1&&sample.Flare<=0&&stable!=null&&stableBrightness==brightness)return stable;
        if(appliedBrightness!=brightness||appliedFlare!=sample.Flare)
        {
            fused.Properties["controls"]=new Vector4((float)LightResponse.BrightnessMix(brightness),(float)LightResponse.GlowMix(sample.Flare,brightness),(float)LightResponse.Level(brightness),(float)Math.Max(1,LightResponse.Power(brightness)));
            fused.Properties["lighting"]=new Vector2((float)LightResponse.Exposure(sample.Flare,brightness),(float)sample.Transmission);
        }
        else if(appliedTransmission!=sample.Transmission)
            fused.Properties["lighting"]=new Vector2((float)LightResponse.Exposure(sample.Flare,brightness),(float)sample.Transmission);
        appliedBrightness=brightness;appliedFlare=sample.Flare;appliedTransmission=sample.Transmission;
        if(allowStable&&sample.Transmission>=1&&sample.Flare<=0)
        {
            stable?.Dispose();stable=Bake(device,Decoded,fused);stableBrightness=brightness;return stable;
        }
        return fused;
    }
    // The prior graph remains a lazy diagnostic oracle. Production draws only
    // one four-input shader, without allocating the seven-node reference graph.
    internal ICanvasImage ReferenceView(LightSample sample,double brightness)=>(reference??=new(lit,boosted,halo,dark)).View(sample,brightness);
    sealed class ReferenceGraph : IDisposable
    {
        readonly ArithmeticCompositeEffect brightnessMix,flareMix,finalMix;
        readonly BlendEffect screen;
        readonly ColorMatrixEffect level;
        readonly PixelShaderEffect exposure;
        public ReferenceGraph(CanvasBitmap lit,CanvasBitmap boosted,CanvasBitmap halo,CanvasBitmap dark)
        {
            brightnessMix=new(){Source1=lit,Source2=boosted,MultiplyAmount=0};
            screen=new(){Background=brightnessMix,Foreground=halo,Mode=BlendEffectMode.Screen};
            flareMix=new(){Source1=brightnessMix,Source2=screen,MultiplyAmount=0};
            level=new(){Source=flareMix,ColorMatrix=Matrix(1)};
            exposure=new(exposureCode){Source1=level};
            finalMix=new(){Source1=dark,Source2=exposure,MultiplyAmount=0};
        }
        public ICanvasImage View(LightSample sample,double brightness)
        {
            var mix=(float)LightResponse.BrightnessMix(brightness);brightnessMix.Source1Amount=1-mix;brightnessMix.Source2Amount=mix;
            var flare=(float)LightResponse.GlowMix(sample.Flare,brightness);flareMix.Source1Amount=1-flare;flareMix.Source2Amount=flare;
            level.ColorMatrix=Matrix((float)LightResponse.Level(brightness));exposure.Properties["lampGain"]=(float)Math.Max(1,LightResponse.Power(brightness));exposure.Properties["exposureStops"]=(float)LightResponse.Exposure(sample.Flare,brightness);
            finalMix.Source1Amount=1-(float)sample.Transmission;finalMix.Source2Amount=(float)sample.Transmission;return finalMix;
        }
        public void Dispose(){brightnessMix.Dispose();flareMix.Dispose();finalMix.Dispose();screen.Dispose();level.Dispose();exposure.Dispose();}
    }
    internal static Matrix5x4 Matrix(float scale,float bias=0)=>new(){M11=scale,M22=scale,M33=scale,M44=1,M51=bias,M52=bias,M53=bias};
    static CanvasRenderTarget Bake(CanvasDevice device,DecodedPhoto photo,ICanvasImage graph)
    {
        var target=new CanvasRenderTarget(device,photo.Width,photo.Height,96);
        try{using var ds=target.CreateDrawingSession();ds.Clear(Microsoft.UI.Colors.Transparent);ds.DrawImage(graph);return target;}catch{target.Dispose();throw;}
    }
    public void Dispose(){Source.Dispose();dark.Dispose();lit.Dispose();boosted.Dispose();halo.Dispose();stable?.Dispose();fused.Dispose();reference?.Dispose();}
}

sealed class OpticalTables : IDisposable
{
    static readonly byte[] highlightData=TableData((r,g,b)=>Mask(r,g,b,.60,.92,false));
    static readonly byte[] switchData=TableData((r,g,b)=>Mask(r,g,b,.50,.90,false));
    static readonly byte[] blackData=TableData((r,g,b)=>Mask(r,g,b,.004,.075,true));
    public EffectTransferTable3D Highlights {get;}
    public EffectTransferTable3D SwitchHighlights {get;}
    public EffectTransferTable3D BlackProtection {get;}
    public OpticalTables(CanvasDevice device)
    {
        Highlights=EffectTransferTable3D.CreateFromBytes(device,highlightData,32,32,32,DirectXPixelFormat.R32G32B32A32Float);
        SwitchHighlights=EffectTransferTable3D.CreateFromBytes(device,switchData,32,32,32,DirectXPixelFormat.R32G32B32A32Float);
        BlackProtection=EffectTransferTable3D.CreateFromBytes(device,blackData,32,32,32,DirectXPixelFormat.R32G32B32A32Float);
    }
    static Vector4 Mask(double r,double g,double b,double low,double high,bool monochrome)
    {
        var mask=Geometry.Smooth((SrgbOptics.Luminance(r,g,b)-low)/(high-low));
        return monochrome?new((float)mask,(float)mask,(float)mask,1):new((float)(r*mask),(float)(g*mask),(float)(b*mask),1);
    }
    static byte[] TableData(Func<double,double,double,Vector4> transform)
    {
        const int side=32;var values=new float[side*side*side*4];int i=0;
        for(int r=0;r<side;r++)for(int g=0;g<side;g++)for(int b=0;b<side;b++)
        {var v=transform(r/31d,g/31d,b/31d);values[i++]=v.X;values[i++]=v.Y;values[i++]=v.Z;values[i++]=v.W;}
        var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);
        return bytes;
    }
    public void Dispose(){Highlights.Dispose();SwitchHighlights.Dispose();BlackProtection.Dispose();}
}

// A generation may close while background builds are still finishing. Leases
// keep immutable lookup tables alive for those builds, without pinning devices
// after the last request. Cached uploads do not instantiate lookup tables.
static class OpticalTablePool
{
    internal sealed class Entry(CanvasDevice device){public readonly Lazy<OpticalTables> Value=new(()=>new(device));public int References;}
    static readonly object gate=new();
    static readonly Dictionary<CanvasDevice,Entry> entries=new(ReferenceEqualityComparer.Instance);
    public sealed class Lease : IDisposable
    {
        readonly Entry entry;readonly CanvasDevice device;bool disposed;
        internal Lease(CanvasDevice device,Entry entry){this.device=device;this.entry=entry;}
        public OpticalTables Tables=>entry.Value.Value;
        public void Dispose()
        {
            lock(gate){if(disposed)return;disposed=true;if(--entry.References!=0)return;entries.Remove(device);}
            if(entry.Value.IsValueCreated)entry.Value.Value.Dispose();
        }
    }
    public static Lease Acquire(CanvasDevice device)
    {
        lock(gate){if(!entries.TryGetValue(device,out var entry)){entry=new(device);entries.Add(device,entry);}entry.References++;return new(device,entry);}
    }
}
