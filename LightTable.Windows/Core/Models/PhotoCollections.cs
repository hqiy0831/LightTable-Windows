namespace LightTable.Core;

public sealed partial class TableDocument
{
    public PhotoCollection EnsureCollection()
    {
        if(Collections.Count==0)
        {
            var first=new PhotoCollection{Paths=Photos.Select(p=>p.Path).ToList(),SourceLabel=SourceLabel,Table=CaptureTable()};
            Collections.Add(first);ActiveCollectionId=first.Id;
        }
        return Collections.Find(c=>c.Id==ActiveCollectionId)??Collections[0];
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<Photo> ActivePhotos
    {
        get{var index=Photos.ToDictionary(p=>p.Path,StringComparer.OrdinalIgnoreCase);return EnsureCollection().Paths.Where(index.ContainsKey).Select(p=>index[p]).ToArray();}
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<Photo> AvailablePhotos
    {get{var placed=Slides.Select(s=>s.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);return ActivePhotos.Where(p=>!placed.Contains(p.Path)).ToArray();}}
    public PhotoCollection AddCollection(string? name=null)
    {
        EnsureCollection();var collection=new PhotoCollection{Name=string.IsNullOrWhiteSpace(name)?$"栏目 {Collections.Count+1}":name.Trim(),Table=new()};
        Collections.Add(collection);SwitchCollection(collection.Id);return collection;
    }
    CollectionTable CaptureTable()=>new(){Slides=Slides,Albums=Albums,TableOrder=TableOrder,Viewport=Viewport,WorldWidth=WorldWidth,WorldHeight=WorldHeight,FitScale=FitScale};
    void ApplyTable(CollectionTable table)
    {Slides=table.Slides;Albums=table.Albums;TableOrder=table.TableOrder;Viewport=table.Viewport;WorldWidth=table.WorldWidth;WorldHeight=table.WorldHeight;FitScale=table.FitScale;}
    public void SaveActiveCollection()
    {var current=EnsureCollection();current.Table=CaptureTable();current.SourceLabel=SourceLabel;}
    public void SwitchCollection(Guid id)
    {
        var next=Collections.Find(c=>c.Id==id)??throw new ArgumentException("栏目不存在");
        if(next.Id==EnsureCollection().Id)return;
        SaveActiveCollection();ActiveCollectionId=next.Id;ApplyTable(next.Table??=new());SourceLabel=next.SourceLabel;
    }
    public void SetCollectionPhotos(PhotoCollection collection,IEnumerable<Photo> photos,bool replace)
    {
        if(!Collections.Contains(collection))throw new ArgumentException("栏目已不存在");
        if(replace)collection.Paths.Clear();var known=Photos.ToDictionary(p=>p.Path,StringComparer.OrdinalIgnoreCase);
        var paths=collection.Paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var photo in photos)
        {
            if(!known.TryGetValue(photo.Path,out var metadata)){Photos.Add(photo);known[photo.Path]=photo;}
            else{metadata.Aspect=photo.Aspect;metadata.ContentHash=photo.ContentHash;metadata.SourceLength=photo.SourceLength;metadata.SourceWriteTicks=photo.SourceWriteTicks;}
            if(paths.Add(photo.Path))collection.Paths.Add(photo.Path);
        }
    }
    public int RemoveFromCollection(IEnumerable<string> paths)
    {
        var removed=paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var count=EnsureCollection().Paths.RemoveAll(removed.Contains);
        // Upstream 0.1.74 removes matching loose films, while albums, other
        // workspaces and disk originals retain their own references.
        var ids=Slides.Where(s=>removed.Contains(s.Path)).Select(s=>s.Id).ToHashSet();
        Slides.RemoveAll(s=>ids.Contains(s.Id));TableOrder.RemoveAll(ids.Contains);PruneCatalog();return count;
    }
    public void ReturnToCollection(IEnumerable<Guid> ids)
    {
        var set=ids.ToHashSet();var collection=EnsureCollection();var paths=collection.Paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var slide in Slides.Where(s=>set.Contains(s.Id)))if(paths.Add(slide.Path))collection.Paths.Add(slide.Path);
        Remove(set);
    }
    public void DeleteCollection(Guid id)
    {
        var index=Collections.FindIndex(c=>c.Id==id);if(index<0)return;
        SaveActiveCollection();var wasActive=ActiveCollectionId==id;Collections.RemoveAt(index);
        if(Collections.Count==0)Collections.Add(new(){Name="栏目一",Table=new()});
        if(wasActive){var next=Collections[Math.Min(index,Collections.Count-1)];ActiveCollectionId=next.Id;ApplyTable(next.Table??=new());SourceLabel=next.SourceLabel;}
        PruneCatalog();
    }
    void PruneCatalog()
    {
        SaveActiveCollection();var retained=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var collection in Collections)
        {
            retained.UnionWith(collection.Paths);
            if(collection.Table is not {} table)continue;
            retained.UnionWith(table.Slides.Select(s=>s.Path));retained.UnionWith(table.Albums.SelectMany(a=>a.Pockets).Select(p=>p.Path).OfType<string>());
        }
        Photos.RemoveAll(p=>!retained.Contains(p.Path));
    }
    void NormalizeCollections()
    {
        Collections=(Collections??[]).OfType<PhotoCollection>().DistinctBy(c=>c.Id).ToList();
        EnsureCollection();if(!Collections.Any(c=>c.Id==ActiveCollectionId))ActiveCollectionId=Collections[0].Id;
        if(SchemaVersion<3&&Collections.All(c=>c.Table==null))MigrateSharedTable();
        // Root table is the active workspace, retained for legacy import/export.
        // Every other workspace has its own independently sanitized layout.
        var active=EnsureCollection();active.Table=CaptureTable();
        foreach(var collection in Collections.Where(c=>c.Id!=active.Id))
        {
            var current=CaptureTable();ApplyTable(collection.Table??new());SanitizeTable();collection.Table=CaptureTable();ApplyTable(current);
        }
        var catalog=Photos.Select(p=>p.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var collection in Collections)
        {
            collection.Paths=(collection.Paths??[]).Where(p=>IsLocal(p)&&catalog.Contains(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if(string.IsNullOrWhiteSpace(collection.Name))collection.Name="未命名栏目";
        }
    }
    void MigrateSharedTable()
    {
        var shared=CaptureTable();var fallback=EnsureCollection();
        var membership=Collections.ToDictionary(c=>c.Id,c=>(c.Paths??[]).ToHashSet(StringComparer.OrdinalIgnoreCase));
        foreach(var collection in Collections)
        {
            var paths=membership[collection.Id];
            var slides=shared.Slides.Where(s=>paths.Contains(s.Path)||collection==fallback&&!membership.Values.Any(p=>p.Contains(s.Path))).Select(s=>new Slide{Id=s.Id,Path=s.Path,X=s.X,Y=s.Y,Width=s.Width,Angle=s.Angle}).ToList();
            // Legacy albums had no column owner. Keep each once, with the first
            // matching photo column (or the old active column if empty/unmatched).
            var albums=shared.Albums.Where(a=>(Collections.FirstOrDefault(c=>a.Pockets.Any(p=>p.Path!=null&&membership[c.Id].Contains(p.Path)))??fallback)==collection).ToList();
            collection.Table=new(){Slides=slides,Albums=albums,TableOrder=shared.TableOrder.ToList(),Viewport=new(){Scale=shared.Viewport.Scale,OffsetX=shared.Viewport.OffsetX,OffsetY=shared.Viewport.OffsetY},WorldWidth=shared.WorldWidth,WorldHeight=shared.WorldHeight,FitScale=shared.FitScale};
        }
        ApplyTable(fallback.Table!);
    }
}
