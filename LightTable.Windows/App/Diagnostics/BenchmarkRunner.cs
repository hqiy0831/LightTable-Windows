using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;
using LightTable.Core;
using Microsoft.UI.Xaml;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    [StructLayout(LayoutKind.Sequential)] struct ProcessIo
    {public ulong ReadOperations,WriteOperations,OtherOperations,ReadBytes,WriteBytes,OtherBytes;}
    [DllImport("kernel32.dll")]static extern bool GetProcessIoCounters(IntPtr process,out ProcessIo counters);
    async Task RunBenchmark(string photoRoot,int count)
    {
        var process=Process.GetCurrentProcess();var samples=new List<object>();var timings=new Dictionary<string,double>();
        var timer=DispatcherQueue.CreateTimer();timer.Interval=TimeSpan.FromMilliseconds(250);
        timer.Tick+=(_,_)=>
        {
            process.Refresh();GetProcessIoCounters(process.Handle,out var io);samples.Add(new{Seconds=Rendering.TableRenderer.Seconds,WorkingSet=process.WorkingSet64,PrivateBytes=process.PrivateMemorySize64,CpuMilliseconds=process.TotalProcessorTime.TotalMilliseconds,CpuCache=session.Images.MemoryBytes,GpuTextureEstimate=renderer.GpuBytes,Io=new{io.ReadBytes,io.WriteBytes,io.ReadOperations,io.WriteOperations}});
            // Preserve the last observed phase even if an external watchdog
            // terminates a slow run before the final report can be written.
            if(samples.Count%8==0)File.WriteAllText(Path.Combine(session.Storage.Root,"logs","benchmark-progress.json"),JsonSerializer.Serialize(new{Phase=renderer.PerformancePhase,Imported=session.Document.Photos.Count,Draws=renderer.FrameTimes.Count,session.Images.DecodeCount,OpticalBuilds=session.Images.OpticalCache.BuildCount,OpticalHits=session.Images.OpticalCache.DiskHits,Memory=process.PrivateMemorySize64,Timings=timings},DocumentStore.JsonOptions));
        };
        string? failure=null;int imported=0;
        try
        {
            if(session.Document.Photos.Count>0)throw new InvalidOperationException("压力测试需要空白隔离数据目录");
            var paths=Directory.EnumerateFiles(photoRoot,"*.jpg").Order().Take(count).ToArray();
            if(paths.Length!=count)throw new InvalidOperationException($"需要 {count} 张照片，实际只有 {paths.Length}");
            timer.Start();await renderer.DeviceReady;renderer.PerformancePhase="Import";var watch=Stopwatch.StartNew();
            await session.ImportAsync(paths);timings["ImportAndThumbnailsMs"]=watch.Elapsed.TotalMilliseconds;imported=session.Document.Photos.Count;
            if(imported!=count)throw new InvalidOperationException($"导入 {imported}/{count}，需检查重复文件或损坏图像，不能以计划数量作为实测规模");
            renderer.PerformancePhase="FitAndGpuPreparation";watch.Restart();session.Fit(true);await renderer.PrepareVisibleTable();timings["FitAndGpuPreparationMs"]=watch.Elapsed.TotalMilliseconds;
            renderer.PerformancePhase="Pan";watch.Restart();for(int i=0;i<120;i++)
            {
                var angle=i*Math.PI/60;session.Document.Viewport.OffsetX=Math.Sin(angle)*22;session.Document.Viewport.OffsetY=Math.Cos(angle)*18;
                board.Invalidate();await Task.Delay(16);
            }
            timings["Pan120StepsWallMs"]=watch.Elapsed.TotalMilliseconds;
            renderer.PerformancePhase="Ignition";session.ToggleLight(Rendering.TableRenderer.Seconds);await Task.Delay(2200);
            renderer.PerformancePhase="CloseViewing";var first=session.Document.Slides[0];renderer.Pointer=Geometry.Place(session.Document,first).Center;renderer.PointerInside=true;session.Loupe=true;session.CloseViewing=true;session.Notify(false);await Task.Delay(550);
            session.Power=LoupePower.Eight;session.Notify(false);await Task.Delay(300);session.CloseViewing=false;await Task.Delay(400);session.Loupe=false;session.Notify(false);
            renderer.PerformancePhase="WarmThumbnails";watch.Restart();foreach(var p in paths)await session.Images.GetAsync(p,220);timings["WarmThumbnailRequestsMs"]=watch.Elapsed.TotalMilliseconds;
            session.Flush();
        }
        catch(Exception ex){failure=ex.ToString();session.Storage.Log($"压力测试失败 {ex}");}
        finally{timer.Stop();}
        var times=renderer.FrameTimes.Order().ToArray();
        double Quantile(double q)=>times.Length==0?0:times[(int)Math.Min(times.Length-1,Math.Floor((times.Length-1)*q))];
        var optical=session.Images.OpticalCache;
        var report=new{Date=DateTimeOffset.Now,Version=BuildInfo.Version,Requested=count,Imported=imported,Passed=failure==null,Failure=failure,Timings=timings,DrawCallbackCpuMs=new{Count=times.Length,P50=Quantile(.5),P95=Quantile(.95),P99=Quantile(.99),Max=times.LastOrDefault()},Notice="DrawCallback是CPU回调耗时，不包含完整GPU呈现，不能作为60FPS证据；当前不包含完整UI输入或长期稳定性验收。",MemoryBytes=session.Images.MemoryBytes,GpuTextureEstimate=renderer.GpuBytes,session.Images.MemoryHits,session.Images.DiskHits,session.Images.DecodeCount,OpticalCache=new{optical.BuildCount,optical.DiskHits,optical.CorruptReads,optical.WriteFailures,Bytes=Directory.Exists(optical.DirectoryPath)?Directory.EnumerateFiles(optical.DirectoryPath,"*.ltoc").Sum(p=>new FileInfo(p).Length):0,Entries=Directory.Exists(optical.DirectoryPath)?Directory.GetFiles(optical.DirectoryPath,"*.ltoc").Length:0},CacheBytes=Directory.EnumerateFiles(Path.Combine(session.Storage.Root,"cache"),"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length),Samples=samples};
        await File.WriteAllTextAsync(Path.Combine(session.Storage.Root,"logs","draw-samples.json"),JsonSerializer.Serialize(renderer.DrawSamples,DocumentStore.JsonOptions));
        await File.WriteAllTextAsync(Path.Combine(session.Storage.Root,"logs","benchmark.json"),JsonSerializer.Serialize(report,DocumentStore.JsonOptions));Environment.ExitCode=failure==null?0:1;Close();
    }
}
