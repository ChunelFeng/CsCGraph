namespace CsCGraph;

using System.Threading;

public abstract class GParam : IDisposable
{
    private readonly ReaderWriterLockSlim _paramLock =
        new(LockRecursionPolicy.SupportsRecursion);
    private readonly Lock _backtraceLock = new();
    private readonly List<string> _backtrace = [];

    private string _key = string.Empty;
    private bool _backtraceEnable;
    private bool _disposed;

    protected virtual CStatus Setup() => new();

    protected virtual void Reset(CStatus curStatus)
    {
    }

    public IReadOnlyList<string> GetBacktrace()
    {
        lock (_backtraceLock)
        {
            return _backtrace.ToArray();
        }
    }

    public CStatus AddBacktrace(string trace)
    {
        if (!_backtraceEnable || string.IsNullOrWhiteSpace(trace))
        {
            return new CStatus();
        }

        lock (_backtraceLock)
        {
            if (!_backtrace.Contains(trace, StringComparer.Ordinal))
            {
                _backtrace.Add(trace);
            }
        }

        return new CStatus();
    }

    public void CleanBacktrace()
    {
        lock (_backtraceLock)
        {
            _backtrace.Clear();
        }
    }

    public string GetKey() => _key;

    internal bool BacktraceEnabled => _backtraceEnable;

    public void Lock() => _paramLock.EnterWriteLock();

    public void Unlock() => _paramLock.ExitWriteLock();

    public bool TryLock() => _paramLock.TryEnterWriteLock(0);

    public void ReadLock() => _paramLock.EnterReadLock();

    public void ReadUnlock() => _paramLock.ExitReadLock();

    public bool TryReadLock() => _paramLock.TryEnterReadLock(0);

    internal CStatus SetupInternal() => Setup();

    internal void ResetInternal(CStatus curStatus) => Reset(curStatus);

    internal void Configure(string key, bool backtrace)
    {
        _key = key;
        _backtraceEnable = backtrace;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _paramLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
