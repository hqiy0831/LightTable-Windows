using LightTable.Core;
using LightTable.Windows.Rendering;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    CancellationTokenSource? collectionCancellation;
    static Image? FindTrayImage(DependencyObject value)
    {
        if(value is Image image)return image;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(value);i++)if(FindTrayImage(VisualTreeHelper.GetChild(value,i)) is {} found)return found;return null;
    }
    IReadOnlyList<CollectionPhoto> VisibleCollectionPhotos()
    {
        var origin=tray.TransformToVisual(root).TransformPoint(new(0,0));var viewport=new RectD(origin.X,origin.Y,tray.ActualWidth,tray.ActualHeight);var photos=new List<CollectionPhoto>();
        for(int i=0;i<trayItems.Count;i++)
        {
            var item=trayItems[i];if(item.AlbumId!=null||item.Thumbnail==null||tray.ContainerFromIndex(i) is not FrameworkElement container)continue;
            var image=FindTrayImage(container);if(image==null||image.ActualWidth<2||image.ActualHeight<2)continue;
            var point=image.TransformToVisual(root).TransformPoint(new(0,0));var frame=new RectD(point.X,point.Y,image.ActualWidth,image.ActualHeight);
            if(frame.Intersects(viewport))photos.Add(new(item.Path,frame));
        }
        return photos.OrderBy(p=>p.Frame.X).ToArray();
    }
    void StartSlideshow()=>_=StartSlideshowAsync();
    async Task StartSlideshowAsync()
    {
        if(session.Document.Photos.Count==0){await ShowMessage("还没有照片","先导入照片，再开启幻灯机。");return;}
        if(slideshow!=null||collectionCancellation!=null||renderer.Flight!=null||dialogOpen||albumOpen)return;
        session.CloseViewing=false;session.Flush();collectionCancellation=new();var cancellation=collectionCancellation.Token;
        var hidden=new List<FrameworkElement>();
        try
        {
            if(!session.ReduceMotion)
            {
                var photos=VisibleCollectionPhotos();
                for(int i=0;photos.Count==0&&i<8;i++){await Task.Delay(32,cancellation);photos=VisibleCollectionPhotos();}
                if(photos.Count==0){await ShowMessage("暂时无法开始幻灯","底栏还没有可放映的照片。请滚动到照片，等待缩略图显示后重试。");return;}
                collectionOverlay.Visibility=Visibility.Visible;await collectionRenderer.PrepareAsync(photos,cancellation);
                var paths=photos.Select(p=>p.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
                for(int i=0;i<trayItems.Count;i++)if(paths.Contains(trayItems[i].Path)&&tray.ContainerFromIndex(i) is FrameworkElement item){item.Opacity=0;hidden.Add(item);}
                await collectionRenderer.PlayAsync(false,cancellation);
            }
            cancellation.ThrowIfCancellationRequested();if(closed)return;
            slideshow=new SlideshowWindow(session,AppWindow.Position,()=>{slideshow=null;if(!closed){Activate();board.Focus(FocusState.Programmatic);}});slideshow.Activate();
        }
        catch(OperationCanceledException)when(cancellation.IsCancellationRequested){}
        catch(Exception ex){session.Storage.Log($"收片准备失败 {ex}");if(!closed)await ShowMessage("暂时无法开始幻灯",ex.Message);}
        finally
        {
            if(!closed){foreach(var item in hidden)item.Opacity=1;collectionOverlay.Visibility=Visibility.Collapsed;collectionRenderer.Clear();}
            collectionCancellation?.Dispose();collectionCancellation=null;
        }
    }
}
