using System.Security.Cryptography;
using LightTable.Core;

namespace LightTable.Windows.Imaging;

public sealed record ImportResult(List<Photo> Photos,List<string> Errors,int Duplicates,IReadOnlyList<string> AcceptedPaths);
public sealed class PhotoImporter(ImageStore images)
{
    static readonly HashSet<string> extensions=new(StringComparer.OrdinalIgnoreCase){".jpg",".jpeg",".png",".tif",".tiff",".bmp",".gif",".heic",".heif",".webp",".avif",".dng",".cr2",".cr3",".nef",".arw",".rw2",".orf",".raf"};
    public async Task<ImportResult> ImportAsync(IEnumerable<string> inputs,IEnumerable<Photo> existing,IProgress<(int Done,int Total)>? progress=null,CancellationToken cancellation=default)
    {
        var errors=new List<string>();var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var directories=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Scan(string path)
        {
            cancellation.ThrowIfCancellationRequested();
            try
            {
                path=Path.GetFullPath(path);
                if(Directory.Exists(path))
                {
                    if(!directories.Add(path)||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)return;
                    foreach(var entry in Directory.EnumerateFileSystemEntries(path))
                    {
                        var name=Path.GetFileName(entry);var attributes=File.GetAttributes(entry);
                        if(name.StartsWith('.')||(attributes&FileAttributes.Hidden)!=0)continue;
                        if((attributes&FileAttributes.Directory)!=0&&new[]{".app",".bundle",".photoslibrary",".photolibrary",".xcodeproj",".playground"}.Contains(Path.GetExtension(entry),StringComparer.OrdinalIgnoreCase))continue;
                        Scan(entry);
                    }
                }
                else if(File.Exists(path)){if(extensions.Contains(Path.GetExtension(path)))paths.Add(path);}
                else errors.Add($"文件不存在：{path}");
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){errors.Add($"{path}：{ex.Message}");}
        }
        await Task.Run(()=>{foreach(var p in inputs)Scan(p);},cancellation);
        var previous=existing.ToArray();var knownPaths=previous.Select(p=>p.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hashes=previous.Where(p=>p.ContentHash!=null).GroupBy(p=>p.ContentHash!).ToDictionary(g=>g.Key,g=>g.First().Path,StringComparer.Ordinal);
        var photos=new List<Photo>();var accepted=new List<string>();int duplicates=0,done=0;
        foreach(var path in paths.Order(NaturalPathComparer.Instance))
        {
            cancellation.ThrowIfCancellationRequested();
            try
            {
                if(knownPaths.Contains(path)){duplicates++;accepted.Add(path);continue;}
                using var source=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,65536,true);
                var hash=Convert.ToHexString(await SHA256.HashDataAsync(source,cancellation));
                if(hashes.TryGetValue(hash,out var original)){duplicates++;accepted.Add(original);continue;}
                var decoded=await images.GetAsync(path,220);
                photos.Add(new Photo{Path=path,Aspect=decoded.Aspect,ContentHash=hash});knownPaths.Add(path);hashes[hash]=path;accepted.Add(path);
            }
            catch(Exception ex) when(ex is not OperationCanceledException){errors.Add($"{Path.GetFileName(path)}：{ex.Message}");}
            finally{progress?.Report((++done,paths.Count));}
        }
        return new(photos,errors,duplicates,accepted.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
sealed class NaturalPathComparer : IComparer<string>
{
    public static readonly NaturalPathComparer Instance=new();
    public int Compare(string? a,string? b)
    {
        if(a==null||b==null)return StringComparer.Ordinal.Compare(a,b);
        var x=Path.GetFileName(a);var y=Path.GetFileName(b);int i=0,j=0;
        while(i<x.Length&&j<y.Length)
        {
            if(char.IsDigit(x[i])&&char.IsDigit(y[j]))
            {
                int s=i,t=j;while(i<x.Length&&char.IsDigit(x[i]))i++;while(j<y.Length&&char.IsDigit(y[j]))j++;
                var nx=x[s..i].TrimStart('0');var ny=y[t..j].TrimStart('0');var c=nx.Length.CompareTo(ny.Length);if(c==0)c=StringComparer.Ordinal.Compare(nx,ny);if(c!=0)return c;
            }
            else{var c=char.ToUpperInvariant(x[i++]).CompareTo(char.ToUpperInvariant(y[j++]));if(c!=0)return c;}
        }
        var remaining=(x.Length-i).CompareTo(y.Length-j);return remaining!=0?remaining:StringComparer.OrdinalIgnoreCase.Compare(a,b);
    }
}
