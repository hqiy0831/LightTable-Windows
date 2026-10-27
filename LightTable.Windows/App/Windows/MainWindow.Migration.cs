using LightTable.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task ImportMacLayout()
    {
        if(dialogOpen||session.Busy||root.XamlRoot==null)return;dialogOpen=true;session.CloseViewing=false;CancelGesture();
        string? error=null;bool imported=false;
        try
        {
            var picker=new FileOpenPicker();picker.FileTypeFilter.Add(".json");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));
            var source=await picker.PickSingleFileAsync();if(source==null)return;
            if(new FileInfo(source.Path).Length>64L*1024*1024)throw new InvalidDataException("布局超过64MiB读取限制。");
            var bytes=await File.ReadAllBytesAsync(source.Path);var directories=MacSavedImporter.SourceDirectories(bytes);
            var fields=new List<(TextBox Mac,TextBox Windows)>();var rows=new StackPanel{Spacing=16};
            var content=new StackPanel{Spacing=18,MaxWidth=590};content.Children.Add(new TextBlock{Text="选择与原 Mac 文件夹相对应的 Windows 文件夹。照片需事先复制到 Windows；文件夹内的相对路径会保留。",TextWrapping=TextWrapping.Wrap});content.Children.Add(rows);
            void AddRow(string prefix)
            {
                var mac=new TextBox{Header="原 Mac 文件夹",Text=prefix,PlaceholderText="/Users/你的名字/Pictures"};
                var windows=new TextBox{Header="Windows 文件夹",PlaceholderText="选择已复制照片的文件夹"};
                var row=new StackPanel{Spacing=8};row.Children.Add(mac);row.Children.Add(windows);
                var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12};var browse=new Button{Content="选择 Windows 文件夹…"};
                browse.Click+=async(_,_)=>
                {
                    try{var folders=new FolderPicker();folders.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(folders,WinRT.Interop.WindowNative.GetWindowHandle(this));var folder=await folders.PickSingleFolderAsync();if(folder!=null)windows.Text=folder.Path;}
                    catch(Exception ex){session.Storage.Log($"迁移文件夹选择失败 {ex}");windows.Header="Windows 文件夹 · 选择失败，可手动输入路径";}
                };
                var remove=new Button{Content="移除此映射"};remove.Click+=(_,_)=>{fields.Remove((mac,windows));rows.Children.Remove(row);};buttons.Children.Add(browse);buttons.Children.Add(remove);row.Children.Add(buttons);fields.Add((mac,windows));rows.Children.Add(row);
            }
            var common=directories.FirstOrDefault()??"/";foreach(var directory in directories.Skip(1))while(common!="/"&&directory!=common&&!directory.StartsWith(common+"/",StringComparison.Ordinal)){var end=common.LastIndexOf('/');common=end<=0?"/":common[..end];}
            AddRow(common);var add=new Button{Content="添加另一个文件夹映射"};add.Click+=(_,_)=>AddRow("");content.Children.Add(add);
            var note=new TextBlock{Text="仅导入布局引用，原照片和 Mac 布局保持原样。确认导入前，会显示照片数量和缺失文件数量。",TextWrapping=TextWrapping.Wrap,Opacity=.7};content.Children.Add(note);
            while(!closed)
            {
                var mappingDialog=new ContentDialog{XamlRoot=root.XamlRoot,Title="导入 Mac 版布局",Content=new ScrollViewer{Content=content,MaxHeight=430},PrimaryButtonText="检查布局",CloseButtonText="取消"};
                if(await mappingDialog.ShowAsync()!=ContentDialogResult.Primary)return;
                MacImportResult result;int unreadable=0;
                try
                {
                    var mappings=fields.Select(f=>new MacPathMap(f.Mac.Text.Trim(),f.Windows.Text.Trim())).ToArray();
                    session.Busy=true;status.Text="正在检查 Mac 布局…";result=await Task.Run(()=>MacSavedImporter.Import(bytes,mappings));
                    int done=0;
                    foreach(var photo in result.Document.Photos)
                    {
                        if(closed)return;
                        if(File.Exists(photo.Path))try{photo.Aspect=await session.Images.ReadAspectAsync(photo.Path);}catch(Exception ex){unreadable++;session.Storage.Log($"迁移照片几何读取失败 {photo.Path}: {ex.Message}");}
                        status.Text=$"检查照片尺寸 {++done} / {result.Document.Photos.Count}";
                    }
                }
                catch(Exception ex){note.Text=ex.Message;continue;}
                finally{session.Busy=false;}
                var summary=$"照片 {result.Document.Photos.Count} 张 · 台面散片 {result.Document.Slides.Count} 张\n旧相册 {result.Document.Albums.Count} 页（照片将加入对应栏目，不保留相册功能） · 已验证旧拖片别名 {result.LegacyAliases} 个\n\n"+
                    (result.MissingPaths.Count==0?"所有引用文件均已找到。":$"有 {result.MissingPaths.Count} 张照片尚未找到，导入时会保留它们的位置：\n"+string.Join("\n",result.MissingPaths.Take(6)))+
                    (unreadable>0?$"\n另有 {unreadable} 张文件存在但无法读取尺寸，将保留布局并在启动准备中提示。":"")+
                    "\n\n导入将替换当前图库和台面布局。当前布局和原 Mac JSON 会先存入数据目录 layouts/imports 的独立备份。";
                var preview=new ContentDialog{XamlRoot=root.XamlRoot,Title="检查导入结果",Content=new ScrollViewer{Content=new TextBlock{Text=summary,TextWrapping=TextWrapping.Wrap,MaxWidth=550},MaxHeight=430},PrimaryButtonText="导入并替换布局",SecondaryButtonText="返回修改",CloseButtonText="取消"};
                var choice=await preview.ShowAsync();if(choice==ContentDialogResult.Secondary)continue;if(choice!=ContentDialogResult.Primary)return;
                photoCount=-1;var archive=session.ImportMacDocument(result.Document,bytes);renderer.DocumentChanged();session.Storage.Log($"Mac布局导入备份：{archive}");imported=true;break;
            }
        }
        catch(Exception ex){session.Storage.Log($"Mac布局导入失败 {ex}");error=ex.Message;}
        finally{dialogOpen=false;if(!closed){status.Text=session.Document.SourceLabel;board.Focus(FocusState.Programmatic);}}
        if(error!=null&&!closed)await ShowMessage("无法导入布局",error);
        if(imported&&!closed)await PrepareStartup(false);
    }
}
