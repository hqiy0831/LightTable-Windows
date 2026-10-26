using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using global::Windows.Graphics.Imaging;
using LightTable.Core;

namespace LightTable.Windows.Imaging;

public sealed record DecodedPhoto(string Key,string Path,int Width,int Height,byte[] Pixels,int Bucket)
{
    public long Cost=>Pixels.LongLength;
    public double Aspect=>(double)Width/Height;
}

// WIC decoding, EXIF orientation and ICC -> sRGB conversion run on workers.
// The separate thumbnail lane cannot queue behind optical/HD requests.
public sealed class ImageStore
{
    readonly DocumentStore storage;
    readonly SemaphoreSlim previews=new(2),thumbnails=new(2);
    readonly ConcurrentDictionary<string,Lazy<Task<DecodedPhoto>>> pending=new();
    readonly Dictionary<string,(DecodedPhoto Image,LinkedListNode<string> Node)> memory=[];
    readonly LinkedList<string> lru=[];
    readonly object gate=new();
    long used;
    public const long MemoryBudget=160L*1024*1024,DiskBudget=750L*1024*1024;
    public long MemoryBytes {get{lock(gate)return used;}}
    public long MemoryHits,DiskHits,DecodeCount,MetadataReads;
    public OpticalLayerCache OpticalCache {get;}
    public ImageStore(DocumentStore storage){this.storage=storage;OpticalCache=new(storage);}
    public Task<double> ReadAspectAsync(string path)=>Task.Run(async()=>
    {
        await thumbnails.WaitAsync().ConfigureAwait(false);
        try
        {
            using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);using var random=file.AsRandomAccessStream();var decoder=await BitmapDecoder.CreateAsync(random);
            Interlocked.Increment(ref MetadataReads);
            if(decoder.OrientedPixelWidth==0||decoder.OrientedPixelHeight==0||(ulong)decoder.PixelWidth*decoder.PixelHeight>800_000_000)throw new InvalidDataException("照片尺寸无效");
            return (double)decoder.OrientedPixelWidth/decoder.OrientedPixelHeight;
        }
        finally{thumbnails.Release();}
    });
    public Task<DecodedPhoto> GetAsync(string path,int bucket=2200)
    {
        bucket=Math.Clamp(bucket,128,2200);
        string key;try{key=SourceKey(path,bucket);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){return Task.FromException<DecodedPhoto>(ex);}
        lock(gate)if(memory.TryGetValue(key,out var e)){lru.Remove(e.Node);lru.AddLast(e.Node);Interlocked.Increment(ref MemoryHits);return Task.FromResult(e.Image);}
        var lazy=pending.GetOrAdd(key,k=>new Lazy<Task<DecodedPhoto>>(()=>Task.Run(()=>LoadAsync(path,bucket,k))));
        return Complete(key,lazy);
    }
    public static string SourceKey(string path,int bucket)
    {
        var info=new FileInfo(path);if(!info.Exists)throw new FileNotFoundException("照片原路径不存在",path);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"wic-srgb-v1|{info.FullName.ToUpperInvariant()}|{info.Length}|{info.LastWriteTimeUtc.Ticks}|{Math.Clamp(bucket,128,2200)}")));
    }
    async Task<DecodedPhoto> Complete(string key,Lazy<Task<DecodedPhoto>> task)
    {
        try{return await task.Value.ConfigureAwait(false);}
        finally{pending.TryRemove(new KeyValuePair<string,Lazy<Task<DecodedPhoto>>>(key,task));}
    }
    async Task<DecodedPhoto> LoadAsync(string path,int bucket,string key)
    {
        var lane=bucket<=256?thumbnails:previews;await lane.WaitAsync().ConfigureAwait(false);
        try
        {
            var dir=Path.Combine(storage.Root,"cache",bucket<=256?"thumbnails":"previews");
            var file=Path.Combine(dir,key+".br");DecodedPhoto? result=null;
            if(File.Exists(file))try
            {
                using var f=File.OpenRead(file);using var z=new BrotliStream(f,CompressionMode.Decompress);using var reader=new BinaryReader(z);
                var w=reader.ReadInt32();var h=reader.ReadInt32();var length=reader.ReadInt32();
                if(w<1||h<1||w>2200||h>2200||length!=checked(w*h*4))throw new InvalidDataException("预览缓存损坏");
                var pixels=reader.ReadBytes(length);if(pixels.Length!=length)throw new InvalidDataException("预览缓存截断");
                result=new(key,path,w,h,pixels,bucket);File.SetLastWriteTimeUtc(file,DateTime.UtcNow);Interlocked.Increment(ref DiskHits);
            }
            catch(Exception ex) when(ex is IOException or InvalidDataException or OverflowException){storage.Log($"缓存重新生成 {key}: {ex.Message}");}
            if(result==null)
            {
                using var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                using var random=f.AsRandomAccessStream();var decoder=await BitmapDecoder.CreateAsync(random);
                if(decoder.PixelWidth==0||decoder.PixelHeight==0||(ulong)decoder.PixelWidth*decoder.PixelHeight>800_000_000)throw new InvalidDataException("图像尺寸超出安全解码限制（8 亿像素）");
                var scale=Math.Min(1,(double)bucket/Math.Max(decoder.PixelWidth,decoder.PixelHeight));
                var transform=new BitmapTransform{ScaledWidth=(uint)Math.Max(1,Math.Round(decoder.PixelWidth*scale)),ScaledHeight=(uint)Math.Max(1,Math.Round(decoder.PixelHeight*scale)),InterpolationMode=BitmapInterpolationMode.Fant};
                using var bitmap=await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,transform,ExifOrientationMode.RespectExifOrientation,ColorManagementMode.ColorManageToSRgb);
                var pixels=new byte[checked(bitmap.PixelWidth*bitmap.PixelHeight*4)];bitmap.CopyToBuffer(pixels.AsBuffer());
                result=new(key,path,bitmap.PixelWidth,bitmap.PixelHeight,pixels,bucket);Interlocked.Increment(ref DecodeCount);
                var temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
                try
                {
                    using(var output=File.Create(temp))using(var z=new BrotliStream(output,CompressionLevel.Fastest))using(var writer=new BinaryWriter(z))
                    {writer.Write(result.Width);writer.Write(result.Height);writer.Write(pixels.Length);writer.Write(pixels);}
                    lock(diskGate)
                    {
                        var usage=Usage(dir);var previous=File.Exists(file)?new FileInfo(file).Length:0;var added=!File.Exists(file);
                        var bytes=new FileInfo(temp).Length;File.Move(temp,file,true);usage.Bytes+=bytes-previous;if(added)usage.Entries++;usage.Writes++;
                        TrimDisk(dir,bucket<=256?100L*1024*1024:DiskBudget);
                    }
                }
                catch(IOException ex){storage.Log($"缓存写入失败: {ex.Message}");}
                finally{if(File.Exists(temp))File.Delete(temp);}
            }
            lock(gate)
            {
                if(!memory.ContainsKey(key)){var node=lru.AddLast(key);memory[key]=(result,node);used+=result.Cost;}
                while(used>MemoryBudget&&lru.Count>1){var k=lru.First!.Value;lru.RemoveFirst();used-=memory[k].Image.Cost;memory.Remove(k);}
            }
            return result;
        }
        finally{lane.Release();}
    }
    static readonly object diskGate=new();
    sealed class DiskUsage{public long Bytes;public int Entries,Writes;}
    readonly Dictionary<string,DiskUsage> diskUsage=[];
    DiskUsage Usage(string dir)
    {
        if(!diskUsage.TryGetValue(dir,out var usage)||usage.Writes>=64)
        {
            var files=new DirectoryInfo(dir).EnumerateFiles("*.br").ToArray();usage=new(){Bytes=files.Sum(f=>f.Length),Entries=files.Length};diskUsage[dir]=usage;
        }
        return usage;
    }
    void TrimDisk(string dir,long budget)
    {
        var usage=Usage(dir);if(usage.Bytes<=budget)return;
        var files=new DirectoryInfo(dir).EnumerateFiles("*.br").ToList();var total=files.Sum(f=>f.Length);
        foreach(var f in files.OrderBy(f=>f.LastWriteTimeUtc)){if(total<=budget)break;try{var length=f.Length;f.Delete();total-=length;}catch(IOException){}}
        usage.Bytes=total;usage.Writes=0;
    }
}
