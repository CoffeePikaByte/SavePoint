namespace GameHub.Infrastructure.IntegrationTests;

public class ConcurrencyWaitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DrainAsync_TimeoutStillWaitsForWorkerAndPreservesTimeout(bool workerFaults)
    {
        var worker = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = ConcurrencyWait.DrainAsync(worker.Task, TimeSpan.Zero);
        Assert.False(waiting.IsCompleted);
        if (workerFaults) worker.SetException(new InvalidOperationException("late worker failure"));
        else worker.SetResult(1);
        await Assert.ThrowsAsync<TimeoutException>(() => waiting);
    }
}
