namespace CsCGraph;

public sealed class GParamManager : GraphManager<GParam>, IDisposable
{
    private readonly Dictionary<string, GParam> _paramsMap =
        new(StringComparer.Ordinal);
    private readonly Lock _mutex = new();
    private bool _disposed;

    internal GParamManager()
    {
    }

    public CStatus Create<T>(string key, bool backtrace = false)
        where T : GParam, new()
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new CStatus("param key is empty");
        }

        lock (_mutex)
        {
            if (_paramsMap.TryGetValue(key, out var current))
            {
                return current.GetType() == typeof(T)
                    ? new CStatus()
                    : new CStatus($"create [{key}] param duplicate");
            }

            var param = new T();
            param.Configure(key, backtrace);
            _paramsMap.Add(key, param);
            return new CStatus();
        }
    }

    public T? Get<T>(string key) where T : GParam
    {
        lock (_mutex)
        {
            if (!_paramsMap.TryGetValue(key, out var param))
            {
                return null;
            }

            param.AddBacktrace(Environment.StackTrace);
            return param as T;
        }
    }

    public CStatus RemoveByKey(string key)
    {
        GParam? removed;
        lock (_mutex)
        {
            if (!_paramsMap.Remove(key, out removed))
            {
                return new CStatus($"param [{key}] not found");
            }
        }

        removed.Dispose();
        return new CStatus();
    }

    public IReadOnlyList<string> GetKeys()
    {
        lock (_mutex)
        {
            return _paramsMap.Keys.Order(StringComparer.Ordinal).ToArray();
        }
    }

    internal CStatus Setup()
    {
        foreach (var param in Snapshot())
        {
            var status = param.SetupInternal();
            if (status.IsErr())
            {
                return status;
            }
        }

        return new CStatus();
    }

    internal void ResetWithStatus(CStatus curStatus)
    {
        foreach (var param in Snapshot())
        {
            param.ResetInternal(curStatus);
        }
    }

    public override CStatus Clear()
    {
        GParam[] parameters;
        lock (_mutex)
        {
            parameters = _paramsMap.Values.ToArray();
            _paramsMap.Clear();
        }

        foreach (var param in parameters)
        {
            param.Dispose();
        }

        return new CStatus();
    }

    public override int GetSize()
    {
        lock (_mutex)
        {
            return _paramsMap.Count;
        }
    }

    private GParam[] Snapshot()
    {
        lock (_mutex)
        {
            return _paramsMap.Values.ToArray();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Clear();
        GC.SuppressFinalize(this);
    }

    internal void Reset(CStatus curStatus) => ResetWithStatus(curStatus);

    internal IReadOnlyList<GParam> RegisteredParams => Snapshot();

    internal CStatus Create(Type type, string key, bool backtrace)
    {
        if (!typeof(GParam).IsAssignableFrom(type) || type.IsAbstract)
            return new CStatus($"invalid param type [{type}]");
        if (Activator.CreateInstance(type, nonPublic: true) is not GParam param)
            return new CStatus($"param type [{type}] needs a parameterless constructor");
        lock (_mutex)
        {
            if (_paramsMap.ContainsKey(key))
                return new CStatus($"param [{key}] duplicate");
            param.Configure(key, backtrace);
            _paramsMap.Add(key, param);
        }
        return new CStatus();
    }
}
