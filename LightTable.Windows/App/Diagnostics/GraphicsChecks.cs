using System.Text.Json;
using LightTable.Core;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    readonly List<CanvasDevice> diagnosticDevices=[];
    async Task<string> CheckGraphicsRecreation()
    {
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        var documentBefore=JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions);var paths=session.Document.PlacedPaths.ToArray();var photos=session.Document.Photos.Take(3).Select((p,i)=>new CollectionPhoto(p.Path,new(40+i*110,460,90,56))).ToArray();
        // CustomDevice is retained by Win2D controls through their asynchronous
        // unload. Closing it here invalidates those controls before EndDraw.
        // Drop our references; the controls release the device after unloading.
        Closed+=(_,_)=>diagnosticDevices.Clear();
        collectionOverlay.Visibility=Visibility.Visible;await collectionRenderer.PrepareAsync(photos);
        var firstPath=session.Document.Photos[0].Path;ProjectionStore? projection=new(session.Images,board.Device);var cues=new List<bool>();
        var playback=new ProjectionPlayback<ProjectionLease>((path,cancellation)=>projection.GetAsync(path,cancellation),cues.Add);playback.BeginPresentation();await playback.ShowAsync(firstPath,true);
        var uploadReached=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var releaseUpload=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);bool held=false;
        renderer.BeforeUpload=(_,bucket)=>{if(bucket==1399&&!held){held=true;uploadReached.TrySetResult();return releaseUpload.Task;}return Task.CompletedTask;};
        var oldLoading=renderer.PrepareAsync(firstPath,1399);await uploadReached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var replacements=new List<object>();
        try
        {
            foreach(bool software in new[]{false,true})
            {
                var priorVersion=renderer.DeviceGeneration;var resources=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Recreated(){if(renderer.DeviceGeneration>priorVersion)resources.TrySetResult();}renderer.ResourceReady+=Recreated;
                var next=new CanvasDevice(software);diagnosticDevices.Add(next);
                try
                {
                    playback.InvalidateResources();projection.Dispose();projection=new(session.Images,next);
                    board.CustomDevice=next;lensOverlay.CustomDevice=next;collectionOverlay.CustomDevice=next;board.Invalidate();lensOverlay.Invalidate();collectionOverlay.Invalidate();
                    await resources.Task.WaitAsync(TimeSpan.FromSeconds(10));await renderer.DeviceReady.WaitAsync(TimeSpan.FromSeconds(10));await collectionRenderer.DeviceReady.WaitAsync(TimeSpan.FromSeconds(10));
                    releaseUpload.TrySetResult();await oldLoading.WaitAsync(TimeSpan.FromSeconds(10));foreach(var path in paths)await renderer.PrepareAsync(path,2200);
                    await playback.RestoreAsync();Require(playback.Phase==ProjectionPhase.Settled&&cues.SequenceEqual(new[]{true}),"设备重建重复机械入场或声音cue");
                    Require(collectionRenderer.CardCount==photos.Length,"设备替换丢失已捕获收片缩略图");using var collection=collectionRenderer.Snapshot(new(960,540),.5);
                    Require(collection.GetPixelColors().Any(c=>c.R+c.G+c.B>100),"新设备收片快照没有实际图像");
                    using var snapshot=renderer.RenderSnapshot(true,0,true);Require(snapshot.GetPixelColors().Any(c=>c.R+c.G+c.B<250),"新设备台面只有灯箱背景");
                    await snapshot.SaveAsync(Path.Combine(session.Storage.Root,"logs",software?"recreated-warp.png":"recreated-hardware.png"),CanvasBitmapFileFormat.Png);
                    Require(!renderer.HandleDeviceError(new IOException("ordinary IO failure")),"普通IO错误被误判为设备丢失");
                    replacements.Add(new{Software=software,Before=priorVersion,After=renderer.DeviceGeneration,Cards=collectionRenderer.CardCount,Cues=cues.Count});
                }
                finally{renderer.ResourceReady-=Recreated;}
            }
            Require(JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions)==documentBefore,"设备替换改变布局");
            return "真实控件CustomDevice先换新硬件设备、再换WARP；迟到GPU上传跨代重试成功、台面/收片重新绘制、幻灯源重建且不重播声音、布局不变。"+JsonSerializer.Serialize(replacements)+"。这是应用设备替换，未禁用显卡，不能代替真实驱动移除。";
        }
        finally{releaseUpload.TrySetResult();renderer.BeforeUpload=null;playback.Dispose();projection?.Dispose();collectionRenderer.Clear();collectionOverlay.Visibility=Visibility.Collapsed;}
    }
    async Task<string> CheckControlDpi()
    {
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        var documentBefore=JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions);var originalScale=board.DpiScale;var lensScale=lensOverlay.DpiScale;var evidence=new List<object>();
        var slide=session.Document.Slides[0];var center=Geometry.Place(session.Document,slide).Center;var expectedHit=renderer.Hit(center);renderer.Pointer=center;
        try
        {
            foreach(var scale in new[]{1.25f,1.5f,2f})
            {
                var before=renderer.DeviceGeneration;var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Changed(CanvasControl sender,CanvasCreateResourcesEventArgs args){if(args.Reason==CanvasCreateResourcesReason.DpiChanged)ready.TrySetResult();}board.CreateResources+=Changed;
                try
                {
                    board.DpiScale=originalScale*scale;lensOverlay.DpiScale=lensScale*scale;board.Invalidate();lensOverlay.Invalidate();await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));await renderer.DeviceReady;
                    Require(renderer.DeviceGeneration==before,"只变DPI却废弃了静态GPU资源代次");Require(renderer.Hit(center)==expectedHit,"DPI变化改变了DIP命中坐标");
                    Require(renderer.Pointer==center,"诊断指针被实时鼠标输入改变");using var lens=renderer.RenderLensSnapshot(0,0);var origin=center+renderer.BoardOrigin;var pixel=lens.GetPixelColors((int)origin.X,(int)origin.Y,1,1)[0];Require(pixel.A==255,$"高DPI镜片中心不透明性丢失：scale={scale},alpha={pixel.A},pointer={renderer.Pointer},origin={origin}");
                    await lens.SaveAsync(Path.Combine(session.Storage.Root,"logs",$"control-dpi-{scale:F2}.png"),CanvasBitmapFileFormat.Png);evidence.Add(new{Scale=scale,board.Dpi,Generation=renderer.DeviceGeneration});
                }
                finally{board.CreateResources-=Changed;}
            }
            Require(JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions)==documentBefore,"DPI变化改变布局");return "真实CanvasControl.DpiScale=125/150/200%事件、GPU镜片中心、DIP命中与静态资源保留通过："+JsonSerializer.Serialize(evidence)+"。不等于系统显示缩放或跨显示器输入验收。";
        }
        finally{board.DpiScale=originalScale;lensOverlay.DpiScale=lensScale;board.Invalidate();lensOverlay.Invalidate();}
    }
}
