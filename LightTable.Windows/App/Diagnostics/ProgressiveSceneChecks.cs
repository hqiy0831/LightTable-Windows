using LightTable.Windows.Rendering;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckSceneRasterLifetime()
    {
        bool wasSelection=session.ShowSelection,wasLoupe=session.Loupe;
        try
        {
            session.Loupe=false;await renderer.PrepareVisibleTable();
            var until=TableRenderer.Seconds+5;while(renderer.ResourcesPending&&TableRenderer.Seconds<until)await Task.Delay(16);
            var builds=renderer.SceneRasterBuildCount;
            for(int i=0;i<16;i++)
            {
                session.ShowSelection=i%2==0;using var snapshot=renderer.RenderSnapshot(true,0,true);
                if(renderer.SceneRasterBytes<=0||renderer.SceneRasterBytes>32L*1024*1024||renderer.SceneRasterBuildCount-renderer.SceneRasterDisposeCount!=1)throw new InvalidOperationException("台面栅格未生成、超预算或遗留旧目标");
            }
            if(renderer.SceneRasterBuildCount-builds<16)throw new InvalidOperationException("场景失效没有重建栅格");
            renderer.DocumentChanged();
            if(renderer.SceneRasterBytes!=0||renderer.SceneRasterBuildCount!=renderer.SceneRasterDisposeCount)throw new InvalidOperationException("丢弃场景后仍持有栅格");
            return $"16次实际GPU场景重建始终仅1个存活目标；丢弃后0个；累计builds={renderer.SceneRasterBuildCount},disposals={renderer.SceneRasterDisposeCount}。不替代长期显存驱动测量。";
        }
        finally{session.ShowSelection=wasSelection;session.Loupe=wasLoupe;}
    }
    async Task<string> CheckProgressiveSceneCache()
    {
        var path=session.Document.Slides[0].Path;
        var reached=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        renderer.BeforeUpload=(_,bucket)=>{if(bucket==1377){reached.TrySetResult();return release.Task;}return Task.CompletedTask;};
        try
        {
            using(var initial=renderer.RenderSnapshot(true,0,true)){}
            var request=renderer.PrepareAsync(path,1377);await reached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var builds=renderer.SceneBuildCount;var reuses=renderer.SceneReuseCount;
            using(var pending=renderer.RenderSnapshot(true,0,true)){}
            if(renderer.SceneReuseCount<=reuses||renderer.SceneBuildCount!=builds)throw new InvalidOperationException("后台请求期间没有复用已有场景");
            release.TrySetResult();await request.WaitAsync(TimeSpan.FromSeconds(10));
            using(var final=renderer.RenderSnapshot(true,0,true)){}
            if(renderer.SceneBuildCount<=builds||renderer.CachedResourcesRevision!=renderer.ResourcesRevision)throw new InvalidOperationException("末次资源完成后仍显示旧场景");
            return $"后台请求中复用命令场景，末次上传后立即记录新资源版本={renderer.ResourcesRevision}；builds={renderer.SceneBuildCount},reuses={renderer.SceneReuseCount}。";
        }
        finally{release.TrySetResult();renderer.BeforeUpload=null;}
    }
}
