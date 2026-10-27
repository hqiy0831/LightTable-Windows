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
        var file=Menu("文件");Item(file,"从文件夹追加照片…     Ctrl+O",async()=>await ChooseFolder());Item(file,"添加照片…     Ctrl+Shift+O",async()=>await ChoosePhotos());Item(file,"保存布局",()=>session.Flush());Item(file,"重试照片读取",()=>{renderer.RetryFailures();board.Invalidate();_=WarmRestoredVisible();});
        Item(file,"打开数据目录",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(session.Storage.Root){UseShellExecute=true}));
        Item(file,"导入 Mac 版布局…",async()=>await ImportMacLayout());
        var view=Menu("观片");Item(view,"开灯 / 关灯     Ctrl+L",()=>session.ToggleLight(Rendering.TableRenderer.Seconds));Item(view,"普通指针     A",()=>{session.Loupe=false;session.CloseViewing=false;session.Notify(false);});Item(view,"放大镜     V",()=>{session.Loupe=true;session.Notify(false);});
        Item(view,"整理全局     Shift+Z",()=>session.Fit());Item(view,"一键平铺",()=>session.Fit(true));Item(view,"清空台面",()=>session.Clear());Item(view,"收回所选到底栏     Delete",()=>session.RemoveSelection());Item(view,"全屏幻灯",()=>StartSlideshow());
        var reduceMotion=new ToggleMenuFlyoutItem{Text="减少动态效果",IsChecked=session.ReduceMotion};reduceMotion.Click+=(_,_)=>{session.ReduceMotion=reduceMotion.IsChecked;var options=ReadOptions();options.ReduceMotion=session.ReduceMotion;WriteOptions(options);session.Notify(false);};view.Items.Add(reduceMotion);
        var help=Menu("帮助");Item(help,"本次更新与使用指引…",async()=>await ShowGuide(true));Item(help,"快捷键…",async()=>await ShowMessage("使用观片台","Ctrl+O 从文件夹追加到当前栏目\nCtrl+Shift+O 添加照片\nCtrl+L 开灯 / 关灯\nA 指针 · V 放大镜 · Tab 切换1× → 2× → 4× → 8×\n按住 Space 凑近，松开恢复\nShift+Z 整理全局\nCtrl+单击 多选 · Shift+拖动 框选\nDelete 台面收回底栏 / 底栏删除照片\nCtrl+滚轮 缩放视野 · 滚轮 平移\nAlt+拖动 旋转胶片 · 右下角拖动 缩放胶片\n右键 胶片旋转、缩放、摆正或移出\n幻灯方向键换片，Esc 退出\n\n收回和删除不改动磁盘原图；栏目台面保持独立。"));
        Item(help,"检查更新…",async()=>await CheckUpdates(true));
        var periodic=new ToggleMenuFlyoutItem{Text="运行期间自动检查更新",IsChecked=ReadOptions().PeriodicChecks};periodic.Click+=(_,_)=>{var o=ReadOptions();o.PeriodicChecks=periodic.IsChecked;WriteOptions(o);};help.Items.Add(periodic);
        Item(help,"关于观片台",async()=>await ShowMessage($"LightTable for Windows {BuildInfo.Version}","原作者：抖音 @怕蛇LightTable\n原项目：github.com/fuyuan9972-svg/LightTable\n\n基于 LightTable v0.1.75 的 Windows 原生移植开发构建。\nWinUI 3 · Win2D / Direct2D · .NET 10\n\n尚在移植与验收中，完整等价尚未通过。\nCopyright (c) 2026 fuyuan9972-svg\nMIT 许可和第三方说明随程序提供。\n\n当前构建未签名。默认机械声为原创合成声，原版录音须另行取得授权。"));
        return bar;
    }
    async Task PrepareStartup(bool afterStartup=true)
    {
        // Restore the layout immediately. Only visible, screen-sized resources
        // are warmed; the independent lens lane requests HD on demand.
        board.Invalidate();_=WarmRestoredVisible();
        if(afterStartup)await AfterStartup();
    }
    async Task WarmRestoredVisible()
    {
        try{await renderer.PrepareVisibleTable();if(!closed)status.Text="台面预览已就绪 · 高清按需载入";}
        catch(Exception ex){session.Storage.Log($"恢复可见照片失败 {ex}");if(!closed)status.Text="部分照片无法读取 · 文件菜单可重试";}
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
            var dialog=new ContentDialog{XamlRoot=root.XamlRoot,Title="让照片回到光里",Content=new TextBlock{Text="底栏始终保留全部照片；上台后显示“已上台”，原顺序和滚动位置保持。\n\n相册功能已移除，旧相册照片自动归入对应栏目，原布局独立备份。\n\n原图只读，普通/凑近四档倍率均实际生效。",TextWrapping=TextWrapping.Wrap,MaxWidth=430},PrimaryButtonText="开始观片",SecondaryButtonText="看看怎么用",DefaultButton=ContentDialogButton.Primary};
            options.GuideVersion=BuildInfo.Version;WriteOptions(options);
            var result=await dialog.ShowAsync();
            if(result==ContentDialogResult.Secondary)
            {
                var steps=new[]{("导入与独立栏目","选择文件夹会追加到当前栏目。点＋新建空白栏目；每栏拥有自己的照片、台面和视角。"),("取片与收回","底栏始终保留全部照片，已上台照片显示“已上台”。选中台面照片按 Delete 或右键移出台面，只清除上台标记；底栏顺序与位置保持。支持 Ctrl 多选。"),("删除照片或栏目","点选底栏照片后按 Delete 删除该张，同时移除当前台面对应散片，其他栏目和原图保留。右键栏目或用⋯菜单删除整栏，其他栏目独立保留。"),("普通放大镜","V 开启放大镜。中心75%保持直线采样，外围有轻微曲率与透明反光。普通镜片也使用所选1×/2×/4×/8×。"),("凑近与倍率","按住 Space 凑近，松开返回并保留灯开关和倍率。Tab、点击或横向拖动胶囊切换四档。高清预览最长边2200px。"),("调整照片区","按住两区之间横线的中央抓手上下拖动，调整照片区与底栏高度；缩略图同步缩放。上下键微调，Home或⋯菜单恢复默认，重启保留尺寸。")};
                for(int i=0;i<steps.Length;i++)
                {
                    var step=new ContentDialog{XamlRoot=root.XamlRoot,Title=steps[i].Item1,Content=new TextBlock{Text=steps[i].Item2,TextWrapping=TextWrapping.Wrap,MaxWidth=430},PrimaryButtonText=i==steps.Length-1?"开始观片":"下一步",SecondaryButtonText=i>0?"上一步":"",CloseButtonText="跳过"};
                    var selected=await step.ShowAsync();if(selected==ContentDialogResult.None)break;if(selected==ContentDialogResult.Secondary)i-=2;
                }
            }
        }
        finally{dialogOpen=false;board.Focus(FocusState.Programmatic);}
    }
    sealed class UserOptions{public string GuideVersion{get;set;}="";public bool PeriodicChecks{get;set;}=true;public bool ReduceMotion{get;set;}public double TrayHeight{get;set;}=PanelSizing.DefaultTrayHeight;}
    UserOptions ReadOptions(){try{return JsonSerializer.Deserialize<UserOptions>(File.ReadAllText(System.IO.Path.Combine(session.Storage.Root,"config","settings.json")))??new();}catch{return new();}}
    void WriteOptions(UserOptions options)
    {
        try{var path=System.IO.Path.Combine(session.Storage.Root,"config","settings.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(options));File.Move(path+".tmp",path,true);}catch(Exception ex){session.Storage.Log($"设置保存失败 {ex}");}
    }
}
