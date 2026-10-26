using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Media.Imaging;
using LightTable.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task ShowAlbums(Guid? selectedPage=null)
    {
        if(dialogOpen||albumOpen||root.XamlRoot==null)return;albumOpen=true;session.CloseViewing=false;
        var page=session.Document.Albums.Find(a=>a.Id==selectedPage)??session.Document.Albums.FirstOrDefault();
        Guid? pocketId=null;PointD? dragStart=null;AlbumPocket? draggedPocket=null;
        var panel=new Grid{Width=Math.Max(280,Math.Min(950,root.ActualWidth-120)),Height=Math.Max(180,Math.Min(620,root.ActualHeight-190)),ColumnSpacing=12};
        bool narrow=panel.Width<700;
        panel.ColumnDefinitions.Add(new(){Width=new(narrow?120:165)});panel.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});panel.ColumnDefinitions.Add(new(){Width=new(narrow?130:180)});
        var left=new Grid{RowSpacing=10};left.RowDefinitions.Add(new(){Height=GridLength.Auto});left.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});left.RowDefinitions.Add(new(){Height=GridLength.Auto});left.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var pages=new ListView{SelectionMode=ListViewSelectionMode.Single};left.Children.Add(new TextBlock{Text="胶片相册",FontSize=18});Grid.SetRow(pages,1);left.Children.Add(pages);
        panel.Children.Add(left);
        var middle=new Grid{RowSpacing=10};middle.RowDefinitions.Add(new(){Height=GridLength.Auto});middle.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});middle.RowDefinitions.Add(new(){Height=GridLength.Auto});Grid.SetColumn(middle,1);panel.Children.Add(middle);
        var top=new Grid{ColumnSpacing=7};top.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});var name=new TextBox{MinWidth=60,PlaceholderText="相册页名称"};top.Children.Add(name);
        var format=new ComboBox{Width=narrow?105:118,ItemsSource=new[]{"135 小格","120 大格"},SelectedIndex=0};Grid.SetColumn(format,1);top.Children.Add(format);middle.Children.Add(top);
        // The popup input surface presents actual table-device GPU snapshots.
        // Explicit Image.Source updates avoid popup CanvasControl device and
        // redraw-lifecycle differences; no independent device owns album assets.
        var preview=new Grid{AllowDrop=true,Background=new SolidColorBrush(global::Windows.UI.Color.FromArgb(255,210,204,188))};
        var previewImage=new Image{IsHitTestVisible=false,Stretch=Stretch.Fill};preview.Children.Add(previewImage);Grid.SetRow(preview,1);middle.Children.Add(preview);
        var previewTimer=DispatcherQueue.CreateTimer();previewTimer.Interval=TimeSpan.FromMilliseconds(16);previewTimer.IsRepeating=false;
        bool previewClosed=false;WriteableBitmap? previewBitmap=null;
        void InvalidatePreview(){if(!previewClosed){previewTimer.Stop();previewTimer.Start();}}
        var note=new TextBlock{Text="点空白处添加袋格；选袋格后，点右侧照片装入。拖动袋格换位。",FontSize=11,TextWrapping=TextWrapping.Wrap};Grid.SetRow(note,2);middle.Children.Add(note);
        var right=new Grid();right.RowDefinitions.Add(new(){Height=GridLength.Auto});right.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});Grid.SetColumn(right,2);panel.Children.Add(right);
        var buttons=new StackPanel{Spacing=7};right.Children.Add(buttons);var photos=new ListView{ItemsSource=session.Document.ActivePhotos,DisplayMemberPath="Name",SelectionMode=ListViewSelectionMode.Single,CanDragItems=true};Grid.SetRow(photos,1);right.Children.Add(photos);
        var dialog=new ContentDialog{Title="胶片袋相册",Content=panel,CloseButtonText="完成",XamlRoot=root.XamlRoot,MaxWidth=Math.Max(320,root.ActualWidth-32),MaxHeight=Math.Max(280,root.ActualHeight-32)};
        dialog.Resources["ContentDialogMaxWidth"]=dialog.MaxWidth;dialog.Resources["ContentDialogMaxHeight"]=dialog.MaxHeight;
        FilmFormat Format()=>format.SelectedIndex==0?FilmFormat.Film135:FilmFormat.Film120;
        void Changed(){session.Notify();InvalidatePreview();}
        void RefreshPages(){pages.ItemsSource=null;pages.ItemsSource=session.Document.Albums;pages.DisplayMemberPath="Name";pages.SelectedItem=page;name.Text=page?.Name??"";InvalidatePreview();}
        void NewPage(){page=new AlbumPage{Name=$"相册第 {session.Document.Albums.Count+1} 页"};session.Document.Albums.Add(page);pocketId=null;RefreshPages();Changed();}
        var newPage=Btn("新建页",NewPage);Grid.SetRow(newPage,2);left.Children.Add(newPage);
        var deletePage=Btn("删除此页",()=>{if(page==null)return;session.Document.Remove([page.Id]);session.Document.Albums.Remove(page);page=session.Document.Albums.FirstOrDefault();pocketId=null;RefreshPages();Changed();});Grid.SetRow(deletePage,3);left.Children.Add(deletePage);
        buttons.Children.Add(Btn("加袋格",()=>{if(page==null)NewPage();var p=page!.Add(Format());pocketId=p?.Id;if(p==null)note.Text="这一页放不下所选袋格，可以新建一页。";Changed();}));
        buttons.Children.Add(Btn("整页 135 · 5 × 4",()=>{if(page==null)NewPage();if(page!.Pockets.Count>0){note.Text="整页袋格仅适用于空页，已有照片保留。";return;}page.Fill(FilmFormat.Film135);Changed();}));
        buttons.Children.Add(Btn("整页 120 · 3 × 3",()=>{if(page==null)NewPage();if(page!.Pockets.Count>0){note.Text="整页袋格仅适用于空页，已有照片保留。";return;}page.Fill(FilmFormat.Film120);Changed();}));
        buttons.Children.Add(Btn("整页放上观片台",()=>{if(page!=null){session.PlaceAlbum(page);dialog.Hide();}}));
        buttons.Children.Add(new TextBlock{Text="选择照片",Margin=new(0,12,0,0)});
        pages.SelectionChanged+=(_,_)=>{if(pages.SelectedItem is AlbumPage a){page=a;pocketId=null;name.Text=a.Name;InvalidatePreview();}};
        name.TextChanged+=(_,_)=>{if(page!=null){page.Name=name.Text;Changed();}};
        void Assign(string path)
        {
            if(page==null)NewPage();var p=page!.Pockets.Find(p=>p.Id==pocketId)??page.Pockets.Find(p=>p.Path==null)??page.Add(Format());
            if(p==null){note.Text="袋格已满，请新建袋格或相册页。";return;}p.Path=path;pocketId=p.Id;Changed();
        }
        photos.ItemClick+=(_,e)=>{if(e.ClickedItem is Photo p)Assign(p.Path);};photos.IsItemClickEnabled=true;
        photos.DragItemsStarting+=(_,e)=>{if(e.Items.FirstOrDefault() is Photo p){e.Data.Properties["LightTable.Path"]=p.Path;e.Data.RequestedOperation=DataPackageOperation.Copy;}};
        PointD Local(global::Windows.Foundation.Point point)
        {
            var scale=Math.Min(preview.ActualWidth/620,preview.ActualHeight/690);return new((point.X-(preview.ActualWidth-620*scale)/2)/scale,(point.Y-(preview.ActualHeight-690*scale)/2)/scale);
        }
        void RenderPreview()
        {
            if(previewClosed||preview.ActualWidth<1||preview.ActualHeight<1)return;
            try
            {
                var dpi=root.XamlRoot.RasterizationScale;int width=Math.Max(1,(int)Math.Ceiling(preview.ActualWidth*dpi)),height=Math.Max(1,(int)Math.Ceiling(preview.ActualHeight*dpi));
                using var target=new CanvasRenderTarget(board.Device,width,height,96);
                using(var draw=target.CreateDrawingSession())
                {
                    draw.Clear(global::Windows.UI.Color.FromArgb(255,210,204,188));
                    if(page!=null)
                    {
                        renderer.AlbumPreview(draw,page,width,height);
                        if(page.Pockets.Find(p=>p.Id==pocketId) is {} p)
                        {
                            var scale=Math.Min(width/620d,height/690d);var r=p.Rect;
                            draw.DrawRectangle(new global::Windows.Foundation.Rect((width-620*scale)/2+r.X*scale,(height-690*scale)/2+r.Y*scale,r.Width*scale,r.Height*scale),Colors.SteelBlue,2);
                        }
                    }
                }
                if(previewBitmap==null||previewBitmap.PixelWidth!=width||previewBitmap.PixelHeight!=height)previewBitmap=new(width,height);
                using(var buffer=previewBitmap.PixelBuffer.AsStream())buffer.Write(target.GetPixelBytes());previewBitmap.Invalidate();previewImage.Source=previewBitmap;
                if(traceViewingInput)session.Storage.Log($"album-preview pockets={page?.Pockets.Count??0} photos={page?.Pockets.Count(p=>p.Path!=null)??0} pixels={width}x{height}");
            }
            catch(Exception ex){if(!renderer.HandleDeviceError(ex))session.Storage.Log($"相册预览失败: {ex}");}
        }
        previewTimer.Tick+=(_,_)=>RenderPreview();preview.SizeChanged+=(_,_)=>InvalidatePreview();
        preview.PointerPressed+=(_,e)=>
        {
            if(page==null)return;var at=Local(e.GetCurrentPoint(preview).Position);var p=page.Pockets.LastOrDefault(p=>p.Rect.Contains(at));
            if(p!=null){pocketId=p.Id;dragStart=at;draggedPocket=p;preview.CapturePointer(e.Pointer);}
            else{var col=(int)Math.Floor((at.X-48)/36);var row=(int)Math.Floor((at.Y-38)/50);pocketId=page.Add(Format(),col,row)?.Id;}
            Changed();
        };
        preview.PointerReleased+=(_,e)=>
        {
            if(page!=null&&dragStart is {} start&&draggedPocket is {} p)
            {
                var end=Local(e.GetCurrentPoint(preview).Position);var delta=end-start;
                if(Math.Abs(delta.X)>12||Math.Abs(delta.Y)>12){var col=p.Column+(int)Math.Round(delta.X/36);var row=p.Row+(int)Math.Round(delta.Y/50);if(!page.Move(p.Id,col,row))note.Text="袋格不能重叠或超出页面；同规格袋格可交换位置。";}
            }
            dragStart=null;draggedPocket=null;preview.ReleasePointerCaptures();Changed();
        };
        preview.RightTapped+=(_,e)=>
        {
            if(page==null)return;var p=page.Pockets.LastOrDefault(p=>p.Rect.Contains(Local(e.GetPosition(preview))));if(p==null)return;pocketId=p.Id;
            var f=new MenuFlyout();MenuItem(f,"清空照片",()=>{p.Path=null;Changed();});MenuItem(f,"移除袋格",()=>{page.Pockets.Remove(p);pocketId=null;Changed();});f.ShowAt(preview,e.GetPosition(preview));
        };
        preview.DragOver+=(_,e)=>{if(e.DataView.Properties.ContainsKey("LightTable.Path"))e.AcceptedOperation=DataPackageOperation.Copy;};
        preview.Drop+=(_,e)=>{if(page==null)return;var p=page.Pockets.LastOrDefault(p=>p.Rect.Contains(Local(e.GetPosition(preview))));if(p!=null)pocketId=p.Id;if(e.DataView.Properties.TryGetValue("LightTable.Path",out var value)&&value is string path)Assign(path);};
        void ResourceReady()=>InvalidatePreview();renderer.ResourceReady+=ResourceReady;
        RefreshPages();
        try{await dialog.ShowAsync();}
        finally{renderer.ResourceReady-=ResourceReady;previewClosed=true;previewTimer.Stop();previewImage.Source=null;previewBitmap=null;albumOpen=false;photoCount=-1;Refresh();session.Notify();board.Focus(FocusState.Programmatic);}
    }
}
