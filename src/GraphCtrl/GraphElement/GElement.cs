namespace CsCGraph;

public abstract class GElement
{
    private readonly HashSet<GElement> _dependence = [];
    private readonly HashSet<GElement> _runBefore = [];

    private GParamManager? _paramManager;
    private GAspectManager? _aspectManager;
    private GEventManager? _eventManager;
    private GEpochTaskTracker? _epochTaskTracker;
    private Task<CStatus>? _timeoutTask;
    private GConcurrencyGate? _concurrencyGate;
    private GStageManager? _stageManager;
    private readonly Dictionary<string, GPassedParam> _localParams =
        new(StringComparer.Ordinal);
    private string _name = string.Empty;
    private readonly string _session = Guid.NewGuid().ToString("N");
    private int _loop = GElementDefaults.CGraphDefaultLoopTimes;
    private int _leftDependCounter;
    private int _state = (int)GElementState.Normal;
    private long _timeoutMillisecond;
    private GElementTimeoutStrategy _timeoutStrategy = GElementTimeoutStrategy.AsError;
    private int _level = GElementDefaults.CGraphDefaultElementLevel;
    private int _bindingIndex = GElementDefaults.CGraphDefaultBindingIndex;
    private bool _visible = true;
    private bool _isMacro;
    private bool _isPrepared;
    private GGroup? _belong;
    private long _perfTicks;
    private int _perfLoop;

    protected GElement(GElementType elementType = GElementType.Element)
    {
        ElementType = elementType;
    }

    internal GElementType ElementType { get; }

    internal IReadOnlyCollection<GElement> Dependencies => _dependence;

    internal IReadOnlyCollection<GElement> Successors => _runBefore;

    internal HashSet<GElement> Dependence => _dependence;

    internal HashSet<GElement> RunBefore => _runBefore;

    protected virtual CStatus Init() => new();

    protected virtual CStatus Run() => new();

    protected virtual CStatus Destroy() => new();
    protected virtual bool IsHold() => false;
    protected virtual bool IsMatch() => false;
    protected virtual CStatus PrepareRun() => new();
    protected virtual CStatus CheckRunResult() => new();

    protected internal virtual ValueTask<CStatus> ExecuteOnceAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Run());
    }

    internal CStatus InitializeInternal()
    {
        _isPrepared = false;
        var status = _aspectManager?.BeginInit() ?? new CStatus();
        if (status.IsOk())
        {
            status += InvokeLifecycle(Init, "init");
        }
        return _aspectManager?.FinishInit(status) ?? status;
    }

    internal CStatus DestroyInternal()
    {
        var status = _aspectManager?.BeginDestroy() ?? new CStatus();
        if (status.IsOk())
        {
            status += InvokeLifecycle(Destroy, "destroy");
        }
        return _aspectManager?.FinishDestroy(status) ?? status;
    }

    internal ValueTask<CStatus> FatRunAsync(CancellationToken cancellationToken)
        => _timeoutMillisecond <= 0
            ? RunBodyAsync(cancellationToken)
            : RunWithTimeoutAsync(cancellationToken);

    private async ValueTask<CStatus> RunBodyAsync(CancellationToken cancellationToken)
    {
        var perfStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            if (!_visible) return new CStatus();
            var status = new CStatus();
            if (!_isPrepared)
            {
                status = PrepareRun();
                if (status.IsErr()) return status;
                _isPrepared = true;
            }

            for (var index = 0;
                 index < _loop && status.IsOk() && GetState() == GElementState.Normal;
                 index++)
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var current = _aspectManager?.BeginRun() ?? new CStatus();
                    if (current.IsOk())
                    {
                        current += await ExecuteOnceAsync(cancellationToken).ConfigureAwait(false);
                    }
                    current = _aspectManager?.FinishRun(current) ?? current;
                    status += current;
                }
                while (status.IsOk() && IsHold());
            }

            status += CheckRunResult();
            return status;
        }
        catch (OperationCanceledException)
        {
            SetState(GElementState.Cancel);
            return new CStatus($"element [{DisplayName()}] run cancelled");
        }
        catch (Exception exception)
        {
            _aspectManager?.EnterCrashed();
            return CException.FromException(exception,
                $"element [{DisplayName()}] run");
        }
        finally
        {
            Interlocked.Add(ref _perfTicks,
                System.Diagnostics.Stopwatch.GetTimestamp() - perfStarted);
            Interlocked.Increment(ref _perfLoop);
        }
    }

    private async ValueTask<CStatus> RunWithTimeoutAsync(
        CancellationToken cancellationToken)
    {
        var runTask = Task.Run(
            async () => await RunBodyAsync(cancellationToken).ConfigureAwait(false),
            CancellationToken.None);
        Volatile.Write(ref _timeoutTask, runTask);
        _epochTaskTracker?.Track(runTask, _timeoutStrategy);

        var completed = await Task.WhenAny(
            runTask,
            Task.Delay(TimeSpan.FromMilliseconds(_timeoutMillisecond)))
            .ConfigureAwait(false);
        if (ReferenceEquals(completed, runTask))
        {
            return await runTask.ConfigureAwait(false);
        }

        SetState(GElementState.Timeout);
        _aspectManager?.EnterTimeout();
        return _timeoutStrategy == GElementTimeoutStrategy.AsError
            ? new CStatus($"element [{DisplayName()}] running time more than [{_timeoutMillisecond}]ms")
            : new CStatus();
    }

    internal CStatus FatRun()
        => FatRunAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    internal bool MatchInternal() => IsMatch();

    internal virtual void SetEpochTaskTracker(GEpochTaskTracker tracker)
        => _epochTaskTracker = tracker;

    internal virtual void SetConcurrencyGate(GConcurrencyGate gate)
        => _concurrencyGate = gate;

    internal virtual void SetGStageManager(GStageManager manager)
        => _stageManager = manager;

    protected ValueTask<CStatus> EnterStageAsync(
        string key,
        CancellationToken cancellationToken = default)
        => _stageManager is null
            ? ValueTask.FromResult(new CStatus("stage manager is not configured"))
            : _stageManager.WaitAsync(key, cancellationToken);

    protected CStatus EnterStage(string key)
        => EnterStageAsync(key).AsTask().GetAwaiter().GetResult();

    internal void ResetRelationsInternal()
    {
        foreach (var dependency in _dependence) dependency._runBefore.Remove(this);
        foreach (var successor in _runBefore) successor._dependence.Remove(this);
        _dependence.Clear();
        _runBefore.Clear();
        _loop = GElementDefaults.CGraphDefaultLoopTimes;
    }

    internal CStatus AddDependencyInternal(GElement dependency)
    {
        if (ReferenceEquals(this, dependency))
            return new CStatus("element cannot depend on itself");
        if (_dependence.Add(dependency)) dependency._runBefore.Add(this);
        return new CStatus();
    }

    internal bool RemoveDependencyInternal(GElement dependency)
    {
        if (!_dependence.Remove(dependency)) return false;
        dependency._runBefore.Remove(this);
        return true;
    }

    internal void SetLoopInternal(int loop) => _loop = loop;

    internal void TrackEpochTask(
        Task<CStatus> task,
        GElementTimeoutStrategy strategy = GElementTimeoutStrategy.HoldByPipeline)
        => _epochTaskTracker?.Track(task, strategy);

    internal ValueTask<CStatus> WaitTimeoutTaskAsync()
    {
        var task = Volatile.Read(ref _timeoutTask);
        return task is null
            ? ValueTask.FromResult(new CStatus($"element [{DisplayName()}] has no timeout task"))
            : new ValueTask<CStatus>(task);
    }

    internal CStatus Configure(
        IEnumerable<GElement>? depends,
        string name,
        int loop = GElementDefaults.CGraphDefaultLoopTimes)
    {
        if (loop <= 0)
        {
            return new CStatus($"element [{name}] loop must be greater than zero");
        }

        _name = name;
        _loop = loop;
        if (depends is not null)
        {
            foreach (var dependency in depends)
            {
                if (ReferenceEquals(this, dependency))
                {
                    return new CStatus($"element [{DisplayName()}] cannot depend on itself");
                }

                if (_dependence.Add(dependency))
                {
                    dependency._runBefore.Add(this);
                }
            }
        }

        ResetDepend();
        return new CStatus();
    }

    internal virtual void SetGParamManager(GParamManager manager)
    {
        _paramManager = manager;
        _aspectManager?.SetGParamManager(manager);
    }

    internal virtual void SetGEventManager(GEventManager manager)
        => _eventManager = manager;

    protected CStatus Notify(string key, GEventType type = GEventType.Sync)
        => _eventManager?.Trigger(key, type) ?? new CStatus("event manager is null");

    public CStatus AddGAspect<TAspect>(params object?[] constructorArgs)
        where TAspect : GAspect
    {
        try
        {
            if (Activator.CreateInstance(typeof(TAspect), constructorArgs) is not TAspect aspect)
            {
                return new CStatus($"cannot construct aspect [{typeof(TAspect).FullName}]");
            }
            _aspectManager ??= new GAspectManager();
            return _aspectManager.Add(aspect, this, _paramManager);
        }
        catch (Exception exception)
        {
            return CException.FromException(exception,
                $"construct aspect [{typeof(TAspect).FullName}]");
        }
    }

    public CStatus AddGAspect<TAspect, TParam>(TParam param)
        where TAspect : GAspect, new()
        where TParam : GPassedParam, new()
    {
        var aspect = new TAspect();
        aspect.SetAParam(GPassedParam.CopyOf(param));
        _aspectManager ??= new GAspectManager();
        return _aspectManager.Add(aspect, this, _paramManager);
    }

    internal CStatus AddGAspect(Type type)
    {
        if (!typeof(GAspect).IsAssignableFrom(type) || type.IsAbstract)
            return new CStatus($"invalid aspect type [{type}]");
        if (Activator.CreateInstance(type, nonPublic: true) is not GAspect aspect)
            return new CStatus($"aspect type [{type}] needs a parameterless constructor");
        _aspectManager ??= new GAspectManager();
        return _aspectManager.Add(aspect, this, _paramManager);
    }

    internal IReadOnlyList<GAspect> RegisteredAspects
        => _aspectManager?.RegisteredAspects ?? [];

    internal void AddElementInfo(IEnumerable<GElement> depends, string name, int loop)
        => _ = Configure(depends, name, loop);

    internal void SetManager(GParamManager manager)
        => SetGParamManager(manager);

    internal bool DecrementDepend()
        => Interlocked.Decrement(ref _leftDependCounter) == 0;

    internal void ResetDepend()
        => Interlocked.Exchange(ref _leftDependCounter, _dependence.Count);

    internal void SetState(GElementState state)
        => Interlocked.Exchange(ref _state, (int)state);

    internal GElementState GetState()
        => (GElementState)Volatile.Read(ref _state);

    public string GetName() => _name;

    public int GetLoop() => _loop;

    public string GetSession() => _session;

    public GElement SetName(string name)
    {
        _name = name ?? string.Empty;
        return this;
    }

    public GElement SetLoop(int loop)
    {
        switch (loop)
        {
            case <= 0:
                throw new ArgumentOutOfRangeException(nameof(loop));
            case > 1 when _timeoutMillisecond > 0:
                throw new InvalidOperationException("timeout element loop cannot exceed one");
            default:
                _loop = loop;
                return this;
        }
    }

    public GElement SetLevel(int level)
    {
        _level = level;
        return this;
    }

    public int GetLevel() => _level;

    public GElement SetVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public bool IsVisible() => _visible;

    public GElement SetBindingIndex(int index)
    {
        _bindingIndex = index;
        return this;
    }

    public int GetBindingIndex() => _bindingIndex;

    public GElement SetMacro(bool macro)
    {
        if (this is GGroup) throw new InvalidOperationException("group cannot be a macro element");
        _isMacro = macro;
        return this;
    }

    public bool IsMacro() => _isMacro;

    public bool IsGGroup() => this is GGroup;

    public bool IsGAdaptor() => this is GAdapter;

    public bool IsGNode() => this is GNode;

    public GElementState GetCurState() => GetState();

    public CStatus AddDependGElements(IEnumerable<GElement> elements)
    {
        var status = new CStatus();
        foreach (var element in elements) status += AddDependencyInternal(element);
        ResetDepend();
        return status;
    }

    public CStatus RemoveDepend(GElement element)
        => RemoveDependencyInternal(element)
            ? new CStatus()
            : new CStatus($"element [{element.GetName()}] is not a dependency of [{DisplayName()}]");

    public GElementRelation GetRelation()
        => new()
        {
            Predecessors = _dependence.ToArray(),
            Successors = _runBefore.ToArray(),
            Children = this is GGroup group ? group.Children.ToArray() : [],
            Belong = _belong,
        };

    internal GGroup? Belong => _belong;

    internal void SetBelong(GGroup? group) => _belong = group;

    public GElement SetTimeout(
        long timeoutMillisecond,
        GElementTimeoutStrategy strategy = GElementTimeoutStrategy.AsError)
    {
        if (timeoutMillisecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMillisecond),
                "timeout cannot be negative");
        }

        if (timeoutMillisecond > 0 && _loop > GElementDefaults.CGraphDefaultLoopTimes)
        {
            throw new InvalidOperationException(
                "cannot set timeout when element loop is greater than one");
        }

        _timeoutMillisecond = timeoutMillisecond;
        _timeoutStrategy = strategy;
        return this;
    }

    public long GetTimeout() => _timeoutMillisecond;

    public GElementTimeoutStrategy GetTimeoutStrategy() => _timeoutStrategy;

    protected CStatus CreateGParam<T>(string key, bool backtrace = false)
        where T : GParam, new()
        => _paramManager is null
            ? new CStatus("param manager is null")
            : _paramManager.Create<T>(key, backtrace);

    protected T? GetGParam<T>(string key) where T : GParam
        => _paramManager?.Get<T>(key);

    protected T GetGParamWithNoEmpty<T>(string key) where T : GParam
        => GetGParam<T>(key)
           ?? throw new KeyNotFoundException($"param [{key}] is not found");

    protected CStatus RemoveGParam(string key)
        => _paramManager?.RemoveByKey(key)
            ?? new CStatus("param manager is null");

    public CStatus AddEParam<TParam>(string key, TParam param)
        where TParam : GPassedParam, new()
    {
        if (string.IsNullOrWhiteSpace(key)) return new CStatus("element param key is empty");
        if (_localParams.ContainsKey(key)) return new CStatus($"element param [{key}] duplicate");
        _localParams.Add(key, GPassedParam.CopyOf(param));
        return new CStatus();
    }

    internal CStatus AddEParam(string key, Type type)
    {
        if (!typeof(GPassedParam).IsAssignableFrom(type) || type.IsAbstract)
            return new CStatus($"invalid element param type [{type}]");
        if (Activator.CreateInstance(type, nonPublic: true) is not GPassedParam param)
            return new CStatus($"element param type [{type}] needs a parameterless constructor");
        if (!_localParams.TryAdd(key, param))
            return new CStatus($"element param [{key}] duplicate");
        return new CStatus();
    }

    internal IReadOnlyList<KeyValuePair<string, GPassedParam>> RegisteredEParams
        => _localParams.ToArray();

    internal void ResetPerfInfo()
    {
        Interlocked.Exchange(ref _perfTicks, 0);
        Interlocked.Exchange(ref _perfLoop, 0);
        _inLongestPath = false;
    }

    internal GPerfInfo GetPerfInfo()
        => new()
        {
            Loop = (uint)Math.Max(0, Volatile.Read(ref _perfLoop)),
            AccuCostTs = Volatile.Read(ref _perfTicks) * 1000.0
                / System.Diagnostics.Stopwatch.Frequency,
            InLongestPath = _inLongestPath,
        };

    private bool _inLongestPath;

    internal void MarkLongestPath() => _inLongestPath = true;

    protected TParam? GetEParam<TParam>(string key) where TParam : GPassedParam
        => _localParams.TryGetValue(key, out var param) ? param as TParam : null;

    private CStatus InvokeLifecycle(Func<CStatus> lifecycle, string phase)
    {
        try
        {
            return lifecycle();
        }
        catch (Exception exception)
        {
            return CException.FromException(exception,
                $"element [{DisplayName()}] {phase}");
        }
    }

    private string DisplayName()
        => string.IsNullOrEmpty(_name) ? _session : _name;
}
