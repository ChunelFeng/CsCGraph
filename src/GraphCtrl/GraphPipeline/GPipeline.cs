namespace CsCGraph;

public sealed class GPipeline : IDisposable, IAsyncDisposable
{
    private enum Lifecycle
    {
        Create,
        Initialized,
        Destroyed,
    }

    private readonly object _lifecycleLock = new();
    private Lifecycle _lifecycle = Lifecycle.Create;
    private CancellationTokenSource? _runCancellation;
    private bool _running;
    private bool _disposed;

    public GPipeline()
    {
        EventManager = new GEventManager(ParamManager);
    }

    internal GElementManager ElementManager { get; } = new();

    internal GParamManager ParamManager { get; } = new();

    internal GEventManager EventManager { get; }

    internal GDaemonManager DaemonManager { get; } = new();

    internal GStageManager StageManager { get; } = new();

    internal GThreadPoolConfig ThreadPoolConfig { get; private set; } = new();

    public CStatus RegisterGElement<T>(
        out T element,
        IReadOnlyCollection<GElement>? depends = null,
        string name = "",
        int loop = GElementDefaults.CGraphDefaultLoopTimes)
        where T : GElement, new()
    {
        element = new T();
        return RegisterGElement(element, depends, name, loop);
    }

    public CStatus RegisterGElement<T>(
        T element,
        IReadOnlyCollection<GElement>? depends = null,
        string name = "",
        int loop = GElementDefaults.CGraphDefaultLoopTimes)
        where T : GElement
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Create || _running)
            {
                return InvalidState("register element");
            }

            if (element.Belong is not null)
            {
                return new CStatus($"element [{element.GetName()}] belongs to group [{element.Belong.GetName()}]");
            }

            var status = element.Configure(depends, name, loop);
            if (status.IsErr())
            {
                return status;
            }

            element.SetGParamManager(ParamManager);
            element.SetGEventManager(EventManager);
            element.SetGStageManager(StageManager);
            return ElementManager.Add(element);
        }
    }

    public CStatus RegisterGElementWithArgs<T>(
        out T element,
        IReadOnlyCollection<GElement>? depends,
        params object?[] constructorArgs)
        where T : GElement
    {
        try
        {
            if (Activator.CreateInstance(typeof(T), constructorArgs) is not T created)
            {
                element = null!;
                return new CStatus($"cannot construct element type [{typeof(T).FullName}]");
            }

            element = created;
            return RegisterGElement(element, depends);
        }
        catch (Exception exception)
        {
            element = null!;
            return CException.FromException(exception,
                $"construct element type [{typeof(T).FullName}]");
        }
    }

    public T CreateGNode<T>(GNodeInfo? info = null) where T : GNode, new()
    {
        var element = new T();
        info ??= new GNodeInfo();
        var status = element.Configure(info.Dependence, info.Name, info.Loop);
        if (status.IsErr())
        {
            throw new CException(status);
        }

        element.SetGParamManager(ParamManager);
        element.SetGEventManager(EventManager);
        element.SetGStageManager(StageManager);
        return element;
    }

    public T CreateGGroup<T>(
        IReadOnlyCollection<GElement> elements,
        IReadOnlyCollection<GElement>? depends = null,
        string name = "",
        int loop = GElementDefaults.CGraphDefaultLoopTimes)
        where T : GGroup, new()
    {
        if (elements is null)
        {
            throw new CException("group elements cannot be null");
        }

        if (loop <= 0)
        {
            throw new CException($"element [{name}] loop must be greater than zero");
        }

        var children = new List<GElement>();
        var childSet = new HashSet<GElement>(ReferenceEqualityComparer.Instance);
        var dependencies = new List<GElement>();
        try
        {
            foreach (var element in elements)
            {
                if (element is null)
                {
                    throw new CException("group child cannot be null");
                }

                if (!childSet.Add(element))
                {
                    throw new CException(
                        $"element [{element.GetName()}] cannot appear in a group twice");
                }

                if (element.Belong is not null)
                {
                    throw new CException(
                        $"element [{element.GetName()}] already belongs to another group");
                }

                children.Add(element);
            }

            if (depends is not null)
            {
                foreach (var dependency in depends)
                {
                    if (dependency is null)
                    {
                        throw new CException("group dependency cannot be null");
                    }

                    dependencies.Add(dependency);
                }
            }
        }
        catch (CException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CException("validate group inputs", exception);
        }

        T group;
        try
        {
            group = new T();
        }
        catch (Exception exception)
        {
            throw new CException($"construct group type [{typeof(T).FullName}]", exception);
        }

        try
        {
            if (dependencies.Any(dependency => ReferenceEquals(group, dependency)))
            {
                throw new CException($"element [{name}] cannot depend on itself");
            }

            foreach (var status in children.Select(element => group.AddElement(element)).Where(status => status.IsErr()))
            {
                throw new CException(status);
            }

            var configureStatus = group.Configure(dependencies, name, loop);
            if (configureStatus.IsErr())
            {
                throw new CException(configureStatus);
            }

            group.SetGParamManager(ParamManager);
            group.SetGEventManager(EventManager);
            group.SetGStageManager(StageManager);
            return group;
        }
        catch (CException)
        {
            group.RollbackElements();
            group.ResetRelationsInternal();
            throw;
        }
        catch (Exception exception)
        {
            group.RollbackElements();
            group.ResetRelationsInternal();
            throw new CException($"create group type [{typeof(T).FullName}]", exception);
        }
    }

    public CStatus RegisterGGroup<T>(
        T group,
        IReadOnlyCollection<GElement>? depends = null,
        string name = "",
        int loop = GElementDefaults.CGraphDefaultLoopTimes)
        where T : GGroup
        => RegisterGElement(group, depends, name, loop);

    public GPipeline SetUniqueThreadPoolConfig(GThreadPoolConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ThreadPoolConfig = new GThreadPoolConfig
        {
            DefaultThreadSize = config.DefaultThreadSize,
            SecondaryThreadSize = config.SecondaryThreadSize,
        };
        ElementManager.ConfigureConcurrency(
            config.DefaultThreadSize + config.SecondaryThreadSize);
        return this;
    }

    public int Trim() => ElementManager.Trim();

    public GPipeline SetGEngineType(GEngineType type)
    {
        var status = ElementManager.SetEngineType(type);
        return status.IsErr() ? throw new CException(status) : this;
    }

    public int GetMaxPara() => ElementManager.CalcMaxParaSize();

    public CStatus MakeSerial()
    {
        if (!ElementManager.CheckSerializable())
            return new CStatus("pipeline graph cannot use serial mode");
        ElementManager.ConfigureConcurrency(1);
        return ElementManager.SetEngineType(GEngineType.Topo);
    }

    public GPipelineState GetCurState()
        => ElementManager.CurrentState switch
        {
            GElementState.Cancel => GPipelineState.Cancel,
            GElementState.Suspend => GPipelineState.Suspend,
            GElementState.Timeout => GPipelineState.Timeout,
            _ => GPipelineState.Normal,
        };

    public bool CheckSeparate(GElement first, GElement second)
    {
        if (ReferenceEquals(first, second)) return false;
        var firstPath = DeepPath(first);
        var secondPath = DeepPath(second);
        if (firstPath.Count == 0 || secondPath.Count == 0) return false;
        var common = 0;
        while (common < firstPath.Count && common < secondPath.Count
            && ReferenceEquals(firstPath[common], secondPath[common])) common++;
        if (common == firstPath.Count || common == secondPath.Count) return true;
        if (common > 0 && firstPath[common - 1] is GGroup group)
            return group.IsSeparate(firstPath[common], secondPath[common]);
        return ElementManager.CheckSeparate(firstPath[common], secondPath[common]);
    }

    private IReadOnlyList<GElement> DeepPath(GElement element)
    {
        var path = new List<GElement>();
        for (var current = element; current is not null; current = current.Belong)
            path.Add(current);
        path.Reverse();
        return path.Count > 0 && ElementManager.Find(path[0]) ? path : [];
    }

    public CStatus Save(string path)
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Create || _running)
                return InvalidState("save");
            return string.IsNullOrWhiteSpace(path) ? new CStatus("storage path is empty") : GStorage.Save(this, path);
        }
    }

    public CStatus Load(string path)
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Create || _running
                || ElementManager.GetSize() != 0)
                return InvalidState("load");
            return string.IsNullOrWhiteSpace(path) ? new CStatus("storage path is empty") : GStorage.Load(this, path);
        }
    }

    internal CStatus RegisterLoadedElement(GElement element)
    {
        element.SetGParamManager(ParamManager);
        element.SetGEventManager(EventManager);
        element.SetGStageManager(StageManager);
        return ElementManager.Add(element);
    }

    internal CStatus RegisterLoadedParam(Type type, string key, bool backtrace)
        => ParamManager.Create(type, key, backtrace);

    internal CStatus RegisterLoadedEvent(Type type, string key)
        => EventManager.Create(type, key);

    internal CStatus RegisterLoadedDaemon(Type type, long interval)
    {
        if (!typeof(GDaemon).IsAssignableFrom(type) || type.IsAbstract)
            return new CStatus($"invalid daemon type [{type}]");
        if (Activator.CreateInstance(type, nonPublic: true) is not GDaemon daemon)
            return new CStatus($"daemon type [{type}] needs a parameterless constructor");
        var status = daemon.Configure(interval, ParamManager, null);
        return status.IsErr() ? status : DaemonManager.Add(daemon);
    }

    internal CStatus RegisterLoadedStage(Type type, string key, int threshold)
        => StageManager.Create(type, key, threshold);

    public CStatus Dump(TextWriter? writer = null)
    {
        writer ??= Console.Out;
        writer.WriteLine("digraph CGraph {");
        foreach (var element in ElementManager.RegisteredElements)
        {
            writer.WriteLine($"  \"{element.GetName()}\";");
            foreach (var dependency in element.Dependencies)
                writer.WriteLine($"  \"{dependency.GetName()}\" -> \"{element.GetName()}\";");
        }
        writer.WriteLine("}");
        return new CStatus();
    }

    public CStatus Perf(TextWriter? writer = null)
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle == Lifecycle.Initialized || _running)
                return InvalidState("perf");
        }
        return GPerf.Perf(this, writer ?? Console.Out);
    }

    public CStatus Init()
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle == Lifecycle.Initialized || _running)
            {
                return InvalidState("init");
            }

            var status = ElementManager.Init();
            if (!status.IsOk()) return status;
            status += DaemonManager.Start();
            if (status.IsOk()) _lifecycle = Lifecycle.Initialized;
            else status += ElementManager.Destroy();

            return status;
        }
    }

    public CStatus Run()
        => RunAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    public ValueTask<CStatus> RunAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Initialized || _running)
            {
                return ValueTask.FromResult(InvalidState("run"));
            }

            _running = true;
            _runCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            return RunCoreAsync(_runCancellation);
        }
    }

    private async ValueTask<CStatus> RunCoreAsync(CancellationTokenSource cancellation)
    {
        var status = new CStatus();
        try
        {
            status = ParamManager.Setup();
            if (status.IsOk())
            {
                status = await ElementManager
                    .RunAsync(cancellation.Token)
                    .ConfigureAwait(false);
                status += EventManager.Reset();
            }

        }
        catch (OperationCanceledException)
        {
            status = new CStatus("pipeline run cancelled");
        }
        catch (Exception exception)
        {
            status = CException.FromException(exception, "pipeline run");
        }
        finally
        {
            ParamManager.ResetWithStatus(status);
            lock (_lifecycleLock)
            {
                _running = false;
                if (ReferenceEquals(_runCancellation, cancellation))
                {
                    _runCancellation = null;
                }
            }

            cancellation.Dispose();
        }

        return status;
    }

    public CStatus Process(int runTimes = 1)
        => AsyncProcess(runTimes, CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();

    public async ValueTask<CStatus> AsyncProcess(
        int runTimes = 1,
        CancellationToken cancellationToken = default)
    {
        if (runTimes < 0)
        {
            return new CStatus("pipeline run times cannot be negative");
        }

        var status = Init();
        if (status.IsErr())
        {
            return status;
        }

        for (var current = 0;
             current < runTimes && status.IsOk() && !cancellationToken.IsCancellationRequested;
             current++)
        {
            status += await RunAsync(cancellationToken).ConfigureAwait(false);
        }

        status += Destroy();
        return status;
    }

    public CStatus Destroy()
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Initialized || _running)
            {
                return InvalidState("destroy");
            }

            var status = EventManager.Reset();
            status += DaemonManager.Stop();
            status += ElementManager.Destroy();
            if (status.IsOk())
            {
                _lifecycle = Lifecycle.Destroyed;
            }

            return status;
        }
    }

    public CStatus Cancel()
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Initialized)
            {
                return InvalidState("cancel");
            }

            ElementManager.Cancel();
            _runCancellation?.Cancel();
            return new CStatus();
        }
    }

    public CStatus Suspend()
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Initialized || !_running)
            {
                return InvalidState("suspend");
            }

            return ElementManager.Suspend();
        }
    }

    public CStatus Resume()
    {
        lock (_lifecycleLock)
        {
            if (_lifecycle != Lifecycle.Initialized || !_running)
            {
                return InvalidState("resume");
            }

            return ElementManager.Resume();
        }
    }

    public T? GetGParam<T>(string key) where T : GParam
        => ParamManager.Get<T>(key);

    public CStatus AddGEvent<TEvent>(string key) where TEvent : GEvent, new()
        => EventManager.Create<TEvent>(key);

    public CStatus AddGEvent<TEvent, TParam>(string key, TParam param)
        where TEvent : GEvent, new()
        where TParam : GPassedParam, new()
        => EventManager.Create<TEvent>(key, GPassedParam.CopyOf(param));

    public CStatus AddGDaemon<TDaemon>(long interval, params object?[] constructorArgs)
        where TDaemon : GDaemon
    {
        try
        {
            if (Activator.CreateInstance(typeof(TDaemon), constructorArgs) is not TDaemon daemon)
                return new CStatus($"cannot construct daemon [{typeof(TDaemon).FullName}]");
            var status = daemon.Configure(interval, ParamManager, null);
            return status.IsErr() ? status : DaemonManager.Add(daemon);
        }
        catch (Exception exception)
        {
            return CException.FromException(exception, $"construct daemon [{typeof(TDaemon).FullName}]");
        }
    }

    public CStatus AddGDaemon<TDaemon, TParam>(long interval, TParam param)
        where TDaemon : GDaemon, new()
        where TParam : GPassedParam, new()
    {
        var daemon = new TDaemon();
        var status = daemon.Configure(interval, ParamManager, GPassedParam.CopyOf(param));
        return status.IsErr() ? status : DaemonManager.Add(daemon);
    }

    public CStatus AddGAspect<TAspect>(IReadOnlyCollection<GElement>? elements = null)
        where TAspect : GAspect
    {
        var targets = elements ?? ElementManager.RegisteredElements;
        var status = new CStatus();
        foreach (var element in targets)
        {
            status += element.AddGAspect<TAspect>();
        }
        return status;
    }

    public CStatus AddGStage<T>(string key, int count) where T : GStage, new()
        => StageManager.Create<T>(key, count);

    public CStatus AddGStage<T, TParam>(string key, int count, TParam param)
        where T : GStage, new()
        where TParam : GStageParam, new()
        => StageManager.Create<T>(key, count, GPassedParam.CopyOf(param) as GStageParam);

    public T? GetGStage<T>(string key) where T : GStage
        => StageManager.Get<T>(key);

    private CStatus InvalidState(string operation)
        => new($"cannot {operation} pipeline in state [{_lifecycle}], running=[{_running}]");

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_lifecycle == Lifecycle.Initialized && !_running)
        {
            _ = Destroy();
        }

        _ = ElementManager.Clear();
        _ = DaemonManager.Clear();
        _ = EventManager.Clear();
        StageManager.Clear();
        ParamManager.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
