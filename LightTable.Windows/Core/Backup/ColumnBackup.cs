using System.Security.Cryptography;
using System.Text.Json;

namespace LightTable.Core;

public sealed record BackupSourcePhoto(string Path,double Aspect);
public sealed record ColumnBackupSource(string Name,IReadOnlyList<BackupSourcePhoto> Photos);
public sealed record BackupProgress(int Completed,int Total,long CopiedBytes,long TotalBytes,string FileName);
public sealed record ExportedColumn(string Folder,int PhotoCount,long Bytes);
public sealed record RestoredColumn(string Name,string Folder,IReadOnlyList<Photo> Photos);

// A portable folder of original byte copies plus a relative-path manifest.
// It deliberately contains only the bottom tray, not machine paths or stage state.
public static class ColumnBackup
{
    public const string ManifestName="column.lighttable.json";
    const string Format="LightTable.ColumnBackup";
    const int MaxPhotos=100000,MaxManifestBytes=32*1024*1024;
    sealed class Manifest
    {
        public string Format {get;set;}="";
        public int Version {get;set;}
        public string Name {get;set;}="";
        public List<Entry> Photos {get;set;}=[];
    }
    sealed class Entry
    {
        public string File {get;set;}="";
        public string OriginalName {get;set;}="";
        public double Aspect {get;set;}
        public long Bytes {get;set;}
        public string Sha256 {get;set;}="";
    }
    sealed record Input(Entry Entry,string Path);
    sealed class Reporter(IProgress<BackupProgress>? target)
    {
        readonly System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();long last=-100;
        public void Report(int done,int total,long copied,long bytes,string file,bool finished=false)
        {var now=watch.ElapsedMilliseconds;if(target!=null&&(finished||now-last>=100)){last=now;target.Report(new(done,total,copied,bytes,file));}}
    }
    static string Full(string path)=>Path.GetFullPath(path);
    static string Child(string parent,string relative)
    {
        var root=Full(parent).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
        var result=Full(Path.Combine(root,relative));
        if(!result.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("备份路径越出所选文件夹。");
        return result;
    }
    static bool FileNameValid(string? name)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Length>255||name is "." or ".."||name.EndsWith('.')||name.EndsWith(' '))return false;
        if(name.IndexOfAny("<>:\"/\\|?*".ToCharArray())>=0||name.Any(char.IsControl))return false;
        var stem=name.Split('.')[0].ToUpperInvariant();
        return stem is not ("CON" or "PRN" or "AUX" or "NUL")&&!(stem.Length==4&&(stem.StartsWith("COM")||stem.StartsWith("LPT"))&&stem[3] is >= '1' and <= '9');
    }
    static string Name(string? value)
    {
        var text=string.IsNullOrWhiteSpace(value)?"备份栏目":value.Trim();
        return string.Concat(System.Globalization.StringInfo.GetTextElementEnumerator(text).AsElements().Take(60));
    }
    static IEnumerable<string> AsElements(this System.Globalization.TextElementEnumerator e)
    {while(e.MoveNext())yield return e.GetTextElement();}
    static string FolderName(string name)
    {
        var cleaned=new string(name.Select(c=>"<>:\"/\\|?*".Contains(c)||char.IsControl(c)?'_':c).ToArray()).TrimEnd(' ','.');
        return "LightTable-"+(cleaned.Length>40?cleaned[..40]:cleaned)+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8];
    }
    static void RejectLink(FileSystemInfo item)
    {
        // LinkTarget distinguishes directory/file symlinks from ordinary cloud
        // placeholders; legitimate OneDrive placeholders may be read normally.
        if(item.LinkTarget!=null)throw new InvalidDataException("备份内不允许指向外部的符号链接。");
    }
    static void RemoveOwnedStage(string path,string parent)
    {
        var full=Full(path);var expected=Child(parent,Path.GetFileName(full));
        if(!StringComparer.OrdinalIgnoreCase.Equals(full,expected)||!Path.GetFileName(full).StartsWith(".lighttable-partial-",StringComparison.Ordinal))throw new IOException("拒绝清理非本次创建的临时目录。");
        if(Directory.Exists(full)){RejectLink(new DirectoryInfo(full));Directory.Delete(full,true);}
    }
    public static void DiscardUncommittedRestore(RestoredColumn backup,string managedParent)
    {
        var full=Full(backup.Folder);var parent=Full(managedParent);
        if(!StringComparer.OrdinalIgnoreCase.Equals(full,Child(parent,Path.GetFileName(full)))||!Guid.TryParseExact(Path.GetFileName(full),"N",out _))throw new IOException("拒绝清理应用管理目录以外的文件。");
        if(Directory.Exists(full)){RejectLink(new DirectoryInfo(full));Directory.Delete(full,true);}
    }
    static async Task<(long Bytes,string Hash)> Copy(string source,string destination,CancellationToken token,Action<long> progress)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await using var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read,128*1024,FileOptions.Asynchronous|FileOptions.SequentialScan);
        await using var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None,128*1024,FileOptions.Asynchronous|FileOptions.SequentialScan);
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[128*1024];long copied=0;
        int n;while((n=await input.ReadAsync(buffer,token))>0){token.ThrowIfCancellationRequested();await output.WriteAsync(buffer.AsMemory(0,n),token);hash.AppendData(buffer,0,n);copied+=n;progress(n);}
        await output.FlushAsync(token);output.Flush(true);
        if(copied!=input.Length)throw new IOException("照片读取过程中长度发生变化，请重新备份。");
        return(copied,Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }
    public static async Task<ExportedColumn> ExportAsync(ColumnBackupSource source,string parent,IProgress<BackupProgress>? progress=null,CancellationToken token=default)
    {
        if(source.Photos.Count>MaxPhotos)throw new InvalidDataException("单个栏目超过10万张备份限制。");
        parent=Full(parent);if(!Directory.Exists(parent))throw new DirectoryNotFoundException("备份目标文件夹不存在。");
        var inputs=source.Photos.Select(p=>new{Photo=p,Info=new FileInfo(p.Path)}).ToArray();long total=0;
        foreach(var item in inputs)
        {
            token.ThrowIfCancellationRequested();if(!item.Info.Exists)throw new FileNotFoundException("原照片缺失，栏目未导出。请恢复原文件后重试。",item.Photo.Path);
            if(!FileNameValid(item.Info.Name)||!double.IsFinite(item.Photo.Aspect)||item.Photo.Aspect is <.001 or >1000)throw new InvalidDataException("照片名称或尺寸信息无效。");
            total=checked(total+item.Info.Length);
        }
        var name=Name(source.Name);var stage=Child(parent,".lighttable-partial-"+Guid.NewGuid().ToString("N"));var destination=Child(parent,FolderName(name));
        Directory.CreateDirectory(stage);long copied=0;int done=0;var manifest=new Manifest{Format=Format,Version=1,Name=name};var report=new Reporter(progress);
        try
        {
            foreach(var input in inputs)
            {
                token.ThrowIfCancellationRequested();var original=input.Info.Name;var shortName=original.Length>100?string.Concat(System.Globalization.StringInfo.GetTextElementEnumerator(Path.GetFileNameWithoutExtension(original)).AsElements().Take(60))+Path.GetExtension(original):original;
                var relative=$"photos/{done+1:000000}_{shortName}";
                var result=await Copy(input.Photo.Path,Child(stage,relative),token,n=>{copied+=n;report.Report(done,inputs.Length,copied,total,original);});
                if(result.Bytes!=input.Info.Length||File.GetLastWriteTimeUtc(input.Photo.Path)!=input.Info.LastWriteTimeUtc)throw new IOException("备份过程中照片发生变化，请重试。");
                manifest.Photos.Add(new(){File=relative,OriginalName=original,Aspect=input.Photo.Aspect,Bytes=result.Bytes,Sha256=result.Hash});done++;report.Report(done,inputs.Length,copied,total,original,done==inputs.Length);
            }
            var bytes=JsonSerializer.SerializeToUtf8Bytes(manifest,DocumentStore.JsonOptions);if(bytes.Length>MaxManifestBytes)throw new InvalidDataException("栏目备份清单超过32MiB。");
            var file=Child(stage,ManifestName);await File.WriteAllBytesAsync(file,bytes,token);
            await File.WriteAllTextAsync(Child(stage,"如何恢复.txt"),"这是 LightTable 栏目备份。请保存整个文件夹（包括 photos 和 column.lighttable.json）。下载后在 App 底栏 ⋯ → 导入栏目备份，选择此文件夹。照片为原字节副本；导入新建独立栏目，台面默认空白，不覆盖已有栏目。\n原作者：抖音 @怕蛇LightTable\n",token);
            token.ThrowIfCancellationRequested();
            if(!StringComparer.OrdinalIgnoreCase.Equals(Full(Path.GetDirectoryName(destination)!),parent))throw new IOException("备份目标路径无效。");
            Directory.Move(stage,destination);return new(destination,done,copied);
        }
        finally{RemoveOwnedStage(stage,parent);}
    }
    public static async Task<RestoredColumn> RestoreAsync(string folder,string managedParent,IProgress<BackupProgress>? progress=null,CancellationToken token=default)
    {
        folder=Full(folder);var manifestFile=Child(folder,ManifestName);var info=new FileInfo(manifestFile);
        if(!info.Exists)throw new InvalidDataException($"所选文件夹没有 {ManifestName}，请选择完整栏目备份的根文件夹。");
        RejectLink(info);if(info.Length>MaxManifestBytes)throw new InvalidDataException("备份清单超过32MiB。");
        await using var stream=new FileStream(manifestFile,FileMode.Open,FileAccess.Read,FileShare.Read);
        var manifest=await JsonSerializer.DeserializeAsync<Manifest>(stream,DocumentStore.JsonOptions,token)??throw new InvalidDataException("备份清单为空。");
        if(manifest.Format!=Format||manifest.Version!=1)throw new InvalidDataException("不是受支持的 LightTable 栏目备份版本。");
        if(manifest.Photos==null||manifest.Photos.Count>MaxPhotos)throw new InvalidDataException("备份照片数量无效。");
        var inputs=new List<Input>();var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
        foreach(var entry in manifest.Photos)
        {
            token.ThrowIfCancellationRequested();if(entry==null)throw new InvalidDataException("备份照片条目为空。");
            var parts=entry.File?.Split('/');
            if(parts is not {Length:2}||parts[0]!="photos"||!FileNameValid(parts[1])||!FileNameValid(entry.OriginalName)||!paths.Add(entry.File!))throw new InvalidDataException("备份照片路径或名称无效。");
            if(entry.Bytes<0||entry.Sha256?.Length!=64||!entry.Sha256.All(Uri.IsHexDigit)||!double.IsFinite(entry.Aspect)||entry.Aspect is <.001 or >1000)throw new InvalidDataException("备份照片校验信息无效。");
            var path=Child(folder,entry.File!);var photo=new FileInfo(path);RejectLink(new DirectoryInfo(Child(folder,"photos")));RejectLink(photo);
            if(!photo.Exists||photo.Length!=entry.Bytes)throw new InvalidDataException($"备份缺片或文件长度不符：{entry.OriginalName}。请确认网盘文件已完整下载。");
            total=checked(total+entry.Bytes);inputs.Add(new(entry,path));
        }
        managedParent=Full(managedParent);Directory.CreateDirectory(managedParent);
        var stage=Child(managedParent,".lighttable-partial-"+Guid.NewGuid().ToString("N"));var destination=Child(managedParent,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        long copied=0;var photos=new List<Photo>();var report=new Reporter(progress);
        try
        {
            foreach(var input in inputs)
            {
                token.ThrowIfCancellationRequested();var relative=$"photos/{photos.Count+1:000000}/{input.Entry.OriginalName}";var target=Child(stage,relative);var done=photos.Count;
                var result=await Copy(input.Path,target,token,n=>{copied+=n;report.Report(done,inputs.Count,copied,total,input.Entry.OriginalName);});
                if(result.Bytes!=input.Entry.Bytes||!StringComparer.OrdinalIgnoreCase.Equals(result.Hash,input.Entry.Sha256))throw new InvalidDataException($"照片校验失败：{input.Entry.OriginalName}。备份可能未完整下载或已改变，未导入栏目。");
                photos.Add(new(){Path=Child(destination,relative),Aspect=input.Entry.Aspect,ContentHash=result.Hash,SourceLength=result.Bytes,SourceWriteTicks=File.GetLastWriteTimeUtc(target).Ticks});
                report.Report(photos.Count,inputs.Count,copied,total,input.Entry.OriginalName,photos.Count==inputs.Count);
            }
            token.ThrowIfCancellationRequested();
            if(!StringComparer.OrdinalIgnoreCase.Equals(Full(Path.GetDirectoryName(destination)!),managedParent))throw new IOException("导入目标路径无效。");
            Directory.Move(stage,destination);return new(Name(manifest.Name),destination,photos);
        }
        finally{RemoveOwnedStage(stage,managedParent);}
    }
}
