using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using LightTable.Core;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace LightTable.Windows;

sealed partial class MainWindow : Window
{
    readonly Grid root=new(){RequestedTheme=ElementTheme.Light};
    readonly CanvasControl board=new(){IsTabStop=true,AllowDrop=true};
    readonly CanvasControl lensOverlay=new(){IsHitTestVisible=false};
    readonly CanvasControl collectionOverlay=new(){Visibility=Visibility.Collapsed};
    readonly CollectionRenderer collectionRenderer;
    readonly TableSession session;
    readonly TableRenderer renderer;
    readonly SolidColorBrush panelBrush=new(global::Windows.UI.Color.FromArgb(255,227,217,191));
    readonly ObservableCollection<TrayItem> trayItems=[];
    readonly ListView tray=new(){SelectionMode=ListViewSelectionMode.Single,CanDragItems=true,CanReorderItems=false,AllowDrop=true,Height=110,Background=new SolidColorBrush(Colors.Transparent),BorderThickness=new(0)};
    readonly TextBlock count=new(){FontSize=11,Foreground=new SolidColorBrush(Color(170))};
    readonly TextBlock status=new(){FontSize=11,Foreground=new SolidColorBrush(Color(170)),TextTrimming=TextTrimming.CharacterEllipsis};
    readonly Button light=new();
    readonly Button loupeButton=new();
    readonly MagnificationSwitch magnification=new(){Visibility=Visibility.Collapsed};
    readonly Microsoft.UI.Dispatching.DispatcherQueueTimer animationTimer;
    readonly Border startup=new(){Background=new SolidColorBrush(global::Windows.UI.Color.FromArgb(245,14,14,14)),Visibility=Visibility.Collapsed};
    readonly ProgressBar startupProgress=new(){Minimum=0,Maximum=1,Height=4};
    readonly TextBlock startupText=new(){FontSize=15,Foreground=new SolidColorBrush(Colors.White),Text="唤醒观片台"};
    readonly StackPanel startupActions=new(){Orientation=Orientation.Horizontal,Spacing=10};
    bool dialogOpen,closed,ready,albumOpen;
    readonly bool traceViewingInput;
    int photoCount=-1,albumCount=-1;
    SlideshowWindow? slideshow;
    public MainWindow()
    {
        Title=$"观片台 · LightTable for Windows {BuildInfo.Version}";
        var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--data-root");
        traceViewingInput=args.Contains("--trace-viewing-input",StringComparer.Ordinal);
        // State/GPU diagnostics supply their own pointer coordinates. Keep
        // live mouse movement from changing a fixture between draw and readback.
        if(args.Contains("--integration-fixtures")||args.Contains("--benchmark-photos")||args.Contains("--optical-cache-probe"))root.IsHitTestVisible=false;
        session=new(DispatcherQueue,index>=0&&index+1<args.Length?args[index+1]:null);
        renderer=new(session,board,lensOverlay);
        collectionRenderer=new(collectionOverlay,session.Images);
        session.ReduceMotion=ReadOptions().ReduceMotion;
        AppWindow.Resize(new(1180,900));
        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory,"Resources","AppIcon.ico"));
        Content=root;
        root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        root.Background=new SolidColorBrush(Color(6));
        var menu=BuildMenu();root.Children.Add(menu);
        var boardHost=new Grid{Margin=new(16,0,16,0),Background=panelBrush,CornerRadius=new(12)};
        boardHost.RowDefinitions.Add(new(){Height=new(64)});boardHost.RowDefinitions.Add(new(){Height=new(1,GridUnitType.Star)});
        Grid.SetRow(boardHost,1);root.Children.Add(boardHost);boardHost.Children.Add(Toolbar());Grid.SetRow(board,1);boardHost.Children.Add(board);
        AutomationProperties.SetName(board,"灯箱工作区");
        var bottom=new StackPanel{Spacing=8,Margin=new(16),Padding=new(14),Background=new SolidColorBrush(Color(17)),CornerRadius=new(16),RequestedTheme=ElementTheme.Dark};
        var actions=new Grid();actions.ColumnDefinitions.Add(new(){Width=GridLength.Auto});actions.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});actions.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var imports=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10};
        imports.Children.Add(Btn("选择文件夹",async()=>await ChooseFolder(),"Ctrl+O"));imports.Children.Add(Btn("添加照片",async()=>await ChoosePhotos(),"Ctrl+Shift+O"));imports.Children.Add(Btn("一键平铺",()=>session.Fit(true),"清除散片摆放，重新排列图库照片"));imports.Children.Add(count);actions.Children.Add(imports);
        Grid.SetColumn(status,1);status.Margin=new(16,8,12,0);actions.Children.Add(status);
        var album=Btn("相册",async()=>await ShowAlbums());Grid.SetColumn(album,2);actions.Children.Add(album);bottom.Children.Add(actions);
        tray.ItemsSource=trayItems;
        tray.ItemsPanel=(ItemsPanelTemplate)XamlReader.Load("<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ItemsStackPanel Orientation='Horizontal'/></ItemsPanelTemplate>");
        tray.ItemTemplate=(DataTemplate)XamlReader.Load("""
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
              <StackPanel Width='104' Spacing='3'>
                <Grid Width='{Binding FrameWidth}' Height='{Binding FrameHeight}' HorizontalAlignment='Center'>
                  <Border Background='#28303030' BorderBrush='#60FFFFFF' BorderThickness='0.5,1,1,1' Visibility='{Binding SleeveVisibility}'/>
                  <Border BorderBrush='#60FFFFFF' BorderThickness='0,1,0,1' Margin='3,6,3,3' Visibility='{Binding SleeveVisibility}'/>
                  <Image Source='{Binding Thumbnail}' Width='{Binding ImageWidth}' Height='{Binding ImageHeight}' Stretch='Uniform' Margin='{Binding ImageMargin}'/>
                  <TextBlock Text='{Binding Mark}' FontSize='11' Foreground='White' HorizontalAlignment='Right' VerticalAlignment='Bottom' Margin='7'/>
                </Grid>
                <TextBlock Text='{Binding Name}' FontSize='9' Foreground='#BBBBBB' TextTrimming='CharacterEllipsis' HorizontalAlignment='Center' MaxWidth='100'/>
              </StackPanel>
            </DataTemplate>
            """);
        tray.ContainerContentChanging+=(sender,e)=>{if(!e.InRecycleQueue&&e.Item is TrayItem item)_=LoadThumbnail(item);};
        tray.DoubleTapped+=async(_,_)=>{if(tray.SelectedItem is TrayItem item){if(item.AlbumId!=null)await ShowAlbums(item.AlbumId);else PlaceTray(item);}};
        tray.DragItemsStarting+=(_,e)=>{if(e.Items.FirstOrDefault() is TrayItem item){e.Data.Properties["LightTable.Path"]=item.Path;e.Data.Properties["LightTable.Album"]=item.AlbumId?.ToString()??"";e.Data.RequestedOperation=DataPackageOperation.Copy;}};
        tray.RightTapped+=(_,e)=>{var item=(e.OriginalSource as FrameworkElement)?.DataContext as TrayItem;if(item==null)return;var flyout=new MenuFlyout();MenuItem(flyout,"放到灯板中央",()=>PlaceTray(item));if(item.AlbumId==null)MenuItem(flyout,"在文件资源管理器中显示",()=>Reveal(item.Path));else MenuItem(flyout,"编辑相册页",async()=>await ShowAlbums(item.AlbumId));flyout.ShowAt(tray,e.GetPosition(tray));};
        tray.DragOver+=(_,e)=>{if(e.DataView.Contains(StandardDataFormats.StorageItems))e.AcceptedOperation=DataPackageOperation.Copy;};
        tray.Drop+=async(_,e)=>{if(e.DataView.Contains(StandardDataFormats.StorageItems)){var items=await e.DataView.GetStorageItemsAsync();await Import(items.Select(i=>i.Path));}};
        bottom.Children.Add(tray);Grid.SetRow(bottom,2);root.Children.Add(bottom);
        var prepare=new StackPanel{Width=390,Spacing=22,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};prepare.Children.Add(startupText);prepare.Children.Add(startupProgress);prepare.Children.Add(startupActions);startup.Child=prepare;
        Grid.SetRowSpan(lensOverlay,3);root.Children.Add(lensOverlay);
        Grid.SetRowSpan(collectionOverlay,3);root.Children.Add(collectionOverlay);
        Grid.SetRowSpan(startup,3);root.Children.Add(startup);
        session.Changed+=Refresh;
        renderer.ResourceReady+=RefreshAlbumThumbnails;
        session.Notice+=text=>{if(closed)return;status.Text=text.Split('\n')[0];_=ShowMessage("观片台",text);};
        board.SizeChanged+=(_,_)=>{session.Width=Math.Max(1,board.ActualWidth);session.Height=Math.Max(1,board.ActualHeight);var at=board.TransformToVisual(root).TransformPoint(new(0,0));renderer.BoardOrigin=new(at.X,at.Y);board.Invalidate();};
        SetupInput();SetupTrayInput();
        animationTimer=DispatcherQueue.CreateTimer();animationTimer.Interval=TimeSpan.FromMilliseconds(16);animationTimer.Tick+=(_,_)=>{if(closed)return;TickFlight(.016);TickTableDrag(.016);var changed=renderer.TickAnimations(TableRenderer.Seconds);magnification.Advance(TableRenderer.Seconds,session.ReduceMotion);if(changed||renderer.IsAnimating){panelBrush.Color=renderer.PanelColor;board.Invalidate();lensOverlay.Invalidate();}};animationTimer.Start();
        Activated+=(_,e)=>{if(e.WindowActivationState==WindowActivationState.Deactivated){session.CloseViewing=false;tabHeld=spaceHeld=false;CancelGesture();CancelTrayFlight();}};
        Closed+=(_,_)=>{closed=true;collectionCancellation?.Cancel();animationTimer.Stop();periodicUpdateTimer?.Stop();session.Flush();renderer.Dispose();collectionRenderer.Dispose();board.RemoveFromVisualTree();lensOverlay.RemoveFromVisualTree();collectionOverlay.RemoveFromVisualTree();slideshow?.Close();};
        root.Loaded+=async(_,_)=>
        {
            if(ready)return;ready=true;
            var opticalProbe=Array.IndexOf(args,"--optical-cache-probe");
            if(opticalProbe>=0&&opticalProbe+1<args.Length)
            {
                if(index<0){Environment.ExitCode=2;Close();return;}
                await RunOpticalCacheProbe(args[opticalProbe+1]);return;
            }
            var benchmark=Array.IndexOf(args,"--benchmark-photos");var benchmarkCount=Array.IndexOf(args,"--benchmark-count");
            if(benchmark>=0&&benchmark+1<args.Length&&benchmarkCount>=0&&benchmarkCount+1<args.Length&&int.TryParse(args[benchmarkCount+1],out var n))
            {
                if(index<0||n<1||n>10000){Environment.ExitCode=2;Close();return;}
                await RunBenchmark(args[benchmark+1],n);return;
            }
            var integration=Array.IndexOf(args,"--integration-fixtures");
            if(integration>=0&&integration+1<args.Length)
            {
                if(index<0){Environment.ExitCode=2;Close();return;}
                await RunIntegration(args[integration+1]);return;
            }
            await PrepareStartup();var files=args.Skip(1).Where(File.Exists).ToArray();if(files.Length>0)await Import(files);
        };
        Refresh();
    }
    Grid Toolbar()
    {
        var panel=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new(12,0,12,0)};
        var arrow=Btn("指针",()=>{session.Loupe=false;session.CloseViewing=false;session.Notify(false);},"A · Ctrl 点选，Shift 拖动框选");panel.Children.Add(arrow);
        loupeButton.Content="放大镜";loupeButton.Click+=(_,_)=>{session.Loupe=!session.Loupe;session.CloseViewing=false;session.Notify(false);};ToolTipService.SetToolTip(loupeButton,"V · 按住空格凑近，Tab 切换倍率");panel.Children.Add(loupeButton);
        magnification.Selected+=value=>{session.Power=value;session.Notify(false);if(traceViewingInput)session.Storage.Log($"magnification-input power={LoupePowers.Scale(value)}");};panel.Children.Add(magnification);
        panel.Children.Add(Btn("−",()=>Zoom(1/1.2),"缩小视野"));panel.Children.Add(Btn("整理全局",()=>session.Fit(),"Shift+Z"));panel.Children.Add(Btn("+",()=>Zoom(1.2),"放大视野"));
        var brightness=new Slider{Minimum=0,Maximum=1.6,Value=1,Width=110,VerticalAlignment=VerticalAlignment.Center};var amount=new TextBlock{Text="100%",FontSize=11,Width=38,VerticalAlignment=VerticalAlignment.Center};
        brightness.ValueChanged+=(_,_)=>{session.Brightness=brightness.Value;amount.Text=$"{Math.Round(brightness.Value*100)}%";session.Notify(false);};brightness.DoubleTapped+=(_,_)=>brightness.Value=1;
        AutomationProperties.SetName(brightness,"灯箱亮度");ToolTipService.SetToolTip(brightness,"0–160% · 0% 为关灯基准 · 双击恢复 100%");panel.Children.Add(brightness);panel.Children.Add(amount);
        light.Click+=(_,_)=>session.ToggleLight(TableRenderer.Seconds);panel.Children.Add(light);panel.Children.Add(Btn("幻灯片",()=>StartSlideshow()));
        var scroller=new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollMode=ScrollMode.Enabled,Content=panel};
        var grid=new Grid();grid.Children.Add(scroller);return grid;
    }
    void Refresh()
    {
        if(closed)return;
        light.Content=session.IsLit?"关灯":"开灯";magnification.Visibility=session.Loupe?Visibility.Visible:Visibility.Collapsed;loupeButton.FontWeight=session.Loupe?Microsoft.UI.Text.FontWeights.SemiBold:Microsoft.UI.Text.FontWeights.Normal;
        magnification.SetValue(session.Power,!session.ReduceMotion);
        count.Text=$"{session.Document.Photos.Count} 张";panelBrush.Color=renderer.PanelColor;board.Invalidate();
        if(photoCount!=session.Document.Photos.Count||albumCount!=session.Document.Albums.Count)
        {
            photoCount=session.Document.Photos.Count;albumCount=session.Document.Albums.Count;trayItems.Clear();
            foreach(var a in session.Document.Albums)trayItems.Add(new TrayItem(a.Name,"",a.Id));
            foreach(var p in session.Document.Photos)trayItems.Add(new TrayItem(p.Name,p.Path));
        }
        var placedSlides=session.Document.Slides.Select(s=>s.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var placedAlbums=session.Document.Albums.Where(a=>a.Position!=null).Select(a=>a.Id).ToHashSet();
        foreach(var item in trayItems)item.Mark=item.AlbumId is {} id ? placedAlbums.Contains(id)?"已上台":"相册页" : placedSlides.Contains(item.Path)?"已上台":"";
        RefreshAlbumThumbnails();
    }
    void RefreshAlbumThumbnails(){if(closed)return;foreach(var item in trayItems.Where(i=>i.AlbumId!=null))if(tray.ContainerFromItem(item)!=null)_=LoadThumbnail(item);}
    async Task LoadThumbnail(TrayItem item)
    {
        if(item.Loading||closed)return;
        AlbumPage? album=item.AlbumId is {} id?session.Document.Albums.Find(a=>a.Id==id):null;var hash=new HashCode();
        if(album!=null){hash.Add(renderer.ResourcesRevision);foreach(var p in album.Pockets){hash.Add(p.Column);hash.Add(p.Row);hash.Add(p.Format);hash.Add(p.Path);hash.Add(p.Path==null?0:session.Document.Aspect(p.Path));}}
        var revision=hash.ToHashCode();if(item.Thumbnail!=null&&(album==null||item.ThumbnailRevision==revision))return;item.Loading=true;
        bool retryAfterRecovery=false;long deviceGeneration=renderer.DeviceGeneration;
        try
        {
            byte[] pixels;int width,height;
            if(album!=null)
            {
                await renderer.DeviceReady;if(closed)return;deviceGeneration=renderer.DeviceGeneration;width=138;height=132;
                using var target=new Microsoft.Graphics.Canvas.CanvasRenderTarget(board.Device,width,height,96);
                using(var ds=target.CreateDrawingSession()){ds.Clear(Color(51));renderer.AlbumPreview(ds,album,width,height);}
                pixels=target.GetPixelBytes();item.ThumbnailRevision=revision;
            }
            else{var decoded=await session.Images.GetAsync(item.Path,220);width=decoded.Width;height=decoded.Height;pixels=decoded.Pixels;}
            var bitmap=new WriteableBitmap(width,height);using(var stream=bitmap.PixelBuffer.AsStream())await stream.WriteAsync(pixels);bitmap.Invalidate();
            if(album!=null&&deviceGeneration!=renderer.DeviceGeneration)retryAfterRecovery=true;else item.Thumbnail=bitmap;
        }
        catch(Exception ex){if(album!=null&&(deviceGeneration!=renderer.DeviceGeneration||renderer.HandleDeviceError(ex)))retryAfterRecovery=true;else item.Mark="无法读取";}
        finally{item.Loading=false;if(retryAfterRecovery&&!closed)_=LoadThumbnail(item);}
    }
    void PlaceTray(TrayItem item){if(item.AlbumId is {} id&&session.Document.Albums.Find(a=>a.Id==id) is {} page)session.PlaceAlbum(page);else session.Place(item.Path,new(session.Width/2,session.Height/2));}
    async Task Import(IEnumerable<string> paths,bool replace=false)
    {
        status.Text="正在读取照片…";
        await session.ImportAsync(paths,replace,new Progress<(int Done,int Total)>(p=>{if(session.Busy)status.Text=$"正在读取照片 {p.Done} / {p.Total}";}));
        status.Text=session.Document.SourceLabel;Refresh();
    }
    async Task ChoosePhotos()
    {
        var picker=new FileOpenPicker();WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));
        foreach(var ext in new[]{".jpg",".jpeg",".png",".tif",".tiff",".bmp",".heic",".heif",".webp",".dng",".cr2",".cr3",".nef",".arw",".raf",".orf",".rw2",".gif",".avif"})picker.FileTypeFilter.Add(ext);
        var files=await picker.PickMultipleFilesAsync();if(files.Count>0)await Import(files.Select(f=>f.Path));
    }
    async Task ChooseFolder()
    {
        var picker=new FolderPicker();picker.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));
        var folder=await picker.PickSingleFolderAsync();if(folder!=null)await Import([folder.Path],true);
    }
    public async Task ShowMessage(string title,string text)
    {
        if(dialogOpen||closed||root.XamlRoot==null)return;dialogOpen=true;session.CloseViewing=false;
        try{await new ContentDialog{Title=title,Content=new ScrollViewer{Content=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,MaxWidth=580},MaxHeight=500},CloseButtonText="好",XamlRoot=root.XamlRoot}.ShowAsync();}
        finally{dialogOpen=false;}
    }
    static Button Btn(string label,Action action,string? tooltip=null)
    {
        var button=new Button{Content=label,MinHeight=32,Padding=new(11,5,11,5),CornerRadius=new(17),FontSize=12};button.Click+=(_,_)=>action();if(tooltip!=null)ToolTipService.SetToolTip(button,tooltip);return button;
    }
    static void MenuItem(MenuFlyout flyout,string text,Action action){var item=new MenuFlyoutItem{Text=text};item.Click+=(_,_)=>action();flyout.Items.Add(item);}
    static global::Windows.UI.Color Color(byte value)=>global::Windows.UI.Color.FromArgb(255,value,value,value);
    static void Reveal(string path){if(File.Exists(path))Process.Start(new ProcessStartInfo("explorer.exe",$"/select,\"{path}\""){UseShellExecute=true});}
}

sealed class TrayItem(string name,string path,Guid? albumId=null) : INotifyPropertyChanged
{
    public string Name {get;}=name;
    public string Path {get;}=path;
    public Guid? AlbumId {get;}=albumId;
    public double FrameWidth=>AlbumId==null?104:69;
    public double FrameHeight=>AlbumId==null?70:66;
    public double ImageWidth=>AlbumId==null?90:69;
    public double ImageHeight=>AlbumId==null?56:66;
    public Thickness ImageMargin=>AlbumId==null?new(7,9,7,5):new(0);
    public Visibility SleeveVisibility=>AlbumId==null?Visibility.Visible:Visibility.Collapsed;
    public int ThumbnailRevision {get;set;}
    public bool Loading {get;set;}
    ImageSource? thumbnail;
    public ImageSource? Thumbnail{get=>thumbnail;set{thumbnail=value;PropertyChanged?.Invoke(this,new(nameof(Thumbnail)));}}
    string mark="";
    public string Mark{get=>mark;set{if(mark==value)return;mark=value;PropertyChanged?.Invoke(this,new(nameof(Mark)));}}
    public event PropertyChangedEventHandler? PropertyChanged;
}
