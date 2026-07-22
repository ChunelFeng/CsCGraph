namespace CsCGraph;

public abstract class GDaemon
{
    private long _intervalMillisecond;
    private GPassedParam? _daemonParam;
    private GParamManager? _paramManager;
    private CancellationTokenSource? _cancellation;
    private Task? _loopTask;
    private CStatus _loopStatus;

    protected abstract void DaemonTask(GPassedParam? param);
    protected virtual long Modify(GPassedParam? param) => 0;
    protected long GetInterval() => _intervalMillisecond;
    internal long Interval => _intervalMillisecond;
    protected T? GetGParam<T>(string key) where T : GParam => _paramManager?.Get<T>(key);

    internal CStatus Configure(long interval, GParamManager manager, GPassedParam? param)
    {
        if (interval <= 0) return new CStatus("daemon interval must be greater than zero");
        _intervalMillisecond = interval;
        _paramManager = manager;
        _daemonParam = param;
        return new CStatus();
    }

    internal CStatus Start()
    {
        if (_loopTask is not null) return new CStatus("daemon already started");
        _cancellation = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_cancellation.Token);
        return new CStatus();
    }

    internal async ValueTask<CStatus> StopAsync()
    {
        if (_loopTask is null) return _loopStatus;
        _cancellation!.Cancel();
        try { await _loopTask.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        _loopTask = null;
        _cancellation.Dispose();
        _cancellation = null;
        return _loopStatus;
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        var next = _intervalMillisecond;
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(next), token).ConfigureAwait(false);
            try
            {
                DaemonTask(_daemonParam);
                var modified = Modify(_daemonParam);
                next = modified > 0 ? modified : _intervalMillisecond;
            }
            catch (Exception exception)
            {
                _loopStatus += CException.FromException(exception,
                    $"daemon [{GetType().Name}] task");
                break;
            }
        }
    }
}
