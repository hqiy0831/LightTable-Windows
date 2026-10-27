namespace LightTable.Core;

public sealed partial class TableDocument
{
    // Album models remain readable solely for old Windows/Mac layouts.
    // Carry every pocket's original reference into its own column, then remove
    // the obsolete sheet. Never alter source files or another column's order.
    public int ConvertLegacyAlbumsToPhotos()
    {
        SaveActiveCollection();var removed=0;
        var catalog=Photos.ToDictionary(p=>p.Path,StringComparer.OrdinalIgnoreCase);
        foreach(var column in Collections)
        {
            if(column.Table is not {} table)continue;
            var members=column.Paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ids=table.Albums.Select(a=>a.Id).ToHashSet();
            foreach(var path in table.Albums.SelectMany(a=>a.Pockets).Select(p=>p.Path).OfType<string>().Where(IsLocal))
            {
                if(members.Add(path))column.Paths.Add(path);
                if(!catalog.ContainsKey(path)){var photo=new Photo{Path=path};Photos.Add(photo);catalog.Add(path,photo);}
            }
            removed+=table.Albums.Count;table.Albums.Clear();table.TableOrder.RemoveAll(ids.Contains);
        }
        ApplyTable(EnsureCollection().Table!);return removed;
    }
}
