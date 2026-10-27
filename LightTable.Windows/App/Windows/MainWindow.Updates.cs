using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    bool checking;
    async Task CheckUpdates(bool manual,bool periodic=false)
    {
        if(checking||closed)return;checking=true;
        try
        {
            var release=await new UpdateService(session.Storage).CheckAsync(manual,periodic);
            if(release==null){if(manual)await ShowMessage("检查更新","没有适用于 Windows x64 的更新。macOS 安装包不会用于此应用。");return;}
            if(dialogOpen||closed)return;dialogOpen=true;
            ContentDialogResult result;
            try
            {
                result=await new ContentDialog{XamlRoot=root.XamlRoot,Title=$"Windows 新版本 {release.Tag}",Content=new ScrollViewer{MaxHeight=430,Content=new TextBlock{Text=release.Notes+"\n\n当前没有可信发行者签名配置。下载后请从发行页面核对并手动安装，程序不会自动运行安装包。SHA256 仅验证文件完整性。",TextWrapping=TextWrapping.Wrap,MaxWidth=580}},PrimaryButtonText="后台下载",SecondaryButtonText="打开发行页面",CloseButtonText="稍后"}.ShowAsync();
            }
            finally{dialogOpen=false;}
            if(result==ContentDialogResult.Secondary)System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(release.Page.AbsoluteUri){UseShellExecute=true});
            if(result==ContentDialogResult.Primary)
            {
                status.Text="正在后台下载更新…";
                var file=await new UpdateService(session.Storage).DownloadAsync(release,new Progress<double>(p=>status.Text=$"下载更新 {p:P0}"));
                session.Flush();await ShowMessage("更新已下载",$"{file}\n\n请手动核对发行者并安装。当前没有可信发行签名，自动执行已禁用；当前应用保持完整。");Reveal(file);status.Text="更新已下载";
            }
        }
        catch(Exception ex){session.Storage.Log($"更新检查/下载失败 {ex}");if(manual)await ShowMessage("暂时无法检查更新",ex.Message+"\n可以稍后重试。当前应用和照片保持完整。");}
        finally{checking=false;}
    }
}
