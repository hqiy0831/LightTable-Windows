using System.IO.Compression;
using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class OpticalLayerCacheTests : IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"LightTable-optical-tests-"+Guid.NewGuid().ToString("N"));
    DocumentStore Storage=>new(root);
    static OpticalRaster Raster(int width=8,int height=6,int salt=0)
    {
        var layers=Enumerable.Range(0,5).Select(layer=>Enumerable.Range(0,width*height*4).Select(i=>(byte)((i*17+layer*31+salt)%256)).ToArray()).ToArray();
        return new(width,height,layers);
    }
    static void EqualPixels(OpticalRaster expected,OpticalRaster actual)
    {
        Assert.Equal(expected.Width,actual.Width);Assert.Equal(expected.Height,actual.Height);
        for(int i=0;i<5;i++)Assert.Equal(expected.Layers[i],actual.Layers[i]);
    }
    [Fact] public async Task ConcurrentColdRequestsShareFiveLayersAndNewInstanceReadsWithoutBuild()
    {
        var cache=new OpticalLayerCache(Storage);var expected=Raster();using var release=new ManualResetEventSlim();var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls=0;
        OpticalRaster Build(){Interlocked.Increment(ref calls);started.TrySetResult();if(!release.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException();return expected;}
        var requests=Enumerable.Range(0,20).Select(_=>cache.GetOrBuildAsync("source",8,Build)).ToArray();await started.Task.WaitAsync(TimeSpan.FromSeconds(5));release.Set();var results=await Task.WhenAll(requests);
        Assert.Equal(1,calls);Assert.All(results,value=>Assert.Same(expected,value));Assert.Equal(1,cache.BuildCount);
        var fresh=new OpticalLayerCache(Storage);var hot=await fresh.GetOrBuildAsync("source",8,()=>throw new InvalidOperationException("hot cache rebuilt"));
        EqualPixels(expected,hot);Assert.Equal(1,fresh.DiskHits);Assert.Equal(0,fresh.BuildCount);Assert.Empty(Directory.EnumerateFiles(cache.DirectoryPath,"*.tmp"));
    }
    [Fact] public async Task RecipeSourceIdentityAndRasterDimensionsEachInvalidateIndependently()
    {
        var first=new OpticalLayerCache(Storage);await first.GetOrBuildAsync("metadata-A",8,()=>Raster());
        var paths=new HashSet<string>{first.EntryPath("metadata-A",8)};
        await first.GetOrBuildAsync("metadata-B",8,()=>Raster(salt:1));paths.Add(first.EntryPath("metadata-B",8));
        await first.GetOrBuildAsync("metadata-A",4,()=>Raster(4,3));paths.Add(first.EntryPath("metadata-A",4));
        var changed=new OpticalLayerCache(Storage,"changed-color-or-optical-recipe");await changed.GetOrBuildAsync("metadata-A",8,()=>Raster(salt:2));paths.Add(changed.EntryPath("metadata-A",8));
        Assert.Equal(4,paths.Count);Assert.All(paths,path=>Assert.True(File.Exists(path)));Assert.Equal(3,first.BuildCount);Assert.Equal(1,changed.BuildCount);
    }
    [Fact] public async Task TruncationPayloadTamperingAndWrongEntryIdentityRegenerateAllLayers()
    {
        var cache=new OpticalLayerCache(Storage);var expected=Raster();await cache.GetOrBuildAsync("source",8,()=>expected);var path=cache.EntryPath("source",8);
        await File.WriteAllBytesAsync(path,[0,1,2]);var truncated=new OpticalLayerCache(Storage);EqualPixels(expected,await truncated.GetOrBuildAsync("source",8,()=>expected));Assert.Equal(1,truncated.CorruptReads);
        byte[] payload;using(var file=File.OpenRead(path))using(var compressed=new BrotliStream(file,CompressionMode.Decompress))using(var output=new MemoryStream()){compressed.CopyTo(output);payload=output.ToArray();}
        payload[^1]^=1;using(var file=File.Create(path))using(var compressed=new BrotliStream(file,CompressionLevel.Fastest))compressed.Write(payload);
        var tampered=new OpticalLayerCache(Storage);EqualPixels(expected,await tampered.GetOrBuildAsync("source",8,()=>expected));Assert.Equal(1,tampered.CorruptReads);Assert.Equal(0,tampered.DiskHits);
        File.Copy(path,cache.EntryPath("another-source",8));var renamed=new OpticalLayerCache(Storage);EqualPixels(expected,await renamed.GetOrBuildAsync("another-source",8,()=>expected));Assert.Equal(1,renamed.CorruptReads);
    }
    [Fact] public async Task LruTouchAndIndependentByteLimitOnlyEvictDisposableOpticalEntries()
    {
        var storage=Storage;var untouched=Path.Combine(storage.Root,"layouts","sentinel.json");File.WriteAllText(untouched,"keep-layout");
        var cache=new OpticalLayerCache(storage,entryLimit:2);await cache.GetOrBuildAsync("A",8,()=>Raster());await cache.GetOrBuildAsync("B",8,()=>Raster());
        File.SetLastWriteTimeUtc(cache.EntryPath("B",8),new DateTime(2000,1,1));await cache.GetOrBuildAsync("A",8,()=>throw new Exception("expected hit"));await cache.GetOrBuildAsync("C",8,()=>Raster());
        Assert.True(File.Exists(cache.EntryPath("A",8)));Assert.False(File.Exists(cache.EntryPath("B",8)));Assert.Equal(2,Directory.GetFiles(cache.DirectoryPath,"*.ltoc").Length);
        var length=new FileInfo(cache.EntryPath("C",8)).Length;var limited=new OpticalLayerCache(storage,budget:length+20);await limited.GetOrBuildAsync("D",8,()=>Raster());
        Assert.Single(Directory.GetFiles(cache.DirectoryPath,"*.ltoc"));Assert.True(File.Exists(limited.EntryPath("D",8)));Assert.Equal("keep-layout",File.ReadAllText(untouched));
    }
    [Fact] public async Task UnwritableCacheStillReturnsUsableGeneratedPixels()
    {
        var storage=Storage;var cache=new OpticalLayerCache(storage);File.WriteAllText(cache.DirectoryPath,"file blocks cache directory");var expected=Raster();
        EqualPixels(expected,await cache.GetOrBuildAsync("source",8,()=>expected));Assert.Equal(1,cache.BuildCount);Assert.Equal(1,cache.WriteFailures);
    }
    [Fact] public async Task BackgroundBuildConcurrencyIsBoundedAndFailureCanRetry()
    {
        var cache=new OpticalLayerCache(Storage);using var release=new ManualResetEventSlim();var twoStarted=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var active=0;var maximum=0;
        OpticalRaster Build()
        {
            var count=Interlocked.Increment(ref active);int old;do{old=Volatile.Read(ref maximum);if(old>=count)break;}while(Interlocked.CompareExchange(ref maximum,count,old)!=old);
            if(count==2)twoStarted.TrySetResult();try{if(!release.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException();return Raster();}finally{Interlocked.Decrement(ref active);}
        }
        var tasks=Enumerable.Range(0,8).Select(i=>cache.GetOrBuildAsync("source-"+i,8,Build)).ToArray();await twoStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));release.Set();await Task.WhenAll(tasks);Assert.Equal(2,maximum);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>cache.GetOrBuildAsync("failed",8,()=>throw new InvalidOperationException("gpu failed")));
        EqualPixels(Raster(),await cache.GetOrBuildAsync("failed",8,()=>Raster()));
        await Assert.ThrowsAsync<InvalidDataException>(()=>cache.GetOrBuildAsync("partial",8,()=>new(8,6,[new byte[8*6*4]])));
        Assert.False(File.Exists(cache.EntryPath("partial",8)));Assert.Empty(Directory.EnumerateFiles(cache.DirectoryPath,"*.tmp"));
    }
    [Fact] public async Task InterruptedTemporaryWritesNeverReplaceCommittedLayersAndOnlyStaleOwnedTempsArePruned()
    {
        var cache=new OpticalLayerCache(Storage);var expected=Raster();await cache.GetOrBuildAsync("source",8,()=>expected);var entry=cache.EntryPath("source",8);
        string Temporary()=>entry+"."+Guid.NewGuid().ToString("N")+".tmp";
        var stale=Temporary();var active=Temporary();var fresh=Temporary();var unrelated=Path.Combine(cache.DirectoryPath,"unrelated.tmp");
        foreach(var path in new[]{stale,active,fresh,unrelated})File.WriteAllText(path,"interrupted, not committed");
        foreach(var path in new[]{stale,active,unrelated})File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddHours(-2));
        using(var held=new FileStream(active,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        {
            var recovered=new OpticalLayerCache(Storage);EqualPixels(expected,await recovered.GetOrBuildAsync("source",8,()=>throw new InvalidOperationException("committed entry lost")));
            Assert.Equal(1,recovered.TempFilesRemoved);Assert.False(File.Exists(stale));Assert.True(File.Exists(active));Assert.True(File.Exists(fresh));Assert.True(File.Exists(unrelated));Assert.Equal(1,recovered.DiskHits);
        }
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
