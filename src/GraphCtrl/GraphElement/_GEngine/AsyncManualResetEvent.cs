namespace CsCGraph;

/// <summary>
/// Allocation-free while signalled; a new completion source is allocated only
/// when the dispatch path transitions into the suspended state.
/// </summary>
internal sealed class AsyncManualResetEvent
{
    private readonly Lock _lock = new();
    private TaskCompletionSource _signal = Create(signalled: true);

    internal ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        var task = Volatile.Read(ref _signal).Task;
        return task.IsCompletedSuccessfully
            ? ValueTask.CompletedTask
            : new ValueTask(task.WaitAsync(cancellationToken));
    }

    internal void Set()
        => Volatile.Read(ref _signal).TrySetResult();

    internal void Reset()
    {
        lock (_lock)
        {
            if (_signal.Task.IsCompleted)
            {
                _signal = Create(signalled: false);
            }
        }
    }

    private static TaskCompletionSource Create(bool signalled)
    {
        var source = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        if (signalled)
        {
            source.TrySetResult();
        }

        return source;
    }
}
