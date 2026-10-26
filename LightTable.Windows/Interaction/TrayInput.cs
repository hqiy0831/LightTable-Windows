using LightTable.Core;
using LightTable.Windows.Rendering;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Geometry=LightTable.Core.Geometry;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    TrayItem? pullingItem;
    PointD? pullStart,pullPointer;
    RectD pullOrigin;
    bool finishingFlight;
    void CancelTrayFlight()
    {
        if(finishingFlight)return;
        var item=pullingItem;pullingItem=null;pullStart=pullPointer=null;tray.ReleasePointerCaptures();
        if(item!=null&&renderer.Flight!=null)_=FinishFlight(item,false);
    }
    void SetupTrayInput()
    {
        tray.CanDragItems=false;
        tray.AddHandler(UIElement.PointerPressedEvent,new PointerEventHandler((_,e)=>
        {
            if(session.Busy||dialogOpen||albumOpen||renderer.Flight!=null)return;
            var point=e.GetCurrentPoint(root);if(!point.Properties.IsLeftButtonPressed)return;
            var element=e.OriginalSource as FrameworkElement;
            while(element!=null&&element.DataContext is not TrayItem)element=VisualTreeHelper.GetParent(element) as FrameworkElement;
            if(element?.DataContext is not TrayItem item)return;
            var container=tray.ContainerFromItem(item) as FrameworkElement;if(container==null)return;
            var image=FindTrayImage(container);if(image==null||image.ActualWidth<2||image.ActualHeight<2)return;var origin=image.TransformToVisual(root).TransformPoint(new(0,0));
            pullingItem=item;pullStart=Pt(point.Position);pullOrigin=new(origin.X,origin.Y,image.ActualWidth,image.ActualHeight);pullPointer=pullStart;
            tray.CapturePointer(e.Pointer);session.CloseViewing=false;
        }),true);
        tray.AddHandler(UIElement.PointerMovedEvent,new PointerEventHandler((_,e)=>
        {
            if(pullingItem==null||pullStart is not {} start||finishingFlight)return;
            var point=e.GetCurrentPoint(root);if(!point.Properties.IsLeftButtonPressed)return;
            var pointer=Pt(point.Position);pullPointer=pointer;
            if(renderer.Flight==null)
            {
                var d=pointer-start;if(Math.Sqrt(d.X*d.X+d.Y*d.Y)<3)return;
                renderer.Flight=new(){Path=pullingItem.Path,AlbumId=pullingItem.AlbumId,Origin=pullOrigin,Start=start,Frame=pullOrigin};
                _=pullingItem.AlbumId==null?renderer.PrepareFlight(pullingItem.Path):Task.CompletedTask;
            }
            var flight=renderer.Flight;
            var target=FlightTarget(pullingItem);
            flight.Expansion=SleeveMotion.Expansion(flight.Origin,pointer,start);
            flight.Frame=SleeveMotion.Frame(flight.Origin,pointer,start,new(target.Width,target.Height));
            var distance=pointer-start;flight.Lift=session.ReduceMotion?0:Geometry.Smooth(Math.Sqrt(distance.X*distance.X+distance.Y*distance.Y)/28);
            var angle=pullingItem.AlbumId is {} id?session.Document.Albums.Find(a=>a.Id==id)?.Angle??0:session.Document.Slides.Find(s=>s.Path==pullingItem.Path)?.Angle??0;
            flight.Angle=angle*flight.Expansion+(session.ReduceMotion?0:Math.Clamp(distance.X/40,-3,3)*(1-flight.Expansion));
            lensOverlay.Invalidate();board.Invalidate();e.Handled=true;
        }),true);
        tray.AddHandler(UIElement.PointerReleasedEvent,new PointerEventHandler(async(_,e)=>
        {
            if(pullingItem==null)return;
            var item=pullingItem;var pointer=Pt(e.GetCurrentPoint(root).Position);pullingItem=null;pullStart=null;pullPointer=null;tray.ReleasePointerCaptures();
            if(renderer.Flight==null)return;
            e.Handled=true;var boardRect=new RectD(renderer.BoardOrigin.X,renderer.BoardOrigin.Y,session.Width,session.Height);
            await FinishFlight(item,boardRect.Contains(pointer));
        }),true);
        tray.PointerCaptureLost+=async(_,_)=>
        {
            if(finishingFlight||pullingItem==null)return;var item=pullingItem;pullingItem=null;pullStart=pullPointer=null;
            if(renderer.Flight!=null)await FinishFlight(item,false);
        };
    }
    Placement FlightTarget(TrayItem item)
    {
        if(item.AlbumId is {} id&&session.Document.Albums.Find(a=>a.Id==id) is {} album)
            return new(new(0,0),620*session.Document.Viewport.Scale*album.Scale,690*session.Document.Viewport.Scale*album.Scale,album.Angle);
        var s=session.Document.Slides.Find(s=>s.Path==item.Path)??new Slide{Path=item.Path};return Geometry.Place(session.Document,s);
    }
    async Task FinishFlight(TrayItem item,bool landing)
    {
        if(renderer.Flight is not {} flight||finishingFlight)return;finishingFlight=true;
        var from=flight.Frame;var expansion=flight.Expansion;var lift=flight.Lift;var angle=flight.Angle;
        var target=FlightTarget(item);var boardCenter=from.Center-renderer.BoardOrigin;
        var to=landing?new RectD(from.Center.X-target.Width/2,from.Center.Y-target.Height/2,target.Width,target.Height):flight.Origin;
        flight.Phase=landing?FlightPhase.Landing:FlightPhase.Returning;
        var duration=session.ReduceMotion?0:landing?.22:.26;var started=Rendering.TableRenderer.Seconds;
        do
        {
            var p=duration==0?1:Math.Clamp((Rendering.TableRenderer.Seconds-started)/duration,0,1);
            var t=landing?ProjectionMotion.Bezier(p,0,0,.58,1):Geometry.Smooth(p);
            flight.Frame=SleeveMotion.Interpolate(from,to,t);flight.Expansion=expansion+((landing?1:0)-expansion)*t;flight.Lift=lift*(1-t);flight.Angle=angle+((landing?target.Angle:0)-angle)*t;
            lensOverlay.Invalidate();if(p>=1)break;await Task.Delay(16);
        }while(!closed&&renderer.Flight?.Id==flight.Id);
        if(!closed&&renderer.Flight?.Id==flight.Id)
        {
            renderer.Flight=null;
            if(landing){if(item.AlbumId is {} id&&session.Document.Albums.Find(a=>a.Id==id) is {} album)session.PlaceAlbum(album,boardCenter);else session.Place(item.Path,boardCenter);}
            board.Invalidate();lensOverlay.Invalidate();
        }
        finishingFlight=false;
    }
    void TickFlight(double seconds)
    {
        if(renderer.Flight?.Phase!=FlightPhase.Dragging||pullPointer is not {} pointer)return;
        var at=pointer-renderer.BoardOrigin;
        if(!new RectD(0,0,session.Width,session.Height).Contains(at))return;
        var v=SleeveMotion.EdgeVelocity(at,new(session.Width,session.Height));
        if(v.X==0&&v.Y==0)return;
        session.Document.Viewport.OffsetX+=v.X*seconds;session.Document.Viewport.OffsetY+=v.Y*seconds;session.Notify();
    }
}
