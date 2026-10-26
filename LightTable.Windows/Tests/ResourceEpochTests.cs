using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class ResourceEpochTests
{
    [Fact] public async Task ResetCancelsAllOldWaitersAndRejectsLateCompletion()
    {
        using var epoch=new ResourceEpoch();var old=epoch.Capture();var pending=Enumerable.Range(0,12).Select(_=>old.Ready.WaitAsync(TimeSpan.FromSeconds(2))).ToArray();
        var nextVersion=epoch.Begin();Assert.True(old.Cancellation.IsCancellationRequested);Assert.False(epoch.IsCurrent(old));Assert.False(epoch.Complete(old.Version));
        foreach(var waiter in pending)await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>waiter);
        var current=epoch.Capture();Assert.False(current.Ready.IsCompleted);Assert.True(epoch.Complete(nextVersion));await current.Ready;Assert.True(epoch.IsCurrent(current));
    }
    [Fact] public async Task CompletedFirstGenerationDoesNotMakeReplacementReady()
    {
        using var epoch=new ResourceEpoch();epoch.Complete(epoch.Version);var old=epoch.Capture();await old.Ready;
        epoch.Begin();var current=epoch.Capture();Assert.True(old.Cancellation.IsCancellationRequested);Assert.False(current.Ready.IsCompleted);
        Assert.False(epoch.Fail(old.Version,new IOException("stale")));Assert.False(current.Ready.IsCompleted);epoch.Complete(current.Version);await current.Ready;
    }
    [Fact] public async Task FaultCanBeRetriedAndDoesNotPoisonNextGeneration()
    {
        using var epoch=new ResourceEpoch();var ticket=epoch.Capture();Assert.True(epoch.Fail(ticket.Version,new IOException("failed upload")));
        Assert.Equal("failed upload",(await Assert.ThrowsAsync<IOException>(()=>ticket.Ready)).Message);epoch.Begin();var next=epoch.Capture();Assert.False(next.Ready.IsCompleted);epoch.Complete(next.Version);await next.Ready;
    }
    [Fact] public async Task ShutdownReleasesWaitersAndRejectsSubsequentReset()
    {
        var epoch=new ResourceEpoch();var ticket=epoch.Capture();epoch.Dispose();epoch.Dispose();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ticket.Ready);
        Assert.True(ticket.Cancellation.IsCancellationRequested);Assert.False(epoch.Complete(ticket.Version));Assert.Throws<ObjectDisposedException>(()=>epoch.Begin());
    }
}
