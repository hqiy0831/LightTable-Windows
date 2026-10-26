using System.Numerics;
using LightTable.Core;
using LightTable.Windows.Imaging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.UI;
using global::Windows.Foundation;
using global::Windows.UI;

namespace LightTable.Windows.Rendering;

sealed record CollectionPhoto(string Path,RectD Frame);
sealed class CollectionRenderer : IDisposable
{
    sealed record Card(CollectionPhoto Photo,CanvasRenderTarget Bitmap,Transform3DEffect Transform) : IDisposable
    {public void Dispose(){Transform.Dispose();Bitmap.Dispose();}}
    readonly CanvasControl canvas;
    readonly ImageStore images;
    List<Card> cards=[];
    readonly ResourceEpoch epoch=new();
    IReadOnlyList<CollectionPhoto> requested=[];
    CanvasDevice? resourceDevice;
    double elapsed;bool disposed;
    public Task DeviceReady=>WaitReady(CancellationToken.None);
    public long DeviceGeneration=>epoch.Version;
    public int CardCount=>cards.Count;
    public event Action? Started;
    public CollectionRenderer(CanvasControl canvas,ImageStore images)
    {
        this.canvas=canvas;this.images=images;canvas.CreateResources+=CreateResources;canvas.Draw+=Draw;
    }
    void CreateResources(CanvasControl sender,CanvasCreateResourcesEventArgs args)
    {
        if(disposed)return;epoch.Begin();ClearCards();
        if(resourceDevice!=null)resourceDevice.DeviceLost-=DeviceLost;resourceDevice=sender.Device;resourceDevice.DeviceLost+=DeviceLost;
        _=Build(epoch.Capture(),resourceDevice,requested.ToArray());
    }
    void DeviceLost(CanvasDevice sender,object args)=>canvas.DispatcherQueue.TryEnqueue(()=>{if(!disposed&&ReferenceEquals(resourceDevice,sender))InvalidateDevice();});
    void InvalidateDevice()
    {
        if(disposed)return;epoch.Begin();ClearCards();if(resourceDevice!=null)resourceDevice.DeviceLost-=DeviceLost;resourceDevice=null;canvas.Invalidate();
    }
    async Task WaitReady(CancellationToken cancellation)
    {
        while(!disposed)
        {
            cancellation.ThrowIfCancellationRequested();var ticket=epoch.Capture();
            try{await ticket.Ready.WaitAsync(cancellation);if(epoch.IsCurrent(ticket)&&resourceDevice!=null&&ReferenceEquals(resourceDevice,canvas.Device))return;await Task.Delay(Timeout.Infinite,ticket.Cancellation).WaitAsync(cancellation);}
            catch(OperationCanceledException)when(!cancellation.IsCancellationRequested&&!epoch.IsCurrent(ticket)){}
        }
        throw new ObjectDisposedException(nameof(CollectionRenderer));
    }
    public async Task PrepareAsync(IReadOnlyList<CollectionPhoto> photos,CancellationToken cancellation=default)
    {
        ObjectDisposedException.ThrowIf(disposed,this);requested=photos.ToArray();
        if(resourceDevice!=null)
        {
            epoch.Begin();ClearCards();_=Build(epoch.Capture(),resourceDevice,requested.ToArray());
        }
        await WaitReady(cancellation);
    }
    async Task Build(ResourceTicket ticket,CanvasDevice device,IReadOnlyList<CollectionPhoto> photos)
    {
        var prepared=new List<Card>();
        try
        {
            foreach(var photo in photos)
            {
                var decoded=await images.GetAsync(photo.Path,220).WaitAsync(ticket.Cancellation);if(!epoch.IsCurrent(ticket))return;
                using var source=CanvasBitmap.CreateFromBytes(device,decoded.Pixels,decoded.Width,decoded.Height,global::Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,96,CanvasAlphaMode.Premultiplied);
                var bitmap=new CanvasRenderTarget(device,(float)photo.Frame.Width,(float)photo.Frame.Height,canvas.Dpi);
                try
                {
                    using(var ds=bitmap.CreateDrawingSession())
                    {
                        ds.Clear(Color.FromArgb(255,14,14,14));var w=Math.Min(photo.Frame.Width,photo.Frame.Height*decoded.Aspect);var h=w/decoded.Aspect;
                        ds.DrawImage(source,new Rect((photo.Frame.Width-w)/2,(photo.Frame.Height-h)/2,w,h),new Rect(0,0,decoded.Width,decoded.Height));
                        ds.DrawRoundedRectangle(new Rect(.25,.25,photo.Frame.Width-.5,photo.Frame.Height-.5),1.5f,1.5f,Color.FromArgb(36,255,255,255),.5f);
                    }
                    prepared.Add(new(photo,bitmap,new Transform3DEffect{Source=bitmap,InterpolationMode=CanvasImageInterpolation.Linear}));
                }
                catch{bitmap.Dispose();throw;}
            }
            if(!epoch.IsCurrent(ticket))return;cards=prepared;prepared=[];epoch.Complete(ticket.Version);canvas.Invalidate();
        }
        catch(OperationCanceledException)when(!epoch.IsCurrent(ticket)){}
        catch(Exception ex){if(epoch.IsCurrent(ticket)&&!GraphicsRecovery.Handle(device,ex,InvalidateDevice))epoch.Fail(ticket.Version,ex);}
        finally{foreach(var card in prepared)card.Dispose();}
    }
    public async Task PlayAsync(bool reducedMotion,CancellationToken cancellation=default)
    {
        if(reducedMotion||requested.Count==0)return;
        elapsed=0;Started?.Invoke();var previous=TableRenderer.Seconds;
        do
        {
            cancellation.ThrowIfCancellationRequested();
            if(!epoch.Capture().Ready.IsCompletedSuccessfully){await WaitReady(cancellation);previous=TableRenderer.Seconds;}
            var now=TableRenderer.Seconds;elapsed+=now-previous;previous=now;canvas.Invalidate();
            if(elapsed>=CollectionMotion.Duration(requested.Count))break;await Task.Delay(16,cancellation);
        }while(!disposed);
    }
    void Draw(CanvasControl _,CanvasDrawEventArgs args){if(!disposed)Render(args.DrawingSession,new(canvas.ActualWidth,canvas.ActualHeight),elapsed);}
    public void Render(CanvasDrawingSession ds,SizeD size,double time)
    {
        ds.Clear(Colors.Transparent);if(requested.Count==0||resourceDevice==null||!ReferenceEquals(resourceDevice,ds.Device))return;
        ds.FillRectangle(new Rect(0,0,size.Width,size.Height),Color.FromArgb((byte)Math.Round(255*CollectionMotion.BlackOpacity(time,requested.Count)),0,0,0));
        var box=CollectionMotion.BoxCenter(size);var destination=box+new PointD(0,3);var alpha=CollectionMotion.BoxOpacity(time,requested.Count);
        using(ds.CreateLayer((float)alpha))DrawBox(ds,box,false);
        DrawCards(ds,destination,time,false);
        using(ds.CreateLayer((float)alpha))DrawBox(ds,box,true);
        DrawCards(ds,destination,time,true);
    }
    void DrawCards(CanvasDrawingSession ds,PointD destination,double time,bool foreground)
    {
        for(int i=0;i<cards.Count;i++)
        {
            var card=cards[i];var pose=CollectionPose.At(card.Photo.Frame,destination,i,CollectionMotion.Progress(time,i,cards.Count));
            if(foreground==pose.BehindFront||pose.Opacity<=0)continue;
            var perspective=Matrix4x4.Identity;perspective.M34=(float)(-.25/Math.Max(card.Photo.Frame.Width,card.Photo.Frame.Height));
            card.Transform.TransformMatrix=Matrix4x4.CreateTranslation(-(float)card.Bitmap.Size.Width/2,-(float)card.Bitmap.Size.Height/2,0)*
                Matrix4x4.CreateRotationZ((float)(pose.Angle*Math.PI/180))*Matrix4x4.CreateRotationX((float)(pose.Tilt*Math.PI/180))*
                Matrix4x4.CreateScale((float)pose.Scale)*perspective*Matrix4x4.CreateTranslation((float)pose.Center.X,(float)pose.Center.Y,0);
            using(ds.CreateLayer((float)pose.Opacity))ds.DrawImage(card.Transform);
        }
    }
    void DrawBox(CanvasDrawingSession ds,PointD center,bool front)
    {
        var old=ds.Transform;ds.Transform=Matrix3x2.CreateTranslation((float)center.X,(float)center.Y)*old;
        if(!front)
        {
            ds.FillRoundedRectangle(new Rect(-65,-20,130,54),5,5,Gray(.12));
            using var top=CanvasGeometry.CreatePolygon(ds,[new(-50.7f,-36.5f),new(50.7f,-36.5f),new(65,-9.5f),new(-65,-9.5f)]);
            using var gradient=new CanvasLinearGradientBrush(canvas.Device,Gray(.23),Gray(.13)){StartPoint=new(0,-36.5f),EndPoint=new(0,-9.5f)};ds.FillGeometry(top,gradient);
            for(int i=0;i<3;i++)ds.FillRoundedRectangle(new Rect(-40+i-1,-25-i*1.4,80-i*2,2),1,1,Gray(.34-i*.05));
            ds.FillRoundedRectangle(new Rect(-43,-22.5,86,5),2,2,Colors.Black);
        }
        else
        {
            using var gradient=new CanvasLinearGradientBrush(canvas.Device,Gray(.18),Gray(.085)){StartPoint=new(0,-13.5f),EndPoint=new(0,29.5f)};
            ds.FillRoundedRectangle(new Rect(-65,-13.5,130,43),4,4,gradient);ds.DrawRoundedRectangle(new Rect(-65,-13.5,130,43),4,4,Color.FromArgb(26,255,255,255),.7f);
            ds.FillRoundedRectangle(new Rect(-52.5,-2.5,105,25),2,2,Color.FromArgb(41,0,0,0));ds.DrawLine(-14,-3,14,-3,Color.FromArgb(31,255,255,255),1);
        }
        ds.Transform=old;
    }
    static Color Gray(double value)=>Color.FromArgb(255,(byte)Math.Round(value*255),(byte)Math.Round(value*255),(byte)Math.Round(value*255));
    public CanvasRenderTarget Snapshot(SizeD size,double time)
    {
        var result=new CanvasRenderTarget(canvas.Device,(float)size.Width,(float)size.Height,96);
        using(var ds=result.CreateDrawingSession())Render(ds,size,time);return result;
    }
    void ClearCards(){foreach(var card in cards)card.Dispose();cards.Clear();}
    public void Clear(){if(disposed)return;requested=[];epoch.Begin();ClearCards();if(resourceDevice!=null)epoch.Complete(epoch.Version);}
    public void Dispose(){if(disposed)return;disposed=true;canvas.CreateResources-=CreateResources;canvas.Draw-=Draw;epoch.Dispose();ClearCards();if(resourceDevice!=null)resourceDevice.DeviceLost-=DeviceLost;resourceDevice=null;}
}
