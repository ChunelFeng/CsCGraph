namespace src;

using System.Collections.Generic;
using System.Threading;

internal class GParamManager
{
    private readonly Dictionary<string, GParam> _params = new Dictionary<string, GParam>();
    private readonly Lock _lock = new Lock();

    internal CStatus Create<T>(string key) where T : GParam, new()
    {
        var status = new CStatus();
        lock (_lock)
        {
            if (_params.TryGetValue(key, out var curParam))
            {
                return curParam.GetType() == typeof(T) 
                    ? status : new CStatus($"create [{key}] param duplicate");
            }

            var param = new T();
            _params.Add(key, param);
        }

        return status;
    }

    internal T? Get<T>(string key) where T : GParam
    {
        lock (_lock)
        {
            return _params.TryGetValue(key, out var param) ? param as T : null;
        }
    }

    internal CStatus Setup()
    {
        var status = new CStatus();
        lock (_lock)
        {
            status = _params.Aggregate(status, (current, kvp) => current + kvp.Value.Setup());
        }
        return status;
    }

    internal void Reset(CStatus curStatus)
    {
        lock (_lock)
        {
            foreach (var kvp in _params)
            {
                kvp.Value.Reset(curStatus);
            }
        }
    }
}