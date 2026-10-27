using LightTable.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    readonly ComboBox collectionPicker=new(){Width=190,DisplayMemberPath="Name"};
    readonly Button removeLibrary=new(){IsEnabled=false,Content="移出栏目",CornerRadius=new(17),FontSize=12};
    readonly TextBlock trayRange=new(){FontSize=11,VerticalAlignment=VerticalAlignment.Center,Text="滚轮横向浏览 · Ctrl 多选"};
    readonly Dictionary<Guid,double> trayOffsets=[];
    ScrollViewer? trayScroller;
    bool updatingCollections;
    long libraryRevision=-1;
    StackPanel BuildCollectionBar()
    {
        var bar=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        collectionPicker.Width=150;AutomationProperties.SetName(collectionPicker,"照片栏目");bar.Children.Add(collectionPicker);
        collectionPicker.RightTapped+=(_,e)=>
        {
            var element=e.OriginalSource as DependencyObject;PhotoCollection? clicked=null;
            while(element!=null){if(element is FrameworkElement view&&view.DataContext is PhotoCollection target){clicked=target;break;}element=VisualTreeHelper.GetParent(element);}
            var id=(clicked??session.Document.EnsureCollection()).Id;var menu=new MenuFlyout();MenuItem(menu,"删除该栏目",async()=>await DeleteCurrentColumn(id));MenuItem(menu,"重命名该栏目",async()=>await RenameCollection(id));menu.ShowAt(collectionPicker,e.GetPosition(collectionPicker));e.Handled=true;
        };
        collectionPicker.DropDownOpened+=(_,_)=>{tray.SelectedItems.Clear();session.Selection.Clear();session.ShowSelection=false;board.Invalidate();};
        collectionPicker.SelectionChanged+=(_,_)=>
        {
            if(updatingCollections||session.Busy||collectionPicker.SelectedItem is not PhotoCollection collection)return;
            if(trayScroller!=null)trayOffsets[session.Document.EnsureCollection().Id]=trayScroller.HorizontalOffset;
            ResetWorkspaceInteraction();session.SwitchCollection(collection.Id);renderer.DocumentChanged();status.Text=collection.SourceLabel;
        };
        bar.Children.Add(Btn("＋",()=>{if(session.Busy)return;if(trayScroller!=null)trayOffsets[session.Document.EnsureCollection().Id]=trayScroller.HorizontalOffset;ResetWorkspaceInteraction();session.NewCollection();renderer.DocumentChanged();status.Text="新栏目为空，请添加照片";},"新栏目与台面均为空白，单独添加照片"));
        removeLibrary.Click+=(_,_)=>RemoveLibrarySelection();ToolTipService.SetToolTip(removeLibrary,"从当前栏目移除所选照片；同时移除当前台面散片；保留其他栏目和源文件");AutomationProperties.SetName(removeLibrary,"从当前栏目移除所选照片，保留源文件");
        return bar;
    }
    void ResetWorkspaceInteraction()
    {
        // A returning/landing animation from another column must never appear
        // or complete a placement in this workspace.
        renderer.Flight=null;pullingItem=null;pullStart=pullPointer=null;finishingFlight=false;tray.ReleasePointerCaptures();CancelGesture();renderer.PointerInside=false;
    }
    async Task DeleteCurrentColumn(Guid? targetId=null)
    {
        if(session.Busy||dialogOpen||root.XamlRoot==null)return;dialogOpen=true;session.CloseViewing=false;
        try
        {
            var target=session.Document.Collections.Find(c=>c.Id==(targetId??session.Document.ActiveCollectionId));if(target==null)return;
            var result=await new ContentDialog{XamlRoot=root.XamlRoot,Title=$"删除栏目「{target.Name}」？",Content="删除该栏目及其台面项目，不删除磁盘原图。其他栏目保持原样。",PrimaryButtonText="删除栏目",CloseButtonText="取消"}.ShowAsync();
            if(result==ContentDialogResult.Primary){ResetWorkspaceInteraction();session.DeleteCollection(target.Id);renderer.DocumentChanged();}
        }
        finally{dialogOpen=false;}
    }
    async Task RenameCollection(Guid? targetId=null)
    {
        if(session.Busy||dialogOpen||root.XamlRoot==null)return;dialogOpen=true;
        var collection=session.Document.Collections.Find(c=>c.Id==(targetId??session.Document.ActiveCollectionId))??session.Document.EnsureCollection();var name=new TextBox{Text=collection.Name,MaxLength=60,PlaceholderText="栏目名称"};
        try
        {
            var result=await new ContentDialog{XamlRoot=root.XamlRoot,Title="重命名栏目",Content=name,PrimaryButtonText="保存",CloseButtonText="取消",DefaultButton=ContentDialogButton.Primary}.ShowAsync();
            if(result==ContentDialogResult.Primary&&!string.IsNullOrWhiteSpace(name.Text)){collection.Name=name.Text.Trim();session.LibraryChanged();}
        }
        finally{dialogOpen=false;}
    }
    void RemoveLibrarySelection(TrayItem? single=null)
    {
        if(session.Busy)return;CancelTrayFlight();
        var paths=single!=null?[single.Path]:tray.SelectedItems.OfType<TrayItem>().Select(i=>i.Path).ToArray();
        var removed=session.Document.RemoveFromCollection(paths);if(removed==0)return;
        session.LibraryChanged();status.Text=$"已从栏目删除 {removed} 张 · 磁盘原图保留";
    }
    static T? FindVisual<T>(DependencyObject element)where T:DependencyObject
    {
        if(element is T found)return found;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(element);i++)if(FindVisual<T>(VisualTreeHelper.GetChild(element,i)) is {} child)return child;return null;
    }
    void InitializeTrayScrolling()
    {
        trayScroller=FindVisual<ScrollViewer>(tray);if(trayScroller==null)return;
        trayScroller.ViewChanged+=(_,_)=>UpdateTrayRange();
        tray.SizeChanged+=(_,_)=>UpdateTrayRange();UpdateTrayRange();
    }
    void ScrollTray(int direction)
    {
        if(trayScroller==null)return;
        trayScroller.ChangeView(Math.Clamp(trayScroller.HorizontalOffset+direction*trayScroller.ViewportWidth*.85,0,trayScroller.ScrollableWidth),null,null);UpdateTrayRange();
    }
    void UpdateTrayRange()
    {
        if(trayScroller==null)return;
        var itemWidth=trayItems.Count==0?116:trayItems[0].TileWidth+12;
        var first=trayItems.Count==0?0:Math.Min(trayItems.Count,1+(int)(trayScroller.HorizontalOffset/itemWidth));
        var last=Math.Min(trayItems.Count,(int)Math.Ceiling((trayScroller.HorizontalOffset+trayScroller.ViewportWidth)/itemWidth));
        trayRange.Text=$"{first}–{last} / {trayItems.Count} 项 · 滚轮横向浏览 · Ctrl 多选";ToolTipService.SetToolTip(count,trayRange.Text);
    }
    bool FocusIsInTray()
    {
        var focus=Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root.XamlRoot) as DependencyObject;
        while(focus!=null){if(ReferenceEquals(focus,tray))return true;focus=VisualTreeHelper.GetParent(focus);}return false;
    }
}
