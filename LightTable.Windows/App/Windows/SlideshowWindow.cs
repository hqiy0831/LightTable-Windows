using System.Numerics;
using LightTable.Core;
using LightTable.Windows.Audio;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using global::Windows.Foundation;
using global::Windows.Graphics;
using Windows.System;

namespace LightTable.Windows;

sealed class SlideshowWindow : Window
{
    readonly TableSession session;
    readonly CanvasControl canvas=new();
    readonly TextBlock hint=new(){Foreground=new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.LightGray),TextWrapping=TextWrapping.Wrap,MaxWidth=600,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
    readonly StackPanel recovery=new(){Orientation=Orientation.Horizontal,Spacing=18,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Margin=new(0,70,0,0),Visibility=Visibility.Collapsed};
    readonly ProgressRing loading=new(){IsActive=true,Width=24,Height=24,Visibility=Visibility.Collapsed,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
    readonly MechanicalAudio sound;
    readonly List<string> order;
    readonly Microsoft.UI.Dispatching.DispatcherQueueTimer timer;
    ProjectionStore? store;
    ProjectionPlayback<ProjectionLease>? playback;
    CanvasDevice? resourceDevice;
    int index;
    bool closed;
    readonly HashSet<VirtualKey> held=[];
    public SlideshowWindow(TableSession session,PointInt32 parentPosition,Action done)
    {
        this.session=session;sound=new(session.Storage.Root,session.Storage.Log);order=session.Document.ActivePhotos.Select(p=>p.Path).ToList();Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(order));
        Title="观片台 · 机械幻灯机";
        var grid=new Grid{Background=new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Black)};grid.Children.Add(canvas);
        var back=new Button{Content="返回观片台",Margin=new(20),CornerRadius=new(19),Height=38,Padding=new(17,0,17,0),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};back.Click+=(_,_)=>Close();grid.Children.Add(back);grid.Children.Add(hint);grid.Children.Add(recovery);grid.Children.Add(loading);Content=grid;
        var retry=new Button{Content="重试"};retry.Click+=async(_,_)=>await Show(index);recovery.Children.Add(retry);
        var next=new Button{Content="下一张"};next.Click+=async(_,_)=>await Navigate(1);recovery.Children.Add(next);
        AppWindow.Move(new(parentPosition.X+40,parentPosition.Y+40));AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        canvas.Draw+=(_,e)=>Render(e.DrawingSession,new(canvas.ActualWidth,canvas.ActualHeight));
        canvas.CreateResources+=CreateResources;
        grid.AddHandler(UIElement.KeyDownEvent,new KeyEventHandler((_,e)=>
        {
            if(e.Key==VirtualKey.Escape){if(held.Add(e.Key))Close();e.Handled=true;}
            else if(e.Key is VirtualKey.Right or VirtualKey.Down){if(held.Add(e.Key))_=Navigate(1);e.Handled=true;}
            else if(e.Key is VirtualKey.Left or VirtualKey.Up){if(held.Add(e.Key))_=Navigate(-1);e.Handled=true;}
        }),true);
        grid.AddHandler(UIElement.KeyUpEvent,new KeyEventHandler((_,e)=>held.Remove(e.Key)),true);
        Activated+=(_,e)=>{if(e.WindowActivationState==WindowActivationState.Deactivated)held.Clear();};
        timer=DispatcherQueue.CreateTimer();timer.Interval=TimeSpan.FromMilliseconds(16);timer.Tick+=(_,_)=>{if(playback?.Phase is ProjectionPhase.Arriving or ProjectionPhase.Departing)canvas.Invalidate();};timer.Start();
        Closed+=(_,_)=>{closed=true;timer.Stop();playback?.Dispose();store?.Dispose();sound.Dispose();canvas.RemoveFromVisualTree();done();};
    }
    void CreateResources(CanvasControl sender,CanvasCreateResourcesEventArgs args)
    {
        if(closed)return;
        if(args.Reason==CanvasCreateResourcesReason.DpiChanged&&ReferenceEquals(resourceDevice,sender.Device)&&store!=null){canvas.Invalidate();return;}
        bool restoring=playback!=null;playback?.InvalidateResources();store?.Dispose();resourceDevice=sender.Device;
        store=new(session.Images,resourceDevice,()=>playback?.InvalidateResources(),session.Storage.Log);
        if(playback==null)
        {
            playback=new((path,cancellation)=>store.GetAsync(path,cancellation),sound.Play,sound.Stop,sound.PrepareAsync);
            playback.Changed+=Update;playback.BeginPresentation();
        }
        if(restoring)_=playback.RestoreAsync(session.ReduceMotion);else _=Show(index);
    }
    public Task Navigate(int delta)=>order.Count==0?Task.CompletedTask:Show((index+delta+order.Count)%order.Count);
    async Task Show(int desired)
    {
        if(closed||playback==null||order.Count==0)return;index=desired;var owner=playback;var requested=order[index];await owner.ShowAsync(requested,session.ReduceMotion);
        if(closed||owner!=playback||owner.RequestedPath!=requested||order[index]!=requested)return;
        if(playback.Error!=null)session.Storage.Log($"幻灯图片失败 {requested}: {playback.Error}");
        else if(store!=null&&order.Count>1)_=Preload(order[(index+1)%order.Count]);
    }
    async Task Preload(string path)
    {
        try{if(store!=null){using var lease=await store.GetAsync(path);}}
        catch(Exception ex){if(!closed)session.Storage.Log($"幻灯预读失败 {path}: {ex.Message}");}
    }
    void Update()
    {
        if(closed)return;hint.Text=playback?.Error!=null?"这张照片暂时无法读取":string.Empty;
        recovery.Visibility=playback?.Error!=null?Visibility.Visible:Visibility.Collapsed;loading.Visibility=playback?.LoadingHint==true?Visibility.Visible:Visibility.Collapsed;canvas.Invalidate();
    }
    void Render(CanvasDrawingSession ds,SizeD size)
    {
        ds.Clear(Colors.Black);if(playback?.Photo is {} lease)ProjectionRenderer.Draw(ds,lease.Photo,playback.Frame,size);
    }
}
