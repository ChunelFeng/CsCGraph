namespace CsCGraph;

internal sealed class GEpochTaskTracker
{
    private readonly Lock _lock = new();
    private readonly List<(Task<CStatus> Task, GElementTimeoutStrategy Strategy)> _tasks = [];

    internal void Track(Task<CStatus> task, GElementTimeoutStrategy strategy)
    {
        lock (_lock)
        {
            _tasks.RemoveAll(static item => item.Task.IsCompleted);
            _tasks.Add((task, strategy));
        }
    }

    internal async ValueTask<CStatus> WaitForHoldsAsync()
    {
        Task<CStatus>[] tasks;
        lock (_lock)
        {
            tasks = _tasks
                .Where(static item => item.Strategy == GElementTimeoutStrategy.HoldByPipeline)
                .Select(static item => item.Task)
                .ToArray();
        }

        var status = new CStatus();
        foreach (var task in tasks)
        {
            status += await task.ConfigureAwait(false);
        }

        Cleanup();
        return status;
    }

    internal CStatus WaitForAll()
    {
        Task<CStatus>[] tasks;
        lock (_lock)
        {
            tasks = _tasks.Select(static item => item.Task).ToArray();
        }

        foreach (var task in tasks)
        {
            _ = task.GetAwaiter().GetResult();
        }

        Cleanup();
        return new CStatus();
    }

    private void Cleanup()
    {
        lock (_lock)
        {
            _tasks.RemoveAll(static item => item.Task.IsCompleted);
        }
    }
}
