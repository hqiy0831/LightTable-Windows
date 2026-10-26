namespace LightTable.Core;

public readonly record struct ResourceTicket(long Version,Task Ready,CancellationToken Cancellation);

// Owned by the UI thread. Worker completions return to that owner before
// checking a ticket; invalidating an epoch never permits an old completion to
// mark the new resources ready. All waiters are released on reset or shutdown.
public sealed class ResourceEpoch : IDisposable
{
    CancellationTokenSource cancellation=new();
    TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool disposed;
    public long Version {get;private set;}
    public ResourceTicket Capture()=>new(Version,ready.Task,cancellation.Token);
    public bool IsCurrent(ResourceTicket ticket)=>!disposed&&ticket.Version==Version&&!ticket.Cancellation.IsCancellationRequested;
    public long Begin()
    {
        ObjectDisposedException.ThrowIf(disposed,this);cancellation.Cancel();cancellation.Dispose();ready.TrySetCanceled();
        cancellation=new();ready=new(TaskCreationOptions.RunContinuationsAsynchronously);return ++Version;
    }
    public bool Complete(long version)=>!disposed&&version==Version&&ready.TrySetResult();
    public bool Fail(long version,Exception error)
    {
        if(disposed||version!=Version||!ready.TrySetException(error))return false;
        _=ready.Task.Exception;return true;
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;Version++;cancellation.Cancel();cancellation.Dispose();ready.TrySetCanceled();
    }
}
