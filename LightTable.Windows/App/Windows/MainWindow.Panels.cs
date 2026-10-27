using LightTable.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using global::Windows.System;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    readonly RowDefinition trayRow=new(){Height=new(PanelSizing.DefaultTrayHeight)};
    double requestedTrayHeight=PanelSizing.DefaultTrayHeight;
    void SetupPanelSizing()
    {
        requestedTrayHeight=ReadOptions().TrayHeight;
        var grip=new PanelResizeGrip();Grid.SetRow(grip,2);root.Children.Add(grip);
        grip.Handle.DragStarted+=(_,_)=>{ResetWorkspaceInteraction();session.CloseViewing=false;};
        grip.Handle.DragDelta+=(_,e)=>ApplyTrayHeight(trayRow.Height.Value-e.VerticalChange);
        grip.Handle.DragCompleted+=(_,_)=>SaveTrayHeight();
        grip.KeyDown+=(_,e)=>
        {
            if(e.Key==VirtualKey.Home)ApplyTrayHeight(PanelSizing.DefaultTrayHeight);
            else if(e.Key==VirtualKey.Up)ApplyTrayHeight(trayRow.Height.Value+16);
            else if(e.Key==VirtualKey.Down)ApplyTrayHeight(trayRow.Height.Value-16);
            else return;
            SaveTrayHeight();e.Handled=true;
        };
        root.SizeChanged+=(_,_)=>ApplyTrayHeight(requestedTrayHeight);
        tray.SizeChanged+=(_,_)=>
        {
            var scale=PanelSizing.ThumbnailScale(tray.ActualHeight);
            foreach(var item in trayItems)item.ThumbnailScale=scale;
        };
    }
    void ApplyTrayHeight(double height)
    {
        requestedTrayHeight=PanelSizing.TrayHeight(height,root.ActualHeight>0?root.ActualHeight:900);
        trayRow.Height=new(requestedTrayHeight);
    }
    void SaveTrayHeight(){var options=ReadOptions();options.TrayHeight=requestedTrayHeight;WriteOptions(options);}
    Grid BuildCompactTrayHeader()
    {
        var header=new Grid{ColumnSpacing=10};header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});header.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        header.Children.Add(BuildCollectionBar());
        var imports=new StackPanel{Orientation=Orientation.Horizontal,Spacing=6};
        imports.Children.Add(Btn("选择文件夹",async()=>await ChooseFolder(),"追加到当前栏目 · Ctrl+O"));imports.Children.Add(Btn("添加照片",async()=>await ChoosePhotos(),"只添加到当前栏目 · Ctrl+Shift+O"));imports.Children.Add(Btn("一键平铺",()=>session.Fit(true),"仅平铺当前栏目的照片"));
        count.VerticalAlignment=VerticalAlignment.Center;count.Margin=new(4,0,0,0);imports.Children.Add(count);status.MaxWidth=130;status.VerticalAlignment=VerticalAlignment.Center;imports.Children.Add(status);
        var importScroll=new ScrollViewer{Content=imports,HorizontalScrollMode=ScrollMode.Enabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollMode=ScrollMode.Disabled,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetColumn(importScroll,1);header.Children.Add(importScroll);
        var tools=new StackPanel{Orientation=Orientation.Horizontal,Spacing=5};
        tools.Children.Add(Btn("‹",()=>ScrollTray(-1),"上一屏照片"));tools.Children.Add(Btn("›",()=>ScrollTray(1),"下一屏照片"));
        removeLibrary.Content=new TextBlock{Text="\uE74D",FontFamily=new FontFamily("Segoe Fluent Icons"),FontSize=14};removeLibrary.MinWidth=32;removeLibrary.Padding=new(8,4,8,4);tools.Children.Add(removeLibrary);
        var more=new Button{Content="⋯",CornerRadius=new(16),Padding=new(8,4,8,4)};var flyout=new MenuFlyout();MenuItem(flyout,"重命名当前栏目",async()=>await RenameCollection());MenuItem(flyout,"删除当前栏目",async()=>await DeleteCurrentColumn());MenuItem(flyout,"恢复默认面板大小",()=>{ApplyTrayHeight(PanelSizing.DefaultTrayHeight);SaveTrayHeight();});more.Flyout=flyout;ToolTipService.SetToolTip(more,"栏目与面板选项");tools.Children.Add(more);
        Grid.SetColumn(tools,2);header.Children.Add(tools);return header;
    }
}
