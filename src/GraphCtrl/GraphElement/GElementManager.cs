namespace CsCGraph;

public sealed class GElementManager : GraphManager<GElement>
{
    private readonly List<GElement> _managerElements = [];
    private readonly GElementRepository _repository = new();
    private readonly AsyncManualResetEvent _dispatchGate = new();
    private readonly GEpochTaskTracker _epochTaskTracker = new();
    private readonly GConcurrencyGate _concurrencyGate = new();
    private GEngine _engine;
    private GEngineType _engineType = GEngineType.Dynamic;
    private GElement[] _sortedElements = [];
    private GElement[] _engineElements = [];
    private int _initialized;
    private int _running;

    public GElementManager()
    {
        _engine = CreateEngine(_engineType);
    }

    internal IReadOnlyList<GElement> RegisteredElements => _managerElements;

    public override CStatus Add(GElement element)
    {
        var status = _repository.Insert(element);
        if (status.IsErr())
        {
            return status;
        }

        _managerElements.Add(element);
        element.SetEpochTaskTracker(_epochTaskTracker);
        element.SetConcurrencyGate(_concurrencyGate);
        return new CStatus();
    }

    public override bool Find(GElement element)
        => _repository.Find(element);

    internal CStatus Init()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
        {
            return new CStatus("element manager already initialized");
        }

        var status = GElementSorter.Sort(_managerElements, out _engineElements);
        if (status.IsErr())
        {
            Interlocked.Exchange(ref _initialized, 0);
            return status;
        }

        _sortedElements = _managerElements
            .Select((element, index) => (element, index))
            .OrderBy(static item => item.element.GetLevel())
            .ThenBy(static item => item.index)
            .Select(static item => item.element)
            .ToArray();

        var initializedCount = 0;
        for (; initializedCount < _sortedElements.Length; initializedCount++)
        {
            status = _sortedElements[initializedCount].InitializeInternal();
            if (status.IsErr())
            {
                break;
            }
        }

        if (status.IsErr())
        {
            for (var current = initializedCount - 1; current >= 0; current--)
            {
                status += _sortedElements[current].DestroyInternal();
            }

            Interlocked.Exchange(ref _initialized, 0);
            return status;
        }

        status = _engine.Setup(_engineElements);
        if (status.IsErr())
        {
            Interlocked.Exchange(ref _initialized, 0);
        }

        return status;
    }

    internal ValueTask<CStatus> RunAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _initialized) == 0)
        {
            return ValueTask.FromResult(new CStatus(
                "element manager is not initialized"));
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return ValueTask.FromResult(new CStatus(
                "element manager is already running"));
        }

        return RunCoreAsync(cancellationToken);
    }

    private async ValueTask<CStatus> RunCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            _repository.PushAllState(GElementState.Normal);
            _dispatchGate.Set();
            var status = await _engine.RunAsync(cancellationToken).ConfigureAwait(false);
            status += await _epochTaskTracker.WaitForHoldsAsync().ConfigureAwait(false);
            return status;
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    internal CStatus Destroy()
    {
        if (Volatile.Read(ref _running) != 0)
        {
            return new CStatus("cannot destroy a running element manager");
        }

        if (Interlocked.Exchange(ref _initialized, 0) == 0)
        {
            return new CStatus();
        }

        var status = _epochTaskTracker.WaitForAll();
        for (var current = _sortedElements.Length - 1; current >= 0; current--)
        {
            status += _sortedElements[current].DestroyInternal();
        }

        _repository.PushAllState(GElementState.Normal);
        return status;
    }

    internal void Cancel()
    {
        _repository.PushAllState(GElementState.Cancel);
        _dispatchGate.Set();
    }

    internal CStatus Suspend()
    {
        if (Volatile.Read(ref _running) == 0)
        {
            return new CStatus("cannot suspend an idle element manager");
        }

        _dispatchGate.Reset();
        _repository.PushAllState(GElementState.Suspend);
        return new CStatus();
    }

    internal CStatus Resume()
    {
        if (Volatile.Read(ref _running) == 0)
        {
            return new CStatus("cannot resume an idle element manager");
        }

        _repository.PushAllState(GElementState.Normal);
        _dispatchGate.Set();
        return new CStatus();
    }

    internal void ConfigureConcurrency(int limit)
        => _concurrencyGate.Configure(limit);

    internal CStatus SetEngineType(GEngineType type)
    {
        if (Volatile.Read(ref _initialized) != 0 || Volatile.Read(ref _running) != 0)
            return new CStatus("cannot change engine type after initialization");
        if (!Enum.IsDefined(type))
            return new CStatus($"unknown engine type [{type}]");
        _engineType = type;
        _engine = CreateEngine(type);
        return new CStatus();
    }

    internal int CalcMaxParaSize()
    {
        if (_managerElements.Any(static element => element is GGroup))
            throw new InvalidOperationException(
                "cannot calculate maximum parallelism for a graph containing groups");

        var count = _managerElements.Count;
        var best = 0;
        var selected = new List<GElement>();
        void Search(int index)
        {
            if (selected.Count + count - index <= best) return;
            if (index == count)
            {
                best = Math.Max(best, selected.Count);
                return;
            }

            var candidate = _managerElements[index];
            if (selected.All(item => !Reaches(item, candidate) && !Reaches(candidate, item)))
            {
                selected.Add(candidate);
                Search(index + 1);
                selected.RemoveAt(selected.Count - 1);
            }
            Search(index + 1);
        }

        Search(0);
        return best;
    }

    internal bool CheckSerializable()
    {
        if (_engineType != GEngineType.Dynamic || _managerElements.Count == 0)
            return false;
        var roots = 0;
        var tails = 0;
        foreach (var element in _managerElements)
        {
            if (element.Dependencies.Count > 1 || element.Successors.Count > 1
                || element.GetTimeout() > 0 || element is GGroup)
                return false;
            if (element.Dependencies.Count == 0) roots++;
            if (element.Successors.Count == 0) tails++;
        }
        return roots == 1 && tails == 1;
    }

    internal bool CheckSeparate(GElement first, GElement second)
        => Find(first) && Find(second)
            && (Reaches(first, second) || Reaches(second, first));

    internal GElementState CurrentState => _repository.CurrentState;

    internal int Trim()
    {
        var removed = 0;
        foreach (var element in _managerElements)
        {
            var dependencies = element.Dependencies.ToArray();
            foreach (var candidate in dependencies)
            {
                if (dependencies.Any(other => !ReferenceEquals(other, candidate)
                    && Reaches(candidate, other)))
                {
                    if (element.RemoveDependencyInternal(candidate)) removed++;
                }
            }
        }

        return removed;
    }

    private static bool Reaches(GElement from, GElement target)
    {
        var visited = new HashSet<GElement>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<GElement>();
        stack.Push(from);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current)) continue;
            foreach (var successor in current.Successors)
            {
                if (ReferenceEquals(successor, target)) return true;
                stack.Push(successor);
            }
        }

        return false;
    }

    public override CStatus Clear()
    {
        if (Volatile.Read(ref _running) != 0)
        {
            return new CStatus("cannot clear a running element manager");
        }

        _ = Destroy();
        _managerElements.Clear();
        _sortedElements = [];
        _engineElements = [];
        _repository.Clear();
        return new CStatus();
    }

    public override int GetSize() => _managerElements.Count;

    private GEngine CreateEngine(GEngineType type)
        => type switch
        {
            GEngineType.Dynamic => new GDynamicEngine(_dispatchGate, _concurrencyGate),
            GEngineType.Topo => new GTopoEngine(_dispatchGate, _concurrencyGate),
            GEngineType.Static => new GStaticEngine(_dispatchGate, _concurrencyGate),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
}
