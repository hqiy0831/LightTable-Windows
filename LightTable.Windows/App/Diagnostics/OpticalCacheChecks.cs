using System.Diagnostics;
using System.Text.Json;
using LightTable.Core;
using LightTable.Windows.Imaging;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckOpticalDiskCache(string sourcePath)
    {
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        var storage=new DocumentStore(Path.Combine(session.Storage.Root,"optical-cache-check"));var images=new ImageStore(storage);var decoded=await images.GetAsync(sourcePath,512);
        var ticks=0;var heartbeat=DispatcherQueue.CreateTimer();heartbeat.Interval=TimeSpan.FromMilliseconds(16);heartbeat.Tick+=(_,_)=>ticks++;heartbeat.Start();
        var watch=Stopwatch.StartNew();OpticalPhoto cold;
        try{cold=await OpticalPhoto.CreateAsync(board.Device,decoded,images.OpticalCache,384);}finally{heartbeat.Stop();}
        using(cold)
        {
            var coldMs=watch.Elapsed.TotalMilliseconds;var coldTicks=ticks;Require(!cold.UsedCachedLayers&&images.OpticalCache.BuildCount==1,"冷启动没有实际生成五层");
            File.Delete(Path.Combine(storage.Root,"cache","previews",decoded.Key+".br"));
            var freshImages=new ImageStore(storage);watch.Restart();
            using var hot=await OpticalPhoto.CreateAsync(board.Device,freshImages,sourcePath,512,384);var hotMs=watch.Elapsed.TotalMilliseconds;
            Require(hot.UsedCachedLayers&&freshImages.OpticalCache.DiskHits==1&&freshImages.OpticalCache.BuildCount==0,"跨Store热启动仍在重新生成光学层");
            Require(freshImages.DecodeCount==0&&freshImages.DiskHits==0&&freshImages.MemoryHits==0,"纯光学热命中仍请求了源预览像素");
            var coldLayers=cold.ReadLayers();var hotLayers=hot.ReadLayers();for(int i=0;i<5;i++)Require(coldLayers.Layers[i].SequenceEqual(hotLayers.Layers[i]),$"缓存第{i}层像素与GPU原始生成不一致");
            using var tables=new OpticalTables(board.Device);var deltas=new List<object>();
            byte[] Frame(OpticalPhoto photo,bool lit,double brightness,double flare)
            {
                using var target=new CanvasRenderTarget(board.Device,photo.Decoded.Width,photo.Decoded.Height,96);using(var ds=target.CreateDrawingSession())
                {ds.Clear(Colors.Transparent);ds.DrawImage(photo.View(new(lit?1:0,flare,.5),brightness,tables,false));}return target.GetPixelBytes();
            }
            foreach(var (lit,brightness,flare) in new[]{(false,1d,0d),(true,0d,0d),(true,1d,0d),(true,1d,1d),(true,1.6,0d),(true,1.6,1d)})
            {
                var expected=Frame(cold,lit,brightness,flare);var actual=Frame(hot,lit,brightness,flare);var maximum=expected.Zip(actual).Max(pair=>Math.Abs(pair.First-pair.Second));
                Require(maximum<=1,$"热缓存显示变化: lit={lit},b={brightness},flare={flare},差={maximum}");deltas.Add(new{Lit=lit,Brightness=brightness,Flare=flare,MaxByteDelta=maximum});
            }
            var entry=images.OpticalCache.EntryPath(decoded.Key,384);await File.WriteAllBytesAsync(entry,[0,1,2]);var recovering=new OpticalLayerCache(storage);
            using var rebuilt=await OpticalPhoto.CreateAsync(board.Device,decoded,recovering,384);Require(recovering.CorruptReads==1&&recovering.BuildCount==1&&!rebuilt.UsedCachedLayers,"坏光学缓存未实际重新生成");
            var rebuiltLayers=rebuilt.ReadLayers();for(int i=0;i<5;i++)Require(coldLayers.Layers[i].SequenceEqual(rebuiltLayers.Layers[i]),"坏缓存重建后五层不同");
            var metadataCopy=Path.Combine(storage.Root,"metadata-copy.jpg");File.Copy(sourcePath,metadataCopy);var copyBefore=await images.GetAsync(metadataCopy,512);using var copyCold=await OpticalPhoto.CreateAsync(board.Device,copyBefore,images.OpticalCache,384);
            File.SetLastWriteTimeUtc(metadataCopy,File.GetLastWriteTimeUtc(metadataCopy).AddSeconds(2));var changed=await images.GetAsync(metadataCopy,512);Require(changed.Key!=copyBefore.Key,"源文件修改时间没有进入解码身份");
            using var copyChanged=await OpticalPhoto.CreateAsync(board.Device,changed,images.OpticalCache,384);Require(!copyChanged.UsedCachedLayers,"源文件元数据变更未使光学缓存失效");
            File.Delete(Path.Combine(storage.Root,"cache","previews",changed.Key+".br"));var lockedImages=new ImageStore(storage);
            using(var held=new FileStream(metadataCopy,FileMode.Open,FileAccess.Read,FileShare.None))
            using(var lockedHot=await OpticalPhoto.CreateAsync(board.Device,lockedImages,metadataCopy,512,384))
            {Require(lockedHot.UsedCachedLayers&&lockedImages.DecodeCount==0&&lockedImages.DiskHits==0,"源图被独占时未直接恢复五层光学缓存");}
            Require(cold.Decoded.Pixels.Length==0&&hot.Decoded.Pixels.Length==0,"GPU实例保留完整五层CPU数组");
            var result=new{Layers=5,Width=cold.Decoded.Width,Height=cold.Decoded.Height,ColdMs=coldMs,HotMs=hotMs,ColdUiTimerTicks=coldTicks,HotSourceDecodes=freshImages.DecodeCount,HotSourceDiskHits=freshImages.DiskHits,HotOpticalBuilds=freshImages.OpticalCache.BuildCount,HotDiskHits=freshImages.OpticalCache.DiskHits,SourcePreviewRemoved=true,LockedSourceHotHit=true,CorruptionRebuilt=true,MetadataInvalidates=true,Deltas=deltas};
            await File.WriteAllTextAsync(Path.Combine(session.Storage.Root,"logs","optical-cache.json"),JsonSerializer.Serialize(result,DocumentStore.JsonOptions));
            return "真实GPU五层跨Store读回逐字节一致，六种亮度/点灯状态差≤1；坏缓存重建、文件元数据失效通过；热启动不解码/不重算。"+JsonSerializer.Serialize(result);
        }
    }
}
