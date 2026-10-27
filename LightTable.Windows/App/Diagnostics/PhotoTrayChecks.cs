using LightTable.Core;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckPersistentPhotoTray(string[] originals)
    {
        var doc=session.Document;var first=doc.EnsureCollection();var oldSelection=session.Selection.ToArray();var existing=doc.Collections.Select(c=>c.Id).ToHashSet();
        var folder=Path.Combine(session.Storage.Root,"tray-100-fixtures");Directory.CreateDirectory(folder);
        try
        {
            session.NewCollection();var column=doc.EnsureCollection();var photos=new List<Photo>();
            for(int i=0;i<100;i++){var path=Path.Combine(folder,$"{i:000}.jpg");File.Copy(originals[i%originals.Length],path,true);photos.Add(new(){Path=path,Aspect=doc.Aspect(originals[i%originals.Length])});}
            doc.SetCollectionPhotos(column,photos,false);session.LibraryChanged();await Task.Delay(80);
            if(trayItems.Count!=100)throw new InvalidOperationException("初始底栏不是100张");
            var items=trayItems.ToArray();
            // Realization changes the virtualized extent; wait for a stable
            // end-of-list layout before measuring whether placement moves it.
            for(int attempt=0;attempt<12;attempt++)
            {
                var end=trayScroller?.ScrollableWidth??0;trayScroller?.ChangeView(end,null,null,true);await Task.Delay(80);
                if(end>0&&Math.Abs((trayScroller?.HorizontalOffset??0)-end)<1&&Math.Abs((trayScroller?.ScrollableWidth??0)-end)<1)break;
            }
            var offset=trayScroller?.HorizontalOffset??0;if(offset<100)throw new InvalidOperationException("底栏没有实际滚动到后面的照片");
            foreach(var p in photos.Take(60))doc.Place(p.Path,new(.5,.5));session.Notify();await Task.Delay(80);
            if(trayItems.Count!=100||trayItems.Count(p=>p.Mark=="已上台")!=60||trayItems.Count(p=>p.Mark=="")!=40)throw new InvalidOperationException("100张/60张已上台/40张未上台数量错误");
            if(!trayItems.Select(p=>p.Path).SequenceEqual(photos.Select(p=>p.Path))||items.Where((p,i)=>!ReferenceEquals(p,trayItems[i])).Any())throw new InvalidOperationException("上台重建或改变了底栏列表");
            if(Math.Abs((trayScroller?.HorizontalOffset??0)-offset)>1)throw new InvalidOperationException($"上台导致底栏滚动位置跳动：{offset:F1}→{trayScroller?.HorizontalOffset:F1}");
            var id=doc.Slides[0].Id;session.Select(id);session.RemoveSelection();await Task.Delay(80);
            if(trayItems.Count!=100||trayItems[0].Mark!=""||trayItems.Count(p=>p.Mark=="已上台")!=59||!ReferenceEquals(items[0],trayItems[0]))throw new InvalidOperationException("移出台面改变底栏成员或标记错误");
            session.Flush();var restored=new DocumentStore(session.Storage.Root).Load(out var warning);if(warning!=null||restored.ActivePhotos.Count!=100||restored.Slides.Count!=59||restored.Albums.Count!=0)throw new InvalidOperationException("100张/59张状态保存恢复错误");
            return $"真实ListView模型100张，上台60张标记、40张保留；全部对象/顺序和滚动位置({offset:F1})不变。移出台面后100张/59个标记；新Store恢复一致；无相册项目。";
        }
        finally
        {
            session.SwitchCollection(first.Id);doc.Collections.RemoveAll(c=>!existing.Contains(c.Id));doc.Photos.RemoveAll(p=>p.Path.StartsWith(folder+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));session.Selection.Clear();session.Selection.UnionWith(oldSelection);session.LibraryChanged();renderer.DocumentChanged();
        }
    }
}
