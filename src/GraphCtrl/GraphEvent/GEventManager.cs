namespace CsCGraph;

internal sealed class GEventManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, GEvent> _events = new(StringComparer.Ordinal);
    private readonly List<Task<CStatus>> _asyncTasks = [];
    private readonly GParamManager _paramManager;

    internal GEventManager(GParamManager manager) => _paramManager = manager;

    internal CStatus Create<TEvent>(string key) where TEvent : GEvent, new()
        => Create<TEvent>(key, null);

    internal CStatus Create<TEvent>(string key, GPassedParam? param)
        where TEvent : GEvent, new()
    {
        lock (_lock)
        {
            if (_events.ContainsKey(key)) return new CStatus($"event [{key}] duplicate");
            var item = new TEvent();
            item.Configure(_paramManager, param);
            _events.Add(key, item);
            return new CStatus();
        }
    }

    internal CStatus Trigger(string key, GEventType type)
    {
        GEvent item;
        lock (_lock)
        {
            if (!_events.TryGetValue(key, out item!)) return new CStatus($"event [{key}] not found");
        }
        if (type == GEventType.Sync) return item.Fire();
        var task = Task.Run(item.Fire);
        lock (_lock) _asyncTasks.Add(task);
        return new CStatus();
    }

    internal CStatus Reset()
    {
        Task<CStatus>[] tasks;
        lock (_lock)
        {
            tasks = _asyncTasks.ToArray();
            _asyncTasks.Clear();
        }
        if (tasks.Length == 0) return new CStatus();
        Task.WaitAll(tasks);
        return tasks.Aggregate(new CStatus(), (status, task) => status + task.Result);
    }

    internal CStatus Clear()
    {
        var status = Reset();
        lock (_lock) _events.Clear();
        return status;
    }

    internal IReadOnlyList<KeyValuePair<string, GEvent>> RegisteredEvents
    {
        get { lock (_lock) return _events.ToArray(); }
    }

    internal CStatus Create(Type type, string key)
    {
        if (!typeof(GEvent).IsAssignableFrom(type) || type.IsAbstract)
            return new CStatus($"invalid event type [{type}]");
        if (Activator.CreateInstance(type, nonPublic: true) is not GEvent item)
            return new CStatus($"event type [{type}] needs a parameterless constructor");
        lock (_lock)
        {
            if (_events.ContainsKey(key))
                return new CStatus($"event [{key}] duplicate");
            item.Configure(_paramManager, null);
            _events.Add(key, item);
        }
        return new CStatus();
    }
}
