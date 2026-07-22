namespace CsCGraph;

public abstract class GGroup : GElement
{
    private readonly List<GElement> _children = [];
    private readonly HashSet<GElement> _childSet =
        new(ReferenceEqualityComparer.Instance);

    protected GGroup(GElementType elementType = GElementType.Group)
        : base(elementType)
    {
    }

    internal IReadOnlyList<GElement> Children => _children;

    internal CStatus AddElement(GElement element)
    {
        if (element is null)
        {
            return new CStatus($"group [{GetName()}] child cannot be null");
        }

        if (ReferenceEquals(this, element))
        {
            return new CStatus($"group [{GetName()}] cannot contain itself");
        }

        if (!_childSet.Add(element))
        {
            return new CStatus($"element [{element.GetName()}] already belongs to group [{GetName()}]");
        }

        if (element.Belong is not null && !ReferenceEquals(element.Belong, this))
        {
            _childSet.Remove(element);
            return new CStatus($"element [{element.GetName()}] already belongs to another group");
        }

        var status = AddElementEx(element);
        if (status.IsErr())
        {
            _childSet.Remove(element);
            return status;
        }

        _children.Add(element);
        element.SetBelong(this);
        return new CStatus();
    }

    internal void RollbackElements()
    {
        ReleaseElements();
        _children.Clear();
        _childSet.Clear();
    }

    protected void ReleaseElements()
    {
        foreach (var element in _children)
        {
            if (ReferenceEquals(element.Belong, this))
            {
                element.SetBelong(null);
            }
        }
    }

    protected virtual CStatus AddElementEx(GElement element) => new();

    internal virtual bool IsSeparate(GElement first, GElement second) => false;

    internal override void SetGParamManager(GParamManager manager)
    {
        base.SetGParamManager(manager);
        foreach (var child in _children)
        {
            child.SetGParamManager(manager);
        }
    }

    internal override void SetGEventManager(GEventManager manager)
    {
        base.SetGEventManager(manager);
        foreach (var child in _children) child.SetGEventManager(manager);
    }

    internal override void SetEpochTaskTracker(GEpochTaskTracker tracker)
    {
        base.SetEpochTaskTracker(tracker);
        foreach (var child in _children) child.SetEpochTaskTracker(tracker);
    }

    internal override void SetConcurrencyGate(GConcurrencyGate gate)
    {
        base.SetConcurrencyGate(gate);
        foreach (var child in _children) child.SetConcurrencyGate(gate);
    }

    internal override void SetGStageManager(GStageManager manager)
    {
        base.SetGStageManager(manager);
        foreach (var child in _children) child.SetGStageManager(manager);
    }

    protected override CStatus Init()
    {
        var initialized = 0;
        var status = new CStatus();
        for (; initialized < _children.Count; initialized++)
        {
            status = _children[initialized].InitializeInternal();
            if (status.IsErr())
            {
                break;
            }
        }

        if (status.IsErr())
        {
            for (var current = initialized - 1; current >= 0; current--)
            {
                status += _children[current].DestroyInternal();
            }
        }

        return status;
    }

    protected override CStatus Destroy()
    {
        var status = new CStatus();
        for (var current = _children.Count - 1; current >= 0; current--)
        {
            status += _children[current].DestroyInternal();
        }

        return status;
    }
}
