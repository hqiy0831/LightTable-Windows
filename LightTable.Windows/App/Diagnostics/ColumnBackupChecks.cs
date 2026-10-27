using LightTable.Core;
using System.Text.Json;
using System.Security.Cryptography;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckColumnBackup()
    {
        var first=session.Document.EnsureCollection();var prior=JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions);var priorSelection=session.Selection.ToArray();
        var source=new ColumnBackupSource(first.Name,session.Document.ActivePhotos.Select(p=>new BackupSourcePhoto(p.Path,p.Aspect)).ToArray());
        var parent=Path.Combine(session.Storage.Root,"backup-check");Directory.CreateDirectory(parent);
        var exported=await ColumnBackup.ExportAsync(source,parent);var imported=await ColumnBackup.RestoreAsync(exported.Folder,Path.Combine(session.Storage.Root,"library","imported-columns"));
        var oldCount=session.Document.Collections.Count;var oldFiles=source.Photos.ToDictionary(p=>p.Path,p=>SHA256.HashData(File.ReadAllBytes(p.Path)));
        try
        {
            // A locked layout exercises the actual atomic save failure before
            // the live session is replaced, without touching any user data.
            session.Flush();
            using(var locked=new FileStream(session.Storage.DocumentPath,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                bool failed=false;try{session.AddRestoredColumn(imported);}catch(IOException){failed=true;}
                if(!failed||session.Document.Collections.Count!=oldCount||JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions)!=prior)throw new InvalidOperationException("导入保存失败时改变了已有栏目");
            }
            session.AddRestoredColumn(imported);renderer.DocumentChanged();
            if(session.Document.Collections.Count!=oldCount+1||session.Document.ActiveCollectionId==first.Id||session.Document.Slides.Count!=0||trayItems.Count!=source.Photos.Count)throw new InvalidOperationException("备份导入没有新建空台面的完整底栏栏目");
            for(int i=0;i<source.Photos.Count;i++)
            {
                var photo=session.Document.ActivePhotos[i];if(photo.Name!=Path.GetFileNameWithoutExtension(source.Photos[i].Path)||!SHA256.HashData(File.ReadAllBytes(photo.Path)).SequenceEqual(oldFiles[source.Photos[i].Path]))throw new InvalidOperationException("恢复后的顺序、名称或原字节不一致");
                await session.Images.ReadAspectAsync(photo.Path);
            }
            var newId=session.Document.ActiveCollectionId;var stored=new DocumentStore(session.Storage.Root).Load(out var warning);if(warning!=null||stored.ActiveCollectionId!=newId||stored.ActivePhotos.Count!=source.Photos.Count)throw new InvalidOperationException("备份栏目没有实际保存");
            stored.SwitchCollection(first.Id);session.SwitchCollection(first.Id);
            var originalColumn=session.Document.Collections.Single(c=>c.Id==first.Id);if(originalColumn.Paths.Count!=source.Photos.Count)throw new InvalidOperationException("导入修改了原栏目的成员");
            session.Document.DeleteCollection(newId);session.Selection.Clear();session.Selection.UnionWith(priorSelection);session.LibraryChanged();renderer.DocumentChanged();
            if(JsonSerializer.Serialize(session.Document,DocumentStore.JsonOptions)!=prior)throw new InvalidOperationException("移除导入测试栏目后原工作区发生变化");
            return $"实际栏目备份/恢复{source.Photos.Count}张JPEG：原字节、原名、顺序与WIC尺寸可读；新建空台面的完整底栏，保存恢复独立。生产布局锁定时导入失败且原栏目不变，成功导入后原栏目也不变。";
        }
        finally{session.SwitchCollection(first.Id);session.Selection.Clear();session.Selection.UnionWith(priorSelection);session.LibraryChanged();renderer.DocumentChanged();}
    }
}
