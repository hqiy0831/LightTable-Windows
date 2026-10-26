using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class CollectionTests
{
    [Theory][InlineData(1)][InlineData(12)][InlineData(40)]
    public void FlightsStartAtRealSleeveAndFinishHiddenInBox(int count)
    {
        var size=new SizeD(1180,780);var destination=CollectionMotion.BoxCenter(size)+new PointD(0,3);
        for(int i=0;i<count;i++)
        {
            var frame=new RectD(30+i*104,700,90,56);var start=CollectionPose.At(frame,destination,i,0);var end=CollectionPose.At(frame,destination,i,1);
            Assert.Equal(frame.Center,start.Center);Assert.Equal(1,start.Scale);Assert.Equal(1,start.Opacity);Assert.False(start.BehindFront);
            Assert.Equal(destination,end.Center);Assert.Equal(.72,end.Scale,8);Assert.Equal(58,end.Tilt);Assert.Equal(0,end.Opacity);Assert.True(end.BehindFront);
            double prior=-1;
            for(int n=0;n<=200;n++)
            {
                var t=CollectionMotion.Duration(count)*n/200;var progress=CollectionMotion.Progress(t,i,count);Assert.InRange(progress,prior,1);prior=progress;
                var pose=CollectionPose.At(frame,destination,i,progress);Assert.True(double.IsFinite(pose.Center.X)&&double.IsFinite(pose.Center.Y));
                Assert.InRange(pose.Scale,.72,1);Assert.InRange(pose.Opacity,0,1);
            }
        }
        Assert.Equal(0,CollectionMotion.BlackOpacity(0,count));Assert.Equal(0,CollectionMotion.BoxOpacity(0,count));
        Assert.Equal(1,CollectionMotion.BlackOpacity(CollectionMotion.Duration(count),count));Assert.Equal(0,CollectionMotion.BoxOpacity(CollectionMotion.Duration(count),count));
        Assert.InRange((count-1)*CollectionMotion.Spacing(count),0,.990000001);
    }
    [Theory][InlineData(1920,1080,1.5)][InlineData(1200,1800,.6)]
    public void ProjectorFitsBothAxesToEightyFourPercent(double width,double height,double aspect)
    {
        var fit=ProjectionMotion.Fit(new(width,height),aspect);Assert.InRange(fit.Width,0,width*.84);Assert.InRange(fit.Height,0,height*.84);
        Assert.Equal(aspect,fit.Width/fit.Height,8);Assert.True(Math.Abs(fit.Width-width*.84)<1e-8||Math.Abs(fit.Height-height*.84)<1e-8);
    }
}

public sealed class ProjectionPlaybackTests
{
    sealed class Photo(string id) : IDisposable
    {
        public string Id=>id;public int Releases {get;private set;}
        public void Dispose()=>Releases++;
    }
    [Fact] public async Task GraphicsRebuildRestoresCurrentSlideWithoutReplayingTransportOrSound()
    {
        var cues=new List<bool>();var waits=new List<double>();int loads=0;var first=new Photo("old device");var second=new Photo("new device");
        using var playback=new ProjectionPlayback<Photo>((_,_)=>Task.FromResult(++loads==1?first:second),cues.Add,delay:(t,_)=>{waits.Add(t.TotalSeconds);return Task.CompletedTask;});playback.BeginPresentation();
        await playback.ShowAsync("same.jpg");Assert.Equal([true],cues);waits.Clear();playback.InvalidateResources();Assert.Null(playback.Photo);Assert.Equal("same.jpg",playback.RequestedPath);Assert.Equal(1,first.Releases);
        await playback.RestoreAsync();Assert.Same(second,playback.Photo);Assert.Equal(ProjectionPhase.Settled,playback.Phase);Assert.Empty(waits);Assert.Equal([true],cues);Assert.Equal(1,playback.Frame.Shutter);
    }
    [Fact] public async Task RebuildBeforeAnyArrivalStillProducesExactlyOneFirstCue()
    {
        var abandoned=new TaskCompletionSource<Photo>();var stale=new Photo("stale");var restored=new Photo("restored");var cues=new List<bool>();int loads=0;
        using var playback=new ProjectionPlayback<Photo>((_,_)=>++loads==1?abandoned.Task:Task.FromResult(restored),cues.Add);playback.BeginPresentation();
        var old=playback.ShowAsync("photo.jpg",true);playback.InvalidateResources();await playback.RestoreAsync(true);abandoned.SetResult(stale);await old;
        Assert.Same(restored,playback.Photo);Assert.Equal([true],cues);Assert.Equal(1,stale.Releases);
    }
    [Fact] public async Task ReadyGatePreventsEarlyFirstCueAndReducedMotionSkipsTransport()
    {
        var photo=new Photo("first");var cues=new List<bool>();using var playback=new ProjectionPlayback<Photo>((_,_)=>Task.FromResult(photo),cues.Add);
        var showing=playback.ShowAsync("first",true);Assert.False(showing.IsCompleted);Assert.Empty(cues);Assert.Equal(0,playback.Frame.Shutter);
        playback.BeginPresentation();await showing;Assert.Same(photo,playback.Photo);Assert.Equal(ProjectionPhase.Settled,playback.Phase);Assert.Equal(new(ProjectionPhase.Settled,0,1,1),playback.Frame);Assert.Equal([true],cues);
    }
    [Fact] public async Task SlowSourceKeepsOldPhotoThenUsesExactMechanicsAndOneOutgoingCue()
    {
        var a=new Photo("a");var b=new Photo("b");var ready=new TaskCompletionSource<Photo>();var cues=new List<bool>();var waits=new List<double>();var phases=new List<ProjectionPhase>();double clock=0;
        ProjectionPlayback<Photo>? playback=null;
        Task Delay(TimeSpan value,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();waits.Add(value.TotalSeconds);
            if(value.TotalSeconds==.08){Assert.Same(a,playback!.Photo);Assert.Equal(0,playback.Frame.Transport,7);clock+=.04;Assert.InRange(playback.Frame.Transport,.5,1);clock+=.04;}
            else clock+=value.TotalSeconds;return Task.CompletedTask;
        }
        using(playback=new((path,_)=>path=="a"?Task.FromResult(a):ready.Task,cues.Add,delay:Delay,clock:()=>clock))
        {
            playback.BeginPresentation();await playback.ShowAsync("a");waits.Clear();playback.Changed+=()=>phases.Add(playback.Phase);
            var switching=playback.ShowAsync("b");Assert.Same(a,playback.Photo);Assert.Equal(ProjectionPhase.Settled,playback.Phase);Assert.Equal([true],cues);
            ready.SetResult(b);await switching;Assert.Same(b,playback.Photo);Assert.Equal(1,a.Releases);
            Assert.Equal([.08,.12,.016,.18],waits);Assert.Equal([true,false],cues);
            Assert.Equal([ProjectionPhase.Settled,ProjectionPhase.Departing,ProjectionPhase.Black,ProjectionPhase.Waiting,ProjectionPhase.Arriving,ProjectionPhase.Settled],phases);
        }
        Assert.Equal(1,b.Releases);
    }
    [Fact] public async Task LateCancelledDecoderCannotReplaceNewPhotoOrReplayAudio()
    {
        var stale=new Photo("stale");var current=new Photo("current");var oldLoader=new TaskCompletionSource<Photo>();var cues=new List<bool>();
        using var playback=new ProjectionPlayback<Photo>((path,_)=>path=="old"?oldLoader.Task:Task.FromResult(current),cues.Add);playback.BeginPresentation();
        var old=playback.ShowAsync("old",true);await playback.ShowAsync("new",true);oldLoader.SetResult(stale);await old;
        Assert.Same(current,playback.Photo);Assert.Equal("new",playback.RequestedPath);Assert.Equal(1,stale.Releases);Assert.Equal(0,current.Releases);Assert.Equal([true],cues);
    }
    [Fact] public async Task InterruptingArrivalClosesShutterUntilNextSourceAndDisposeReleasesAll()
    {
        var a=new Photo("a");var b=new Photo("b");var arrival=new TaskCompletionSource();var next=new TaskCompletionSource<Photo>();
        Task Delay(TimeSpan t,CancellationToken token)=>t.TotalSeconds==.18?arrival.Task.WaitAsync(token):Task.CompletedTask;
        var playback=new ProjectionPlayback<Photo>((path,_)=>path=="a"?Task.FromResult(a):next.Task,delay:Delay);playback.BeginPresentation();
        var first=playback.ShowAsync("a");Assert.Equal(ProjectionPhase.Arriving,playback.Phase);var second=playback.ShowAsync("b",true);
        Assert.Equal(ProjectionPhase.Black,playback.Phase);Assert.Equal(0,playback.Frame.Shutter);next.SetResult(b);await second;await first;
        Assert.Same(b,playback.Photo);Assert.Equal(1,a.Releases);playback.Dispose();Assert.Equal(1,b.Releases);Assert.Null(playback.Photo);
    }
    [Fact] public async Task FailureClosesOldPhotoAndRetryClearsErrorImmediately()
    {
        var photo=new Photo("a");var retry=new TaskCompletionSource<Photo>();
        using var playback=new ProjectionPlayback<Photo>((path,_)=>path switch{"a"=>Task.FromResult(photo),"bad"=>Task.FromException<Photo>(new IOException("missing")),_=>retry.Task});playback.BeginPresentation();
        await playback.ShowAsync("a",true);await playback.ShowAsync("bad",true);Assert.Equal("missing",playback.Error);Assert.Null(playback.Photo);Assert.Equal(0,playback.Frame.Shutter);Assert.Equal(1,photo.Releases);
        var observedError="not updated";playback.Changed+=()=>observedError=playback.Error;var retrying=playback.ShowAsync("retry",true);Assert.Null(observedError);retry.SetResult(new("retry"));await retrying;Assert.Null(playback.Error);
    }
    [Fact] public async Task LoadingHintAppearsAfterSlowRequestButNeverReturnsAfterSettlement()
    {
        var ready=new TaskCompletionSource<Photo>();var hinted=new TaskCompletionSource();using var playback=new ProjectionPlayback<Photo>((_,_)=>ready.Task);playback.BeginPresentation();
        playback.Changed+=()=>{if(playback.LoadingHint)hinted.TrySetResult();};var loading=playback.ShowAsync("slow",true);Assert.False(playback.LoadingHint);
        await hinted.Task.WaitAsync(TimeSpan.FromSeconds(3));ready.SetResult(new("slow"));await loading;Assert.False(playback.LoadingHint);
        await playback.ShowAsync("cached",true);await Task.Delay(650);Assert.False(playback.LoadingHint);Assert.Equal(ProjectionPhase.Settled,playback.Phase);
    }
}
