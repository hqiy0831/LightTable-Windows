using LightTable.Core;
using LightTable.Windows.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using global::Windows.Foundation;
using global::Windows.Graphics.DirectX;

namespace LightTable.Windows.Rendering;

sealed record ProjectionMasks(byte[] Alpha,byte[] Illumination)
{
    public static ProjectionMasks Create(int width,int height,CancellationToken cancellation)
    {
        var alpha=new byte[checked(width*height*4)];var illumination=new byte[alpha.Length];var longest=Math.Max(width,height);
        double feather=Math.Max(1.5,longest*.008),radius=Math.Max(2,longest*.012);
        for(int y=0;y<height;y++)
        {
            if(y%64==0)cancellation.ThrowIfCancellationRequested();
            for(int x=0;x<width;x++)
            {
                double dx=Math.Min(x+.5,width-x-.5),dy=Math.Min(y+.5,height-y-.5);double nx=(x+.5)*2/width-1,ny=(y+.5)*2/height-1;
                var falloff=(byte)Math.Round((1-.13*Geometry.Smooth((nx*nx+ny*ny-.55)/1.45))*255);
                double distance=Math.Min(dx,dy);if(dx<radius&&dy<radius)distance=radius-Math.Sqrt((radius-dx)*(radius-dx)+(radius-dy)*(radius-dy));
                var a=(byte)Math.Round(Geometry.Smooth((distance-.5)/feather)*255);int i=(y*width+x)*4;
                alpha[i]=alpha[i+1]=alpha[i+2]=alpha[i+3]=a;
                illumination[i]=illumination[i+1]=illumination[i+2]=falloff;illumination[i+3]=255;
            }
        }
        return new(alpha,illumination);
    }
}
sealed class ProjectionPhoto : IDisposable
{
    public string Key {get;}
    public string Path {get;}
    public CanvasRenderTarget Sharp {get;}
    public CanvasRenderTarget Soft {get;}
    public CanvasRenderTarget Glow {get;}
    public double GlowOutset {get;}
    public int Leases {get;set;}
    public long Used {get;set;}
    public double Aspect=>Sharp.Size.Width/Sharp.Size.Height;
    public ProjectionPhoto(CanvasDevice device,DecodedPhoto decoded,OpticalPhoto optical,ProjectionMasks masks)
    {
        Key=decoded.Key;Path=decoded.Path;int width=decoded.Width,height=decoded.Height,longest=Math.Max(width,height);
        // The upstream projector uses preview.lit, not the raw source preview.
        using var mask=CanvasBitmap.CreateFromBytes(device,masks.Alpha,width,height,DirectXPixelFormat.B8G8R8A8UIntNormalized,96,CanvasAlphaMode.Premultiplied);
        using var vignette=CanvasBitmap.CreateFromBytes(device,masks.Illumination,width,height,DirectXPixelFormat.B8G8R8A8UIntNormalized,96,CanvasAlphaMode.Premultiplied);
        using var attenuated=new BlendEffect{Mode=BlendEffectMode.Multiply,Background=optical.Lit,Foreground=vignette};
        using var blur=new GaussianBlurEffect{Source=attenuated,BlurAmount=Math.Max(.8f,longest*.0012f),BorderMode=EffectBorderMode.Hard};
        using var sharpGraph=new AlphaMaskEffect{Source=attenuated,AlphaMask=mask};
        using var softGraph=new AlphaMaskEffect{Source=blur,AlphaMask=mask};
        Sharp=new CanvasRenderTarget(device,width,height,96);Soft=new CanvasRenderTarget(device,width,height,96);
        using(var ds=Sharp.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(sharpGraph);}
        using(var ds=Soft.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(softGraph);}
        var scale=Math.Min(1,500d/longest);var smallW=Math.Max(1,(int)Math.Round(width*scale));var smallH=Math.Max(1,(int)Math.Round(height*scale));var padding=(int)Math.Ceiling(longest*scale*.10);
        using var small=new CanvasRenderTarget(device,smallW,smallH,96);
        using(var ds=small.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(Sharp,new Rect(0,0,smallW,smallH),new Rect(0,0,width,height));}
        Glow=new CanvasRenderTarget(device,smallW+2*padding,smallH+2*padding,96);
        using(var glowGraph=new GaussianBlurEffect{Source=small,BlurAmount=Math.Max(1,(float)(longest*scale*.025))})
        using(var ds=Glow.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(glowGraph,padding,padding);}
        GlowOutset=padding/(longest*scale);
    }
    public void Dispose(){Sharp.Dispose();Soft.Dispose();Glow.Dispose();}
}
sealed class ProjectionLease(ProjectionPhoto photo,Action released) : IDisposable
{
    bool disposed;
    public ProjectionPhoto Photo {get;}=photo;
    public void Dispose(){if(disposed)return;disposed=true;Photo.Leases--;released();}
}
sealed class ProjectionStore : IDisposable
{
    readonly ImageStore images;
    readonly CanvasDevice device;
    readonly Action? invalidated;
    readonly Action<string>? log;
    readonly Dictionary<string,ProjectionPhoto> cache=[];
    readonly Dictionary<string,Task<ProjectionPhoto>> pending=[];
    readonly Dictionary<string,int> waiting=[];
    readonly SemaphoreSlim maskLane=new(2);
    bool disposed;long sequence;
    public int Count=>cache.Count;
    public ProjectionStore(ImageStore images,CanvasDevice device,Action? invalidated=null,Action<string>? log=null){this.images=images;this.device=device;this.invalidated=invalidated;this.log=log;}
    public async Task<ProjectionLease> GetAsync(string path,CancellationToken cancellation=default)
    {
        ObjectDisposedException.ThrowIf(disposed,this);cancellation.ThrowIfCancellationRequested();var key=ImageStore.SourceKey(path,2200);
        if(!cache.TryGetValue(key,out var photo))
        {
            waiting[key]=waiting.GetValueOrDefault(key)+1;
            try
            {
                if(!pending.TryGetValue(key,out var task))
                {
                    var completion=new TaskCompletionSource<ProjectionPhoto>(TaskCreationOptions.RunContinuationsAsynchronously);pending[key]=completion.Task;
                    _=completion.Task.ContinueWith(t=>{_=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
                    _=Build(path,key,completion);task=completion.Task;
                }
                photo=await task.WaitAsync(cancellation);
            }
            finally{if(waiting[key]==1)waiting.Remove(key);else waiting[key]--;}
        }
        ObjectDisposedException.ThrowIf(disposed,this);photo.Used=++sequence;photo.Leases++;Trim();return new(photo,Trim);
    }
    async Task Build(string path,string key,TaskCompletionSource<ProjectionPhoto> completion)
    {
        await maskLane.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(disposed,this);using var optical=await OpticalPhoto.CreateAsync(device,images,path);
            if(optical.Decoded.Key!=key)throw new IOException("投影片在读取过程中发生变化，请重试");
            var decoded=optical.Decoded;var masks=await Task.Run(()=>ProjectionMasks.Create(decoded.Width,decoded.Height,CancellationToken.None));
            ObjectDisposedException.ThrowIf(disposed,this);var photo=new ProjectionPhoto(device,decoded,optical,masks){Used=++sequence};cache[key]=photo;completion.TrySetResult(photo);Trim();
        }
        catch(Exception ex){if(!disposed)GraphicsRecovery.Handle(device,ex,()=>invalidated?.Invoke(),log);completion.TrySetException(ex);}
        finally{maskLane.Release();pending.Remove(key);}
    }
    void Trim()
    {
        if(disposed)return;
        while(cache.Count>3)
        {
            var victim=cache.Values.Where(p=>p.Leases==0&&!waiting.ContainsKey(p.Key)).MinBy(p=>p.Used);if(victim==null)break;cache.Remove(victim.Key);victim.Dispose();
        }
    }
    public void Dispose(){if(disposed)return;disposed=true;foreach(var photo in cache.Values)photo.Dispose();cache.Clear();}
}
