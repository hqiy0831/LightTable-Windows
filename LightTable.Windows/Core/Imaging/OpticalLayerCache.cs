using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace LightTable.Core;

// Pixels are premultiplied BGRA8 at 96 DPI. The recipe identifies the working
// color space and all five static optical operations, including the OFF floor.
public sealed record OpticalRaster(int Width,int Height,byte[][] Layers)
{
    public const int LayerCount=5;
    public void Validate(int width,int height)
    {
        if(width<1||height<1||width>2200||height>2200||Width!=width||Height!=height||Layers.Length!=LayerCount)
            throw new InvalidDataException("光学缓存尺寸或层数不匹配");
        var length=checked(width*height*4);
        if(Layers.Any(layer=>layer==null||layer.Length!=length))throw new InvalidDataException("光学缓存层数据不完整");
    }
}

public sealed class OpticalLayerCache
{
    public const string CurrentRecipe="srgb-bgra8-scatter-v4-srgb-luminance-zero-floor";
    public const long DiskBudget=750L*1024*1024;
    // Small table mips should survive a large-library restart. Keep the same
    // 750 MiB byte ceiling, with a higher metadata/file-count ceiling than Mac.
    public const int EntryLimit=2048;
    static readonly byte[] magic="LTOPTC02"u8.ToArray();
    static readonly object diskGate=new();
    readonly ConcurrentDictionary<string,Lazy<Task<OpticalRaster>>> pending=new();
    // At most two five-layer buffers/builds exist in this lane; completed data
    // is not retained in another CPU memory cache after callers upload it.
    readonly SemaphoreSlim lane=new(2);
    readonly Action<string>? log;
    readonly string recipe;
    readonly long budget;
    readonly int entryLimit;
    int cleanupStarted;
    long trackedBytes;
    int trackedEntries,writesSinceScan;
    bool inventoryReady;
    public string DirectoryPath {get;}
    public long DiskHits,BuildCount,CorruptReads,WriteFailures,TempFilesRemoved;
    public OpticalLayerCache(DocumentStore storage,string recipe=CurrentRecipe,long budget=DiskBudget,int entryLimit=EntryLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipe);
        if(budget<1||entryLimit<1)throw new ArgumentOutOfRangeException(nameof(budget));
        DirectoryPath=Path.Combine(storage.Root,"cache","optical-v1");this.recipe=recipe;this.budget=budget;this.entryLimit=entryLimit;log=storage.Log;
    }
    public string EntryPath(string sourceKey,int maxPixel)=>Path.Combine(DirectoryPath,Identity(sourceKey,maxPixel)+".ltoc");
    string Identity(string sourceKey,int maxPixel)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{recipe}|request-v2|{sourceKey}|{maxPixel}")));
    public Task<OpticalRaster> GetOrBuildAsync(string sourceKey,int maxPixel,Func<OpticalRaster> build)=>GetOrBuildPreviewAsync(sourceKey,maxPixel,()=>Task.FromResult(build()));
    public async Task<OpticalRaster> GetOrBuildPreviewAsync(string sourceKey,int maxPixel,Func<Task<OpticalRaster>> build)
    {
        // The source identity includes metadata and decoder bucket. The raster
        // limit locates an entry before opening or decoding any source pixels.
        if(maxPixel<1||maxPixel>2200)throw new ArgumentOutOfRangeException(nameof(maxPixel));
        var identity=Identity(sourceKey,maxPixel);
        var task=pending.GetOrAdd(identity,_=>new Lazy<Task<OpticalRaster>>(()=>Task.Run(()=>LoadOrBuild(identity,maxPixel,build))));
        try{return await task.Value.ConfigureAwait(false);}
        finally{pending.TryRemove(new KeyValuePair<string,Lazy<Task<OpticalRaster>>>(identity,task));}
    }
    async Task<OpticalRaster> LoadOrBuild(string identity,int maxPixel,Func<Task<OpticalRaster>> build)
    {
        await lane.WaitAsync().ConfigureAwait(false);
        try
        {
            if(Interlocked.Exchange(ref cleanupStarted,1)==0)RemoveStaleTemps();
            var path=Path.Combine(DirectoryPath,identity+".ltoc");
            if(File.Exists(path))
            {
                try
                {
                    var raster=Read(path,identity,maxPixel);
                    try{File.SetLastWriteTimeUtc(path,DateTime.UtcNow);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Log($"光学缓存访问时间未更新: {ex.Message}");}
                    Interlocked.Increment(ref DiskHits);return raster;
                }
                catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException)
                {Interlocked.Increment(ref CorruptReads);Log($"光学缓存重新生成 {identity}: {ex.Message}");}
            }
            var generated=await build().ConfigureAwait(false);generated.Validate(generated.Width,generated.Height);
            if(Math.Max(generated.Width,generated.Height)>maxPixel)throw new InvalidDataException("光学栅格超过请求尺寸");
            Interlocked.Increment(ref BuildCount);
            Write(path,identity,generated);return generated;
        }
        finally{lane.Release();}
    }
    static OpticalRaster Read(string path,string identity,int maxPixel)
    {
        using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete);
        if(input.Length>128L*1024*1024)throw new InvalidDataException("光学缓存超过读取限制");
        using var compressed=new BrotliStream(input,CompressionMode.Decompress);using var reader=new BinaryReader(compressed);
        if(!reader.ReadBytes(magic.Length).SequenceEqual(magic)||!reader.ReadBytes(32).SequenceEqual(Convert.FromHexString(identity)))
            throw new InvalidDataException("光学缓存配方或头信息无效");
        var width=reader.ReadInt32();var height=reader.ReadInt32();
        if(width<1||height<1||width>maxPixel||height>maxPixel||reader.ReadInt32()!=OpticalRaster.LayerCount)throw new InvalidDataException("光学缓存尺寸或层数无效");
        var length=checked(width*height*4);var layers=new byte[OpticalRaster.LayerCount][];
        for(int i=0;i<layers.Length;i++)
        {
            var expected=reader.ReadBytes(32);layers[i]=reader.ReadBytes(length);
            if(layers[i].Length!=length||!SHA256.HashData(layers[i]).SequenceEqual(expected))throw new InvalidDataException("光学缓存层截断或校验失败");
        }
        if(compressed.ReadByte()!=-1)throw new InvalidDataException("光学缓存包含多余数据");
        return new(width,height,layers);
    }
    void Write(string path,string identity,OpticalRaster raster)
    {
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                using(var compressed=new BrotliStream(output,CompressionLevel.Fastest,true))using(var writer=new BinaryWriter(compressed))
                {
                    writer.Write(magic);writer.Write(Convert.FromHexString(identity));writer.Write(raster.Width);writer.Write(raster.Height);writer.Write(OpticalRaster.LayerCount);
                    foreach(var layer in raster.Layers){writer.Write(SHA256.HashData(layer));writer.Write(layer);}
                }
                output.Flush(true);
            }
            lock(diskGate)
            {
                if(!inventoryReady||writesSinceScan>=64)ScanInventory();
                var old=File.Exists(path)?new FileInfo(path).Length:0;var added=!File.Exists(path);
                var bytes=new FileInfo(temp).Length;File.Move(temp,path,true);trackedBytes+=bytes-old;if(added)trackedEntries++;writesSinceScan++;Trim(path);
            }
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException)
        {Interlocked.Increment(ref WriteFailures);Log($"光学缓存写入失败，继续使用已生成图像: {ex.Message}");}
        finally
        {
            try{if(File.Exists(temp))File.Delete(temp);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Log($"光学临时缓存清理失败: {ex.Message}");}
        }
    }
    void Trim(string keeping)
    {
        if(trackedBytes<=budget&&trackedEntries<=entryLimit)return;
        var files=new DirectoryInfo(DirectoryPath).EnumerateFiles("*.ltoc").ToList();var total=files.Sum(f=>f.Length);var count=files.Count;
        foreach(var file in files.OrderBy(f=>f.LastWriteTimeUtc))
        {
            if(total<=budget&&count<=entryLimit)break;
            if(StringComparer.OrdinalIgnoreCase.Equals(file.FullName,keeping))continue;
            try{var length=file.Length;file.Delete();total-=length;count--;}
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Log($"光学缓存淘汰稍后重试: {ex.Message}");}
        }
        trackedBytes=total;trackedEntries=count;
    }
    void ScanInventory()
    {
        var files=new DirectoryInfo(DirectoryPath).EnumerateFiles("*.ltoc").ToArray();trackedBytes=files.Sum(f=>f.Length);trackedEntries=files.Length;writesSinceScan=0;inventoryReady=true;
    }
    void RemoveStaleTemps()
    {
        try
        {
            if(!Directory.Exists(DirectoryPath))return;
            foreach(var file in new DirectoryInfo(DirectoryPath).EnumerateFiles("*.ltoc.*.tmp"))
            {
                var parts=file.Name.Split('.');
                if(parts.Length!=4||parts[0].Length!=64||parts[2].Length!=32||!parts[0].All(Uri.IsHexDigit)||!parts[2].All(Uri.IsHexDigit)||
                    file.LastWriteTimeUtc>=DateTime.UtcNow.AddHours(-1)||(file.Attributes&FileAttributes.ReparsePoint)!=0)continue;
                try{file.Delete();Interlocked.Increment(ref TempFilesRemoved);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Log($"保留仍占用的光学临时文件: {ex.Message}");}
            }
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Log($"光学临时文件清理稍后重试: {ex.Message}");}
    }
    void Log(string message){try{log?.Invoke(message);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){/* Cache failure must not make a usable preview fail. */}}
}
