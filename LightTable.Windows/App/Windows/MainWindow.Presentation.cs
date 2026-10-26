using System.Text.Json;
using LightTable.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    Microsoft.UI.Dispatching.DispatcherQueueTimer? periodicUpdateTimer;
    MenuBar BuildMenu()
    {
        var bar=new MenuBar{RequestedTheme=ElementTheme.Dark,Background=new SolidColorBrush(Color(6))};
        MenuBarItem Menu(string title){var m=new MenuBarItem{Title=title};bar.Items.Add(m);return m;}
        void Item(MenuBarItem menu,string title,Action action){var item=new MenuFlyoutItem{Text=title};item.Click+=(_,_)=>{if(startup.Visibility!=Visibility.Visible&&!session.Busy&&collectionCancellation==null)action();};menu.Items.Add(item);}
        var file=Menu("文件");Item(file,"选择照片文件夹…     Ctrl+O",async()=>await ChooseFolder());Item(file,"添加照片…     Ctrl+Shift+O",async()=>await ChoosePhotos());Item(file,"保存布局",()=>session.Flush());
        Item(file,"打开数据目录",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(session.Storage.Root){UseShellExecute=true}));
        Item(file,"导入 Mac 版布局…",async()=>await ImportMacLayout());
        var view=Menu("观片");Item(view,"开灯 / 关灯     Ctrl+L",()=>session.ToggleLight(Rendering.TableRenderer.Seconds));Item(view,"普通指针     A",()=>{session.Loupe=false;session.CloseViewing=false;session.Notify(false);});Item(view,"放大镜     V",()=>{session.Loupe=true;session.Notify(false);});
        Item(view,"整理全局     Shift+Z",()=>session.Fit());Item(view,"一键平铺",()=>session.Fit(true));Item(view,"清空台面",()=>session.Clear());Item(view,"移出所选     Delete",()=>session.RemoveSelection());Item(view,"胶片相册…",async()=>await ShowAlbums());Item(view,"全屏幻灯",()=>StartSlideshow());
        var reduceMotion=new ToggleMenuFlyoutItem{Text="减少动态效果",IsChecked=session.ReduceMotion};reduceMotion.Click+=(_,_)=>{session.ReduceMotion=reduceMotion.IsChecked;var options=ReadOptions();options.ReduceMotion=session.ReduceMotion;WriteOptions(options);session.Notify(false);};view.Items.Add(reduceMotion);
        var help=Menu("帮助");Item(help,"本次更新与使用指引…",async()=>await ShowGuide(true));Item(help,"快捷键…",async()=>await ShowMessage("使用观片台","Ctrl+O 选择文件夹（替换图库，保留台面）\nCtrl+Shift+O 添加照片\nCtrl+L 开灯 / 关灯\nA 指针 · V 放大镜 · Tab 切换1× → 2× → 4× → 8×\n按住 Space 凑近，松开恢复\nShift+Z 整理全局\nCtrl+单击 多选 · Shift+拖动 框选\nDelete 移出台面\nCtrl+滚轮 缩放视野 · 滚轮 平移\nAlt+拖动 旋转胶片 · 右下角拖动 缩放胶片\n右键 胶片旋转、缩放、摆正或移出\n幻灯方向键换片，Esc 退出\n\n删除台面项目只改变布局，原照片保持原样。"));
        Item(help,"检查更新…",async()=>await CheckUpdates(true));
        var periodic=new ToggleMenuFlyoutItem{Text="运行期间自动检查更新",IsChecked=ReadOptions().PeriodicChecks};periodic.Click+=(_,_)=>{var o=ReadOptions();o.PeriodicChecks=periodic.IsChecked;WriteOptions(o);};help.Items.Add(periodic);
        Item(help,"关于观片台",async()=>await ShowMessage($"LightTable for Windows {BuildInfo.Version}","基于 LightTable v0.1.72 的 Windows 原生移植开发构建。\nWinUI 3 · Win2D / Direct2D · .NET 10\n\n尚在移植与验收中，完整等价尚未通过。\nCopyright (c) 2026 fuyuan9972-svg\nMIT 许可和第三方说明随程序提供。\n\n当前构建未签名。默认机械声为原创合成声，原版录音须另行取得授权。"));
        return bar;
    }
    async Task PrepareStartup(bool afterStartup=true)
    {
        var paths=session.Document.PlacedPaths.ToList();
        if(paths.Count>0)
        {
            startup.Visibility=Visibility.Visible;startupActions.Children.Clear();var failures=new List<string>();int done=0;
            startupText.Text="恢复照片摆放";startupProgress.Value=0;
            foreach(var path in paths)
            {
                if(closed)return;
                try{await renderer.PrepareAsync(path,2200);}
                catch(Exception ex){failures.Add(path);session.Storage.Log($"启动准备失败 {path}: {ex}");}
                done++;startupText.Text=$"准备高清照片 · {done} / {paths.Count}";startupProgress.Value=(double)done/paths.Count;
            }
            if(failures.Count>0)
            {
                startupText.Text=$"{failures.Count} 张照片无法读取\n"+string.Join("\n",failures.Take(3).Select(System.IO.Path.GetFileName));
                startupActions.Children.Add(Btn("重新读取",async()=>{renderer.RetryFailures();await PrepareStartup(afterStartup);}));
                startupActions.Children.Add(Btn("先查看可用照片",async()=>{startup.Visibility=Visibility.Collapsed;if(afterStartup)await AfterStartup();}));return;
            }
            startup.Visibility=Visibility.Collapsed;
        }
        if(afterStartup)await AfterStartup();
    }
    async Task AfterStartup()
    {
        if(session.StartupWarning is {} warning)await ShowMessage("布局恢复",warning);
        await ShowGuide(false);if(closed)return;_=CheckUpdates(false);
        if(periodicUpdateTimer!=null)return;
        periodicUpdateTimer=DispatcherQueue.CreateTimer();periodicUpdateTimer.Interval=TimeSpan.FromHours(24);
        periodicUpdateTimer.Tick+=async(_,_)=>{if(ReadOptions().PeriodicChecks)await CheckUpdates(false,true);};periodicUpdateTimer.Start();
    }
    async Task ShowGuide(bool replay)
    {
        var options=ReadOptions();if(!replay&&options.GuideVersion==BuildInfo.Version||dialogOpen||root.XamlRoot==null)return;
        dialogOpen=true;session.CloseViewing=false;
        try
        {
            var dialog=new ContentDialog{XamlRoot=root.XamlRoot,Title="让照片回到光里",Content=new TextBlock{Text="无边框胶片、薄侧沿与接触阴影。\n\n从底栏拖片到台面，自由摆放；开灯后，透射光短暂过曝，再自然回落。\n\n重启时准备高清照片，按 Shift+Z 整理全局。",TextWrapping=TextWrapping.Wrap,MaxWidth=430},PrimaryButtonText="开始观片",SecondaryButtonText="看看怎么用",DefaultButton=ContentDialogButton.Primary};
            options.GuideVersion=BuildInfo.Version;WriteOptions(options);
            var result=await dialog.ShowAsync();
            if(result==ContentDialogResult.Secondary)
            {
                var steps=new[]{("拖动胶片","选择文件夹或添加照片。拖动底栏胶片到灯箱，拖动散片移动；在胶片上右键旋转和缩放。"),("整理全局","Shift+Z 恢复散片默认尺寸并整理台面；一键平铺则将图库全部照片重新放上台面。Ctrl 点选和 Shift 框选后可批量移出。"),("凑近观察","V 切换放大镜，按住空格凑近。Tab、点击或拖动倍率控件可切换1×、2×、4×、8×。松开空格回到 1.5× 小镜。高清预览最长边 2200px。")};
                for(int i=0;i<steps.Length;i++)
                {
                    var step=new ContentDialog{XamlRoot=root.XamlRoot,Title=steps[i].Item1,Content=new TextBlock{Text=steps[i].Item2,TextWrapping=TextWrapping.Wrap,MaxWidth=430},PrimaryButtonText=i==2?"开始观片":"下一步",SecondaryButtonText=i>0?"上一步":"",CloseButtonText="跳过"};
                    var selected=await step.ShowAsync();if(selected==ContentDialogResult.None)break;if(selected==ContentDialogResult.Secondary)i-=2;
                }
            }
        }
        finally{dialogOpen=false;board.Focus(FocusState.Programmatic);}
    }
    sealed class UserOptions{public string GuideVersion{get;set;}="";public bool PeriodicChecks{get;set;}=true;public bool ReduceMotion{get;set;}}
    UserOptions ReadOptions(){try{return JsonSerializer.Deserialize<UserOptions>(File.ReadAllText(System.IO.Path.Combine(session.Storage.Root,"config","settings.json")))??new();}catch{return new();}}
    void WriteOptions(UserOptions options)
    {
        try{var path=System.IO.Path.Combine(session.Storage.Root,"config","settings.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(options));File.Move(path+".tmp",path,true);}catch(Exception ex){session.Storage.Log($"设置保存失败 {ex}");}
    }
}
