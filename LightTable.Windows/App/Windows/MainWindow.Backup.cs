using LightTable.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    CancellationTokenSource? backupCancellation;
    async Task<string?> PickBackupFolder()
    {
        var picker=new FolderPicker();picker.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));
        return (await picker.PickSingleFolderAsync())?.Path;
    }
    async Task RunColumnBackup(bool restore)
    {
        if(session.Busy||dialogOpen||closed||root.XamlRoot==null)return;
        dialogOpen=true;session.CloseViewing=false;ResetWorkspaceInteraction();session.Busy=true;session.Notify(false);
        string? message=null,error=null;ContentDialog? dialog=null;
        RestoredColumn? pendingImport=null;bool committed=false;
        var managed=System.IO.Path.Combine(session.Storage.Root,"library","imported-columns");
        using var cancellation=new CancellationTokenSource();backupCancellation=cancellation;
        try
        {
            var folder=await PickBackupFolder();if(folder==null||closed)return;
            var name=session.Document.EnsureCollection().Name;
            var source=new ColumnBackupSource(name,session.Document.ActivePhotos.Select(p=>new BackupSourcePhoto(p.Path,p.Aspect)).ToArray());
            var text=new TextBlock{Text=restore?"检查栏目备份…":"按底栏顺序复制全部照片…",TextWrapping=TextWrapping.Wrap,MaxWidth=430};
            var bar=new ProgressBar{Minimum=0,Maximum=1,IsIndeterminate=true};var panel=new StackPanel{Spacing=14};panel.Children.Add(text);panel.Children.Add(bar);
            panel.Children.Add(new TextBlock{Text=restore?"校验完整后新建栏目，台面为空白；已有栏目保持原样。":"只复制原照片，不移动、不修改源文件。请将生成的整个文件夹保存到网盘。",TextWrapping=TextWrapping.Wrap,MaxWidth=430,Opacity=.7});
            dialog=new ContentDialog{XamlRoot=root.XamlRoot,Title=restore?"导入栏目备份":"备份当前栏目",Content=panel,CloseButtonText="取消"};dialog.CloseButtonClick+=(_,_)=>cancellation.Cancel();
            var progress=new Progress<BackupProgress>(p=>
            {
                if(closed||cancellation.IsCancellationRequested)return;
                bar.IsIndeterminate=false;bar.Value=p.TotalBytes>0?Math.Clamp((double)p.CopiedBytes/p.TotalBytes,0,1):p.Total>0?(double)p.Completed/p.Total:1;
                text.Text=$"{(restore?"校验并导入":"复制照片")} {p.Completed} / {p.Total}\n{p.CopiedBytes/1048576d:F1} / {p.TotalBytes/1048576d:F1} MiB\n{p.FileName}";
            });
            var shown=dialog.ShowAsync();
            try
            {
                if(restore)
                {
                    var imported=await Task.Run(()=>ColumnBackup.RestoreAsync(folder,managed,progress,cancellation.Token));pendingImport=imported;
                    cancellation.Token.ThrowIfCancellationRequested();if(closed)return;
                    session.AddRestoredColumn(imported);committed=true;renderer.DocumentChanged();
                    message=$"已导入栏目「{imported.Name}」，底栏 {imported.Photos.Count} 张照片，台面为空白。\n\n照片副本已保存到应用数据目录；可移动或删除下载的备份文件夹，当前导入不依赖它。原始照片和其他栏目保持原样。";
                }
                else
                {
                    var exported=await Task.Run(()=>ColumnBackup.ExportAsync(source,folder,progress,cancellation.Token));
                    message=$"已备份栏目「{source.Name}」，共 {exported.PhotoCount} 张照片。\n\n{exported.Folder}\n\n请将整个文件夹（包括 photos 和 {ColumnBackup.ManifestName}）保存到网盘。下载完整后使用「导入栏目备份」选择该文件夹。上台状态和台面摆放不在本备份中。";
                }
            }
            finally{if(!closed)dialog.Hide();await shown;}
        }
        catch(OperationCanceledException){if(!closed)status.Text="栏目备份操作已取消";}
        catch(Exception ex){session.Storage.Log($"栏目备份失败：{ex}");error=ex.Message;}
        finally
        {
            if(pendingImport!=null&&!committed)try{ColumnBackup.DiscardUncommittedRestore(pendingImport,managed);}catch(Exception ex){session.Storage.Log($"未提交的导入副本清理失败：{ex.Message}");}
            backupCancellation=null;session.Busy=false;dialogOpen=false;if(!closed){session.Notify(false);board.Focus(FocusState.Programmatic);}
        }
        if(!closed&&error!=null)await ShowMessage("栏目备份未完成",error+"\n\n已有栏目和原照片不会被替换或删除。");
        else if(!closed&&message!=null)await ShowMessage(restore?"栏目已导入":"栏目备份完成",message);
    }
}
