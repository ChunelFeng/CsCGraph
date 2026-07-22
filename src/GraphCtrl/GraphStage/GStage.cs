namespace CsCGraph;

public class GStageParam : GPassedParam
{
    public override void Clone(GPassedParam param)
    {
        ArgumentNullException.ThrowIfNull(param);
    }
}

public class GStage
{
    private readonly object _lock = new();
    private TaskCompletionSource _generation = CreateGeneration();
    private int _threshold;
    private int _current;
    private GStageParam? _param;

    public string Key { get; internal set; } = string.Empty;
    public int TargetCount => _threshold;
    public int CurrentCount
    {
        get { lock (_lock) return _current; }
    }

    protected virtual void Launch(GStageParam? param)
    {
    }

    internal void Configure(string key, int threshold, GStageParam? param)
    {
        Key = key;
        _threshold = threshold;
        _param = param;
    }

    internal async ValueTask<CStatus> WaitingAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource generation;
        var launches = false;
        lock (_lock)
        {
            generation = _generation;
            _current++;
            if (_current >= _threshold)
            {
                _current = 0;
                _generation = CreateGeneration();
                launches = true;
            }
        }

        if (launches)
        {
            try
            {
                Launch(_param);
                generation.TrySetResult();
                return new CStatus();
            }
            catch (Exception exception)
            {
                generation.TrySetException(exception);
                return CException.FromException(exception, $"stage [{Key}] launch");
            }
        }

        try
        {
            await generation.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new CStatus();
        }
        catch (OperationCanceledException)
        {
            lock (_lock)
            {
                if (ReferenceEquals(_generation, generation) && _current > 0) _current--;
            }
            return new CStatus($"stage [{Key}] wait cancelled");
        }
        catch (Exception exception)
        {
            return CException.FromException(exception, $"stage [{Key}] wait");
        }
    }

    public bool IsFinished() => CurrentCount == 0;

    private static TaskCompletionSource CreateGeneration()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
