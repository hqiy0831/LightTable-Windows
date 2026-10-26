namespace LightTable.Core;

public sealed partial class TableDocument
{
    public PhotoCollection EnsureCollection()
    {
        if(Collections.Count==0)
        {
            var first=new PhotoCollection{Paths=Photos.Select(p=>p.Path).ToList(),SourceLabel=SourceLabel};
            Collections.Add(first);ActiveCollectionId=first.Id;
        }
        return Collections.Find(c=>c.Id==ActiveCollectionId)??Collections[0];
    }
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<Photo> ActivePhotos
    {
        get{var index=Photos.ToDictionary(p=>p.Path,StringComparer.OrdinalIgnoreCase);return EnsureCollection().Paths.Where(index.ContainsKey).Select(p=>index[p]).ToArray();}
    }
    public PhotoCollection AddCollection(string? name=null)
    {
        EnsureCollection();var collection=new PhotoCollection{Name=string.IsNullOrWhiteSpace(name)?$"栏目 {Collections.Count+1}":name.Trim()};
        Collections.Add(collection);ActiveCollectionId=collection.Id;return collection;
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
        // Membership only: placed slides, album pockets, other columns and source files survive.
        return EnsureCollection().Paths.RemoveAll(removed.Contains);
    }
    void NormalizeCollections()
    {
        Collections=(Collections??[]).OfType<PhotoCollection>().DistinctBy(c=>c.Id).ToList();
        EnsureCollection();var catalog=Photos.Select(p=>p.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var collection in Collections)
        {
            collection.Paths=(collection.Paths??[]).Where(p=>IsLocal(p)&&catalog.Contains(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if(string.IsNullOrWhiteSpace(collection.Name))collection.Name="未命名栏目";
        }
        if(!Collections.Any(c=>c.Id==ActiveCollectionId))ActiveCollectionId=Collections[0].Id;
    }
}
