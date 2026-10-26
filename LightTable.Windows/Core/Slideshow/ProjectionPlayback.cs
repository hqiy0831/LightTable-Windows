using System.Diagnostics;

namespace LightTable.Core;

public readonly record struct ProjectionFrame(ProjectionPhase Phase,double Transport,double Focus,double Shutter);

// Owns transport and audio cues; the platform window only renders its snapshot.
// A shared decoder may ignore cancellation. Generation checks release stale
// sources without ever replacing or replaying them in a newer request.
public sealed class ProjectionPlayback<T> : IDisposable where T:class
{
    readonly Func<string,CancellationToken,Task<T>> loader;
    readonly Func<Task> prepareAudio;
    readonly Action<bool> cue;
    readonly Action stop;
    readonly Action<T> release;
    readonly Func<TimeSpan,CancellationToken,Task> delay;
    readonly Func<double> clock;
    readonly TaskCompletionSource presentation=new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationTokenSource? request;
    long generation;
    double phaseStarted;
    bool disposed,hasPresented,reduceMotion,isLoading;
    public ProjectionPhase Phase {get;private set;}=ProjectionPhase.Waiting;
    public T? Photo {get;private set;}
    public string? RequestedPath {get;private set;}
    public string? Error {get;private set;}
    public bool LoadingHint {get;private set;}
    public event Action? Changed;
    public Task Pending {get;private set;}=Task.CompletedTask;
    public ProjectionPlayback(Func<string,CancellationToken,Task<T>> loader,Action<bool>? cue=null,Action? stop=null,Func<Task>? prepareAudio=null,Action<T>? release=null,Func<TimeSpan,CancellationToken,Task>? delay=null,Func<double>? clock=null)
    {
        this.loader=loader;this.cue=cue??(_=>{});this.stop=stop??(()=>{});this.prepareAudio=prepareAudio??(()=>Task.CompletedTask);
        this.release=release??(value=>(value as IDisposable)?.Dispose());this.delay=delay??Task.Delay;
        this.clock=clock??(()=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency);
    }
    public void BeginPresentation()=>presentation.TrySetResult();
    public Task ShowAsync(string path,bool reduceMotion=false)=>Start(path,reduceMotion,false);
    public Task RestoreAsync(bool reduceMotion=false)=>RequestedPath is {} path?Start(path,reduceMotion,hasPresented):Task.CompletedTask;
    public void InvalidateResources()
    {
        if(disposed)return;generation++;request?.Cancel();request?.Dispose();request=null;stop();
        if(Photo!=null)release(Photo);Photo=null;Error=null;LoadingHint=false;isLoading=false;SetPhase(ProjectionPhase.Waiting);
    }
    Task Start(string path,bool reduceMotion,bool restoring)
    {
        ObjectDisposedException.ThrowIf(disposed,this);request?.Cancel();request?.Dispose();request=new();
        var token=request.Token;var id=++generation;this.reduceMotion=reduceMotion;RequestedPath=path;Error=null;LoadingHint=false;isLoading=true;stop();
        if(Phase is ProjectionPhase.Departing or ProjectionPhase.Arriving)SetPhase(ProjectionPhase.Black);
        else Changed?.Invoke();
        _=Hint(id,token);Pending=Run(path,id,token,restoring);return Pending;
    }
    bool Current(long id)=>!disposed&&id==generation;
    void SetPhase(ProjectionPhase phase){Phase=phase;phaseStarted=clock();Changed?.Invoke();}
    async Task Hint(long id,CancellationToken token)
    {
        try{await Task.Delay(600,token);if(Current(id)&&isLoading){LoadingHint=true;Changed?.Invoke();}}
        catch(OperationCanceledException){}
    }
    async Task Run(string path,long id,CancellationToken token,bool restoring)
    {
        T? prepared=null;
        try
        {
            var audio=prepareAudio();
            _=audio.ContinueWith(t=>{_=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            prepared=await loader(path,token);await audio;
            if(!Current(id))return;token.ThrowIfCancellationRequested();LoadingHint=false;isLoading=false;
            await presentation.Task.WaitAsync(token);if(!Current(id))return;
            if(Phase==ProjectionPhase.Settled&&Photo!=null)
            {
                SetPhase(ProjectionPhase.Departing);cue(false);
                if(!reduceMotion)await delay(TimeSpan.FromSeconds(.08),token);
                if(!Current(id))return;SetPhase(ProjectionPhase.Black);
            }
            if(Phase==ProjectionPhase.Black&&!reduceMotion)await delay(TimeSpan.FromSeconds(.12),token);
            if(!Current(id))return;
            if(Photo!=null)release(Photo);Photo=prepared;prepared=null;SetPhase(ProjectionPhase.Waiting);
            if(restoring){SetPhase(ProjectionPhase.Settled);return;}
            if(!reduceMotion)await delay(TimeSpan.FromSeconds(.016),token);
            if(!Current(id))return;
            SetPhase(ProjectionPhase.Arriving);
            if(!hasPresented){hasPresented=true;cue(true);}
            if(!reduceMotion)await delay(TimeSpan.FromSeconds(.18),token);
            if(!Current(id))return;SetPhase(ProjectionPhase.Settled);
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){}
        catch(Exception ex)
        {
            if(Current(id))
            {
                Error=ex.Message;LoadingHint=false;isLoading=false;if(Photo!=null)release(Photo);Photo=null;SetPhase(ProjectionPhase.Black);
            }
        }
        finally{if(prepared!=null)release(prepared);}
    }
    public ProjectionFrame Frame
    {
        get
        {
            if(Phase is ProjectionPhase.Waiting or ProjectionPhase.Black)return new(Phase,-1,0,0);
            if(reduceMotion||Phase==ProjectionPhase.Settled)return new(Phase,0,1,1);
            var elapsed=clock()-phaseStarted;
            if(Phase==ProjectionPhase.Departing)return new(Phase,ProjectionMotion.Bezier(elapsed/.08,.12,.80,.25,1),1,1);
            return new(Phase,-1+ProjectionMotion.Bezier(elapsed/.18,.20,.65,.35,1),ProjectionMotion.Bezier(elapsed/.18,0,0,.58,1),ProjectionMotion.Bezier(elapsed/.08,0,0,.58,1));
        }
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;generation++;request?.Cancel();request?.Dispose();stop();if(Photo!=null)release(Photo);Photo=null;
        Phase=ProjectionPhase.Waiting;RequestedPath=null;Error=null;LoadingHint=false;
    }
}
