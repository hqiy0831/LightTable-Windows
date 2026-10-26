using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using LightTable.Core;
using LightTable.Windows.Imaging;
using LightTable.Windows.Rendering;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    sealed record OpticalProbeRequest(string[] Paths,int Bucket=512,int RasterLimit=384,bool RequireCached=false);
    async Task RunOpticalCacheProbe(string requestPath)
    {
        var results=new List<object>();string? failure=null;var watch=Stopwatch.StartNew();var cacheHits=0;
        try
        {
            if(session.Document.Photos.Count!=0||session.Document.Slides.Count!=0||session.Document.Albums.Count!=0)throw new InvalidOperationException("光学缓存探针只允许空白隔离布局");
            if(new FileInfo(requestPath).Length>1024*1024)throw new InvalidDataException("探针请求超过1MiB");
            var request=JsonSerializer.Deserialize<OpticalProbeRequest>(await File.ReadAllTextAsync(requestPath),DocumentStore.JsonOptions)??throw new InvalidDataException("探针请求为空");
            if(request.Paths.Length is <1 or >4096||request.Bucket is <128 or >2200||request.RasterLimit is <1 or >2200)throw new InvalidDataException("探针请求数量或尺寸无效");
            await renderer.DeviceReady;watch.Restart();
            foreach(var path in request.Paths)
            {
                using var photo=await OpticalPhoto.CreateAsync(board.Device,session.Images,path,request.Bucket,request.RasterLimit);
                if(request.RequireCached&&!photo.UsedCachedLayers)throw new InvalidOperationException("要求热命中但发生了光学重新生成: "+path);
                if(photo.UsedCachedLayers)cacheHits++;
                results.Add(new{Path=path,photo.Decoded.Key,photo.Decoded.Width,photo.Decoded.Height,photo.UsedCachedLayers,LayerSha256=photo.ReadLayers().Layers.Select(layer=>Convert.ToHexString(SHA256.HashData(layer))).ToArray()});
            }
            if(request.RequireCached&&(session.Images.DecodeCount!=0||session.Images.DiskHits!=0||session.Images.MemoryHits!=0||session.Images.OpticalCache.BuildCount!=0))throw new InvalidOperationException("纯光学命中仍请求了源像素或重新生成");
        }
        catch(Exception ex){failure=ex.ToString();}
        var report=new{Date=DateTimeOffset.Now,Version=BuildInfo.Version,Scope="独立进程调用生产光学工厂与GPU读回；不是完整应用启动或UI帧率验收",Passed=failure==null,Failure=failure,Milliseconds=watch.Elapsed.TotalMilliseconds,Count=results.Count,Cached=cacheHits,session.Images.DecodeCount,SourceDiskHits=session.Images.DiskHits,SourceMemoryHits=session.Images.MemoryHits,OpticalBuilds=session.Images.OpticalCache.BuildCount,OpticalDiskHits=session.Images.OpticalCache.DiskHits,Results=results};
        await File.WriteAllTextAsync(Path.Combine(session.Storage.Root,"logs","optical-probe.json"),JsonSerializer.Serialize(report,DocumentStore.JsonOptions));Environment.ExitCode=report.Passed?0:1;Close();
    }
}
