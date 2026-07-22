namespace CsCGraph;

internal sealed class GConcurrencyGate
{
    private readonly Lock _lock = new();
    private readonly Queue<TaskCompletionSource> _waiters = new();
    private int _limit = int.MaxValue;
    private int _active;

    internal void Configure(int limit)
    {
        lock (_lock)
        {
            _limit = limit <= 0 ? int.MaxValue : limit;
            ReleaseWaiters();
        }
    }

    internal ValueTask EnterAsync()
    {
        lock (_lock)
        {
            if (_active < _limit)
            {
                _active++;
                return ValueTask.CompletedTask;
            }

            var waiter = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(waiter);
            return new ValueTask(waiter.Task);
        }
    }

    internal void Exit()
    {
        lock (_lock)
        {
            _active--;
            ReleaseWaiters();
        }
    }

    private void ReleaseWaiters()
    {
        while (_active < _limit && _waiters.Count > 0)
        {
            _active++;
            _waiters.Dequeue().TrySetResult();
        }
    }
}
